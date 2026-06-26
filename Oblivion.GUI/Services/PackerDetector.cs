using Oblivion.Data.Snapshots;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Oblivion.GUI.Services
{
    public static class PackerDetector
    {
        public static void EnrichSnapshotWithPacker(PESnapshot snapshot, string filePath)
        {
            if (snapshot == null || !File.Exists(filePath))
                return;

            byte[] fileBytes;
            try { fileBytes = File.ReadAllBytes(filePath); }
            catch { return; }

            // Try each detection method, stop on first match
            string? packer = DetectByEpSignature(snapshot, fileBytes)
                          ?? DetectBySectionNames(snapshot)
                          ?? DetectByImports(snapshot)
                          ?? DetectByOverlay(snapshot, fileBytes);

            if (packer != null)
                snapshot.DetectedPacker = packer;
        }

        private static string? DetectByEpSignature(PESnapshot snapshot, byte[] fileBytes)
        {
            // Find EP file offset via section mapping
            uint epRva = (uint)snapshot.EntryPoint;
            if (epRva == 0 || snapshot.Sections == null)
                return null;

            int epOffset = -1;
            foreach (var sec in snapshot.Sections)
            {
                if (epRva >= sec.VirtualAddress && epRva < sec.VirtualAddress + sec.VirtualSize)
                {
                    epOffset = (int)(epRva - sec.VirtualAddress + sec.RawDataPointer);
                    break;
                }
            }

            if (epOffset < 0 || epOffset + 16 > fileBytes.Length)
                return null;

            foreach (var sig in EpSignatures)
            {
                if (MatchPattern(fileBytes, epOffset, sig.Pattern, sig.Mask))
                    return sig.Name;
            }

            return null;
        }

        private static string? DetectBySectionNames(PESnapshot snapshot)
        {
            if (snapshot.Sections == null || snapshot.Sections.Count == 0)
                return null;

            var names = snapshot.Sections.Select(s => s.Name ?? "").ToList();

            foreach (var rule in SectionRules)
            {
                if (rule.Matcher(names))
                    return rule.Name;
            }

            return null;
        }

        private static string? DetectByImports(PESnapshot snapshot)
        {
            if (snapshot.Imports == null)
                return null;

            var modules = snapshot.Imports
                .Select(i => i.ModuleName?.ToLowerInvariant() ?? "")
                .ToHashSet();

            var allFunctions = snapshot.Imports
                .Where(i => i.Functions != null)
                .SelectMany(i => i.Functions)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            // Themida / Oreans — single import from kernel32 with only a few functions
            if (modules.Count == 1 && modules.Contains("kernel32.dll")
                && allFunctions.Count <= 5
                && snapshot.OverallEntropy > 6.5)
                return "Themida / Oreans (probable)";

            // .NET native (not a packer, but worth noting)
            if (modules.Contains("mscoree.dll") && allFunctions.Contains("_CorExeMain"))
                return null; // .NET — skip, not a packer

            // VMProtect — typically imports only kernel32 + user32 with VirtualProtect
            if (modules.Count <= 3
                && modules.Contains("kernel32.dll")
                && allFunctions.Contains("VirtualProtect")
                && snapshot.OverallEntropy > 6.8
                && snapshot.Sections != null
                && snapshot.Sections.Any(s => s.Name != null
                    && (s.Name.StartsWith(".vmp", StringComparison.OrdinalIgnoreCase)
                        || s.Name.StartsWith("vmp", StringComparison.OrdinalIgnoreCase))))
                return "VMProtect";

            return null;
        }

        private static string? DetectByOverlay(PESnapshot snapshot, byte[] fileBytes)
        {
            if (snapshot.Sections == null || snapshot.Sections.Count == 0)
                return null;

            // Check for known magic bytes at the start of overlay data
            uint lastSectionEnd = snapshot.Sections
                .Where(s => s.RawDataSize > 0)
                .Select(s => s.RawDataPointer + s.RawDataSize)
                .DefaultIfEmpty(0u)
                .Max();

            if (lastSectionEnd == 0 || lastSectionEnd >= fileBytes.Length)
                return null;

            int overlaySize = fileBytes.Length - (int)lastSectionEnd;
            if (overlaySize < 16)
                return null;

            int ofs = (int)lastSectionEnd;

            // AutoIt — overlay starts with "AU3!EA06"
            if (overlaySize >= 8 && Encoding.ASCII.GetString(fileBytes, ofs, 8) == "AU3!EA06")
                return "AutoIt";

            return null;
        }

        #region Signature matching

        private static bool MatchPattern(byte[] data, int offset, byte[] pattern, byte[]? mask)
        {
            if (offset + pattern.Length > data.Length)
                return false;

            for (int i = 0; i < pattern.Length; i++)
            {
                if (mask != null && mask[i] == 0x00)
                    continue; // wildcard
                if (data[offset + i] != pattern[i])
                    return false;
            }
            return true;
        }

        #endregion

        #region Signature database

        private class EpSig
        {
            public string Name;
            public byte[] Pattern;
            public byte[]? Mask; // 0xFF = exact, 0x00 = wildcard
        }

        private static readonly EpSig[] EpSignatures = new[]
        {
            // UPX — "push ebp / mov ebp,esp / pusha / ... / UPX" or common UPX stub
            new EpSig
            {
                Name = "UPX",
                Pattern = new byte[] { 0x60, 0xBE, 0x00, 0x00, 0x00, 0x00, 0x8D, 0xBE, 0x00, 0x00, 0x00, 0x00, 0x57 },
                Mask    = new byte[] { 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00, 0xFF }
            },
            // UPX variant (x86) — pushad; mov esi, ...
            new EpSig
            {
                Name = "UPX",
                Pattern = new byte[] { 0x60, 0xBE, 0x00, 0x00, 0x00, 0x00, 0x8D, 0xBE, 0x00, 0x00, 0xFF, 0xFF },
                Mask    = new byte[] { 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0x00, 0x00, 0xFF, 0xFF }
            },
            // ASPack — pushad; call $+5
            new EpSig
            {
                Name = "ASPack",
                Pattern = new byte[] { 0x60, 0xE8, 0x03, 0x00, 0x00, 0x00, 0xE9, 0xEB },
                Mask    = null
            },
            // PECompact — push ebp / mov ebp, esp / push -1
            new EpSig
            {
                Name = "PECompact",
                Pattern = new byte[] { 0xB8, 0x00, 0x00, 0x00, 0x00, 0x50, 0x64, 0xFF, 0x35, 0x00, 0x00, 0x00, 0x00, 0x64, 0x89, 0x25 },
                Mask    = new byte[] { 0xFF, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }
            },
            // MPRESS — pushad; push esp
            new EpSig
            {
                Name = "MPRESS",
                Pattern = new byte[] { 0x60, 0xE8, 0x00, 0x00, 0x00, 0x00, 0x58, 0x05, 0x00, 0x00, 0x00, 0x00, 0x8B, 0x30 },
                Mask    = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF }
            },
            // Petite — push ebp / mov ebp,esp / push FFFFFFXX
            new EpSig
            {
                Name = "Petite",
                Pattern = new byte[] { 0xB8, 0x00, 0x00, 0x00, 0x00, 0x66, 0x9C, 0x60, 0x50 },
                Mask    = new byte[] { 0xFF, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF }
            },
            // FSG — typical jmp+mov pattern
            new EpSig
            {
                Name = "FSG",
                Pattern = new byte[] { 0x87, 0x25, 0x00, 0x00, 0x00, 0x00, 0x61, 0x94, 0x55 },
                Mask    = new byte[] { 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0xFF }
            },
            // Enigma Protector
            new EpSig
            {
                Name = "Enigma Protector",
                Pattern = new byte[] { 0x60, 0xE8, 0x00, 0x00, 0x00, 0x00, 0x5D, 0x81, 0xED, 0x00, 0x00, 0x00, 0x00, 0x81 },
                Mask    = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00, 0xFF }
            },
            // Themida — common entry stub
            new EpSig
            {
                Name = "Themida",
                Pattern = new byte[] { 0xB8, 0x00, 0x00, 0x00, 0x00, 0x60, 0x0B, 0xC0, 0x74, 0x68 },
                Mask    = new byte[] { 0xFF, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }
            },
            // VMProtect x86 — push reg / call
            new EpSig
            {
                Name = "VMProtect",
                Pattern = new byte[] { 0x68, 0x00, 0x00, 0x00, 0x00, 0xE8, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 },
                Mask    = new byte[] { 0xFF, 0x00, 0x00, 0x00, 0x00, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }
            },
        };

        private class SectionRule
        {
            public string Name;
            public Func<List<string>, bool> Matcher;
        }

        private static readonly SectionRule[] SectionRules = new[]
        {
            new SectionRule
            {
                Name = "UPX",
                Matcher = names => names.Any(n => n.StartsWith("UPX", StringComparison.OrdinalIgnoreCase))
                                || (names.Count >= 3
                                    && string.Equals(names[0], "UPX0", StringComparison.OrdinalIgnoreCase)
                                    && string.Equals(names[1], "UPX1", StringComparison.OrdinalIgnoreCase))
            },
            new SectionRule
            {
                Name = "ASPack",
                Matcher = names => names.Any(n => string.Equals(n, ".aspack", StringComparison.OrdinalIgnoreCase)
                                              || string.Equals(n, ".adata", StringComparison.OrdinalIgnoreCase)
                                              || string.Equals(n, "ASPack", StringComparison.OrdinalIgnoreCase))
            },
            new SectionRule
            {
                Name = "PECompact",
                Matcher = names => names.Any(n => string.Equals(n, "PEC2TO", StringComparison.OrdinalIgnoreCase)
                                              || string.Equals(n, "PEC2", StringComparison.OrdinalIgnoreCase)
                                              || string.Equals(n, "pec1", StringComparison.OrdinalIgnoreCase)
                                              || string.Equals(n, "PECompa", StringComparison.OrdinalIgnoreCase))
            },
            new SectionRule
            {
                Name = "MPRESS",
                Matcher = names => names.Any(n => string.Equals(n, ".MPRESS1", StringComparison.OrdinalIgnoreCase)
                                              || string.Equals(n, ".MPRESS2", StringComparison.OrdinalIgnoreCase))
            },
            new SectionRule
            {
                Name = "Themida",
                Matcher = names => names.Any(n => string.Equals(n, ".themida", StringComparison.OrdinalIgnoreCase)
                                              || string.Equals(n, ".winlice", StringComparison.OrdinalIgnoreCase)
                                              || string.Equals(n, "Themida", StringComparison.OrdinalIgnoreCase))
            },
            new SectionRule
            {
                Name = "VMProtect",
                Matcher = names => names.Any(n => n.StartsWith(".vmp", StringComparison.OrdinalIgnoreCase)
                                              || n.StartsWith("vmp", StringComparison.OrdinalIgnoreCase))
            },
            new SectionRule
            {
                Name = "Enigma Protector",
                Matcher = names => names.Any(n => string.Equals(n, ".enigma1", StringComparison.OrdinalIgnoreCase)
                                              || string.Equals(n, ".enigma2", StringComparison.OrdinalIgnoreCase))
            },
            new SectionRule
            {
                Name = "Obsidium",
                Matcher = names => names.Any(n => string.Equals(n, ".obsidiu", StringComparison.OrdinalIgnoreCase)
                                              || string.Equals(n, ".obsfusc", StringComparison.OrdinalIgnoreCase))
            },
            new SectionRule
            {
                Name = "Petite",
                Matcher = names => names.Any(n => string.Equals(n, ".petite", StringComparison.OrdinalIgnoreCase))
            },
            new SectionRule
            {
                Name = "FSG",
                Matcher = names => names.Count >= 2
                                && names.Count(n => n.Trim() == "" || n.All(c => c == '.')) >= 2
                                && names.Any(n => n == "" || n == "...")
            },
            new SectionRule
            {
                Name = "tElock",
                Matcher = names => names.Any(n => string.Equals(n, ".tElock", StringComparison.Ordinal))
            },
            new SectionRule
            {
                Name = "Armadillo",
                Matcher = names => names.Any(n => string.Equals(n, ".armdilo", StringComparison.OrdinalIgnoreCase))
            },
            new SectionRule
            {
                Name = "ConfuserEx (.NET)",
                Matcher = names => names.Any(n => string.Equals(n, "Confuser", StringComparison.OrdinalIgnoreCase))
            },
            new SectionRule
            {
                Name = ".NET Reactor",
                Matcher = names => names.Any(n => string.Equals(n, ".reacto", StringComparison.OrdinalIgnoreCase)
                                              || string.Equals(n, "reacto", StringComparison.OrdinalIgnoreCase))
            },
            new SectionRule
            {
                Name = "Safengine",
                Matcher = names => names.Any(n => n.StartsWith(".sforce", StringComparison.OrdinalIgnoreCase)
                                              || n.StartsWith(".spack", StringComparison.OrdinalIgnoreCase))
            },
            new SectionRule
            {
                Name = "ExeStealth",
                Matcher = names => names.Any(n => string.Equals(n, "ExeS", StringComparison.OrdinalIgnoreCase))
            },
            new SectionRule
            {
                Name = "MEW",
                Matcher = names => names.Any(n => string.Equals(n, "MEW", StringComparison.OrdinalIgnoreCase))
            },
            new SectionRule
            {
                Name = "PELock",
                Matcher = names => names.Any(n => string.Equals(n, "PELOCKnt", StringComparison.OrdinalIgnoreCase))
            },
            new SectionRule
            {
                Name = "NsPack",
                Matcher = names => names.Any(n => string.Equals(n, ".nsp0", StringComparison.OrdinalIgnoreCase)
                                              || string.Equals(n, ".nsp1", StringComparison.OrdinalIgnoreCase)
                                              || string.Equals(n, "nsp0", StringComparison.OrdinalIgnoreCase)
                                              || string.Equals(n, "nsp1", StringComparison.OrdinalIgnoreCase))
            },
        };

        #endregion
    }
}

using Oblivion.Data.Snapshots;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Oblivion.GUI.Services
{
    public static class InstallerDetector
    {
        public static void EnrichSnapshotWithInstaller(PESnapshot snapshot, string filePath)
        {
            if (snapshot == null || !File.Exists(filePath))
                return;

            byte[] fileBytes;
            try { fileBytes = File.ReadAllBytes(filePath); }
            catch { return; }

            string? installer = DetectByOverlay(snapshot, fileBytes)
                             ?? DetectBySectionNames(snapshot)
                             ?? DetectByImports(snapshot)
                             ?? DetectByVersionInfo(fileBytes);

            if (installer != null)
                snapshot.DetectedInstaller = installer;
        }

        private static string? DetectByOverlay(PESnapshot snapshot, byte[] fileBytes)
        {
            if (snapshot.Sections == null || snapshot.Sections.Count == 0)
                return null;

            uint lastSectionEnd = snapshot.Sections
                .Where(s => s.RawDataSize > 0)
                .Select(s => s.RawDataPointer + s.RawDataSize)
                .DefaultIfEmpty(0u)
                .Max();

            if (lastSectionEnd == 0 || lastSectionEnd >= fileBytes.Length)
                return null;

            int overlaySize = fileBytes.Length - (int)lastSectionEnd;
            if (overlaySize < 4)
                return null;

            int ofs = (int)lastSectionEnd;

            // NSIS — overlay starts with 0xEF 0xBE 0xAD 0xDE (NullSoft deadbeef)
            if (overlaySize >= 4
                && fileBytes[ofs] == 0xEF && fileBytes[ofs + 1] == 0xBE
                && fileBytes[ofs + 2] == 0xAD && fileBytes[ofs + 3] == 0xDE)
                return "NSIS Installer";

            // Inno Setup — overlay contains "Inno Setup" in first 512 bytes
            int searchLen = Math.Min(overlaySize, 512);
            string overlayHead = Encoding.ASCII.GetString(fileBytes, ofs, searchLen);

            if (overlayHead.Contains("Inno Setup"))
                return "Inno Setup";

            // InstallShield — "ISSetupStream" or magic bytes 0x49 0x53 0x63 0x28
            if (overlayHead.Contains("ISSetupStream"))
                return "InstallShield";

            if (overlaySize >= 4
                && fileBytes[ofs] == 0x49 && fileBytes[ofs + 1] == 0x53
                && fileBytes[ofs + 2] == 0x63 && fileBytes[ofs + 3] == 0x28)
                return "InstallShield";

            // WiX Burn — "wixburn"
            if (overlayHead.Contains("wixburn"))
                return "WiX Burn";

            // Setup Factory
            if (overlayHead.Contains("Setup Factory"))
                return "Setup Factory";

            return null;
        }

        private static string? DetectBySectionNames(PESnapshot snapshot)
        {
            if (snapshot.Sections == null || snapshot.Sections.Count == 0)
                return null;

            var names = snapshot.Sections.Select(s => s.Name ?? "").ToList();

            if (names.Any(n => string.Equals(n, ".ndata", StringComparison.OrdinalIgnoreCase)))
                return "NSIS Installer";

            if (names.Any(n => string.Equals(n, ".wixburn", StringComparison.OrdinalIgnoreCase)))
                return "WiX Burn";

            if (names.Any(n => string.Equals(n, "WiseMain", StringComparison.OrdinalIgnoreCase)
                            || string.Equals(n, ".WISE", StringComparison.OrdinalIgnoreCase)))
                return "WISE Installer";

            return null;
        }

        private static string? DetectByImports(PESnapshot snapshot)
        {
            if (snapshot.Imports == null)
                return null;

            var modules = snapshot.Imports
                .Select(i => i.ModuleName?.ToLowerInvariant() ?? "")
                .ToHashSet();

            if (modules.Contains("msi.dll"))
                return "MSI-based Installer";

            if (modules.Contains("isrt.dll"))
                return "InstallShield";

            return null;
        }

        private static string? DetectByVersionInfo(byte[] fileBytes)
        {
            // Search for installer-related UTF-16LE strings near FileDescription/ProductName
            // in the last 2MB of the file (resource section area). Fallback method.
            int searchStart = Math.Max(0, fileBytes.Length - 2 * 1024 * 1024);
            int searchLength = fileBytes.Length - searchStart;

            if (searchLength < 100)
                return null;

            // Look for VS_VERSION_INFO marker (UTF-16LE)
            byte[] vsVersionMarker = Encoding.Unicode.GetBytes("VS_VERSION_INFO");
            int vsPos = FindBytes(fileBytes, searchStart, searchLength, vsVersionMarker);
            if (vsPos < 0)
                return null;

            // Search within a reasonable window after VS_VERSION_INFO
            int windowStart = vsPos;
            int windowEnd = Math.Min(fileBytes.Length, vsPos + 8192);
            int windowLen = windowEnd - windowStart;

            string versionBlock;
            try { versionBlock = Encoding.Unicode.GetString(fileBytes, windowStart, windowLen); }
            catch { return null; }

            // Check for known installer strings near FileDescription/ProductName
            if (versionBlock.Contains("FileDescription") || versionBlock.Contains("ProductName"))
            {
                if (versionBlock.Contains("Nullsoft") || versionBlock.Contains("NSIS"))
                    return "NSIS Installer";
                if (versionBlock.Contains("Inno Setup"))
                    return "Inno Setup";
                if (versionBlock.Contains("InstallShield"))
                    return "InstallShield";
                if (versionBlock.Contains("Setup Factory"))
                    return "Setup Factory";
                if (versionBlock.Contains("Wise Installation") || versionBlock.Contains("WISE"))
                    return "WISE Installer";
            }

            return null;
        }

        private static int FindBytes(byte[] data, int offset, int length, byte[] pattern)
        {
            int end = offset + length - pattern.Length;
            for (int i = offset; i <= end; i++)
            {
                bool match = true;
                for (int j = 0; j < pattern.Length; j++)
                {
                    if (data[i + j] != pattern[j])
                    {
                        match = false;
                        break;
                    }
                }
                if (match)
                    return i;
            }
            return -1;
        }
    }
}

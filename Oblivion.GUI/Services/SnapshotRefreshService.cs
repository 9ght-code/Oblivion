using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.PortableExecutable;
using System.Text;
using Oblivion.Data.Snapshots;

namespace Oblivion.GUI.Services
{
    /// <summary>
    /// Re-parse PE headers from byte[] using System.Reflection.PortableExecutable.PEReader.
    /// No dependency on native Oblivion.Core DLL — used for in-memory re-analysis after edits.
    /// </summary>
    public static class SnapshotRefreshService
    {
        /// <summary>
        /// Parse a full PESnapshot from raw file bytes.
        /// </summary>
        public static PESnapshot ParseFromBytes(byte[] fileBytes)
        {
            using var stream = new MemoryStream(fileBytes);
            using var peReader = new PEReader(stream);

            var headers = peReader.PEHeaders;
            var peHeader = headers.PEHeader!;
            var coffHeader = headers.CoffHeader;

            bool is64 = peHeader.Magic == PEMagic.PE32Plus;

            var snapshot = new PESnapshot
            {
                Architecture = coffHeader.Machine == Machine.Amd64 ? "8664" :
                               coffHeader.Machine == Machine.I386 ? "14C" :
                               coffHeader.Machine == Machine.Arm64 ? "AA64" :
                               ((ushort)coffHeader.Machine).ToString("X"),
                EntryPoint = peHeader.AddressOfEntryPoint,
                ImageBase = (long)peHeader.ImageBase,
                Header = new PEHeaderSnapshot
                {
                    Machine = (ushort)coffHeader.Machine,
                    Characteristics = (int)coffHeader.Characteristics,
                    TimeDateStamp = headers.CoffHeader.TimeDateStamp,
                    Subsystem = (ushort)peHeader.Subsystem,
                    DllCharacteristics = (ushort)peHeader.DllCharacteristics,
                },
                Sections = new List<PESectionSnapshot>(),
                Imports = new List<ImportSnapshot>()
            };

            // Parse sections
            foreach (var section in headers.SectionHeaders)
            {
                uint chars = (uint)section.SectionCharacteristics;
                snapshot.Sections.Add(new PESectionSnapshot
                {
                    Name = section.Name,
                    VirtualAddress = (uint)section.VirtualAddress,
                    VirtualSize = (uint)section.VirtualSize,
                    RawDataPointer = (uint)section.PointerToRawData,
                    RawDataSize = (uint)section.SizeOfRawData,
                    Characteristics = chars,
                    IsReadable = (chars & 0x40000000) != 0,
                    IsWritable = (chars & 0x80000000) != 0,
                    IsExecutable = (chars & 0x20000000) != 0,
                });
            }

            // Parse imports from raw bytes (PEReader doesn't directly expose import names easily)
            ParseImports(fileBytes, snapshot, is64);

            return snapshot;
        }

        private static void ParseImports(byte[] data, PESnapshot snapshot, bool is64)
        {
            try
            {
                if (data.Length < 0x40) return;
                int ntOffset = BitConverter.ToInt32(data, 0x3C);
                if (ntOffset <= 0 || ntOffset + 24 > data.Length) return;

                int optOffset = ntOffset + 24;
                int ddOffset = is64 ? optOffset + 112 : optOffset + 96;
                int importDdOffset = ddOffset + 8; // DataDirectory[1]

                if (importDdOffset + 8 > data.Length) return;

                uint importRva = BitConverter.ToUInt32(data, importDdOffset);
                uint importSize = BitConverter.ToUInt32(data, importDdOffset + 4);
                if (importRva == 0 || importSize == 0) return;

                long importFileOffset = RvaToOffset(importRva, snapshot.Sections);
                if (importFileOffset < 0 || importFileOffset >= data.Length) return;

                int descOffset = (int)importFileOffset;

                while (descOffset + 20 <= data.Length)
                {
                    uint originalFirstThunk = BitConverter.ToUInt32(data, descOffset);
                    uint nameRva = BitConverter.ToUInt32(data, descOffset + 12);
                    uint firstThunk = BitConverter.ToUInt32(data, descOffset + 16);

                    if (nameRva == 0) break;

                    // Read DLL name
                    long nameOffset = RvaToOffset(nameRva, snapshot.Sections);
                    string dllName = "Unknown";
                    if (nameOffset >= 0 && nameOffset < data.Length)
                    {
                        int end = (int)nameOffset;
                        while (end < data.Length && data[end] != 0 && end - (int)nameOffset < 256)
                            end++;
                        dllName = Encoding.ASCII.GetString(data, (int)nameOffset, end - (int)nameOffset);
                    }

                    var import = new ImportSnapshot { ModuleName = dllName, Functions = new List<string>() };

                    // Parse functions
                    uint thunkRva = originalFirstThunk != 0 ? originalFirstThunk : firstThunk;
                    long thunkOffset = RvaToOffset(thunkRva, snapshot.Sections);

                    if (thunkOffset >= 0 && thunkOffset < data.Length)
                    {
                        int entrySize = is64 ? 8 : 4;
                        int pos = (int)thunkOffset;

                        while (pos + entrySize <= data.Length)
                        {
                            ulong entry = is64
                                ? BitConverter.ToUInt64(data, pos)
                                : BitConverter.ToUInt32(data, pos);

                            if (entry == 0) break;

                            bool byOrdinal = is64
                                ? (entry & 0x8000000000000000) != 0
                                : (entry & 0x80000000) != 0;

                            if (byOrdinal)
                            {
                                import.Functions.Add($"Ordinal #{(ushort)(entry & 0xFFFF)}");
                            }
                            else
                            {
                                uint hintNameRva = (uint)(entry & 0x7FFFFFFF);
                                long hintOffset = RvaToOffset(hintNameRva, snapshot.Sections);

                                if (hintOffset >= 0 && hintOffset + 2 < data.Length)
                                {
                                    int nameStart = (int)hintOffset + 2;
                                    int nameEnd = nameStart;
                                    while (nameEnd < data.Length && data[nameEnd] != 0 && nameEnd - nameStart < 256)
                                        nameEnd++;

                                    if (nameEnd > nameStart)
                                        import.Functions.Add(Encoding.ASCII.GetString(data, nameStart, nameEnd - nameStart));
                                }
                            }

                            pos += entrySize;
                            if (import.Functions.Count > 1000) break;
                        }
                    }

                    snapshot.Imports.Add(import);
                    descOffset += 20;
                }
            }
            catch
            {
                // Import parsing failed — imports will be empty/partial
            }
        }

        private static long RvaToOffset(uint rva, List<PESectionSnapshot> sections)
        {
            foreach (var section in sections)
            {
                if (rva >= section.VirtualAddress &&
                    rva < section.VirtualAddress + section.VirtualSize)
                {
                    return rva - section.VirtualAddress + section.RawDataPointer;
                }
            }
            return -1;
        }
    }
}

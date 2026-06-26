using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Oblivion.GUI.Services
{
    /// <summary>
    /// Static service for raw binary PE file modifications.
    /// All operations work on byte[] in memory — no dependency on native Oblivion.Core.
    /// </summary>
    public static class PEWriterService
    {
        #region Core Operations

        /// <summary>
        /// Write arbitrary bytes at a given file offset.
        /// </summary>
        public static void PatchBytes(byte[] fileBytes, long offset, byte[] newBytes)
        {
            if (offset < 0 || offset + newBytes.Length > fileBytes.Length)
                throw new ArgumentOutOfRangeException(nameof(offset),
                    $"Patch at 0x{offset:X} with {newBytes.Length} bytes exceeds file bounds (0x{fileBytes.Length:X}).");

            Array.Copy(newBytes, 0, fileBytes, offset, newBytes.Length);
        }

        /// <summary>
        /// Change AddressOfEntryPoint in the PE optional header.
        /// Handles both PE32 (0x10B) and PE32+ (0x20B).
        /// </summary>
        public static void ChangeEntryPoint(byte[] fileBytes, uint newRva)
        {
            int peOffset = FindPEHeaderOffset(fileBytes);
            // Optional header starts after PE signature (4) + file header (20) = +24
            int optionalHeaderOffset = peOffset + 24;

            if (optionalHeaderOffset + 2 > fileBytes.Length)
                throw new InvalidOperationException("File too small for optional header.");

            ushort magic = BitConverter.ToUInt16(fileBytes, optionalHeaderOffset);

            // AddressOfEntryPoint is at offset 16 from start of optional header
            int epOffset = optionalHeaderOffset + 16;

            if (magic != 0x10B && magic != 0x20B)
                throw new InvalidOperationException($"Unknown PE magic: 0x{magic:X4}");

            if (epOffset + 4 > fileBytes.Length)
                throw new InvalidOperationException("File too small for entry point field.");

            byte[] epBytes = BitConverter.GetBytes(newRva);
            Array.Copy(epBytes, 0, fileBytes, epOffset, 4);
        }

        /// <summary>
        /// Read the current AddressOfEntryPoint.
        /// </summary>
        public static uint ReadEntryPoint(byte[] fileBytes)
        {
            int peOffset = FindPEHeaderOffset(fileBytes);
            int optionalHeaderOffset = peOffset + 24;
            int epOffset = optionalHeaderOffset + 16;

            if (epOffset + 4 > fileBytes.Length)
                throw new InvalidOperationException("File too small.");

            return BitConverter.ToUInt32(fileBytes, epOffset);
        }

        /// <summary>
        /// Add a new section to the PE file.
        /// Returns the file offset where section data was written.
        /// </summary>
        public static byte[] AddSection(byte[] fileBytes, string name, byte[] data, uint characteristics)
        {
            if (name.Length > 8)
                throw new ArgumentException("Section name must be 8 characters or less.");

            int peOffset = FindPEHeaderOffset(fileBytes);
            int fileHeaderOffset = peOffset + 4;
            int optionalHeaderOffset = peOffset + 24;

            ushort numberOfSections = BitConverter.ToUInt16(fileBytes, fileHeaderOffset + 2);
            ushort sizeOfOptionalHeader = BitConverter.ToUInt16(fileBytes, fileHeaderOffset + 16);

            int sectionTableOffset = optionalHeaderOffset + sizeOfOptionalHeader;
            int newSectionHeaderOffset = sectionTableOffset + numberOfSections * 40;

            // Check there's room for one more section header (40 bytes)
            // The section headers must fit before the first section's raw data
            uint firstSectionRawPointer = uint.MaxValue;
            for (int i = 0; i < numberOfSections; i++)
            {
                int off = sectionTableOffset + i * 40;
                uint rawPtr = BitConverter.ToUInt32(fileBytes, off + 20);
                if (rawPtr > 0 && rawPtr < firstSectionRawPointer)
                    firstSectionRawPointer = rawPtr;
            }

            if (newSectionHeaderOffset + 40 > firstSectionRawPointer && firstSectionRawPointer != uint.MaxValue)
                throw new InvalidOperationException("No room in section table for a new header. The header area is full.");

            // Read alignment values from optional header
            ushort magic = BitConverter.ToUInt16(fileBytes, optionalHeaderOffset);
            uint sectionAlignment, fileAlignment;

            if (magic == 0x20B) // PE32+
            {
                sectionAlignment = BitConverter.ToUInt32(fileBytes, optionalHeaderOffset + 32);
                fileAlignment = BitConverter.ToUInt32(fileBytes, optionalHeaderOffset + 36);
            }
            else // PE32
            {
                sectionAlignment = BitConverter.ToUInt32(fileBytes, optionalHeaderOffset + 32);
                fileAlignment = BitConverter.ToUInt32(fileBytes, optionalHeaderOffset + 36);
            }

            // Calculate new section's VA and file offset
            // Find the last section to determine where to place new one
            uint lastSectionVa = 0;
            uint lastSectionVSize = 0;
            uint lastSectionRawPtr = 0;
            uint lastSectionRawSize = 0;

            for (int i = 0; i < numberOfSections; i++)
            {
                int off = sectionTableOffset + i * 40;
                uint va = BitConverter.ToUInt32(fileBytes, off + 12);
                uint vs = BitConverter.ToUInt32(fileBytes, off + 8);
                uint rawPtr = BitConverter.ToUInt32(fileBytes, off + 20);
                uint rawSz = BitConverter.ToUInt32(fileBytes, off + 16);

                if (va + vs > lastSectionVa + lastSectionVSize)
                {
                    lastSectionVa = va;
                    lastSectionVSize = vs;
                    lastSectionRawPtr = rawPtr;
                    lastSectionRawSize = rawSz;
                }
            }

            uint newVa = Align(lastSectionVa + lastSectionVSize, sectionAlignment);
            uint newRawSize = Align((uint)data.Length, fileAlignment);
            uint newRawPointer = Align((uint)fileBytes.Length, fileAlignment);
            uint newVirtualSize = (uint)data.Length;

            // Build the new section header (40 bytes)
            byte[] sectionHeader = new byte[40];
            byte[] nameBytes = Encoding.ASCII.GetBytes(name);
            Array.Copy(nameBytes, 0, sectionHeader, 0, Math.Min(nameBytes.Length, 8));

            // VirtualSize at offset 8
            Array.Copy(BitConverter.GetBytes(newVirtualSize), 0, sectionHeader, 8, 4);
            // VirtualAddress at offset 12
            Array.Copy(BitConverter.GetBytes(newVa), 0, sectionHeader, 12, 4);
            // SizeOfRawData at offset 16
            Array.Copy(BitConverter.GetBytes(newRawSize), 0, sectionHeader, 16, 4);
            // PointerToRawData at offset 20
            Array.Copy(BitConverter.GetBytes(newRawPointer), 0, sectionHeader, 20, 4);
            // Characteristics at offset 36
            Array.Copy(BitConverter.GetBytes(characteristics), 0, sectionHeader, 36, 4);

            // Write section header
            Array.Copy(sectionHeader, 0, fileBytes, newSectionHeaderOffset, 40);

            // Update NumberOfSections
            byte[] newNumSections = BitConverter.GetBytes((ushort)(numberOfSections + 1));
            Array.Copy(newNumSections, 0, fileBytes, fileHeaderOffset + 2, 2);

            // Update SizeOfImage in optional header
            uint newSizeOfImage = Align(newVa + newVirtualSize, sectionAlignment);
            int sizeOfImageOffset = magic == 0x20B ? optionalHeaderOffset + 56 : optionalHeaderOffset + 56;
            Array.Copy(BitConverter.GetBytes(newSizeOfImage), 0, fileBytes, sizeOfImageOffset, 4);

            // Extend file with section data
            byte[] newFile = new byte[newRawPointer + newRawSize];
            Array.Copy(fileBytes, 0, newFile, 0, fileBytes.Length);

            // Pad between old end and new raw pointer with zeros (already zero from new array)
            Array.Copy(data, 0, newFile, newRawPointer, data.Length);

            return newFile;
        }

        /// <summary>
        /// Modify characteristics and optionally sizes of an existing section.
        /// </summary>
        public static void ModifySectionProperties(byte[] fileBytes, int sectionIndex,
            uint? newCharacteristics = null, uint? newVirtualSize = null, uint? newRawSize = null)
        {
            int sectionTableOffset = GetSectionTableOffset(fileBytes);
            int sectionHeaderOffset = sectionTableOffset + sectionIndex * 40;

            if (sectionHeaderOffset + 40 > fileBytes.Length)
                throw new ArgumentOutOfRangeException(nameof(sectionIndex), "Section index out of range.");

            if (newVirtualSize.HasValue)
                Array.Copy(BitConverter.GetBytes(newVirtualSize.Value), 0, fileBytes, sectionHeaderOffset + 8, 4);

            if (newRawSize.HasValue)
                Array.Copy(BitConverter.GetBytes(newRawSize.Value), 0, fileBytes, sectionHeaderOffset + 16, 4);

            if (newCharacteristics.HasValue)
                Array.Copy(BitConverter.GetBytes(newCharacteristics.Value), 0, fileBytes, sectionHeaderOffset + 36, 4);
        }

        /// <summary>
        /// Inject shellcode by creating a new executable section.
        /// Returns the RVA of the injected code and the new file bytes.
        /// </summary>
        public static (byte[] newFileBytes, uint shellcodeRva) InjectShellcode(
            byte[] fileBytes, byte[] shellcode, string sectionName = ".obli")
        {
            // Read+Execute+Code characteristics
            uint characteristics = 0x60000020; // IMAGE_SCN_MEM_READ | IMAGE_SCN_MEM_EXECUTE | IMAGE_SCN_CNT_CODE

            byte[] newFile = AddSection(fileBytes, sectionName, shellcode, characteristics);

            // The new section's VA is in the last section header
            int peOffset = FindPEHeaderOffset(newFile);
            int fileHeaderOffset = peOffset + 4;
            ushort numSections = BitConverter.ToUInt16(newFile, fileHeaderOffset + 2);
            int sectionTableOffset = GetSectionTableOffset(newFile);
            int lastSectionOffset = sectionTableOffset + (numSections - 1) * 40;
            uint shellcodeRva = BitConverter.ToUInt32(newFile, lastSectionOffset + 12);

            return (newFile, shellcodeRva);
        }

        /// <summary>
        /// Inject shellcode and redirect entry point to it.
        /// Optionally prepend a JMP back to the original entry point.
        /// </summary>
        public static byte[] InjectShellcodeWithEpRedirect(
            byte[] fileBytes, byte[] shellcode, string sectionName, bool redirectBack, int bitness)
        {
            uint originalEp = ReadEntryPoint(fileBytes);

            byte[] payload;
            if (redirectBack)
            {
                // We'll append a JMP to original EP at the end of shellcode
                // For simplicity, use a 5-byte relative JMP (works for both 32 and 64 when reachable)
                // The actual JMP target is calculated after we know the shellcode RVA
                payload = new byte[shellcode.Length + 5];
                Array.Copy(shellcode, 0, payload, 0, shellcode.Length);
                // JMP placeholder — will be patched after we know the RVA
                payload[shellcode.Length] = 0xE9; // JMP rel32
            }
            else
            {
                payload = shellcode;
            }

            var (newFile, shellcodeRva) = InjectShellcode(fileBytes, payload, sectionName);

            if (redirectBack)
            {
                // Calculate relative JMP from end of shellcode to original EP
                uint jmpInstructionRva = shellcodeRva + (uint)shellcode.Length;
                int relOffset = (int)(originalEp - (jmpInstructionRva + 5));
                byte[] relBytes = BitConverter.GetBytes(relOffset);

                // Find file offset of the JMP operand
                int peOff = FindPEHeaderOffset(newFile);
                int fhOff = peOff + 4;
                ushort numSections = BitConverter.ToUInt16(newFile, fhOff + 2);
                int stOff = GetSectionTableOffsetFromPE(newFile, peOff);
                int lastSecOff = stOff + (numSections - 1) * 40;
                uint rawPtr = BitConverter.ToUInt32(newFile, lastSecOff + 20);

                long jmpOperandFileOffset = rawPtr + shellcode.Length + 1; // +1 for 0xE9 opcode
                Array.Copy(relBytes, 0, newFile, jmpOperandFileOffset, 4);
            }

            // Set entry point to shellcode
            ChangeEntryPoint(newFile, shellcodeRva);

            return newFile;
        }

        /// <summary>
        /// Find code caves (runs of zero bytes) in existing sections.
        /// Returns list of (fileOffset, length) pairs.
        /// </summary>
        public static List<(long offset, int length)> FindCodeCaves(
            byte[] fileBytes, int minSize, int peOffset)
        {
            var caves = new List<(long offset, int length)>();

            int fileHeaderOffset = peOffset + 4;
            ushort numSections = BitConverter.ToUInt16(fileBytes, fileHeaderOffset + 2);
            int sectionTableOffset = GetSectionTableOffsetFromPE(fileBytes, peOffset);

            for (int i = 0; i < numSections; i++)
            {
                int secOff = sectionTableOffset + i * 40;
                uint rawPtr = BitConverter.ToUInt32(fileBytes, secOff + 20);
                uint rawSize = BitConverter.ToUInt32(fileBytes, secOff + 16);

                if (rawPtr == 0 || rawSize == 0) continue;

                int zeroRun = 0;
                long runStart = 0;

                for (long j = rawPtr; j < rawPtr + rawSize && j < fileBytes.Length; j++)
                {
                    if (fileBytes[j] == 0x00)
                    {
                        if (zeroRun == 0) runStart = j;
                        zeroRun++;
                    }
                    else
                    {
                        if (zeroRun >= minSize)
                            caves.Add((runStart, zeroRun));
                        zeroRun = 0;
                    }
                }

                if (zeroRun >= minSize)
                    caves.Add((runStart, zeroRun));
            }

            return caves;
        }

        /// <summary>
        /// Save modified bytes to disk.
        /// </summary>
        public static void SaveFile(byte[] fileBytes, string outputPath)
        {
            File.WriteAllBytes(outputPath, fileBytes);
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Parse DOS header e_lfanew to find PE signature offset.
        /// Validates MZ and PE signatures.
        /// </summary>
        public static int FindPEHeaderOffset(byte[] fileBytes)
        {
            if (fileBytes.Length < 64)
                throw new InvalidOperationException("File too small for DOS header.");

            // Check MZ signature
            if (fileBytes[0] != 0x4D || fileBytes[1] != 0x5A)
                throw new InvalidOperationException("Not a valid PE file: missing MZ signature.");

            int peOffset = BitConverter.ToInt32(fileBytes, 0x3C); // e_lfanew

            if (peOffset <= 0 || peOffset + 4 > fileBytes.Length)
                throw new InvalidOperationException($"Invalid e_lfanew value: 0x{peOffset:X}");

            // Check PE signature
            if (fileBytes[peOffset] != 0x50 || fileBytes[peOffset + 1] != 0x45 ||
                fileBytes[peOffset + 2] != 0x00 || fileBytes[peOffset + 3] != 0x00)
                throw new InvalidOperationException("Not a valid PE file: missing PE signature.");

            return peOffset;
        }

        /// <summary>
        /// Calculate section table start offset.
        /// </summary>
        public static int GetSectionTableOffset(byte[] fileBytes)
        {
            int peOffset = FindPEHeaderOffset(fileBytes);
            return GetSectionTableOffsetFromPE(fileBytes, peOffset);
        }

        private static int GetSectionTableOffsetFromPE(byte[] fileBytes, int peOffset)
        {
            int fileHeaderOffset = peOffset + 4;
            ushort sizeOfOptionalHeader = BitConverter.ToUInt16(fileBytes, fileHeaderOffset + 16);
            return peOffset + 24 + sizeOfOptionalHeader;
        }

        /// <summary>
        /// Recalculate PE checksum and write it back.
        /// </summary>
        public static void RecalculateChecksum(byte[] fileBytes)
        {
            int peOffset = FindPEHeaderOffset(fileBytes);
            int optionalHeaderOffset = peOffset + 24;
            ushort magic = BitConverter.ToUInt16(fileBytes, optionalHeaderOffset);

            // CheckSum offset is at optional header + 64
            int checksumOffset = optionalHeaderOffset + 64;

            if (checksumOffset + 4 > fileBytes.Length) return;

            // Zero out current checksum
            fileBytes[checksumOffset] = 0;
            fileBytes[checksumOffset + 1] = 0;
            fileBytes[checksumOffset + 2] = 0;
            fileBytes[checksumOffset + 3] = 0;

            // Calculate checksum
            long checksum = 0;
            int remainder = fileBytes.Length % 2;

            for (int i = 0; i < fileBytes.Length - remainder; i += 2)
            {
                int value = fileBytes[i] | (fileBytes[i + 1] << 8);
                checksum += value;
                checksum = (checksum & 0xFFFF) + (checksum >> 16);
            }

            if (remainder != 0)
            {
                checksum += fileBytes[fileBytes.Length - 1];
                checksum = (checksum & 0xFFFF) + (checksum >> 16);
            }

            checksum = (checksum & 0xFFFF) + (checksum >> 16);
            checksum += fileBytes.Length;

            byte[] checksumBytes = BitConverter.GetBytes((uint)checksum);
            Array.Copy(checksumBytes, 0, fileBytes, checksumOffset, 4);
        }

        /// <summary>
        /// Determine if PE is 64-bit (PE32+).
        /// </summary>
        public static bool IsPE64(byte[] fileBytes)
        {
            int peOffset = FindPEHeaderOffset(fileBytes);
            int optionalHeaderOffset = peOffset + 24;
            ushort magic = BitConverter.ToUInt16(fileBytes, optionalHeaderOffset);
            return magic == 0x20B;
        }

        /// <summary>
        /// Get number of sections.
        /// </summary>
        public static ushort GetNumberOfSections(byte[] fileBytes)
        {
            int peOffset = FindPEHeaderOffset(fileBytes);
            return BitConverter.ToUInt16(fileBytes, peOffset + 4 + 2);
        }

        /// <summary>
        /// Read section header info at given index.
        /// </summary>
        public static (string name, uint va, uint vs, uint rawPtr, uint rawSize, uint characteristics)
            ReadSectionHeader(byte[] fileBytes, int index)
        {
            int sectionTableOffset = GetSectionTableOffset(fileBytes);
            int off = sectionTableOffset + index * 40;

            if (off + 40 > fileBytes.Length)
                throw new ArgumentOutOfRangeException(nameof(index));

            string name = Encoding.ASCII.GetString(fileBytes, off, 8).TrimEnd('\0');
            uint vs = BitConverter.ToUInt32(fileBytes, off + 8);
            uint va = BitConverter.ToUInt32(fileBytes, off + 12);
            uint rawSize = BitConverter.ToUInt32(fileBytes, off + 16);
            uint rawPtr = BitConverter.ToUInt32(fileBytes, off + 20);
            uint chars = BitConverter.ToUInt32(fileBytes, off + 36);

            return (name, va, vs, rawPtr, rawSize, chars);
        }

        /// <summary>
        /// Convert RVA to file offset (RAW) using section table.
        /// Returns -1 if RVA is not within any section.
        /// </summary>
        public static long RvaToRaw(byte[] fileBytes, uint rva)
        {
            int peOffset = FindPEHeaderOffset(fileBytes);
            int fileHeaderOffset = peOffset + 4;
            ushort numSections = BitConverter.ToUInt16(fileBytes, fileHeaderOffset + 2);
            int sectionTableOffset = GetSectionTableOffsetFromPE(fileBytes, peOffset);

            for (int i = 0; i < numSections; i++)
            {
                int off = sectionTableOffset + i * 40;
                uint va = BitConverter.ToUInt32(fileBytes, off + 12);
                uint vs = BitConverter.ToUInt32(fileBytes, off + 8);
                uint rawPtr = BitConverter.ToUInt32(fileBytes, off + 20);
                uint rawSize = BitConverter.ToUInt32(fileBytes, off + 16);

                // Use the larger of VirtualSize and SizeOfRawData for coverage
                uint sectionCoverage = Math.Max(vs, rawSize);
                if (rva >= va && rva < va + sectionCoverage)
                {
                    return rawPtr + (rva - va);
                }
            }

            // RVA might be in header (before first section)
            if (rva < GetFirstSectionRawPointer(fileBytes, sectionTableOffset, numSections))
                return rva;

            return -1;
        }

        /// <summary>
        /// Convert file offset (RAW) to RVA using section table.
        /// Returns -1 if offset is not within any section.
        /// </summary>
        public static long RawToRva(byte[] fileBytes, uint rawOffset)
        {
            int peOffset = FindPEHeaderOffset(fileBytes);
            int fileHeaderOffset = peOffset + 4;
            ushort numSections = BitConverter.ToUInt16(fileBytes, fileHeaderOffset + 2);
            int sectionTableOffset = GetSectionTableOffsetFromPE(fileBytes, peOffset);

            for (int i = 0; i < numSections; i++)
            {
                int off = sectionTableOffset + i * 40;
                uint va = BitConverter.ToUInt32(fileBytes, off + 12);
                uint rawPtr = BitConverter.ToUInt32(fileBytes, off + 20);
                uint rawSize = BitConverter.ToUInt32(fileBytes, off + 16);

                if (rawPtr > 0 && rawOffset >= rawPtr && rawOffset < rawPtr + rawSize)
                {
                    return va + (rawOffset - rawPtr);
                }
            }

            // Offset in header area maps 1:1
            uint firstRawPtr = GetFirstSectionRawPointer(fileBytes, sectionTableOffset, numSections);
            if (rawOffset < firstRawPtr)
                return rawOffset;

            return -1;
        }

        /// <summary>
        /// Convert RVA to VA (Virtual Address) by adding ImageBase.
        /// </summary>
        public static ulong RvaToVa(byte[] fileBytes, uint rva)
        {
            int peOffset = FindPEHeaderOffset(fileBytes);
            int optionalHeaderOffset = peOffset + 24;
            ushort magic = BitConverter.ToUInt16(fileBytes, optionalHeaderOffset);

            ulong imageBase;
            if (magic == 0x20B) // PE32+
                imageBase = BitConverter.ToUInt64(fileBytes, optionalHeaderOffset + 24);
            else
                imageBase = BitConverter.ToUInt32(fileBytes, optionalHeaderOffset + 28);

            return imageBase + rva;
        }

        /// <summary>
        /// Convert VA to RVA by subtracting ImageBase.
        /// Returns -1 if VA is below ImageBase.
        /// </summary>
        public static long VaToRva(byte[] fileBytes, ulong va)
        {
            int peOffset = FindPEHeaderOffset(fileBytes);
            int optionalHeaderOffset = peOffset + 24;
            ushort magic = BitConverter.ToUInt16(fileBytes, optionalHeaderOffset);

            ulong imageBase;
            if (magic == 0x20B)
                imageBase = BitConverter.ToUInt64(fileBytes, optionalHeaderOffset + 24);
            else
                imageBase = BitConverter.ToUInt32(fileBytes, optionalHeaderOffset + 28);

            if (va < imageBase) return -1;
            return (long)(va - imageBase);
        }

        /// <summary>
        /// Get the containing section name for a given RVA.
        /// </summary>
        public static string? GetSectionForRva(byte[] fileBytes, uint rva)
        {
            int peOffset = FindPEHeaderOffset(fileBytes);
            int fileHeaderOffset = peOffset + 4;
            ushort numSections = BitConverter.ToUInt16(fileBytes, fileHeaderOffset + 2);
            int sectionTableOffset = GetSectionTableOffsetFromPE(fileBytes, peOffset);

            for (int i = 0; i < numSections; i++)
            {
                int off = sectionTableOffset + i * 40;
                uint va = BitConverter.ToUInt32(fileBytes, off + 12);
                uint vs = BitConverter.ToUInt32(fileBytes, off + 8);
                string name = Encoding.ASCII.GetString(fileBytes, off, 8).TrimEnd('\0');

                if (rva >= va && rva < va + vs)
                    return name;
            }

            return null;
        }

        private static uint GetFirstSectionRawPointer(byte[] fileBytes, int sectionTableOffset, int numSections)
        {
            uint first = uint.MaxValue;
            for (int i = 0; i < numSections; i++)
            {
                uint rawPtr = BitConverter.ToUInt32(fileBytes, sectionTableOffset + i * 40 + 20);
                if (rawPtr > 0 && rawPtr < first)
                    first = rawPtr;
            }
            return first == uint.MaxValue ? 0 : first;
        }

        private static uint Align(uint value, uint alignment)
        {
            if (alignment == 0) return value;
            uint remainder = value % alignment;
            return remainder == 0 ? value : value + alignment - remainder;
        }

        #endregion
    }
}

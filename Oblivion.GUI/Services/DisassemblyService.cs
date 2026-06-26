using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Iced.Intel;
using Oblivion.Data.Snapshots;
using Oblivion.GUI.MVVM.Model;
using Decoder = Iced.Intel.Decoder;

namespace Oblivion.GUI.Services
{
    public static class DisassemblyService
    {
        public static List<DisassembledInstruction> Disassemble(
            byte[] fileBytes,
            int rva,
            IList<PESectionSnapshot> sections,
            int bitness,
            long imageBase,
            int maxInstructions = 500)
        {
            var results = new List<DisassembledInstruction>();

            long fileOffset = RvaToFileOffset(rva, sections);
            if (fileOffset < 0 || fileOffset >= fileBytes.Length)
                return results;

            int availableBytes = (int)(fileBytes.Length - fileOffset);
            var codeBytes = new byte[availableBytes];
            Array.Copy(fileBytes, fileOffset, codeBytes, 0, availableBytes);

            ulong codeRip = (ulong)(imageBase + rva);
            var decoder = Decoder.Create(bitness, codeBytes);
            decoder.IP = codeRip;

            var formatter = new NasmFormatter();
            formatter.Options.DigitSeparator = "";
            formatter.Options.FirstOperandCharIndex = 0;
            formatter.Options.SpaceAfterOperandSeparator = true;
            formatter.Options.UppercaseHex = true;

            var output = new StringOutput();
            int count = 0;
            ulong endRip = codeRip + (ulong)availableBytes;

            while (decoder.IP < endRip && count < maxInstructions)
            {
                decoder.Decode(out var instruction);

                if (instruction.IsInvalid)
                    break;

                formatter.Format(instruction, output);
                string formatted = output.ToStringAndReset();

                // Split formatted string into mnemonic + operands
                string mnemonic;
                string operands;
                int spaceIdx = formatted.IndexOf(' ');
                if (spaceIdx >= 0)
                {
                    mnemonic = formatted[..spaceIdx];
                    operands = formatted[(spaceIdx + 1)..].TrimStart();
                }
                else
                {
                    mnemonic = formatted;
                    operands = "";
                }

                // Build hex bytes string
                int instrLen = instruction.Length;
                long instrFileOffset = fileOffset + (long)(instruction.IP - codeRip);
                var bytesStr = new StringBuilder(instrLen * 3);
                for (int i = 0; i < instrLen && instrFileOffset + i < fileBytes.Length; i++)
                {
                    if (i > 0) bytesStr.Append(' ');
                    bytesStr.Append($"{fileBytes[instrFileOffset + i]:X2}");
                }

                results.Add(new DisassembledInstruction
                {
                    Address = $"{instruction.IP:X16}",
                    Bytes = bytesStr.ToString(),
                    Mnemonic = mnemonic,
                    Operands = operands,
                    RawAddress = instruction.IP,
                    Category = CategorizeInstruction(instruction),
                    Length = instrLen,
                    FileOffset = instrFileOffset
                });

                count++;
            }

            return results;
        }

        /// <summary>
        /// Assemble a single instruction text using Iced.Intel.Assembler.
        /// Returns assembled bytes.
        /// Note: Iced's Assembler uses a fluent API, not text parsing.
        /// For text-based assembly, we do manual encoding of common patterns.
        /// </summary>
        public static byte[] AssembleInstruction(string asmText, int bitness, ulong ip)
        {
            string text = asmText.Trim().ToLowerInvariant();

            // Handle NOP
            if (text == "nop")
                return [0x90];

            // Handle RET
            if (text == "ret")
                return [0xC3];

            // Handle INT3
            if (text == "int3")
                return [0xCC];

            // Handle simple instructions via Iced Assembler fluent API
            try
            {
                var asm = new Assembler(bitness);

                if (TryAssembleWithIced(asm, text, bitness))
                {
                    using var stream = new MemoryStream();
                    asm.Assemble(new StreamCodeWriter(stream), ip);
                    return stream.ToArray();
                }
            }
            catch
            {
                // Fall through to raw hex parsing
            }

            // If text looks like hex bytes (e.g., "90 90 CC" or "0x90"), parse as raw bytes
            return ParseHexBytes(text);
        }

        private static bool TryAssembleWithIced(Assembler asm, string text, int bitness)
        {
            // Parse common instruction patterns
            // Format: "mnemonic" or "mnemonic operand" or "mnemonic op1, op2"
            var parts = text.Split([' ', '\t'], 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return false;

            string mnemonic = parts[0];
            string operandsStr = parts.Length > 1 ? parts[1].Trim() : "";

            switch (mnemonic)
            {
                case "nop":
                    asm.nop();
                    return true;
                case "ret":
                    asm.ret();
                    return true;
                case "int3":
                    asm.int3();
                    return true;
                case "push" when TryParseRegister(operandsStr, bitness, out var reg):
                    asm.push(reg);
                    return true;
                case "pop" when TryParseRegister(operandsStr, bitness, out var regPop):
                    asm.pop(regPop);
                    return true;
                case "xor" when TryParseTwoRegisters(operandsStr, bitness, out var r1, out var r2):
                    asm.xor(r1, r2);
                    return true;
                case "mov" when TryParseTwoRegisters(operandsStr, bitness, out var mr1, out var mr2):
                    asm.mov(mr1, mr2);
                    return true;
                default:
                    return false;
            }
        }

        private static bool TryParseRegister(string text, int bitness, out AssemblerRegister64 reg)
        {
            reg = default;
            text = text.Trim().ToLowerInvariant();

            // Map of register names to AssemblerRegisters
            reg = text switch
            {
                "rax" => AssemblerRegisters.rax,
                "rbx" => AssemblerRegisters.rbx,
                "rcx" => AssemblerRegisters.rcx,
                "rdx" => AssemblerRegisters.rdx,
                "rsp" => AssemblerRegisters.rsp,
                "rbp" => AssemblerRegisters.rbp,
                "rsi" => AssemblerRegisters.rsi,
                "rdi" => AssemblerRegisters.rdi,
                "r8" => AssemblerRegisters.r8,
                "r9" => AssemblerRegisters.r9,
                "r10" => AssemblerRegisters.r10,
                "r11" => AssemblerRegisters.r11,
                "r12" => AssemblerRegisters.r12,
                "r13" => AssemblerRegisters.r13,
                "r14" => AssemblerRegisters.r14,
                "r15" => AssemblerRegisters.r15,
                _ => default
            };

            return text is "rax" or "rbx" or "rcx" or "rdx" or "rsp" or "rbp"
                or "rsi" or "rdi" or "r8" or "r9" or "r10" or "r11"
                or "r12" or "r13" or "r14" or "r15";
        }

        private static bool TryParseTwoRegisters(string operands, int bitness,
            out AssemblerRegister64 r1, out AssemblerRegister64 r2)
        {
            r1 = default;
            r2 = default;

            var parts = operands.Split(',', StringSplitOptions.TrimEntries);
            if (parts.Length != 2) return false;

            return TryParseRegister(parts[0], bitness, out r1) &&
                   TryParseRegister(parts[1], bitness, out r2);
        }

        /// <summary>
        /// NOP out an instruction at the given file offset.
        /// </summary>
        public static void NopInstruction(byte[] fileBytes, long fileOffset, int length)
        {
            for (int i = 0; i < length && fileOffset + i < fileBytes.Length; i++)
                fileBytes[fileOffset + i] = 0x90;
        }

        /// <summary>
        /// Patch an instruction at the given file offset with new bytes.
        /// Pads with NOPs if new bytes are shorter than original instruction.
        /// </summary>
        public static void PatchInstruction(byte[] fileBytes, long fileOffset, int originalLength, byte[] newBytes)
        {
            if (newBytes.Length > originalLength)
                throw new InvalidOperationException(
                    $"New bytes ({newBytes.Length}) exceed original instruction length ({originalLength}). " +
                    "Use a smaller instruction or NOP adjacent instructions first.");

            Array.Copy(newBytes, 0, fileBytes, fileOffset, newBytes.Length);

            // Pad remainder with NOPs
            for (int i = newBytes.Length; i < originalLength; i++)
                fileBytes[fileOffset + i] = 0x90;
        }

        /// <summary>
        /// Parse space-separated hex string into bytes.
        /// </summary>
        public static byte[] ParseHexBytes(string hex)
        {
            if (string.IsNullOrWhiteSpace(hex))
                return Array.Empty<byte>();

            hex = hex.Replace("0x", "").Replace(",", " ").Replace("-", " ");
            var parts = hex.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
            var bytes = new byte[parts.Length];

            for (int i = 0; i < parts.Length; i++)
                bytes[i] = Convert.ToByte(parts[i], 16);

            return bytes;
        }

        public static long RvaToFileOffset(int rva, IList<PESectionSnapshot> sections)
        {
            if (sections == null) return -1;

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

        private static InstructionCategory CategorizeInstruction(Instruction instruction)
        {
            return instruction.FlowControl switch
            {
                FlowControl.Call or
                FlowControl.IndirectCall => InstructionCategory.Call,

                FlowControl.ConditionalBranch or
                FlowControl.UnconditionalBranch or
                FlowControl.IndirectBranch => InstructionCategory.Jump,

                FlowControl.Return => InstructionCategory.Return,

                _ => instruction.Mnemonic == Mnemonic.Nop
                    ? InstructionCategory.Nop
                    : InstructionCategory.Normal,
            };
        }
    }
}

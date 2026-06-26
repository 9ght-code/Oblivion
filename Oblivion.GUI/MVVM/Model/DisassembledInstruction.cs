namespace Oblivion.GUI.MVVM.Model
{
    public enum InstructionCategory
    {
        Normal,
        Call,
        Jump,
        Return,
        Nop
    }

    public class DisassembledInstruction
    {
        public string Address { get; set; } = "";
        public string Bytes { get; set; } = "";
        public string Mnemonic { get; set; } = "";
        public string Operands { get; set; } = "";
        public ulong RawAddress { get; set; }
        public InstructionCategory Category { get; set; }

        /// <summary>Instruction byte count.</summary>
        public int Length { get; set; }

        /// <summary>File offset of this instruction (for patching back).</summary>
        public long FileOffset { get; set; }
    }
}

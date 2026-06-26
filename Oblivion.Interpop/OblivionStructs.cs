using System.Runtime.InteropServices;

namespace Oblivion.Interpop
{
    public static class OblivionStructs
    {
        public const int OBLIVION_MAX_SECTIONS  = 96;
        public const int OBLIVION_MAX_IMPORTS   = 128;
        public const int OBLIVION_MAX_FUNCTIONS = 512;
        public const int OBLIVION_FUNC_NAME_LEN = 64;
        public const int OBLIVION_DLL_NAME_LEN  = 128;

        // sizeof = 128 + 512*64 + 4 = 32900 bytes
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct OBLIVION_IMPORT
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = OBLIVION_DLL_NAME_LEN)]
            public string DllName;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = OBLIVION_MAX_FUNCTIONS * OBLIVION_FUNC_NAME_LEN)]
            public byte[] FunctionsRaw;

            public int FunctionCount;

            public string[] GetFunctions()
            {
                if (FunctionsRaw == null || FunctionCount == 0)
                    return [];

                int count = FunctionCount < OBLIVION_MAX_FUNCTIONS ? FunctionCount : OBLIVION_MAX_FUNCTIONS;
                var result = new string[count];
                for (int i = 0; i < count; i++)
                {
                    int start = i * OBLIVION_FUNC_NAME_LEN;
                    int end   = start;
                    while (end < start + OBLIVION_FUNC_NAME_LEN && FunctionsRaw[end] != 0)
                        end++;
                    result[i] = System.Text.Encoding.ASCII.GetString(FunctionsRaw, start, end - start);
                }
                return result;
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct OBLIVION_SECTION
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 9)]
            public string Name;

            public uint   VirtualAddress;
            public uint   VirtualSize;
            public uint   RawAddress;
            public uint   RawSize;
            public uint   Characteristics;
            public double Entropy;
        }

        // OBLIVION_RESULT with imports as pointer (heap-allocated by C core)
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct OBLIVION_RESULT_NATIVE
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 8)]
            public string Architecture;

            public ulong  ImageBase;
            public uint   EntryPoint;
            public ushort Machine;
            public ushort Characteristics;
            public uint   Timestamp;
            public ushort Subsystem;
            public ushort DllCharacteristics;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = OBLIVION_MAX_SECTIONS)]
            public OBLIVION_SECTION[] Sections;

            public int    SectionCount;

            public IntPtr ImportsPtr;   // OBLIVION_IMPORT* — heap array
            public int    ImportCount;

            public uint   OverlayOffset;
            public uint   OverlaySize;
            public double OverallEntropy;
        }
    }
}

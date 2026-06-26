using System;
using System.Runtime.InteropServices;
using static Oblivion.Interpop.OblivionStructs;

namespace Oblivion.Interpop
{
    public class OblivionApiService
    {
        [DllImport("Oblivion.Core.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern IntPtr Oblivion_AnalyzePE(
            [MarshalAs(UnmanagedType.LPStr)] string filePath,
            out int errorCode);

        [DllImport("Oblivion.Core.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void Oblivion_FreeResult(IntPtr result);

        /// <summary>
        /// Analyze a PE file. On success errorCode == 0 and the returned struct is fully populated.
        /// Returns null on failure; errorCode contains the PE_Error value.
        /// </summary>
        public AnalysisResult? AnalyzePE(string filePath, out int errorCode)
        {
            IntPtr ptr = Oblivion_AnalyzePE(filePath, out errorCode);
            if (ptr == IntPtr.Zero)
                return null;

            try
            {
                var native  = Marshal.PtrToStructure<OBLIVION_RESULT_NATIVE>(ptr);
                var imports = ReadImports(native.ImportsPtr, native.ImportCount);
                return new AnalysisResult(native, imports);
            }
            finally
            {
                Oblivion_FreeResult(ptr);
            }
        }

        private static OBLIVION_IMPORT[] ReadImports(IntPtr importsPtr, int count)
        {
            if (importsPtr == IntPtr.Zero || count <= 0)
                return [];

            int importSize = Marshal.SizeOf<OBLIVION_IMPORT>();
            var imports    = new OBLIVION_IMPORT[count];
            for (int i = 0; i < count; i++)
            {
                IntPtr entry = IntPtr.Add(importsPtr, i * importSize);
                imports[i]   = Marshal.PtrToStructure<OBLIVION_IMPORT>(entry);
            }
            return imports;
        }
    }

    public sealed class AnalysisResult(OblivionStructs.OBLIVION_RESULT_NATIVE native, OblivionStructs.OBLIVION_IMPORT[] imports)
    {
        public OblivionStructs.OBLIVION_RESULT_NATIVE Native  { get; } = native;
        public OblivionStructs.OBLIVION_IMPORT[]       Imports { get; } = imports;
    }
}

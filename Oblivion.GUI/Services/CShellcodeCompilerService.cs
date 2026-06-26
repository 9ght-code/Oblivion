using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;

namespace Oblivion.GUI.Services
{
    /// <summary>
    /// Compiles C source code into position-independent shellcode bytes.
    /// Detects available compilers (MinGW gcc, MSVC cl.exe, clang) and uses
    /// appropriate flags to produce a raw .text section blob.
    /// </summary>
    public static class CShellcodeCompilerService
    {
        public enum CompilerType
        {
            None,
            Gcc,
            Clang,
            Msvc
        }

        public class CompilerInfo
        {
            public CompilerType Type { get; init; }
            public string Path { get; init; } = "";
            public string DisplayName { get; init; } = "";
        }

        public class CompileResult
        {
            public bool Success { get; init; }
            public byte[] ShellcodeBytes { get; init; } = Array.Empty<byte>();
            public string Output { get; init; } = "";
            public string Error { get; init; } = "";
        }

        /// <summary>
        /// Detect available C compilers on the system.
        /// </summary>
        public static List<CompilerInfo> DetectCompilers()
        {
            var compilers = new List<CompilerInfo>();

            // Check gcc (MinGW)
            string? gccPath = FindInPath("gcc.exe") ?? FindInPath("x86_64-w64-mingw32-gcc.exe");
            if (gccPath != null)
            {
                compilers.Add(new CompilerInfo
                {
                    Type = CompilerType.Gcc,
                    Path = gccPath,
                    DisplayName = $"GCC (MinGW) — {gccPath}"
                });
            }

            // Check clang
            string? clangPath = FindInPath("clang.exe");
            if (clangPath != null)
            {
                compilers.Add(new CompilerInfo
                {
                    Type = CompilerType.Clang,
                    Path = clangPath,
                    DisplayName = $"Clang — {clangPath}"
                });
            }

            // Check MSVC cl.exe (common VS paths)
            string? clPath = FindInPath("cl.exe");
            if (clPath == null)
            {
                // Try common VS installation paths
                var vsBasePaths = new[]
                {
                    @"C:\Program Files\Microsoft Visual Studio",
                    @"C:\Program Files (x86)\Microsoft Visual Studio"
                };

                foreach (var vsBase in vsBasePaths)
                {
                    if (!Directory.Exists(vsBase)) continue;

                    try
                    {
                        var clFiles = Directory.GetFiles(vsBase, "cl.exe", SearchOption.AllDirectories);
                        var x64Cl = clFiles.FirstOrDefault(f =>
                            f.Contains("Hostx64", StringComparison.OrdinalIgnoreCase) &&
                            f.Contains("x64", StringComparison.OrdinalIgnoreCase));
                        clPath = x64Cl ?? clFiles.FirstOrDefault();
                    }
                    catch
                    {
                        // Access denied to some directories
                    }

                    if (clPath != null) break;
                }
            }

            if (clPath != null)
            {
                compilers.Add(new CompilerInfo
                {
                    Type = CompilerType.Msvc,
                    Path = clPath,
                    DisplayName = $"MSVC (cl.exe) — {clPath}"
                });
            }

            return compilers;
        }

        /// <summary>
        /// Compile C source code to shellcode bytes.
        /// The C code should be self-contained (no stdlib calls unless resolved manually).
        /// </summary>
        public static CompileResult Compile(string cSource, CompilerInfo compiler, bool is64Bit = true)
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "oblivion_shellcode_" + Guid.NewGuid().ToString("N")[..8]);
            Directory.CreateDirectory(tempDir);

            try
            {
                string srcFile = Path.Combine(tempDir, "shellcode.c");
                string objFile = Path.Combine(tempDir, "shellcode.o");
                string binFile = Path.Combine(tempDir, "shellcode.bin");

                File.WriteAllText(srcFile, cSource, Encoding.UTF8);

                return compiler.Type switch
                {
                    CompilerType.Gcc => CompileWithGcc(compiler.Path, srcFile, objFile, binFile, tempDir, is64Bit),
                    CompilerType.Clang => CompileWithClang(compiler.Path, srcFile, objFile, binFile, tempDir, is64Bit),
                    CompilerType.Msvc => CompileWithMsvc(compiler.Path, srcFile, objFile, binFile, tempDir, is64Bit),
                    _ => new CompileResult { Success = false, Error = "No compiler selected." }
                };
            }
            finally
            {
                try { Directory.Delete(tempDir, true); }
                catch { /* cleanup best effort */ }
            }
        }

        private static CompileResult CompileWithGcc(string gccPath, string srcFile, string objFile,
            string binFile, string tempDir, bool is64Bit)
        {
            var errors = new StringBuilder();
            var output = new StringBuilder();

            // Step 1: Compile to object file (position-independent, no stdlib, freestanding)
            string arch = is64Bit ? "-m64" : "-m32";
            string compileArgs = $"{arch} -c -Os -fno-stack-protector -fno-ident -fno-asynchronous-unwind-tables " +
                                 $"-nostdlib -ffreestanding -o \"{objFile}\" \"{srcFile}\"";

            var (exitCode1, stdout1, stderr1) = RunProcess(gccPath, compileArgs, tempDir);
            output.AppendLine($"[Compile] gcc {compileArgs}");
            if (!string.IsNullOrEmpty(stdout1)) output.AppendLine(stdout1);

            if (exitCode1 != 0)
            {
                return new CompileResult
                {
                    Success = false,
                    Error = $"Compilation failed (exit code {exitCode1}):\n{stderr1}",
                    Output = output.ToString()
                };
            }

            // Step 2: Extract .text section from the object file
            byte[] shellcode = ExtractTextSection(objFile);

            if (shellcode.Length == 0)
            {
                // Fallback: try using objcopy to extract raw binary
                string objcopyPath = Path.Combine(Path.GetDirectoryName(gccPath) ?? "", "objcopy.exe");
                if (!File.Exists(objcopyPath))
                    objcopyPath = FindInPath("objcopy.exe") ?? "objcopy.exe";

                string objcopyArgs = $"-O binary -j .text \"{objFile}\" \"{binFile}\"";
                var (exitCode2, stdout2, stderr2) = RunProcess(objcopyPath, objcopyArgs, tempDir);
                output.AppendLine($"[Extract] objcopy {objcopyArgs}");

                if (exitCode2 == 0 && File.Exists(binFile))
                {
                    shellcode = File.ReadAllBytes(binFile);
                }
                else
                {
                    return new CompileResult
                    {
                        Success = false,
                        Error = $"Failed to extract .text section:\n{stderr2}",
                        Output = output.ToString()
                    };
                }
            }

            output.AppendLine($"[OK] Extracted {shellcode.Length} bytes of shellcode.");

            return new CompileResult
            {
                Success = true,
                ShellcodeBytes = shellcode,
                Output = output.ToString()
            };
        }

        private static CompileResult CompileWithClang(string clangPath, string srcFile, string objFile,
            string binFile, string tempDir, bool is64Bit)
        {
            var output = new StringBuilder();

            string target = is64Bit ? "--target=x86_64-pc-windows-msvc" : "--target=i686-pc-windows-msvc";
            string compileArgs = $"{target} -c -Os -fno-stack-protector -fno-ident " +
                                 $"-nostdlib -ffreestanding -o \"{objFile}\" \"{srcFile}\"";

            var (exitCode1, stdout1, stderr1) = RunProcess(clangPath, compileArgs, tempDir);
            output.AppendLine($"[Compile] clang {compileArgs}");
            if (!string.IsNullOrEmpty(stdout1)) output.AppendLine(stdout1);

            if (exitCode1 != 0)
            {
                return new CompileResult
                {
                    Success = false,
                    Error = $"Compilation failed (exit code {exitCode1}):\n{stderr1}",
                    Output = output.ToString()
                };
            }

            byte[] shellcode = ExtractTextSection(objFile);

            if (shellcode.Length == 0)
            {
                return new CompileResult
                {
                    Success = false,
                    Error = "Failed to extract .text section from object file.",
                    Output = output.ToString()
                };
            }

            output.AppendLine($"[OK] Extracted {shellcode.Length} bytes of shellcode.");

            return new CompileResult
            {
                Success = true,
                ShellcodeBytes = shellcode,
                Output = output.ToString()
            };
        }

        private static CompileResult CompileWithMsvc(string clPath, string srcFile, string objFile,
            string binFile, string tempDir, bool is64Bit)
        {
            var output = new StringBuilder();

            // MSVC: compile to object, then extract .text
            string compileArgs = $"/c /Os /GS- /Zl /FA /Fo\"{objFile}\" \"{srcFile}\"";

            var (exitCode1, stdout1, stderr1) = RunProcess(clPath, compileArgs, tempDir);
            output.AppendLine($"[Compile] cl.exe {compileArgs}");
            if (!string.IsNullOrEmpty(stdout1)) output.AppendLine(stdout1);

            if (exitCode1 != 0)
            {
                return new CompileResult
                {
                    Success = false,
                    Error = $"Compilation failed (exit code {exitCode1}):\n{stderr1}",
                    Output = output.ToString()
                };
            }

            byte[] shellcode = ExtractTextSection(objFile);

            if (shellcode.Length == 0)
            {
                return new CompileResult
                {
                    Success = false,
                    Error = "Failed to extract .text section from COFF object.",
                    Output = output.ToString()
                };
            }

            output.AppendLine($"[OK] Extracted {shellcode.Length} bytes of shellcode.");

            return new CompileResult
            {
                Success = true,
                ShellcodeBytes = shellcode,
                Output = output.ToString()
            };
        }

        /// <summary>
        /// Extract .text section bytes from a COFF/PE object file using PEReader.
        /// Falls back to manual COFF parsing if PEReader fails.
        /// </summary>
        private static byte[] ExtractTextSection(string objFile)
        {
            if (!File.Exists(objFile)) return Array.Empty<byte>();

            byte[] fileBytes = File.ReadAllBytes(objFile);

            // Try manual COFF parsing (works for .o files from gcc/clang/msvc)
            try
            {
                return ExtractTextFromCoff(fileBytes);
            }
            catch
            {
                // Not a valid COFF
            }

            // Try PEReader (works for PE/COFF)
            try
            {
                using var stream = new MemoryStream(fileBytes);
                using var peReader = new PEReader(stream);

                foreach (var sectionHeader in peReader.PEHeaders.SectionHeaders)
                {
                    if (sectionHeader.Name.StartsWith(".text"))
                    {
                        var sectionData = peReader.GetSectionData(sectionHeader.VirtualAddress);
                        return sectionData.GetContent().ToArray();
                    }
                }
            }
            catch
            {
                // Not a PE file
            }

            return Array.Empty<byte>();
        }

        /// <summary>
        /// Parse COFF object file manually and extract .text section.
        /// COFF header: Machine(2) + NumberOfSections(2) + TimeDateStamp(4) +
        ///   PointerToSymbolTable(4) + NumberOfSymbols(4) + SizeOfOptionalHeader(2) + Characteristics(2)
        /// Section header (40 bytes each): Name(8) + VirtualSize(4) + VirtualAddress(4) +
        ///   SizeOfRawData(4) + PointerToRawData(4) + ...
        /// </summary>
        private static byte[] ExtractTextFromCoff(byte[] data)
        {
            if (data.Length < 20) return Array.Empty<byte>();

            ushort machine = BitConverter.ToUInt16(data, 0);
            // Validate it looks like a COFF (x86 or x64)
            if (machine != 0x14C && machine != 0x8664)
                return Array.Empty<byte>();

            ushort numSections = BitConverter.ToUInt16(data, 2);
            ushort sizeOfOptionalHeader = BitConverter.ToUInt16(data, 16);

            int sectionTableStart = 20 + sizeOfOptionalHeader;

            for (int i = 0; i < numSections; i++)
            {
                int off = sectionTableStart + i * 40;
                if (off + 40 > data.Length) break;

                string name = Encoding.ASCII.GetString(data, off, 8).TrimEnd('\0');

                if (name == ".text")
                {
                    uint rawSize = BitConverter.ToUInt32(data, off + 16);
                    uint rawPtr = BitConverter.ToUInt32(data, off + 20);

                    if (rawPtr == 0 || rawSize == 0 || rawPtr + rawSize > data.Length)
                        return Array.Empty<byte>();

                    byte[] section = new byte[rawSize];
                    Array.Copy(data, rawPtr, section, 0, rawSize);
                    return section;
                }
            }

            return Array.Empty<byte>();
        }

        private static string? FindInPath(string executable)
        {
            string? pathVar = Environment.GetEnvironmentVariable("PATH");
            if (string.IsNullOrEmpty(pathVar)) return null;

            foreach (string dir in pathVar.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                try
                {
                    string fullPath = Path.Combine(dir.Trim(), executable);
                    if (File.Exists(fullPath))
                        return fullPath;
                }
                catch
                {
                    // Invalid path entry
                }
            }

            return null;
        }

        private static (int exitCode, string stdout, string stderr) RunProcess(
            string fileName, string arguments, string workingDir)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    WorkingDirectory = workingDir,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                if (process == null)
                    return (-1, "", "Failed to start process.");

                string stdout = process.StandardOutput.ReadToEnd();
                string stderr = process.StandardError.ReadToEnd();

                process.WaitForExit(30000); // 30s timeout

                if (!process.HasExited)
                {
                    process.Kill();
                    return (-1, stdout, "Process timed out after 30 seconds.");
                }

                return (process.ExitCode, stdout, stderr);
            }
            catch (Exception ex)
            {
                return (-1, "", $"Process error: {ex.Message}");
            }
        }
    }
}

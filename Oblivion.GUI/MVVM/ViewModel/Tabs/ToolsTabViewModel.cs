using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Oblivion.GUI.Domain.Abstractions;
using Oblivion.GUI.Services;

namespace Oblivion.GUI.MVVM.ViewModel.Tabs;

public partial class ToolsTabViewModel : ViewModelBase
{
    private readonly IAnalysisContext _context;

    // RVA/RAW Converter
    [ObservableProperty]
    private string _converterInput = "";

    [ObservableProperty]
    private string _converterResult = "";

    [ObservableProperty]
    private string _converterSection = "";

    public string ImageBase { get; }

    // C to Shellcode
    [ObservableProperty]
    private string _cSourceCode = "// Position-independent C code\n// No stdlib — resolve APIs manually\nvoid shellcode() {\n    // Your code here\n}";

    [ObservableProperty]
    private string _cCompileOutput = "";

    [ObservableProperty]
    private string _cCompiledHex = "";

    [ObservableProperty]
    private string _selectedCompiler = "";

    [ObservableProperty]
    private ObservableCollection<string> _availableCompilers = new();

    [ObservableProperty]
    private bool _cCompileIs64 = true;

    private List<CShellcodeCompilerService.CompilerInfo> _detectedCompilers = new();

    public ToolsTabViewModel(IAnalysisContext context, string imageBase)
    {
        _context = context;
        ImageBase = imageBase;
        DetectCompilers();
    }

    #region RVA/RAW Converter

    [RelayCommand]
    private void ConvertRvaToRaw()
    {
        if (_context.FileBytes == null || string.IsNullOrWhiteSpace(ConverterInput))
        {
            ConverterResult = "No input.";
            return;
        }

        try
        {
            uint value = ParseHexUint(ConverterInput);
            long raw = PEWriterService.RvaToRaw(_context.FileBytes, value);
            ulong va = PEWriterService.RvaToVa(_context.FileBytes, value);
            string? section = PEWriterService.GetSectionForRva(_context.FileBytes, value);

            if (raw < 0)
            {
                ConverterResult = $"RVA 0x{value:X} is not mapped to any section.";
                ConverterSection = "";
            }
            else
            {
                ConverterResult = $"RVA:  0x{value:X}\nRAW:  0x{raw:X}\nVA:   0x{va:X}";
                ConverterSection = section ?? "Header";
            }
        }
        catch (Exception ex)
        {
            ConverterResult = $"Error: {ex.Message}";
            ConverterSection = "";
        }
    }

    [RelayCommand]
    private void ConvertRawToRva()
    {
        if (_context.FileBytes == null || string.IsNullOrWhiteSpace(ConverterInput))
        {
            ConverterResult = "No input.";
            return;
        }

        try
        {
            uint value = ParseHexUint(ConverterInput);
            long rva = PEWriterService.RawToRva(_context.FileBytes, value);

            if (rva < 0)
            {
                ConverterResult = $"RAW 0x{value:X} is not mapped to any section.";
                ConverterSection = "";
            }
            else
            {
                ulong va = PEWriterService.RvaToVa(_context.FileBytes, (uint)rva);
                string? section = PEWriterService.GetSectionForRva(_context.FileBytes, (uint)rva);

                ConverterResult = $"RAW:  0x{value:X}\nRVA:  0x{rva:X}\nVA:   0x{va:X}";
                ConverterSection = section ?? "Header";
            }
        }
        catch (Exception ex)
        {
            ConverterResult = $"Error: {ex.Message}";
            ConverterSection = "";
        }
    }

    [RelayCommand]
    private void ConvertVaToRva()
    {
        if (_context.FileBytes == null || string.IsNullOrWhiteSpace(ConverterInput))
        {
            ConverterResult = "No input.";
            return;
        }

        try
        {
            ulong value = ParseHexUlong(ConverterInput);
            long rva = PEWriterService.VaToRva(_context.FileBytes, value);

            if (rva < 0)
            {
                ConverterResult = $"VA 0x{value:X} is below ImageBase.";
                ConverterSection = "";
            }
            else
            {
                long raw = PEWriterService.RvaToRaw(_context.FileBytes, (uint)rva);
                string? section = PEWriterService.GetSectionForRva(_context.FileBytes, (uint)rva);

                ConverterResult = raw >= 0
                    ? $"VA:   0x{value:X}\nRVA:  0x{rva:X}\nRAW:  0x{raw:X}"
                    : $"VA:   0x{value:X}\nRVA:  0x{rva:X}\nRAW:  (unmapped)";
                ConverterSection = section ?? (raw >= 0 ? "Header" : "");
            }
        }
        catch (Exception ex)
        {
            ConverterResult = $"Error: {ex.Message}";
            ConverterSection = "";
        }
    }

    [RelayCommand]
    private void GoToConverterResultInHex()
    {
        if (_context.FileBytes == null || string.IsNullOrWhiteSpace(ConverterInput)) return;

        try
        {
            uint value = ParseHexUint(ConverterInput);
            long raw = PEWriterService.RvaToRaw(_context.FileBytes, value);
            _context.SwitchToHexAtOffset(raw >= 0 ? raw : value);
        }
        catch { }
    }

    private static uint ParseHexUint(string input)
    {
        string cleaned = input.Trim();
        if (cleaned.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            cleaned = cleaned[2..];
        return uint.Parse(cleaned, System.Globalization.NumberStyles.HexNumber);
    }

    private static ulong ParseHexUlong(string input)
    {
        string cleaned = input.Trim();
        if (cleaned.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            cleaned = cleaned[2..];
        return ulong.Parse(cleaned, System.Globalization.NumberStyles.HexNumber);
    }

    #endregion

    #region C to Shellcode

    [RelayCommand]
    private void DetectCompilers()
    {
        var compilers = CShellcodeCompilerService.DetectCompilers();
        _detectedCompilers = compilers;

        AvailableCompilers.Clear();
        foreach (var c in compilers)
            AvailableCompilers.Add(c.DisplayName);

        if (compilers.Count > 0)
        {
            SelectedCompiler = compilers[0].DisplayName;
            CCompileOutput = $"Found {compilers.Count} compiler(s).";
        }
        else
        {
            CCompileOutput = "No C compilers found in PATH.\nInstall MinGW (gcc), Clang, or MSVC (cl.exe).";
        }
    }

    [RelayCommand]
    private void CompileCToShellcode()
    {
        if (string.IsNullOrWhiteSpace(CSourceCode))
        {
            CCompileOutput = "No source code.";
            return;
        }

        if (_detectedCompilers.Count == 0)
        {
            DetectCompilers();
            if (_detectedCompilers.Count == 0) return;
        }

        var compiler = _detectedCompilers.FirstOrDefault(c => c.DisplayName == SelectedCompiler)
                       ?? _detectedCompilers[0];

        CCompileOutput = $"Compiling with {compiler.DisplayName}...";

        try
        {
            var result = CShellcodeCompilerService.Compile(CSourceCode, compiler, CCompileIs64);

            CCompileOutput = result.Output;

            if (result.Success)
            {
                CCompiledHex = BitConverter.ToString(result.ShellcodeBytes).Replace("-", " ");
                CCompileOutput += $"\nShellcode: {result.ShellcodeBytes.Length} bytes";
            }
            else
            {
                CCompiledHex = "";
                CCompileOutput += $"\n{result.Error}";
            }
        }
        catch (Exception ex)
        {
            CCompileOutput = $"Compile error: {ex.Message}";
            CCompiledHex = "";
        }
    }

    [RelayCommand]
    private void CopyShellcodeToInjector()
    {
        if (!string.IsNullOrEmpty(CCompiledHex))
        {
            _context.ShellcodeHex = CCompiledHex;
            _context.ActiveTabIndex = 9; // Shellcode tab
            _context.EditStatus = "Shellcode copied to Injector tab.";
        }
    }

    #endregion
}

# Oblivion

A Windows desktop application for static analysis of PE files (`.exe`, `.dll`). Organize binaries into workspaces, import them, parse their structure with a native C engine, and explore the results through a modern Fluent UI.

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white)
![C](https://img.shields.io/badge/Core-C-00599C?logo=c&logoColor=white)
![WPF](https://img.shields.io/badge/UI-WPF%20%2B%20WPF--UI-blue)
![Platform](https://img.shields.io/badge/platform-Windows-0078D6?logo=windows&logoColor=white)
![License](https://img.shields.io/badge/license-MIT-green)

---

## Features

- **Workspaces** — organize analyzed files into folders, import, rename, move, and delete.
- **Integrity tracking** — hash-based verification with `Verified` / `Modified` / `Deleted` badges, plus per-file modification notes.
- **Native PE parser** — a low-level C engine (`Oblivion.Core`) handles MZ/PE validation, section parsing, and import directory walking.
- **Rich analysis surface** — a tabbed analysis window covering:
  - **Overview** — architecture, file size, entry point, and key header fields
  - **Sections** — section table with R/W/X permission badges and per-section entropy
  - **Imports** — imported libraries and resolved functions, annotated against a WinAPI function database
  - **Strings** — extracted ASCII/Unicode strings
  - **Disassembler** — x86/x64 disassembly powered by [Iced](https://github.com/icedland/iced), with instruction categorization
  - **Entry Point** — disassembly around the entry point
  - **Hex Editor** — raw byte inspection and editing
  - **Security** — anomaly detection, packer/installer heuristics, and overlay analysis
  - **Functions DB** — WinAPI function lookups
  - **Tools** — RVA ↔ RAW converter and an embedded C compiler helper
- **Entropy visualization** — per-section entropy to spot packed or encrypted regions.
- **PE editing** — add/edit sections and write changes back to disk.
- **PDF export** — generate analysis reports via QuestPDF.
- **Theming** — five built-in palettes (GitHub Dark, Monokai, Nord, Solarized Dark, Light) with a live hex color editor; themes persist across sessions.

## Architecture

Oblivion is a multi-project .NET 8 solution with a native C core:

| Project | Role |
|---|---|
| **Oblivion.Core** | Native C DLL — low-level PE parser (MZ/PE validation, sections, imports) |
| **Oblivion.Interpop** | C# P/Invoke wrapper over `Oblivion.Core.dll` |
| **Oblivion.Data** | EF Core + SQLite — entities, repositories, snapshots, hashing |
| **Oblivion.GUI** | WPF application (WPF-UI Fluent), MVVM via CommunityToolkit.Mvvm |

```
GUI ──> Data + Interop ──(P/Invoke)──> Core.dll
```

The GUI follows a **ViewModel-first MVVM** pattern: navigation is driven by implicit `DataTemplate` mapping, dependency injection is wired through `Microsoft.Extensions.Hosting`, and observable state uses CommunityToolkit source generators.

## Tech stack

- .NET 8, C# 12, WPF + [WPF-UI](https://github.com/lepoco/wpfui) 4.1
- [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet) 8.4 (`[ObservableProperty]`, `[RelayCommand]`)
- EF Core 8 + SQLite
- [Iced](https://github.com/icedland/iced) disassembler, [QuestPDF](https://www.questpdf.com/) reports, [WPFHexaEditor](https://github.com/abbaye/WpfHexEditorControl)
- Native C (Oblivion.Core)

## Build

Requires the **.NET 8 SDK** and, for the native core, **Visual Studio with the C++ workload** (the C engine builds as `Oblivion.Core.dll`).

```bash
# Build the full solution
dotnet build Oblivion.sln

# Or just the GUI
dotnet build Oblivion.GUI/Oblivion.GUI.csproj
```

Then run `Oblivion.GUI`. On first launch the app creates its SQLite database and theme file under `%AppData%/Oblivion/`.

> **Note:** The native `Oblivion.Core.dll` must be built (Visual Studio C++ workload) and available to `Oblivion.Interpop` for PE parsing to work.

## Project layout

```
Oblivion.Core/        Native C PE parser (DLL)
Oblivion.Interpop/    P/Invoke wrapper
Oblivion.Data/        EF Core + SQLite, repositories, snapshots
Oblivion.GUI/         WPF MVVM application
```

## License

[MIT](LICENSE) © Oblivion contributors

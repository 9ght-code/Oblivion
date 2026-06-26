using Oblivion.Data.Snapshots;
using Oblivion.GUI.MVVM.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Oblivion.GUI.Services
{
    public static class PEAnomalyAnalyzer
    {
        private static readonly HashSet<string> StandardSectionNames = new(StringComparer.OrdinalIgnoreCase)
        {
            ".text", ".data", ".rdata", ".bss", ".idata", ".edata",
            ".rsrc", ".reloc", ".tls", ".pdata", ".debug",
            "CODE", "DATA", "BSS", ".CRT", ".xdata",
            ".didat", ".gfids", ".giats", ".gehcont", ".00cfg", ".voltbl",
            ".mrdata", ".orpc", "INIT", "PAGE", ".shared"
        };

        public static List<PEAnomaly> Analyze(PESnapshot snapshot)
        {
            var anomalies = new List<PEAnomaly>();

            if (snapshot.Sections != null)
            {
                foreach (var section in snapshot.Sections)
                {
                    // 1. W+X section
                    if (section.IsWritable && section.IsExecutable)
                    {
                        anomalies.Add(new PEAnomaly
                        {
                            Title = $"Writable + Executable section: {section.Name}",
                            Description = "Section has both write and execute permissions, which is a common indicator of packed or self-modifying code.",
                            Severity = AnomalySeverity.High
                        });
                    }

                    // 2. Non-standard section name
                    if (!string.IsNullOrEmpty(section.Name) && !StandardSectionNames.Contains(section.Name))
                    {
                        anomalies.Add(new PEAnomaly
                        {
                            Title = $"Non-standard section name: {section.Name}",
                            Description = "Section name is not in the standard set, which may indicate packing or custom toolchain.",
                            Severity = AnomalySeverity.Low
                        });
                    }

                    // 4. Zero-size section
                    if (section.RawDataSize == 0 && !string.Equals(section.Name, ".bss", StringComparison.OrdinalIgnoreCase))
                    {
                        anomalies.Add(new PEAnomaly
                        {
                            Title = $"Zero-size section: {section.Name}",
                            Description = "Section has no raw data on disk.",
                            Severity = AnomalySeverity.Low
                        });
                    }

                    // 5. Section size mismatch (ignore small sections under 4KB — alignment artifacts)
                    if (section.VirtualSize > 0 && section.RawDataSize > 0
                        && (section.VirtualSize > 0x1000 || section.RawDataSize > 0x1000))
                    {
                        if (section.VirtualSize > section.RawDataSize * 10 || section.RawDataSize > section.VirtualSize * 10)
                        {
                            anomalies.Add(new PEAnomaly
                            {
                                Title = $"Section size mismatch: {section.Name}",
                                Description = $"Large discrepancy between virtual size (0x{section.VirtualSize:X}) and raw size (0x{section.RawDataSize:X}).",
                                Severity = AnomalySeverity.Medium
                            });
                        }
                    }

                    // 12. High entropy section
                    if (section.Entropy > 7.0)
                    {
                        anomalies.Add(new PEAnomaly
                        {
                            Title = $"High entropy section: {section.Name}",
                            Description = $"Entropy {section.Entropy:F2} bits/byte may indicate packing or encryption.",
                            Severity = AnomalySeverity.Medium
                        });
                    }
                }

                // 3. Entry point outside .text
                var textSection = snapshot.Sections.FirstOrDefault(s =>
                    string.Equals(s.Name, ".text", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(s.Name, "CODE", StringComparison.OrdinalIgnoreCase));

                if (textSection != null)
                {
                    uint ep = (uint)snapshot.EntryPoint;
                    uint textStart = textSection.VirtualAddress;
                    uint textEnd = textStart + textSection.VirtualSize;

                    if (ep < textStart || ep >= textEnd)
                    {
                        anomalies.Add(new PEAnomaly
                        {
                            Title = "Entry point outside .text",
                            Description = $"Entry point RVA 0x{ep:X} is not within the code section.",
                            Severity = AnomalySeverity.Medium
                        });
                    }
                }
            }

            // 6. No imports
            if (snapshot.Imports == null || snapshot.Imports.Count == 0)
            {
                anomalies.Add(new PEAnomaly
                {
                    Title = "No imports",
                    Description = "The file imports no DLLs, which is unusual and may indicate packing.",
                    Severity = AnomalySeverity.High
                });
            }
            // 7. Very few imports
            else if (snapshot.Imports.Count <= 2)
            {
                anomalies.Add(new PEAnomaly
                {
                    Title = "Very few imports",
                    Description = $"Only {snapshot.Imports.Count} imported module(s), which may indicate packing.",
                    Severity = AnomalySeverity.Low
                });
            }

            // 8-10. DllCharacteristics checks
            if (snapshot.Header?.DllCharacteristics != null)
            {
                ushort dc = snapshot.Header.DllCharacteristics.Value;

                // 8. No ASLR
                if ((dc & 0x0040) == 0)
                {
                    anomalies.Add(new PEAnomaly
                    {
                        Title = "No ASLR enabled",
                        Description = "IMAGE_DLLCHARACTERISTICS_DYNAMIC_BASE is not set.",
                        Severity = AnomalySeverity.Low
                    });
                }

                // 9. No DEP/NX
                if ((dc & 0x0100) == 0)
                {
                    anomalies.Add(new PEAnomaly
                    {
                        Title = "No DEP/NX enabled",
                        Description = "IMAGE_DLLCHARACTERISTICS_NX_COMPAT is not set.",
                        Severity = AnomalySeverity.Low
                    });
                }

                // 10. No CFG
                if ((dc & 0x4000) == 0)
                {
                    anomalies.Add(new PEAnomaly
                    {
                        Title = "No CFG enabled",
                        Description = "IMAGE_DLLCHARACTERISTICS_GUARD_CF is not set.",
                        Severity = AnomalySeverity.Info
                    });
                }
            }

            // 11. Timestamp anomaly
            if (snapshot.Header != null)
            {
                uint ts = unchecked((uint)snapshot.Header.TimeDateStamp);
                if (ts == 0)
                {
                    anomalies.Add(new PEAnomaly
                    {
                        Title = "Zero timestamp",
                        Description = "TimeDateStamp is 0, which may indicate stripped or forged metadata.",
                        Severity = AnomalySeverity.Low
                    });
                }
                else
                {
                    var epoch = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);
                    var fileDate = epoch.AddSeconds(ts);
                    var minDate = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);
                    // Reproducible builds (MSVC /Brepro) use hash-based timestamps
                    // that often appear as far-future dates — only flag truly ancient ones
                    bool isReproBuild = ts > (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                    if (!isReproBuild && fileDate < minDate)
                    {
                        anomalies.Add(new PEAnomaly
                        {
                            Title = "Timestamp anomaly",
                            Description = $"TimeDateStamp ({fileDate:yyyy-MM-dd}) is before year 2000.",
                            Severity = AnomalySeverity.Low
                        });
                    }
                }
            }

            // 13. High overall entropy
            if (snapshot.OverallEntropy > 7.2)
            {
                anomalies.Add(new PEAnomaly
                {
                    Title = "High overall entropy",
                    Description = $"Overall entropy {snapshot.OverallEntropy:F2} bits/byte suggests the file may be packed or encrypted.",
                    Severity = AnomalySeverity.High
                });
            }

            // Downgrade anomaly severity for known installers
            if (!string.IsNullOrEmpty(snapshot.DetectedInstaller))
                DowngradeForInstaller(anomalies, snapshot.DetectedInstaller);

            // Downgrade anomaly severity for DLL files
            bool isDll = snapshot.Header != null && (snapshot.Header.Characteristics & 0x2000) != 0;
            if (isDll)
                DowngradeForDll(anomalies);

            // Sort: High first, then Medium, Low, Info
            anomalies.Sort((a, b) => b.Severity.CompareTo(a.Severity));

            return anomalies;
        }

        private static void DowngradeForDll(List<PEAnomaly> anomalies)
        {
            foreach (var anomaly in anomalies)
            {
                bool shouldDowngrade = anomaly.Title == "Entry point outside .text"
                    || anomaly.Title == "No imports"
                    || anomaly.Title == "Very few imports"
                    || anomaly.Title == "Zero timestamp"
                    || anomaly.Title.StartsWith("Non-standard section name:");

                if (shouldDowngrade)
                {
                    anomaly.Severity = AnomalySeverity.Info;
                    anomaly.Description += " (expected for DLL)";
                }
            }
        }

        private static void DowngradeForInstaller(List<PEAnomaly> anomalies, string installerName)
        {
            foreach (var anomaly in anomalies)
            {
                bool shouldDowngrade = anomaly.Title == "No imports"
                    || anomaly.Title == "High overall entropy"
                    || anomaly.Title == "Entry point outside .text"
                    || anomaly.Title.StartsWith("High entropy section:")
                    || anomaly.Title == "Very few imports";

                if (shouldDowngrade)
                {
                    anomaly.Severity = AnomalySeverity.Info;
                    anomaly.Description += $" (expected for {installerName})";
                }
            }
        }
    }
}

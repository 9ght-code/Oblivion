using Oblivion.Data.Snapshots;
using Oblivion.GUI.MVVM.Model;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Linq;

namespace Oblivion.GUI.Services
{
    public class PdfExportService
    {
        // Dark security theme palette
        private static readonly string BgColor = "#0D1117";
        private static readonly string CardColor = "#161B22";
        private static readonly string BorderColor = "#30363D";
        private static readonly string TextPrimary = "#E6EDF3";
        private static readonly string TextSecondary = "#8B949E";
        private static readonly string Accent = "#58A6FF";
        private static readonly string Green = "#3FB950";
        private static readonly string Red = "#F85149";
        private static readonly string Orange = "#D29922";
        private static readonly string Teal = "#39D0D8";

        public PdfExportService()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public void Export(string outputPath, FileUIModel file, PESnapshot? snapshot)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.PageColor(Color.FromHex(BgColor));
                    page.DefaultTextStyle(x => x.FontColor(Color.FromHex(TextPrimary)).FontSize(9).FontFamily("Consolas"));

                    page.Content().Column(col =>
                    {
                        // Header
                        col.Item().Background(Color.FromHex(CardColor))
                            .Border(1).BorderColor(Color.FromHex(Accent))
                            .Padding(16).Column(header =>
                            {
                                header.Item().Text("OBLIVION PE ANALYSIS REPORT")
                                    .FontSize(18).FontColor(Color.FromHex(Accent)).Bold();
                                header.Item().PaddingTop(4).Text(file.Name)
                                    .FontSize(12).FontColor(Color.FromHex(TextPrimary));
                                header.Item().PaddingTop(2).Text($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}")
                                    .FontSize(8).FontColor(Color.FromHex(TextSecondary));
                            });

                        col.Item().Height(12);

                        // File Metadata section
                        col.Item().Background(Color.FromHex(CardColor))
                            .Border(1).BorderColor(Color.FromHex(BorderColor))
                            .Padding(12).Column(meta =>
                            {
                                meta.Item().Text("FILE METADATA")
                                    .FontSize(10).FontColor(Color.FromHex(Accent)).Bold();
                                meta.Item().Height(8);

                                meta.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(cols =>
                                    {
                                        cols.RelativeColumn(1);
                                        cols.RelativeColumn(2);
                                    });

                                    void Row(string key, string value)
                                    {
                                        table.Cell().Background(Color.FromHex(BgColor)).Padding(4)
                                            .Text(key).FontColor(Color.FromHex(TextSecondary)).FontSize(8);
                                        table.Cell().Background(Color.FromHex(BgColor)).Padding(4)
                                            .Text(value ?? "-").FontColor(Color.FromHex(TextPrimary)).FontSize(8);
                                    }

                                    Row("Architecture", file.Architecture ?? "-");
                                    Row("File Size", $"{file.FileSize:N0} bytes");
                                    Row("Entry Point", file.EntryPoint ?? "-");
                                    Row("SHA-256", file.Hash ?? "-");
                                    Row("Health Status", file.HealthStatus ?? "-");
                                    Row("Anomaly Count", file.AnomalyCount.ToString());
                                    Row("Overall Entropy", $"{file.OverallEntropy:F4}");
                                    Row("Detected Packer", string.IsNullOrEmpty(file.DetectedPacker) ? "None" : file.DetectedPacker);
                                    Row("Detected Installer", string.IsNullOrEmpty(file.DetectedInstaller) ? "None" : file.DetectedInstaller);
                                    Row("Overlay", file.HasOverlay
                                        ? $"Offset: 0x{file.OverlayOffset:X}, Size: {file.OverlaySize:N0} bytes"
                                        : "None");
                                });
                            });

                        col.Item().Height(12);

                        // PE Sections
                        if (snapshot?.Sections?.Count > 0)
                        {
                            col.Item().Background(Color.FromHex(CardColor))
                                .Border(1).BorderColor(Color.FromHex(BorderColor))
                                .Padding(12).Column(sec =>
                                {
                                    sec.Item().Text("PE SECTIONS")
                                        .FontSize(10).FontColor(Color.FromHex(Accent)).Bold();
                                    sec.Item().Height(8);

                                    sec.Item().Table(table =>
                                    {
                                        table.ColumnsDefinition(cols =>
                                        {
                                            cols.RelativeColumn(2); // Name
                                            cols.RelativeColumn(2); // VA
                                            cols.RelativeColumn(2); // VSize
                                            cols.RelativeColumn(2); // Raw Ptr
                                            cols.RelativeColumn(2); // Raw Size
                                            cols.RelativeColumn(1); // R
                                            cols.RelativeColumn(1); // W
                                            cols.RelativeColumn(1); // X
                                            cols.RelativeColumn(2); // Entropy
                                        });

                                        void HeaderCell(string text)
                                        {
                                            table.Cell().Background(Color.FromHex(BgColor))
                                                .BorderBottom(1).BorderColor(Color.FromHex(BorderColor))
                                                .Padding(4).Text(text)
                                                .FontColor(Color.FromHex(TextSecondary)).FontSize(7).Bold();
                                        }

                                        HeaderCell("Name");
                                        HeaderCell("VA");
                                        HeaderCell("VSize");
                                        HeaderCell("Raw Ptr");
                                        HeaderCell("Raw Size");
                                        HeaderCell("R");
                                        HeaderCell("W");
                                        HeaderCell("X");
                                        HeaderCell("Entropy");

                                        foreach (var s in snapshot.Sections)
                                        {
                                            table.Cell().Padding(3).Text(s.Name ?? "-")
                                                .FontColor(Color.FromHex(TextPrimary)).FontSize(7);
                                            table.Cell().Padding(3).Text($"0x{s.VirtualAddress:X}")
                                                .FontColor(Color.FromHex(TextPrimary)).FontSize(7);
                                            table.Cell().Padding(3).Text($"0x{s.VirtualSize:X}")
                                                .FontColor(Color.FromHex(TextPrimary)).FontSize(7);
                                            table.Cell().Padding(3).Text($"0x{s.RawDataPointer:X}")
                                                .FontColor(Color.FromHex(TextPrimary)).FontSize(7);
                                            table.Cell().Padding(3).Text($"0x{s.RawDataSize:X}")
                                                .FontColor(Color.FromHex(TextPrimary)).FontSize(7);

                                            table.Cell().Padding(3).Text(s.IsReadable ? "R" : "-")
                                                .FontColor(s.IsReadable ? Color.FromHex(Green) : Color.FromHex(Red)).FontSize(7).Bold();
                                            table.Cell().Padding(3).Text(s.IsWritable ? "W" : "-")
                                                .FontColor(s.IsWritable ? Color.FromHex(Green) : Color.FromHex(Red)).FontSize(7).Bold();
                                            table.Cell().Padding(3).Text(s.IsExecutable ? "X" : "-")
                                                .FontColor(s.IsExecutable ? Color.FromHex(Orange) : Color.FromHex(Red)).FontSize(7).Bold();
                                            table.Cell().Padding(3).Text($"{s.Entropy:F4}")
                                                .FontColor(Color.FromHex(TextPrimary)).FontSize(7);
                                        }
                                    });
                                });

                            col.Item().Height(12);
                        }

                        // Imports
                        if (snapshot?.Imports?.Count > 0)
                        {
                            col.Item().Background(Color.FromHex(CardColor))
                                .Border(1).BorderColor(Color.FromHex(BorderColor))
                                .Padding(12).Column(imp =>
                                {
                                    imp.Item().Text("IMPORTED LIBRARIES")
                                        .FontSize(10).FontColor(Color.FromHex(Accent)).Bold();
                                    imp.Item().Height(8);

                                    foreach (var lib in snapshot.Imports)
                                    {
                                        imp.Item().PaddingBottom(8).Column(libCol =>
                                        {
                                            libCol.Item().Text(lib.ModuleName ?? "Unknown")
                                                .FontColor(Color.FromHex(Teal)).FontSize(8).Bold();

                                            if (lib.Functions?.Count > 0)
                                            {
                                                foreach (var fn in lib.Functions)
                                                {
                                                    libCol.Item().PaddingLeft(12).Text($"  {fn}")
                                                        .FontColor(Color.FromHex(TextSecondary)).FontSize(7);
                                                }
                                            }
                                        });
                                    }
                                });

                            col.Item().Height(12);
                        }

                        // Anomalies
                        if (file.Anomalies?.Count > 0)
                        {
                            col.Item().Background(Color.FromHex(CardColor))
                                .Border(1).BorderColor(Color.FromHex(BorderColor))
                                .Padding(12).Column(anom =>
                                {
                                    anom.Item().Text("ANOMALIES")
                                        .FontSize(10).FontColor(Color.FromHex(Accent)).Bold();
                                    anom.Item().Height(8);

                                    anom.Item().Table(table =>
                                    {
                                        table.ColumnsDefinition(cols =>
                                        {
                                            cols.ConstantColumn(60); // Severity
                                            cols.RelativeColumn(2);  // Title
                                            cols.RelativeColumn(3);  // Description
                                        });

                                        void HeaderCell(string text)
                                        {
                                            table.Cell().Background(Color.FromHex(BgColor))
                                                .BorderBottom(1).BorderColor(Color.FromHex(BorderColor))
                                                .Padding(4).Text(text)
                                                .FontColor(Color.FromHex(TextSecondary)).FontSize(7).Bold();
                                        }

                                        HeaderCell("Severity");
                                        HeaderCell("Title");
                                        HeaderCell("Description");

                                        foreach (var a in file.Anomalies)
                                        {
                                            var severityColor = a.Severity switch
                                            {
                                                MVVM.Model.AnomalySeverity.Info => Teal,
                                                MVVM.Model.AnomalySeverity.Low => TextSecondary,
                                                MVVM.Model.AnomalySeverity.Medium => Orange,
                                                MVVM.Model.AnomalySeverity.High => Red,
                                                _ => TextSecondary
                                            };

                                            table.Cell().Padding(3).Text(a.Severity.ToString())
                                                .FontColor(Color.FromHex(severityColor)).FontSize(7).Bold();
                                            table.Cell().Padding(3).Text(a.Title ?? "-")
                                                .FontColor(Color.FromHex(TextPrimary)).FontSize(7);
                                            table.Cell().Padding(3).Text(a.Description ?? "-")
                                                .FontColor(Color.FromHex(TextSecondary)).FontSize(7);
                                        }
                                    });
                                });
                        }
                    });

                    page.Footer().AlignCenter().Text(t =>
                    {
                        t.Span("Oblivion PE Analyzer  •  Page ")
                            .FontColor(Color.FromHex(TextSecondary)).FontSize(7);
                        t.CurrentPageNumber().FontColor(Color.FromHex(TextSecondary)).FontSize(7);
                        t.Span(" / ").FontColor(Color.FromHex(TextSecondary)).FontSize(7);
                        t.TotalPages().FontColor(Color.FromHex(TextSecondary)).FontSize(7);
                    });
                });
            });

            document.GeneratePdf(outputPath);
        }
    }
}

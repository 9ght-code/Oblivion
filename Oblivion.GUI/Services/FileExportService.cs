using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Oblivion.Data.Snapshots;
using Oblivion.GUI.MVVM.Model;

namespace Oblivion.GUI.Services;

public class FileExportService(PdfExportService pdfExportService, NotificationService notify)
{
    public void ExportJson(string filePath, FileUIModel file, PESnapshot? snapshot)
    {
        var report = new
        {
            FileName = file.Name,
            FilePath = file.Path,
            Architecture = file.Architecture,
            FileSize = file.FileSize,
            EntryPoint = file.EntryPoint,
            SHA256 = file.Hash,
            HealthStatus = file.HealthStatus,
            AnomalyCount = file.AnomalyCount,
            OverallEntropy = file.OverallEntropy,
            DetectedPacker = file.DetectedPacker,
            DetectedInstaller = file.DetectedInstaller,
            HasOverlay = file.HasOverlay,
            OverlayOffset = file.HasOverlay ? (long?)file.OverlayOffset : null,
            OverlaySize = file.HasOverlay ? (long?)file.OverlaySize : null,
            Anomalies = file.Anomalies?.Select(a => new
            {
                a.Title,
                a.Description,
                Severity = a.Severity.ToString()
            }),
            Sections = snapshot?.Sections?.Select(s => new
            {
                s.Name,
                VirtualAddress = $"0x{s.VirtualAddress:X}",
                VirtualSize = $"0x{s.VirtualSize:X}",
                RawDataPointer = $"0x{s.RawDataPointer:X}",
                RawDataSize = $"0x{s.RawDataSize:X}",
                s.IsReadable,
                s.IsWritable,
                s.IsExecutable,
                Entropy = Math.Round(s.Entropy, 4)
            }),
            Imports = snapshot?.Imports?.Select(i => new
            {
                i.ModuleName,
                i.Functions
            }),
            Header = snapshot?.Header != null ? new
            {
                Machine = $"0x{snapshot.Header.Machine:X}",
                Characteristics = $"0x{snapshot.Header.Characteristics:X}",
                DllCharacteristics = $"0x{(snapshot.Header.DllCharacteristics ?? 0):X}",
                Subsystem = snapshot.Header.Subsystem,
                TimeDateStamp = snapshot.Header.TimeDateStamp
            } : null
        };

        var json = JsonSerializer.Serialize(report, new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        File.WriteAllText(filePath, json);
        notify.Success("Report Exported", Path.GetFileName(filePath));
    }

    public void ExportPdf(string filePath, FileUIModel file, PESnapshot? snapshot)
    {
        try
        {
            pdfExportService.Export(filePath, file, snapshot);
            notify.Success("PDF Exported", Path.GetFileName(filePath));
        }
        catch (Exception ex)
        {
            notify.Error("Export Failed", ex.Message);
        }
    }
}

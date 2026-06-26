using CommunityToolkit.Mvvm.ComponentModel;
using Oblivion.Data.Entities;
using Oblivion.Data.Services;
using Oblivion.Data.Snapshots;
using Oblivion.GUI.Services;
using System.Collections.ObjectModel;

namespace Oblivion.GUI.MVVM.Model
{
    public partial class FileUIModel : ObservableObject
    {
        public Guid Id { get; set; }

        [ObservableProperty]
        private string _name;

        public long FileSize { get; set; }
        public string? EntryPoint { get; set; }
        public string Path { get; set; }
        public string Hash { get; set; }
        public string Architecture { get; set; } = "Unknown";

        [ObservableProperty]
        private bool _isHashChanged = false;

        [ObservableProperty]
        private bool _isDeleted = false;

        [ObservableProperty]
        private bool _isVisible = true;

        public ObservableCollection<PESectionSnapshot>? Sections { get; set; }
        public ObservableCollection<ImportSnapshot>? Imports { get; set; }

        public double OverallEntropy { get; set; }
        public ObservableCollection<PEAnomaly>? Anomalies { get; set; }
        public string HealthStatus { get; set; } = "Unknown";
        public int AnomalyCount { get; set; }
        public string? DetectedPacker { get; set; }
        public string? DetectedInstaller { get; set; }
        public long OverlayOffset { get; set; }
        public long OverlaySize { get; set; }
        public bool HasOverlay => OverlaySize > 0;

        public FileUIModel(AnalyzedFile model)
        {
            Id = model.ID;
            Name = model.Name;
            FileSize = model.FileSize;
            Path = model.FilePath;
            Hash = model.Sha256;

            var snapshot = SnapshotSerializer.Deserialize(model.SnapshotJson);

            if (snapshot != null)
            {
                EntryPoint = "0x" + snapshot.EntryPoint.ToString("X");
                Architecture = snapshot.Architecture == "14C" ? "X86" : "X64";
                Sections = snapshot.Sections != null
                    ? new ObservableCollection<PESectionSnapshot>(snapshot.Sections)
                    : new ObservableCollection<PESectionSnapshot>();
                Imports = snapshot.Imports != null
                    ? new ObservableCollection<ImportSnapshot>(snapshot.Imports)
                    : new ObservableCollection<ImportSnapshot>();

                OverallEntropy = snapshot.OverallEntropy;
                DetectedPacker = snapshot.DetectedPacker;
                DetectedInstaller = snapshot.DetectedInstaller;
                OverlayOffset = snapshot.OverlayOffset;
                OverlaySize = snapshot.OverlaySize;

                var anomalyList = PEAnomalyAnalyzer.Analyze(snapshot);
                Anomalies = new ObservableCollection<PEAnomaly>(anomalyList);
                AnomalyCount = anomalyList.Count;

                HealthStatus = ComputeHealthStatus(anomalyList);
            }
        }

        private static string ComputeHealthStatus(System.Collections.Generic.List<PEAnomaly> anomalies)
        {
            if (anomalies.Count == 0)
                return "Clean";

            int highCount = anomalies.Count(a => a.Severity == AnomalySeverity.High);
            int mediumCount = anomalies.Count(a => a.Severity == AnomalySeverity.Medium);
            int lowCount = anomalies.Count(a => a.Severity == AnomalySeverity.Low);

            if (highCount >= 2 || (highCount >= 1 && mediumCount >= 2))
                return "High Risk";

            if (highCount >= 1 || mediumCount >= 3)
                return "Moderate";

            if (mediumCount >= 1 || lowCount >= 3)
                return "Low Risk";

            return "Clean";
        }
    }
}

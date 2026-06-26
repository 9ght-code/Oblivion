using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Oblivion.Data.Snapshots
{
    public class PESnapshot
    {
        public string Version { get; set; } = "1.0";
        public string Architecture { get; set; } // x86 / x64
        public long ImageBase {  get; set; }
        public int EntryPoint { get; set; }
        public PEHeaderSnapshot Header { get; set; }
        public List<PESectionSnapshot> Sections { get; set; } = new();
        public List<ImportSnapshot> Imports { get; set; } = new();
        public double OverallEntropy { get; set; }
        public string? DetectedPacker { get; set; }
        public string? DetectedInstaller { get; set; }
        public long OverlayOffset { get; set; }
        public long OverlaySize { get; set; }
    }
}

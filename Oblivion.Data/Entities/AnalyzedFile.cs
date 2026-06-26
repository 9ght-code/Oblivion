using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Oblivion.Data.Entities
{
    public class AnalyzedFile
    {
        public Guid ID { get; set; } = Guid.NewGuid();
        public required string Name { get; set; }
        public required string FilePath { get; set; }
        public required string Sha256 { get; set; }
        public required long FileSize { get; set; }
        public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
        public DateTime LoadedAt { get; set;} = DateTime.UtcNow;
        public string? SnapshotJson { get; set; } //PESnapshot
        public ICollection<FileModificationNotes> Notes { get; set; } = new List<FileModificationNotes>();
        public bool IsAnalyzed { get; set; } = false;
        public List<Workspace> Workspaces { get; set; } = new();
    }
}

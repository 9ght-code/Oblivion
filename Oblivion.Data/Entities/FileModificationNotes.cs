using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Oblivion.Data.Entities
{
    public class FileModificationNotes
    {
        public Guid ID { get; set; } = Guid.NewGuid();
        public required Guid AnalyzedFileID { get; set; }
        public AnalyzedFile AnalyzedFile { get; set; }
        public required string Note { get; set; } = "-";
        public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
    }
}

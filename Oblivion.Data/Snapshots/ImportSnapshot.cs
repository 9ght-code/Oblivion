using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Oblivion.Data.Snapshots
{
    public class ImportSnapshot
    {
        public string ModuleName { get; set; }
        public List<string> Functions { get; set; } = new ();
    }
}

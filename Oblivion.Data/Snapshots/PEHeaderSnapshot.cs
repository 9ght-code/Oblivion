using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Oblivion.Data.Snapshots
{
    public class PEHeaderSnapshot
    {
        public ushort Machine { get; set; }
        public int Characteristics { get; set; }
        public int TimeDateStamp { get; set; }
        public ushort Subsystem { get; set; }
        public ushort? DllCharacteristics { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Oblivion.Data.Snapshots
{
    public class PESectionSnapshot
    {
        public string Name { get; set; }
        public uint VirtualAddress { get; set; }
        public uint VirtualSize { get; set; }
        public uint RawDataPointer { get; set; }
        public uint RawDataSize { get; set; }
        public uint Characteristics { get; set; }
        public bool IsWritable { get; set; }
        public bool IsReadable { get; set; }
        public bool IsExecutable { get; set; }
        public double Entropy { get; set; }
    }
}

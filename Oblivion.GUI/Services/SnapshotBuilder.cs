using Oblivion.Data.Snapshots;
using Oblivion.Interpop;
using System.Collections.Generic;
using System.Linq;
using static Oblivion.Interpop.OblivionStructs;

namespace Oblivion.GUI.Services
{
    public static class SnapshotBuilder
    {
        public static PESnapshot BuildSnapshot(AnalysisResult r)
        {
            var native = r.Native;

            var sections = native.Sections
                .Take(native.SectionCount)
                .Select(MapSection)
                .ToList();

            var imports = r.Imports
                .Select(MapImport)
                .ToList();

            return new PESnapshot
            {
                Architecture     = native.Architecture,
                ImageBase        = (long)native.ImageBase,
                EntryPoint       = (int)native.EntryPoint,
                OverallEntropy   = native.OverallEntropy,
                OverlayOffset    = native.OverlayOffset,
                OverlaySize      = native.OverlaySize,

                Header = new PEHeaderSnapshot
                {
                    Machine             = native.Machine,
                    Characteristics     = native.Characteristics,
                    TimeDateStamp       = (int)native.Timestamp,
                    Subsystem           = native.Subsystem,
                    DllCharacteristics  = native.DllCharacteristics,
                },

                Sections = sections,
                Imports  = imports,
            };
        }

        private static PESectionSnapshot MapSection(OBLIVION_SECTION s) => new()
        {
            Name            = s.Name,
            VirtualAddress  = s.VirtualAddress,
            VirtualSize     = s.VirtualSize,
            RawDataPointer  = s.RawAddress,
            RawDataSize     = s.RawSize,
            Characteristics = s.Characteristics,
            Entropy         = s.Entropy,
            IsWritable      = (s.Characteristics & 0x80000000) != 0,
            IsReadable      = (s.Characteristics & 0x40000000) != 0,
            IsExecutable    = (s.Characteristics & 0x20000000) != 0,
        };

        private static ImportSnapshot MapImport(OBLIVION_IMPORT i) => new()
        {
            ModuleName = i.DllName,
            Functions  = i.GetFunctions().ToList(),
        };
    }
}

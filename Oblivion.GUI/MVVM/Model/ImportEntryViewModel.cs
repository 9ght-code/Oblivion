using System.Collections.Generic;

namespace Oblivion.GUI.MVVM.Model
{
    public class ImportEntryViewModel
    {
        public string ModuleName { get; set; } = "";
        public List<ImportFunctionEntry> Functions { get; set; } = new();
    }
}

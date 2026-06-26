using Wpf.Ui.Controls;

namespace Oblivion.GUI.MVVM.Model
{
    public class SecurityFlag
    {
        public required string Title { get; init; }
        public required string Description { get; init; }
        public required SymbolRegular Icon { get; init; }
        public required bool IsEnabled { get; init; }
        public bool IsWarning { get; init; }
    }
}

using System.Windows;
using System.Windows.Controls;
using Oblivion.Data.Snapshots;
using Oblivion.GUI.MVVM.ViewModel;
using Oblivion.GUI.UI.Dialogs;

namespace Oblivion.GUI.UI.AnalysisWidgets
{
    public partial class SectionsTab : UserControl
    {
        public SectionsTab()
        {
            InitializeComponent();
        }

        private AnalysisViewModel? GetVm() => DataContext as AnalysisViewModel;

        private void OnAddSectionClick(object sender, RoutedEventArgs e)
        {
            var vm = GetVm();
            if (vm?.FileBytes == null) return;

            var dialog = new AddSectionDialog { Owner = Window.GetWindow(this) };
            if (dialog.ShowDialog() == true)
                vm.AddNewSectionCommand.Execute(dialog.GetResult());
        }

        private void OnEditSectionClick(object sender, RoutedEventArgs e)
        {
            var vm = GetVm();
            if (vm?.FileBytes == null || SectionsGrid.SelectedItem is not PESectionSnapshot section) return;

            int index = vm.Sections.IndexOf(section);
            if (index < 0) return;

            var dialog = new EditSectionDialog(section) { Owner = Window.GetWindow(this) };
            if (dialog.ShowDialog() == true)
                vm.EditSectionCharacteristicsCommand.Execute((index, dialog.GetCharacteristics()));
        }

        private void OnViewInHexClick(object sender, RoutedEventArgs e)
        {
            if (SectionsGrid.SelectedItem is PESectionSnapshot section)
                GetVm()?.ViewSectionInHexCommand.Execute(section);
        }

        private void OnViewInDisasmClick(object sender, RoutedEventArgs e)
        {
            if (SectionsGrid.SelectedItem is PESectionSnapshot section)
                GetVm()?.ViewSectionInDisasmCommand.Execute(section);
        }
    }
}

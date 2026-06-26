using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Oblivion.GUI.MVVM.ViewModel;

namespace Oblivion.GUI.UI.AnalysisWidgets
{
    public partial class ShellcodeTab : UserControl
    {
        public ShellcodeTab()
        {
            InitializeComponent();
        }

        private AnalysisViewModel? GetVm() => DataContext as AnalysisViewModel;

        // File dialog requires code-behind — no way around it in WPF
        private void OnLoadFromFileClick(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "All files (*.*)|*.*|Binary files (*.bin)|*.bin"
            };
            if (dialog.ShowDialog() == true)
                GetVm()?.LoadShellcodeFromFileCommand.Execute(dialog.FileName);
        }

        private void OnInjectionModeChanged(object sender, RoutedEventArgs e)
        {
            var vm = GetVm();
            if (vm == null) return;

            if (RbNewSection.IsChecked == true)
            {
                vm.ShellcodeInjectionMode = "NewSection";
                if (SectionNamePanel != null) SectionNamePanel.Visibility = Visibility.Visible;
            }
            else if (RbCodeCave.IsChecked == true)
            {
                vm.ShellcodeInjectionMode = "CodeCave";
                if (SectionNamePanel != null) SectionNamePanel.Visibility = Visibility.Collapsed;
            }
            else if (RbAppendLast.IsChecked == true)
            {
                vm.ShellcodeInjectionMode = "AppendLast";
                if (SectionNamePanel != null) SectionNamePanel.Visibility = Visibility.Collapsed;
            }
        }
    }
}

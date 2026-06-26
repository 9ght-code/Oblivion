using System.Windows;
using System.Windows.Controls;
using Oblivion.GUI.MVVM.Model;
using Oblivion.GUI.MVVM.ViewModel;

namespace Oblivion.GUI.UI.AnalysisWidgets
{
    public partial class DisassemblerTab : UserControl
    {
        private DisassembledInstruction? _patchTarget;

        public DisassemblerTab()
        {
            InitializeComponent();
        }

        private AnalysisViewModel? GetVm() => DataContext as AnalysisViewModel;

        private void OnNopSelectedClick(object sender, RoutedEventArgs e)
        {
            if (DisasmListView.SelectedItem is DisassembledInstruction instr)
                GetVm()?.NopInstructionCommand.Execute(instr);
        }

        private void OnPatchBytesClick(object sender, RoutedEventArgs e)
        {
            if (DisasmListView.SelectedItem is DisassembledInstruction instr)
                ShowPatchPanel(instr);
        }

        private void OnNopContextClick(object sender, RoutedEventArgs e)
        {
            if (DisasmListView.SelectedItem is DisassembledInstruction instr)
                GetVm()?.NopInstructionCommand.Execute(instr);
        }

        private void OnPatchContextClick(object sender, RoutedEventArgs e)
        {
            if (DisasmListView.SelectedItem is DisassembledInstruction instr)
                ShowPatchPanel(instr);
        }

        private void OnViewInHexContextClick(object sender, RoutedEventArgs e)
        {
            if (DisasmListView.SelectedItem is DisassembledInstruction instr)
                GetVm()?.ViewInHexCommand.Execute(instr);
        }

        private void OnCopyAddressClick(object sender, RoutedEventArgs e)
        {
            if (DisasmListView.SelectedItem is DisassembledInstruction instr)
                Clipboard.SetText(instr.Address);
        }

        // Patch panel is minimal UI state — not business logic
        private void ShowPatchPanel(DisassembledInstruction instr)
        {
            _patchTarget = instr;
            PatchTargetLabel.Text = $"Patch at {instr.Address} ({instr.Length}b): {instr.Bytes}";
            PatchBytesBox.Text = instr.Bytes;
            PatchPanel.Visibility = Visibility.Visible;
        }

        private void OnApplyPatchClick(object sender, RoutedEventArgs e)
        {
            if (_patchTarget == null) return;
            var vm = GetVm();
            if (vm == null) return;

            vm.PatchBytesInput = PatchBytesBox.Text;
            vm.PatchInstructionBytesCommand.Execute(_patchTarget);
            PatchPanel.Visibility = Visibility.Collapsed;
            _patchTarget = null;
        }

        private void OnCancelPatchClick(object sender, RoutedEventArgs e)
        {
            PatchPanel.Visibility = Visibility.Collapsed;
            _patchTarget = null;
        }
    }
}

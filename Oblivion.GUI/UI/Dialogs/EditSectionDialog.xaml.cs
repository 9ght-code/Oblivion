using System.Windows;
using Oblivion.Data.Snapshots;

namespace Oblivion.GUI.UI.Dialogs
{
    public partial class EditSectionDialog : Window
    {
        public EditSectionDialog(PESectionSnapshot section)
        {
            InitializeComponent();

            SectionNameRun.Text = section.Name;
            CurrentCharsRun.Text = $"{section.Characteristics:X8}";

            ChkRead.IsChecked = section.IsReadable;
            ChkWrite.IsChecked = section.IsWritable;
            ChkExecute.IsChecked = section.IsExecutable;
            ChkCode.IsChecked = (section.Characteristics & 0x00000020) != 0;
            ChkInitData.IsChecked = (section.Characteristics & 0x00000040) != 0;
        }

        public uint GetCharacteristics()
        {
            uint chars = 0;
            if (ChkRead.IsChecked == true) chars |= 0x40000000;
            if (ChkWrite.IsChecked == true) chars |= 0x80000000;
            if (ChkExecute.IsChecked == true) chars |= 0x20000000;
            if (ChkCode.IsChecked == true) chars |= 0x00000020;
            if (ChkInitData.IsChecked == true) chars |= 0x00000040;
            return chars;
        }

        private void OnConfirm(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void OnCancel(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}

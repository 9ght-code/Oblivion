using System;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.Win32;
using Oblivion.GUI.Services;

namespace Oblivion.GUI.UI.Dialogs
{
    public partial class AddSectionDialog : Window
    {
        public AddSectionDialog()
        {
            InitializeComponent();
        }

        public (string name, byte[] data, uint characteristics) GetResult()
        {
            string name = SectionNameBox.Text.Trim();
            if (string.IsNullOrEmpty(name)) name = ".obli";

            byte[] data;
            try
            {
                data = DisassemblyService.ParseHexBytes(SectionDataBox.Text);
            }
            catch
            {
                data = Array.Empty<byte>();
            }

            if (data.Length == 0)
                data = new byte[512]; // default empty section

            uint chars = 0;
            if (ChkRead.IsChecked == true) chars |= 0x40000000;
            if (ChkWrite.IsChecked == true) chars |= 0x80000000;
            if (ChkExecute.IsChecked == true) chars |= 0x20000000;
            if (ChkCode.IsChecked == true) chars |= 0x00000020;
            if (ChkInitData.IsChecked == true) chars |= 0x00000040;

            return (name, data, chars);
        }

        private void OnLoadFromFile(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "All files (*.*)|*.*|Binary files (*.bin)|*.bin"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    byte[] data = File.ReadAllBytes(dialog.FileName);
                    SectionDataBox.Text = BitConverter.ToString(data).Replace("-", " ");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to load file: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
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

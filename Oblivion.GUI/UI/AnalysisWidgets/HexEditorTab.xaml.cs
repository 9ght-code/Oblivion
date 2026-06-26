using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Win32;
using Oblivion.GUI.MVVM.ViewModel;
using Oblivion.GUI.Services;
using WpfHexaEditor;
using WpfHexaEditor.Core;

namespace Oblivion.GUI.UI.AnalysisWidgets
{
    public partial class HexEditorTab : UserControl
    {
        private AnalysisViewModel? _vm;
        private MemoryStream? _hexStream;

        public HexEditorTab()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is AnalysisViewModel oldVm)
            {
                oldVm.PropertyChanged -= OnVmPropertyChanged;
                oldVm.HexStreamRefreshRequested -= OnHexStreamRefresh;
            }

            if (e.NewValue is AnalysisViewModel vm)
            {
                _vm = vm;
                vm.PropertyChanged += OnVmPropertyChanged;
                vm.HexStreamRefreshRequested += OnHexStreamRefresh;
                LoadFile();
                HighlightSections();
                BuildSectionLegend();
                UpdateModifiedIndicator();
            }
        }

        private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AnalysisViewModel.HexGoToTarget) && _vm?.HexGoToTarget != null)
            {
                long target = _vm.HexGoToTarget.Value;
                HexEditorControl.SetPosition(target, 1);
                _vm.HexGoToTarget = null;
            }

            if (e.PropertyName == nameof(AnalysisViewModel.IsModified))
            {
                UpdateModifiedIndicator();
            }

            if (e.PropertyName == nameof(AnalysisViewModel.FileBytes))
            {
                ReloadStream();
            }
        }

        private void OnHexStreamRefresh()
        {
            Dispatcher.Invoke(ReloadStream);
        }

        private void LoadFile()
        {
            if (_vm?.FileBytes == null || _vm.FileBytes.Length == 0) return;

            _hexStream = new MemoryStream(_vm.FileBytes);
            HexEditorControl.Stream = _hexStream;
        }

        private void ReloadStream()
        {
            if (_vm?.FileBytes == null || _vm.FileBytes.Length == 0) return;

            _hexStream?.Dispose();
            _hexStream = new MemoryStream(_vm.FileBytes);
            HexEditorControl.Stream = _hexStream;
            HighlightSections();
            BuildSectionLegend();
        }

        private void UpdateModifiedIndicator()
        {
            if (_vm == null) return;

            if (_vm.IsModified)
            {
                ModifiedIndicator.Text = "Yes";
                ModifiedIndicator.Foreground = (Brush)Application.Current.Resources["OblivionOrange"];
            }
            else
            {
                ModifiedIndicator.Text = "No";
                ModifiedIndicator.Foreground = (Brush)Application.Current.Resources["OblivionGreen"];
            }
        }

        /// <summary>
        /// Sync hex editor stream back into VM's FileBytes before saving.
        /// Called before save operations since WpfHexaEditor works on its own stream.
        /// </summary>
        private void SyncHexToVm()
        {
            if (_vm == null || _hexStream == null) return;

            HexEditorControl.SubmitChanges();
            _vm.FileBytes = _hexStream.ToArray();
            _vm.NotifyFileModified();
        }

        private void OnSaveClick(object sender, RoutedEventArgs e)
        {
            SyncHexToVm();
            GetVm()?.SaveFileCommand.Execute(null);
        }

        private AnalysisViewModel? GetVm() => _vm;

        private void OnSaveAsClick(object sender, RoutedEventArgs e)
        {
            SyncHexToVm();
            if (_vm?.FileBytes == null) return;

            var dialog = new SaveFileDialog
            {
                Filter = "All files (*.*)|*.*|Executable files (*.exe)|*.exe|DLL files (*.dll)|*.dll",
                FileName = _vm.FileName
            };

            if (dialog.ShowDialog() == true)
            {
                _vm.SaveFileAsCommand.Execute(dialog.FileName);
            }
        }

        private void HighlightSections()
        {
            if (_vm?.Snapshot?.Sections == null) return;

            HexEditorControl.CustomBackgroundBlockItems.Clear();

            var sectionColors = GetSectionColors();
            int colorIndex = 0;

            foreach (var section in _vm.Snapshot.Sections)
            {
                if (section.RawDataSize == 0) continue;

                var color = sectionColors[colorIndex % sectionColors.Length];
                var semiTransparent = Color.FromArgb(40, color.R, color.G, color.B);

                HexEditorControl.CustomBackgroundBlockItems.Add(
                    new CustomBackgroundBlock(
                        section.RawDataPointer,
                        section.RawDataSize,
                        new SolidColorBrush(semiTransparent)));

                colorIndex++;
            }
        }

        private void BuildSectionLegend()
        {
            SectionLegend.Items.Clear();

            if (_vm?.Snapshot?.Sections == null) return;

            var sectionColors = GetSectionColors();
            int colorIndex = 0;

            foreach (var section in _vm.Snapshot.Sections)
            {
                if (section.RawDataSize == 0) continue;

                var color = sectionColors[colorIndex % sectionColors.Length];
                var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 12, 4) };

                panel.Children.Add(new Rectangle
                {
                    Width = 12,
                    Height = 12,
                    Fill = new SolidColorBrush(color),
                    RadiusX = 2,
                    RadiusY = 2,
                    Margin = new Thickness(0, 0, 4, 0),
                    VerticalAlignment = VerticalAlignment.Center
                });

                panel.Children.Add(new TextBlock
                {
                    Text = $"{section.Name} (0x{section.RawDataPointer:X}-0x{section.RawDataPointer + section.RawDataSize:X})",
                    FontSize = 11,
                    FontFamily = new FontFamily("Cascadia Code, Consolas"),
                    Foreground = (Brush)Application.Current.Resources["OblivionTextSecondary"],
                    VerticalAlignment = VerticalAlignment.Center
                });

                SectionLegend.Items.Add(panel);
                colorIndex++;
            }
        }

        private static Color[] GetSectionColors()
        {
            return
            [
                Color.FromRgb(0x58, 0xA6, 0xFF),
                Color.FromRgb(0x3F, 0xB9, 0x50),
                Color.FromRgb(0xD2, 0x9A, 0x22),
                Color.FromRgb(0xF8, 0x51, 0x49),
                Color.FromRgb(0xBC, 0x8C, 0xFF),
                Color.FromRgb(0x39, 0xD3, 0x53),
                Color.FromRgb(0xFF, 0x7B, 0x72),
                Color.FromRgb(0x79, 0xC0, 0xFF),
            ];
        }

        private void OnFindClick(object sender, RoutedEventArgs e)
        {
            var pattern = ParseHexString(SearchBox.Text);
            if (pattern == null || pattern.Length == 0) return;

            HexEditorControl.FindFirst(pattern);
        }

        private void OnFindNextClick(object sender, RoutedEventArgs e)
        {
            var pattern = ParseHexString(SearchBox.Text);
            if (pattern == null || pattern.Length == 0) return;

            HexEditorControl.FindNext(pattern);
        }

        private static byte[]? ParseHexString(string hex)
        {
            if (string.IsNullOrWhiteSpace(hex)) return null;

            try
            {
                var parts = hex.Split([' ', ',', '-'], StringSplitOptions.RemoveEmptyEntries);
                return parts.Select(p => Convert.ToByte(p, 16)).ToArray();
            }
            catch
            {
                return null;
            }
        }
    }
}

using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Oblivion.GUI.MVVM.ViewModel;

namespace Oblivion.GUI.Widgets
{
    public partial class Entropy : UserControl
    {
        private static readonly Color[] SectionColors =
        [
            Color.FromRgb(0x58, 0xA6, 0xFF), // blue
            Color.FromRgb(0xD2, 0x99, 0x22), // yellow
            Color.FromRgb(0x3F, 0xB9, 0x50), // green
            Color.FromRgb(0xF8, 0x51, 0x49), // red
            Color.FromRgb(0xBC, 0x8C, 0xFF), // purple
            Color.FromRgb(0x39, 0xD3, 0x53), // lime
            Color.FromRgb(0xFF, 0x7B, 0x72), // coral
            Color.FromRgb(0x79, 0xC0, 0xFF), // light blue
        ];

        public Entropy()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is INotifyPropertyChanged oldVm)
                oldVm.PropertyChanged -= OnVmPropertyChanged;

            if (e.NewValue is INotifyPropertyChanged newVm)
                newVm.PropertyChanged += OnVmPropertyChanged;

            Rebuild();
        }

        private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "SelectedFile")
                Rebuild();
        }

        private void Rebuild()
        {
            SectionBar.ColumnDefinitions.Clear();
            SectionBar.Children.Clear();
            SectionLegend.Children.Clear();
            EntropyBars.Children.Clear();

            var vm = DataContext as ShellViewModel;
            var sections = vm?.SelectedFile?.Sections;
            if (sections == null || sections.Count == 0) return;

            long totalSize = sections.Sum(s => (long)s.RawDataSize);
            if (totalSize == 0) return;

            for (int i = 0; i < sections.Count; i++)
            {
                var section = sections[i];
                if (section.RawDataSize == 0) continue;

                double fraction = (double)section.RawDataSize / totalSize;
                var color = SectionColors[i % SectionColors.Length];

                // Bar segment
                SectionBar.ColumnDefinitions.Add(new ColumnDefinition
                {
                    Width = new GridLength(fraction, GridUnitType.Star)
                });

                var bar = new Border { Background = new SolidColorBrush(color) };
                bar.ToolTip = $"{section.Name} — {section.RawDataSize:N0} bytes ({fraction:P0})";
                Grid.SetColumn(bar, SectionBar.ColumnDefinitions.Count - 1);
                SectionBar.Children.Add(bar);

                // Legend item
                var legendPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(0, 0, 20, 0)
                };

                legendPanel.Children.Add(new Rectangle
                {
                    Width = 10, Height = 10,
                    Fill = new SolidColorBrush(color),
                    RadiusX = 3, RadiusY = 3,
                    Margin = new Thickness(0, 0, 6, 0)
                });

                legendPanel.Children.Add(new TextBlock
                {
                    Text = $"{section.Name} ({fraction:P0})",
                    FontSize = 11,
                    Foreground = (Brush)Application.Current.Resources["OblivionTextSecondary"]
                });

                SectionLegend.Children.Add(legendPanel);

                // Entropy bar row
                var entropyColor = section.Entropy switch
                {
                    > 7.0 => (Brush)Application.Current.Resources["OblivionRed"],
                    > 5.0 => (Brush)Application.Current.Resources["OblivionOrange"],
                    _     => (Brush)Application.Current.Resources["OblivionTeal"]
                };

                var rowGrid = new Grid { Margin = new Thickness(0, 3, 0, 0) };
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(72) });
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });

                var nameText = new TextBlock
                {
                    Text = section.Name,
                    FontSize = 11,
                    FontFamily = new FontFamily("Cascadia Code, Consolas"),
                    Foreground = (Brush)Application.Current.Resources["OblivionTextSecondary"],
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(nameText, 0);

                var progressBar = new ProgressBar
                {
                    Minimum = 0,
                    Maximum = 8,
                    Value = section.Entropy,
                    Height = 8,
                    Background = (Brush)Application.Current.Resources["OblivionBorder"],
                    Foreground = entropyColor,
                    BorderThickness = new Thickness(0),
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(4, 0, 4, 0)
                };
                Grid.SetColumn(progressBar, 1);

                var valueText = new TextBlock
                {
                    Text = $"{section.Entropy:F2}",
                    FontSize = 11,
                    FontFamily = new FontFamily("Cascadia Code, Consolas"),
                    Foreground = entropyColor,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextAlignment = TextAlignment.Right
                };
                Grid.SetColumn(valueText, 2);

                rowGrid.Children.Add(nameText);
                rowGrid.Children.Add(progressBar);
                rowGrid.Children.Add(valueText);
                EntropyBars.Children.Add(rowGrid);
            }
        }
    }
}

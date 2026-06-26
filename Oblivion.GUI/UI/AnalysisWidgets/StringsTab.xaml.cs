using Oblivion.GUI.MVVM.ViewModel;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Data;

namespace Oblivion.GUI.UI.AnalysisWidgets
{
    public partial class StringsTab : UserControl
    {
        public StringsTab()
        {
            InitializeComponent();
        }

        private void StringSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (StringsGrid.ItemsSource == null) return;

            var view = CollectionViewSource.GetDefaultView(StringsGrid.ItemsSource);
            if (view == null) return;

            string filter = StringSearchBox.Text?.Trim().ToLowerInvariant() ?? "";

            if (string.IsNullOrEmpty(filter))
            {
                view.Filter = null;
            }
            else
            {
                view.Filter = item =>
                {
                    if (item is ExtractedString s)
                        return s.Value.Contains(filter, System.StringComparison.OrdinalIgnoreCase);
                    return true;
                };
            }
        }
    }
}

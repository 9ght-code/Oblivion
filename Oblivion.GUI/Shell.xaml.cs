using System.IO;
using System.Linq;
using System.Windows;
using Oblivion.GUI.MVVM.ViewModel;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace Oblivion.GUI
{
    public partial class Shell : FluentWindow
    {
        private static readonly string[] ValidExtensions = [".exe", ".dll"];

        public Shell()
        {
            InitializeComponent();
        }

        private void Grid_DragOver(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.None;
                return;
            }

            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            bool hasValid = files.Any(f => ValidExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()));
            e.Effects = hasValid ? DragDropEffects.Copy : DragDropEffects.None;
        }

        private async void Grid_Drop(object sender, DragEventArgs e)
        {
            if (e.Handled) return;
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
            if (DataContext is not ShellViewModel vm) return;

            var files = (string[])e.Data.GetData(DataFormats.FileDrop);

            var workspace = vm.Workspaces.FirstOrDefault();
            if (workspace == null)
            {
                var snackbar = App.AppHost.Services.GetService(typeof(ISnackbarService)) as ISnackbarService;
                snackbar?.Show("No Workspace", "Create a workspace first.", ControlAppearance.Caution, null, System.TimeSpan.FromSeconds(3));
                return;
            }

            await vm.ImportFilesFromPaths(files, workspace);
        }
    }
}

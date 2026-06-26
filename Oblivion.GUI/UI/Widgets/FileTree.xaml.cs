using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Oblivion.GUI.MVVM.Model;
using Oblivion.GUI.MVVM.ViewModel;

namespace Oblivion.GUI.Widgets
{
    public partial class FileTree : UserControl
    {
        private static readonly string[] ValidExtensions = [".exe", ".dll"];

        public FileTree()
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
        }

        private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(StartupViewModel.CollapseAllTrigger))
            {
                CollapseAllItems(MainTreeView);
            }
        }

        private static void CollapseAllItems(ItemsControl control)
        {
            foreach (var item in control.Items)
            {
                if (control.ItemContainerGenerator.ContainerFromItem(item) is TreeViewItem tvi)
                {
                    tvi.IsExpanded = false;
                    CollapseAllItems(tvi);
                }
            }
        }

        private void TreeView_DragOver(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.None;
                e.Handled = true;
                return;
            }

            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            bool hasValid = files.Any(f => ValidExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()));
            e.Effects = hasValid ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;
        }

        private void TreeView_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                MainTreeView.BorderThickness = new Thickness(2);
                MainTreeView.BorderBrush = (Brush)FindResource("OblivionAccent");
            }
        }

        private void TreeView_DragLeave(object sender, DragEventArgs e)
        {
            MainTreeView.BorderThickness = new Thickness(0);
            MainTreeView.BorderBrush = null;
        }

        private async void TreeView_Drop(object sender, DragEventArgs e)
        {
            MainTreeView.BorderThickness = new Thickness(0);
            MainTreeView.BorderBrush = null;

            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
                return;

            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (DataContext is not StartupViewModel vm)
                return;

            // Try to find the workspace under the cursor via visual tree
            WorkspaceUIModel? targetWorkspace = null;
            if (e.OriginalSource is DependencyObject source)
            {
                var treeViewItem = FindAncestor<TreeViewItem>(source);
                if (treeViewItem?.DataContext is WorkspaceUIModel ws)
                    targetWorkspace = ws;
                else if (treeViewItem?.DataContext is FileUIModel file)
                    targetWorkspace = vm.Workspaces.FirstOrDefault(w => w.Files.Contains(file));
            }

            targetWorkspace ??= vm.Workspaces.FirstOrDefault();

            if (targetWorkspace == null)
                return;

            e.Handled = true;
            await vm.ImportFilesFromPaths(files, targetWorkspace);
        }

        private static T? FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            while (current != null)
            {
                if (current is T match)
                    return match;
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }
    }
}

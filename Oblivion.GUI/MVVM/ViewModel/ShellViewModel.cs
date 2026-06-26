using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Oblivion.Data.Entities;
using Oblivion.Data.Repositories;
using Oblivion.Data.Services;
using Oblivion.Data.Snapshots;
using Oblivion.GUI.Domain.Abstractions;
using Oblivion.GUI.MVVM.Model;
using Oblivion.GUI.Services;
using Oblivion.GUI.UI.Dialogs;
using Oblivion.Interpop;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Oblivion.GUI.MVVM.ViewModel
{
    public partial class ShellViewModel(
        AppNavigationService navigationService,
        WorkspaceRepository workspaceRepository,
        FileRepository fileRepository,
        OblivionApiService oblivionApi,
        ThemeService themeService,
        NotificationService notify,
        FileExportService fileExportService) : ViewModelBase
    {
        public AppNavigationService NavigationService { get; } = navigationService;

        [ObservableProperty]
        private ObservableCollection<WorkspaceUIModel> _workspaces = new();

        [ObservableProperty]
        private FileUIModel _selectedFile;

        [ObservableProperty]
        private bool _isFlyoutOpen;

        [ObservableProperty]
        private bool _isImporting;

        [ObservableProperty]
        private string _newFolderName = "";

        [ObservableProperty]
        private string _searchText = "";

        [ObservableProperty]
        private bool _isSidebarCollapsed;

        [ObservableProperty]
        private bool _isRenamingFolder;

        [ObservableProperty]
        private string _renameFolderText = "";

        [ObservableProperty]
        private WorkspaceUIModel _renamingWorkspace;

        [ObservableProperty]
        private bool _isRenamingFile;

        [ObservableProperty]
        private string _renameFileText = "";

        [ObservableProperty]
        private FileUIModel _renamingFile;

        [ObservableProperty]
        private WorkspaceUIModel _renamingFileWorkspace;

        [ObservableProperty]
        private bool _isMovingFile;

        [ObservableProperty]
        private FileUIModel _movingFile;

        [ObservableProperty]
        private WorkspaceUIModel _movingFileSourceWorkspace;

        [ObservableProperty]
        private WorkspaceUIModel _movingFileTargetWorkspace;

        [ObservableProperty]
        private int _collapseAllTrigger;

        [ObservableProperty]
        private bool _isAnalysisActive;

        [ObservableProperty]
        private AnalysisViewModel _currentAnalysis;

        [RelayCommand]
        private async Task InitializePage()
        {
            foreach (var ws in await workspaceRepository.GetWorkspaces())
            {
                Workspaces.Add(new WorkspaceUIModel(ws));
            }
        }

        [RelayCommand]
        private void ToggleSidebar() => IsSidebarCollapsed = !IsSidebarCollapsed;

        [RelayCommand]
        private void OpenSettings()
        {
            var dialog = new ThemeSettingsDialog(themeService)
            {
                Owner = Application.Current.MainWindow
            };
            dialog.ShowDialog();
        }

        [RelayCommand]
        private void Analyze()
        {
            if (SelectedFile == null) return;

            var snapshot = WorkspaceFileHelper.GetSnapshotForFile(Workspaces, SelectedFile.Id);
            CurrentAnalysis = new AnalysisViewModel(SelectedFile, snapshot);
            IsAnalysisActive = true;
        }

        [RelayCommand]
        private void BackToHome()
        {
            IsAnalysisActive = false;
            CurrentAnalysis = null;
        }

        [RelayCommand]
        private void ExtractOverlay()
        {
            if (SelectedFile == null || !SelectedFile.HasOverlay) return;

            if (!File.Exists(SelectedFile.Path))
            {
                notify.Error("Extract Failed", "Source file not found.");
                return;
            }

            var baseName = Path.GetFileNameWithoutExtension(SelectedFile.Name);
            var dialog = new SaveFileDialog
            {
                FileName = baseName + "_overlay.bin",
                Filter = "Binary files (*.bin)|*.bin|All files (*.*)|*.*",
                DefaultExt = ".bin"
            };

            if (dialog.ShowDialog() != true) return;

            try
            {
                OverlayAnalyzer.ExtractOverlay(
                    SelectedFile.Path,
                    SelectedFile.OverlayOffset,
                    SelectedFile.OverlaySize,
                    dialog.FileName);

                notify.Success("Overlay Extracted", $"Size: {SelectedFile.OverlaySize:N0} bytes");
            }
            catch (Exception ex)
            {
                notify.Error("Extract Failed", ex.Message);
            }
        }

        [RelayCommand]
        private void Export()
        {
            if (SelectedFile == null) return;

            var snapshot = WorkspaceFileHelper.GetSnapshotForFile(Workspaces, SelectedFile.Id);

            var dialog = new SaveFileDialog
            {
                FileName = Path.GetFileNameWithoutExtension(SelectedFile.Name) + "_analysis.json",
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                DefaultExt = ".json"
            };

            if (dialog.ShowDialog() != true) return;
            fileExportService.ExportJson(dialog.FileName, SelectedFile, snapshot);
        }

        [RelayCommand]
        private void ExportPdf()
        {
            if (SelectedFile == null) return;

            var snapshot = WorkspaceFileHelper.GetSnapshotForFile(Workspaces, SelectedFile.Id);

            var dialog = new SaveFileDialog
            {
                FileName = Path.GetFileNameWithoutExtension(SelectedFile.Name) + "_analysis.pdf",
                Filter = "PDF files (*.pdf)|*.pdf|All files (*.*)|*.*",
                DefaultExt = ".pdf"
            };

            if (dialog.ShowDialog() != true) return;
            fileExportService.ExportPdf(dialog.FileName, SelectedFile, snapshot);
        }

        #region Folder operations

        [RelayCommand]
        private async Task CreateFolder()
        {
            if (string.IsNullOrWhiteSpace(NewFolderName))
                return;

            var (entity, created) = await workspaceRepository.AddAsync(new Workspace
            {
                Name = NewFolderName.Trim(),
            });

            NewFolderName = "";
            IsFlyoutOpen = false;

            if (created)
            {
                Workspaces.Add(new WorkspaceUIModel(entity));
                notify.Info("Folder Created", entity.Name);
            }
        }

        [RelayCommand]
        private void OpenFlyout() => IsFlyoutOpen = true;

        [RelayCommand]
        private void CollapseAll() => CollapseAllTrigger++;

        [RelayCommand]
        private void RenameFolder(WorkspaceUIModel workspace)
        {
            if (workspace == null) return;

            RenamingWorkspace = workspace;
            RenameFolderText = workspace.Name;
            IsRenamingFolder = true;
        }

        [RelayCommand]
        private async Task ConfirmRenameFolder()
        {
            if (RenamingWorkspace == null || string.IsNullOrWhiteSpace(RenameFolderText))
                return;

            var trimmed = RenameFolderText.Trim();
            var target = RenamingWorkspace;

            await workspaceRepository.Rename(target.Model.ID, trimmed);
            target.Name = trimmed;

            int idx = Workspaces.IndexOf(target);
            if (idx >= 0)
            {
                Workspaces.RemoveAt(idx);
                Workspaces.Insert(idx, target);
            }

            IsRenamingFolder = false;
            RenamingWorkspace = null;
            RenameFolderText = "";
        }

        [RelayCommand]
        private void CancelRenameFolder()
        {
            IsRenamingFolder = false;
            RenamingWorkspace = null;
            RenameFolderText = "";
        }

        [RelayCommand]
        private async Task DeleteFolder(WorkspaceUIModel workspace)
        {
            var name = workspace.Name;
            await workspaceRepository.Delete(workspace.Model);
            Workspaces.Remove(workspace);
            notify.Warning("Folder Deleted", name);
        }

        #endregion

        #region File operations

        [RelayCommand]
        private async Task ImportFile(WorkspaceUIModel workspace)
        {
            if (workspace == null)
                return;

            AnalyzedFile? file = await DTOHelper.FillFileInfoAsync(workspace.Model.ID, oblivionApi);

            if (file != null)
            {
                IsImporting = true;
                try
                {
                    await DTOHelper.SerializeSnapshotAsync(file, oblivionApi);
                    var saved = await fileRepository.AddOrUpdateFileAsync(file, workspace.Model.ID);
                    workspace.Files.Add(new FileUIModel(saved));
                    workspace.Model.Files.Add(saved);
                    notify.Success("File Imported", file.Name);
                }
                catch (Exception ex)
                {
                    notify.Error("Import Failed", ex.Message);
                }
                finally
                {
                    IsImporting = false;
                }
            }
        }

        [RelayCommand]
        private void RenameFile(FileUIModel file)
        {
            if (file == null) return;

            var workspace = WorkspaceFileHelper.FindWorkspaceContaining(Workspaces, file);
            if (workspace == null) return;

            RenamingFile = file;
            RenamingFileWorkspace = workspace;
            RenameFileText = file.Name;
            IsRenamingFile = true;
        }

        [RelayCommand]
        private async Task ConfirmRenameFile()
        {
            if (RenamingFile == null || string.IsNullOrWhiteSpace(RenameFileText))
                return;

            var trimmed = RenameFileText.Trim();

            await fileRepository.RenameFile(RenamingFile.Id, trimmed);
            RenamingFile.Name = trimmed;

            var entityFile = RenamingFileWorkspace?.Model.Files
                .FirstOrDefault(f => f.ID == RenamingFile.Id);
            if (entityFile != null)
                entityFile.Name = trimmed;

            IsRenamingFile = false;
            RenamingFile = null;
            RenamingFileWorkspace = null;
            RenameFileText = "";
        }

        [RelayCommand]
        private void CancelRenameFile()
        {
            IsRenamingFile = false;
            RenamingFile = null;
            RenamingFileWorkspace = null;
            RenameFileText = "";
        }

        [RelayCommand]
        private async Task DeleteFile(FileUIModel file)
        {
            if (file == null) return;

            var workspace = WorkspaceFileHelper.FindWorkspaceContaining(Workspaces, file);
            if (workspace == null) return;

            var name = file.Name;
            await fileRepository.RemoveFromWorkspace(file.Id, workspace.Model.ID);

            workspace.Files.Remove(file);
            var entityFile = workspace.Model.Files.FirstOrDefault(f => f.ID == file.Id);
            if (entityFile != null)
                workspace.Model.Files.Remove(entityFile);

            if (SelectedFile == file)
                SelectedFile = null;

            notify.Warning("File Removed", name);
        }

        [RelayCommand]
        private void StartMoveFile(FileUIModel file)
        {
            if (file == null) return;

            var workspace = WorkspaceFileHelper.FindWorkspaceContaining(Workspaces, file);
            if (workspace == null) return;

            MovingFile = file;
            MovingFileSourceWorkspace = workspace;
            MovingFileTargetWorkspace = null;
            IsMovingFile = true;
        }

        [RelayCommand]
        private async Task ConfirmMoveFile()
        {
            if (MovingFile == null || MovingFileSourceWorkspace == null || MovingFileTargetWorkspace == null)
                return;

            if (MovingFileSourceWorkspace == MovingFileTargetWorkspace)
            {
                IsMovingFile = false;
                return;
            }

            bool moved = await fileRepository.MoveFile(
                MovingFile.Id,
                MovingFileSourceWorkspace.Model.ID,
                MovingFileTargetWorkspace.Model.ID);

            if (moved)
            {
                MovingFileSourceWorkspace.Files.Remove(MovingFile);
                var entityFile = MovingFileSourceWorkspace.Model.Files.FirstOrDefault(f => f.ID == MovingFile.Id);
                if (entityFile != null)
                {
                    MovingFileSourceWorkspace.Model.Files.Remove(entityFile);
                    MovingFileTargetWorkspace.Model.Files.Add(entityFile);
                }

                MovingFileTargetWorkspace.Files.Add(MovingFile);
            }

            IsMovingFile = false;
            MovingFile = null;
            MovingFileSourceWorkspace = null;
            MovingFileTargetWorkspace = null;
        }

        [RelayCommand]
        private void CancelMoveFile()
        {
            IsMovingFile = false;
            MovingFile = null;
            MovingFileSourceWorkspace = null;
            MovingFileTargetWorkspace = null;
        }

        [RelayCommand]
        private async Task FixFile()
        {
            if (SelectedFile == null) return;

            var dialog = new OpenFileDialog
            {
                Title = $"Locate {SelectedFile.Name}",
                Filter = "PE Files (*.exe;*.dll)|*.exe;*.dll|All files (*.*)|*.*",
                FileName = SelectedFile.Name
            };

            if (dialog.ShowDialog() != true) return;

            var entity = WorkspaceFileHelper.FindEntity(Workspaces, SelectedFile.Id);

            if (entity != null)
            {
                entity.FilePath = dialog.FileName;
                SelectedFile.Path = dialog.FileName;

                IsImporting = true;
                try
                {
                    await DTOHelper.SerializeSnapshotAsync(entity, oblivionApi);
                }
                catch (Exception ex)
                {
                    notify.Error("Re-analysis Failed", ex.Message);
                    return;
                }
                finally
                {
                    IsImporting = false;
                }

                var workspace = WorkspaceFileHelper.FindWorkspaceContainingById(Workspaces, SelectedFile.Id);
                if (workspace != null)
                    await fileRepository.AddOrUpdateFileAsync(entity, workspace.Model.ID);

                SelectedFile.IsDeleted = false;
                SelectedFile.IsHashChanged = false;
                OnSelectedFileChanged(SelectedFile);
            }
        }

        public async Task ImportFilesFromPaths(string[] filePaths, WorkspaceUIModel workspace)
        {
            var validExtensions = new[] { ".exe", ".dll" };

            var valid = filePaths
                .Where(p => validExtensions.Contains(Path.GetExtension(p).ToLowerInvariant()))
                .ToArray();

            var invalid = filePaths.Length - valid.Length;
            if (invalid > 0)
                notify.Warning("Invalid File", "Only .exe and .dll files are supported.");

            if (valid.Length == 0) return;

            IsImporting = true;
            try
            {
                var tasks = valid.Select(async filePath =>
                {
                    var file = await DTOHelper.CreateFileFromPath(filePath);
                    await DTOHelper.SerializeSnapshotAsync(file, oblivionApi);
                    return file;
                }).ToArray();

                var results = await Task.WhenAll(tasks.Select(t => t.ContinueWith(r =>
                    (file: r.IsCompletedSuccessfully ? r.Result : null,
                     error: r.Exception?.InnerException?.Message ?? r.Exception?.Message))));

                foreach (var (file, error) in results)
                {
                    if (file != null)
                    {
                        var saved = await fileRepository.AddOrUpdateFileAsync(file, workspace.Model.ID);
                        workspace.Files.Add(new FileUIModel(saved));
                        workspace.Model.Files.Add(saved);
                        notify.Success("File Imported", file.Name);
                    }
                    else if (error != null)
                    {
                        notify.Error("Import Failed", error);
                    }
                }
            }
            finally
            {
                IsImporting = false;
            }
        }

        #endregion

        partial void OnSearchTextChanged(string value)
        {
            string filter = (value ?? "").Trim().ToLowerInvariant();
            foreach (var ws in Workspaces)
            {
                foreach (var file in ws.Files)
                {
                    file.IsVisible = string.IsNullOrEmpty(filter) ||
                                     file.Name.Contains(filter, StringComparison.OrdinalIgnoreCase);
                }
            }
        }

        partial void OnSelectedFileChanged(FileUIModel value)
        {
            if (value == null) return;

            if (!Path.Exists(value.Path))
            {
                value.IsDeleted = true;
                return;
            }

            value.IsDeleted = false;
            value.IsHashChanged = !(HashService.ComputeSHA256NoAsync(value.Path) == value.Hash);
        }
    }
}

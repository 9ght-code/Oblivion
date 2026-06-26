using System;
using System.Collections.ObjectModel;
using System.Linq;
using Oblivion.Data.Entities;
using Oblivion.Data.Services;
using Oblivion.Data.Snapshots;
using Oblivion.GUI.MVVM.Model;

namespace Oblivion.GUI.Services;

public static class WorkspaceFileHelper
{
    public static PESnapshot? GetSnapshotForFile(ObservableCollection<WorkspaceUIModel> workspaces, Guid fileId)
    {
        try
        {
            var entity = FindEntity(workspaces, fileId);
            if (entity != null)
                return SnapshotSerializer.Deserialize(entity.SnapshotJson);
        }
        catch { }
        return null;
    }

    public static AnalyzedFile? FindEntity(ObservableCollection<WorkspaceUIModel> workspaces, Guid fileId)
    {
        return workspaces
            .SelectMany(w => w.Model.Files)
            .FirstOrDefault(f => f.ID == fileId);
    }

    public static WorkspaceUIModel? FindWorkspaceContaining(ObservableCollection<WorkspaceUIModel> workspaces, FileUIModel file)
    {
        return workspaces.FirstOrDefault(w => w.Files.Contains(file));
    }

    public static WorkspaceUIModel? FindWorkspaceContainingById(ObservableCollection<WorkspaceUIModel> workspaces, Guid fileId)
    {
        return workspaces.FirstOrDefault(w => w.Model.Files.Any(f => f.ID == fileId));
    }
}

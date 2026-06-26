using Microsoft.EntityFrameworkCore;
using Oblivion.Data.Db;
using Oblivion.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Oblivion.Data.Repositories
{
    public class FileRepository(IDbContextFactory<OblivionDbContext> factory)
    {
        public async Task<AnalyzedFile> AddOrUpdateFileAsync(AnalyzedFile file, Guid workspaceID)
        {
            using var db = factory.CreateDbContext();

            var workspace = await db.Workspaces
                           .Include(w => w.Files)
                           .FirstOrDefaultAsync(w => w.ID == workspaceID);

            if (workspace == null)
                throw new InvalidOperationException($"Workspace {workspaceID} not found.");

            var existing = await db.Files.FirstOrDefaultAsync(f => f.Sha256 == file.Sha256);

            AnalyzedFile trackedEntity;
            if (existing != null)
            {
                existing.LoadedAt = DateTime.UtcNow;
                existing.FilePath = file.FilePath;
                existing.FileSize = file.FileSize;
                existing.Name = file.Name;
                existing.SnapshotJson = file.SnapshotJson;
                trackedEntity = existing;
            }
            else
            {
                db.Files.Add(file);
                trackedEntity = file;
            }

            if (!workspace.Files.Any(f => f.ID == trackedEntity.ID))
                workspace.Files.Add(trackedEntity);

            await db.SaveChangesAsync();
            return trackedEntity;
        }

        public async Task RenameFile(Guid fileId, string newName)
        {
            using var db = factory.CreateDbContext();
            var file = await db.Files.FindAsync(fileId);

            if (file != null)
            {
                file.Name = newName;
                await db.SaveChangesAsync();
            }
        }

        public async Task<bool> RemoveFromWorkspace(Guid fileId, Guid workspaceId)
        {
            using var db = factory.CreateDbContext();

            var workspace = await db.Workspaces
                .Include(w => w.Files)
                .FirstOrDefaultAsync(w => w.ID == workspaceId);

            if (workspace == null)
                return false;

            var file = workspace.Files.FirstOrDefault(f => f.ID == fileId);

            if (file == null)
                return false;

            // Check if file belongs to any other workspace before removing the link
            bool hasOtherWorkspaces = await db.Workspaces
                .AnyAsync(w => w.ID != workspaceId && w.Files.Any(f => f.ID == fileId));

            if (!hasOtherWorkspaces)
                // Delete the file entity directly — cascade will remove the join row too
                db.Files.Remove(file);

            else
                // Only remove the link, keep the file for other workspaces
                workspace.Files.Remove(file);

            await db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> MoveFile(Guid fileId, Guid fromWorkspaceId, Guid toWorkspaceId)
        {
            using var db = factory.CreateDbContext();

            var fromWs = await db.Workspaces
                .Include(w => w.Files)
                .FirstOrDefaultAsync(w => w.ID == fromWorkspaceId);

            var toWs = await db.Workspaces
                .Include(w => w.Files)
                .FirstOrDefaultAsync(w => w.ID == toWorkspaceId);

            if (fromWs == null || toWs == null)
                return false;

            var file = fromWs.Files.FirstOrDefault(f => f.ID == fileId);

            if (file == null)
                return false;

            fromWs.Files.Remove(file);

            if (!toWs.Files.Any(f => f.ID == fileId))
                toWs.Files.Add(file);

            await db.SaveChangesAsync();
            return true;
        }

        public async Task<List<AnalyzedFile>> GetFiles(Guid workspaceID)
        {
            using var db = factory.CreateDbContext();
            return await db.Workspaces
                .Where(w => w.ID == workspaceID)
                .SelectMany(w => w.Files)
                .OrderByDescending(f => f.LoadedAt)
                .ToListAsync();
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Oblivion.Data.Db;
using Oblivion.Data.Entities;

namespace Oblivion.Data.Repositories
{
    public class WorkspaceRepository(IDbContextFactory<OblivionDbContext> factory)
    {
        public async Task<List<Workspace>> GetWorkspaces()
        {
            using var db = factory.CreateDbContext();
            return await db.Workspaces.Include(w => w.Files)
                .OrderByDescending(w => w.Name)
                .ToListAsync();
        }

        public async Task<(Workspace entity, bool created)> AddAsync(Workspace workspace)
        {
            using var db = factory.CreateDbContext();
            var existing = await db.Workspaces.FirstOrDefaultAsync(w => w.Name == workspace.Name);

            if (existing != null)
                return (existing, false);

            db.Workspaces.Add(workspace);
            await db.SaveChangesAsync();
            return (workspace, true);
        }

        public async Task Rename(Guid workspaceId, string newName)
        {
            using var db = factory.CreateDbContext();
            var existing = await db.Workspaces.FindAsync(workspaceId);

            if (existing != null)
            {
                existing.Name = newName;
                await db.SaveChangesAsync();
            }
        }

        public async Task Delete(Workspace workspace)
        {
            using var db = factory.CreateDbContext();
            var existing = await db.Workspaces
                .Include(w => w.Files)
                .FirstOrDefaultAsync(w => w.Name == workspace.Name);

            if (existing == null)
                return;

            db.Workspaces.Remove(existing);
            await db.SaveChangesAsync();
        }

    }
}

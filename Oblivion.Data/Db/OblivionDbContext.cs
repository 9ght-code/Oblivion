using Microsoft.EntityFrameworkCore;
using Oblivion.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Oblivion.Data.Db
{
    public class OblivionDbContext : DbContext
    {
        public DbSet<Workspace> Workspaces { get; set; }
        public DbSet<AnalyzedFile> Files { get; set; }
        public DbSet<FileModificationNotes> Notes { get; set; }

        public OblivionDbContext(DbContextOptions<OblivionDbContext> options) : base(options) { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
                optionsBuilder.UseSqlite("Data Source=oblivion.db");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Workspace>()
                .HasMany(w => w.Files)
                .WithMany(f => f.Workspaces)
                .UsingEntity<Dictionary<string, object>>(
                    "WorkspaceFiles",
                    j => j
                           .HasOne<AnalyzedFile>()
                           .WithMany()
                           .HasForeignKey("FileID")
                           .OnDelete(DeleteBehavior.Cascade),
                    j => j
                           .HasOne<Workspace>()
                           .WithMany()
                           .HasForeignKey("WorkspaceID")
                           .OnDelete(DeleteBehavior.Cascade));

            modelBuilder.Entity<AnalyzedFile>()
                .HasMany(f => f.Notes)
                .WithOne(n => n.AnalyzedFile)
                .HasForeignKey(n => n.AnalyzedFileID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AnalyzedFile>()
                .Property(f => f.IsAnalyzed)
                .HasColumnName("IsAnalized");

            modelBuilder.Entity<AnalyzedFile>()
                .HasIndex(f => f.Sha256)
                .IsUnique();
        }
    }
}

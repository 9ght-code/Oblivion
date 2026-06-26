using Microsoft.Win32;
using Oblivion.Data.Entities;
using Oblivion.Data.Services;
using Oblivion.Interpop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Oblivion.GUI.Services
{
    public static class DTOHelper
    {
        private static readonly Dictionary<int, string> PeErrorMessages = new()
        {
            { 1, "File not found" },
            { 2, "Failed to read file" },
            { 3, "Out of memory" },
            { 4, "Invalid MZ signature" },
            { 5, "Invalid PE signature" },
            { 6, "Unsupported architecture" },
            { 7, "Import directory out of range" },
            { 8, "Invalid section headers" },
        };

        public static async Task<AnalyzedFile?> FillFileInfoAsync(Guid workspaceID, OblivionApiService api)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "PE Files (*.exe;*.dll)|*.exe;*.dll|All files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                return new AnalyzedFile
                {
                    Name     = openFileDialog.SafeFileName,
                    FilePath = openFileDialog.FileName,
                    FileSize = new FileInfo(openFileDialog.FileName).Length,
                    Sha256   = await HashService.ComputeSHA256(openFileDialog.FileName),
                };
            }

            return null;
        }

        public static async Task<AnalyzedFile> CreateFileFromPath(string filePath)
        {
            var info = new FileInfo(filePath);
            return new AnalyzedFile
            {
                Name     = info.Name,
                FilePath = filePath,
                FileSize = info.Length,
                Sha256   = await HashService.ComputeSHA256(filePath),
            };
        }

        public static Task SerializeSnapshotAsync(AnalyzedFile file, OblivionApiService api)
        {
            return Task.Run(() =>
            {
                var result = api.AnalyzePE(file.FilePath, out int errorCode);

                if (result == null)
                {
                    string msg = PeErrorMessages.TryGetValue(errorCode, out var desc)
                        ? desc
                        : $"Unknown error (code {errorCode})";
                    throw new InvalidOperationException(
                        $"PE analysis failed for '{file.FilePath}': {msg}");
                }

                var snapshot = SnapshotBuilder.BuildSnapshot(result);

                InstallerDetector.EnrichSnapshotWithInstaller(snapshot, file.FilePath);
                PackerDetector.EnrichSnapshotWithPacker(snapshot, file.FilePath);

                file.SnapshotJson = SnapshotSerializer.Serialize(snapshot);
            });
        }
    }
}

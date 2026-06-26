using Oblivion.Data.Snapshots;
using System.Text.Json;

namespace Oblivion.Data.Services
{
    public static class SnapshotSerializer
    {
        private static readonly JsonSerializerOptions Options = new()
        {
            WriteIndented = true,
            IncludeFields = true,
        };

        public static string Serialize(PESnapshot snapshot)
        {
            return JsonSerializer.Serialize(snapshot, Options);
        }

        public static PESnapshot? Deserialize(string? json)
        {
            if (string.IsNullOrEmpty(json))
                return null;

            return JsonSerializer.Deserialize<PESnapshot>(json, Options);
        }
    }
}
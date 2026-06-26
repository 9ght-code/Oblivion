namespace Oblivion.Data.Entities
{
    public class Workspace
    {
        public Guid ID { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = "Default workspace";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastOpenedAt { get; set;} = DateTime.UtcNow;
        public ICollection<AnalyzedFile> Files { get; set; } = new List<AnalyzedFile>(); 
    }
}

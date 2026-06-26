namespace Oblivion.GUI.MVVM.Model
{
    public class FunctionInfo
    {
        public string Module { get; set; } = "";
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Severity { get; set; } = "Safe"; // Safe, Medium, Dangerous
    }
}

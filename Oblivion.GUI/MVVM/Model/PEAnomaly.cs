namespace Oblivion.GUI.MVVM.Model
{
    public enum AnomalySeverity { Info, Low, Medium, High }

    public class PEAnomaly
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public AnomalySeverity Severity { get; set; }
    }
}

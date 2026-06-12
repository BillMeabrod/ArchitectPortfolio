namespace StationAI.Core.Models
{
    public class RiskAssessment
    {
        public int BiohazardLevel {  get; set; }
        public int ChemicalHazardLevel { get; set; }
        public int SecurityHazardLevel { get; set; }
        public string Recommendation { get; set;  } = string.Empty;
    }
}

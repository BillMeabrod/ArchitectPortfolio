using System.ComponentModel.DataAnnotations;

namespace StationAI.Core.Models
{
    public class RiskAssessment
    {
        [Range(0, 10)]
        public int BiohazardLevel { get; set; }

        [Range(0, 10)]
        public int ChemicalHazardLevel { get; set; }

        [Range(0, 10)]
        public int SecurityHazardLevel { get; set; }

        public string Recommendation { get; set; } = string.Empty;

        public bool InappropriateContent { get; set; }
    }
}
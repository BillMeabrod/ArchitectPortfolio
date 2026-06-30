using StationAI.Core.Models.Constants;
using System.ComponentModel.DataAnnotations;

namespace StationAI.Core.Models.Validation
{
    public class ValidLoreCategoryAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is string category && LoreCategories.IsValid(category))
                return ValidationResult.Success;

            return new ValidationResult(
                $"Category must be one of: {string.Join(", ", LoreCategories.All)}");
        }
    }
}

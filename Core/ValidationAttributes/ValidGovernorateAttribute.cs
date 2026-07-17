using Core.Enums;
using Core.Helpers;
using System.ComponentModel.DataAnnotations;

namespace Core.ValidationAttributes
{
    /// <summary>
    /// Validates that the selected governorate matches the venue's coordinates
    /// </summary>
    public class ValidGovernorateAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null)
            {
                return new ValidationResult("Governorate is required");
            }

            var governorate = (Governorate)value;

            // Get the model instance to access Latitude and Longitude
            var model = validationContext.ObjectInstance;
            var latitudeProperty = model.GetType().GetProperty("Latitude");
            var longitudeProperty = model.GetType().GetProperty("Longitude");

            if (latitudeProperty == null || longitudeProperty == null)
            {
                // Can't validate without coordinates - allow it
                return ValidationResult.Success;
            }

            var latitude = latitudeProperty.GetValue(model) as double?;
            var longitude = longitudeProperty.GetValue(model) as double?;

            // If coordinates aren't set, can't validate
            if (!latitude.HasValue || !longitude.HasValue)
            {
                return ValidationResult.Success;
            }

            // Validate using GovernorateGeometry
            bool isValid = GovernorateGeometry.ValidateGovernorate(governorate, latitude.Value, longitude.Value);

            if (!isValid)
            {
                // Try to detect the correct governorate
                var detectedGovernorate = GovernorateGeometry.DetectGovernorate(latitude.Value, longitude.Value);

                if (detectedGovernorate.HasValue && detectedGovernorate.Value != governorate)
                {
                    var detectedName = GovernorateHelper.GetDisplayName(detectedGovernorate.Value);
                    var selectedName = GovernorateHelper.GetDisplayName(governorate);

                    return new ValidationResult(
                        $"The selected location appears to be in {detectedName}, not {selectedName}. " +
                        $"Please verify the coordinates or change the governorate."
                    );
                }

                return new ValidationResult(
                    $"The selected location doesn't match {GovernorateHelper.GetDisplayName(governorate)} governorate boundaries."
                );
            }

            return ValidationResult.Success;
        }
    }
}
using System;
using System.ComponentModel.DataAnnotations;
using Core.Enums;

namespace Core.ValidationAttributes
{
    public class ValidDiscountValueAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            var discountVm = (dynamic)validationContext.ObjectInstance;

            if (discountVm == null)
                return ValidationResult.Success;

            if (value == null)
                return new ValidationResult("Discount value is required");

            decimal discountValue = Convert.ToDecimal(value);
            var discountType = (DiscountType)discountVm.DiscountType;

            if (discountType == DiscountType.Percentage)
            {
                if (discountValue <= 0 || discountValue > 100)
                    return new ValidationResult("Percentage discount must be between 0.01 and 100.");
            }
            else if (discountType == DiscountType.FixedAmount)
            {
                if (discountValue <= 0)
                    return new ValidationResult("Fixed amount discount must be greater than 0.");
            }

            return ValidationResult.Success;
        }
    }
}
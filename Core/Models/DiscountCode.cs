using System;
using System.ComponentModel.DataAnnotations;
using Core.Enums;
using Core.Models.EventModels;

namespace Core.Models
{
    public class DiscountCode
    {
        public int DiscountCodeId { get; set; }

        [Required]
        [StringLength(50)]
        public string Code { get; set; } = string.Empty;

        [Required]
        public DiscountType DiscountType { get; set; }

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal DiscountValue { get; set; }

        public DateTime? ValidFrom { get; set; } 

        public DateTime? ValidUntil { get; set; }

        [Range(0, int.MaxValue)]
        public int? MaxUsageCount { get; set; }

        public int CurrentUsageCount { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        // FK to Event
        public int EventId { get; set; }
        public EventBase? Event { get; set; }

        // Creator
        public int CreatedByUserId { get; set; }
        public User? CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Validation helper
        public bool IsValid()
        {
            if (!IsActive) return false;

            var now = DateTime.UtcNow;
            if (ValidFrom.HasValue && now < ValidFrom.Value) return false;
            if (ValidUntil.HasValue && now > ValidUntil.Value) return false;

            if (MaxUsageCount.HasValue && CurrentUsageCount >= MaxUsageCount.Value)
                return false;

            return true;
        }

        public decimal CalculateDiscount(decimal originalPrice)
        {
            if (!IsValid()) return 0;

            return DiscountType switch
            {
                DiscountType.Percentage => originalPrice * (DiscountValue / 100m),
                DiscountType.FixedAmount => Math.Min(DiscountValue, originalPrice),
                _ => 0
            };
        }
    }
}
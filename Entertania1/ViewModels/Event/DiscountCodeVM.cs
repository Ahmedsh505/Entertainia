using Core.Enums;
using Core.ValidationAttributes;
using System.ComponentModel.DataAnnotations;

namespace Entertania1.ViewModels.Event
{
    public class DiscountCodeVM
    {
        public int DiscountCodeId { get; set; }

        [Required(ErrorMessage = "Code is required")]
        [StringLength(50)]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "Discount type is required")]
        public DiscountType DiscountType { get; set; }

        [Required(ErrorMessage = "Discount value is required")]
        [ValidDiscountValue] // ✅ بدل Range القديمة
        public decimal DiscountValue { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime? ValidFrom { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime? ValidUntil { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Max usage must be at least 1")]
        public int? MaxUsageCount { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
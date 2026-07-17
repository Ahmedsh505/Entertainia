using Core.Enums;

namespace Core.DTOs
{
    public class DiscountCodeDTO
    {
        public int DiscountCodeId { get; set; }
        public string Code { get; set; } = string.Empty;
        public DiscountType DiscountType { get; set; }
        public decimal DiscountValue { get; set; }
        public DateTime? ValidFrom { get; set; }
        public DateTime? ValidUntil { get; set; }
        public int? MaxUsageCount { get; set; }
        public bool IsActive { get; set; }
    }
}
using System;
using System.Collections.Generic;

namespace Engagement.API.Entities;

public partial class PromoVouchers
{
    public Guid Id { get; set; }

    public string Code { get; set; } = null!;

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public string DiscountType { get; set; } = null!;

    public decimal DiscountValue { get; set; }

    public decimal? MaxDiscountAmount { get; set; }

    public decimal MinOrderValue { get; set; }

    public int TotalQuantity { get; set; }

    public int UsedQuantity { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<PromoVoucherUsages> PromoVoucherUsages { get; set; } = new List<PromoVoucherUsages>();
}

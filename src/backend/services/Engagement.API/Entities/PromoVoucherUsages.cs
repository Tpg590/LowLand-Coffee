using System;
using System.Collections.Generic;

namespace Engagement.API.Entities;

public partial class PromoVoucherUsages
{
    public Guid Id { get; set; }

    public Guid VoucherId { get; set; }

    public Guid UserId { get; set; }

    public Guid OrderId { get; set; }

    public decimal DiscountAmount { get; set; }

    public DateTime UsedAt { get; set; }

    public virtual PromoVouchers Voucher { get; set; } = null!;
}

using System;
using System.Collections.Generic;

namespace Engagement.API.Entities;

public partial class LoyaltyTiers
{
    public Guid Id { get; set; }

    public string TierCode { get; set; } = null!;

    public string TierName { get; set; } = null!;

    public int MinPoints { get; set; }

    public decimal PointMultiplier { get; set; }

    public decimal BirthdayDiscountPercent { get; set; }

    public decimal FreeToppingMinOrder { get; set; }

    public int FreeToppingCount { get; set; }

    public string? BenefitsDescription { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<LoyaltyMembers> LoyaltyMembers { get; set; } = new List<LoyaltyMembers>();
}

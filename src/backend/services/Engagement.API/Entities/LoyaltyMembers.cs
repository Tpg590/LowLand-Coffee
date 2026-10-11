using System;
using System.Collections.Generic;

namespace Engagement.API.Entities;

public partial class LoyaltyMembers
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid TierId { get; set; }

    public int CurrentPoints { get; set; }

    public int TotalAccumulatedPoints { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<LoyaltyPointTransactions> LoyaltyPointTransactions { get; set; } = new List<LoyaltyPointTransactions>();

    public virtual LoyaltyTiers Tier { get; set; } = null!;
}

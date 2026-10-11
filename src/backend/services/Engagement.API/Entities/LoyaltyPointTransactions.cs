using System;
using System.Collections.Generic;

namespace Engagement.API.Entities;

public partial class LoyaltyPointTransactions
{
    public Guid Id { get; set; }

    public Guid MemberId { get; set; }

    public Guid? OrderId { get; set; }

    public string TransactionType { get; set; } = null!;

    public int PointsChanged { get; set; }

    public int BalanceAfter { get; set; }

    public string? Reason { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual LoyaltyMembers Member { get; set; } = null!;
}

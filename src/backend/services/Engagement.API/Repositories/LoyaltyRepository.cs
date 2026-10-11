using Engagement.API.Data;
using Engagement.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace Engagement.API.Repositories;

public class LoyaltyRepository : ILoyaltyRepository
{
    private readonly EngagementDbContext _context;

    public LoyaltyRepository(EngagementDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<LoyaltyTiers>> GetTiersAsync()
    {
        return await _context.LoyaltyTiers
            .OrderBy(t => t.MinPoints)
            .ToListAsync();
    }

    public async Task<LoyaltyTiers?> GetTierByIdAsync(Guid id)
    {
        return await _context.LoyaltyTiers
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<LoyaltyTiers?> GetTierByCodeAsync(string tierCode)
    {
        return await _context.LoyaltyTiers
            .FirstOrDefaultAsync(t => t.TierCode.ToUpper() == tierCode.Trim().ToUpper());
    }

    public async Task<LoyaltyTiers?> GetTierForPointsAsync(int accumulatedPoints)
    {
        return await _context.LoyaltyTiers
            .Where(t => accumulatedPoints >= t.MinPoints)
            .OrderByDescending(t => t.MinPoints)
            .FirstOrDefaultAsync();
    }

    public async Task<LoyaltyMembers?> GetMemberByUserIdAsync(Guid userId)
    {
        return await _context.LoyaltyMembers
            .Include(m => m.Tier)
            .FirstOrDefaultAsync(m => m.UserId == userId);
    }

    public async Task<LoyaltyMembers> AddMemberAsync(LoyaltyMembers member)
    {
        await _context.LoyaltyMembers.AddAsync(member);
        return member;
    }

    public Task UpdateMemberAsync(LoyaltyMembers member)
    {
        member.UpdatedAt = DateTime.UtcNow;
        _context.LoyaltyMembers.Update(member);
        return Task.CompletedTask;
    }

    public async Task AddTransactionAsync(LoyaltyPointTransactions transaction)
    {
        await _context.LoyaltyPointTransactions.AddAsync(transaction);
    }

    public async Task<IEnumerable<LoyaltyPointTransactions>> GetTransactionsByMemberIdAsync(Guid memberId, int limit = 20)
    {
        return await _context.LoyaltyPointTransactions
            .Where(t => t.MemberId == memberId)
            .OrderByDescending(t => t.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}

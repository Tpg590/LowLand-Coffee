using Engagement.API.Entities;

namespace Engagement.API.Repositories;

public interface ILoyaltyRepository
{
    Task<IEnumerable<LoyaltyTiers>> GetTiersAsync();
    Task<LoyaltyTiers?> GetTierByIdAsync(Guid id);
    Task<LoyaltyTiers?> GetTierByCodeAsync(string tierCode);
    Task<LoyaltyTiers?> GetTierForPointsAsync(int accumulatedPoints);
    Task<LoyaltyMembers?> GetMemberByUserIdAsync(Guid userId);
    Task<LoyaltyMembers> AddMemberAsync(LoyaltyMembers member);
    Task UpdateMemberAsync(LoyaltyMembers member);
    Task AddTransactionAsync(LoyaltyPointTransactions transaction);
    Task<IEnumerable<LoyaltyPointTransactions>> GetTransactionsByMemberIdAsync(Guid memberId, int limit = 20);
    Task SaveChangesAsync();
}

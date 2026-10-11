using Engagement.API.DTOs.Loyalty;

namespace Engagement.API.Services;

public interface ILoyaltyService
{
    Task<IEnumerable<LoyaltyTierDto>> GetAllTiersAsync();
    Task<LoyaltyMemberProfileDto> GetMemberProfileAsync(Guid userId);
    Task<AddPointsResponse> AddPointsAsync(AddPointsRequest request);
    Task<RedeemPointsResponse> RedeemPointsAsync(RedeemPointsRequest request);
    Task<IEnumerable<PointTransactionDto>> GetPointTransactionsAsync(Guid userId, int limit = 20);
}

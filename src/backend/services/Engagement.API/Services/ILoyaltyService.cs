using Engagement.API.DTOs.Loyalty;

namespace Engagement.API.Services;

/// <summary>
/// Giao diện định nghĩa các nghiệp vụ quản lý khách hàng thân thiết và tích điểm.
/// </summary>
public interface ILoyaltyService
{
    /// <summary>
    /// Lấy danh sách tất cả các hạng thành viên kèm điều kiện và quyền lợi.
    /// </summary>
    Task<IEnumerable<LoyaltyTierDto>> GetAllTiersAsync();

    /// <summary>
    /// Lấy thông tin hồ sơ thành viên, điểm tích lũy và tiến trình thăng hạng của người dùng.
    /// </summary>
    Task<LoyaltyMemberProfileDto> GetMemberProfileAsync(Guid userId);

    /// <summary>
    /// Tích điểm cho người dùng từ đơn hàng và tự động xét nâng hạng.
    /// </summary>
    Task<AddPointsResponse> AddPointsAsync(AddPointsRequest request);

    /// <summary>
    /// Quy đổi điểm tích lũy thành số tiền giảm giá cho đơn hàng.
    /// </summary>
    Task<RedeemPointsResponse> RedeemPointsAsync(RedeemPointsRequest request);

    /// <summary>
    /// Lấy danh sách lịch sử giao dịch biến động điểm của người dùng.
    /// </summary>
    Task<IEnumerable<PointTransactionDto>> GetPointTransactionsAsync(Guid userId, int limit = 20);
}

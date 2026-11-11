using Engagement.API.Entities;

namespace Engagement.API.Repositories;

/// <summary>
/// Giao diện truy xuất dữ liệu cho hệ thống khách hàng thân thiết và tích điểm.
/// </summary>
public interface ILoyaltyRepository
{
    /// <summary>
    /// Lấy danh sách tất cả các hạng thành viên sắp xếp theo điểm tối thiểu tăng dần.
    /// </summary>
    Task<IEnumerable<LoyaltyTiers>> GetTiersAsync();

    /// <summary>
    /// Tìm hạng thành viên theo mã ID.
    /// </summary>
    Task<LoyaltyTiers?> GetTierByIdAsync(Guid id);

    /// <summary>
    /// Tìm hạng thành viên theo mã code (BRONZE, SILVER, GOLD, DIAMOND).
    /// </summary>
    Task<LoyaltyTiers?> GetTierByCodeAsync(string tierCode);

    /// <summary>
    /// Tìm hạng thành viên tương ứng với tổng điểm tích lũy trọn đời.
    /// </summary>
    Task<LoyaltyTiers?> GetTierForPointsAsync(int accumulatedPoints);

    /// <summary>
    /// Lấy thông tin thành viên theo mã định danh người dùng (kèm thông tin Tier).
    /// </summary>
    Task<LoyaltyMembers?> GetMemberByUserIdAsync(Guid userId);

    /// <summary>
    /// Thêm mới một hồ sơ thành viên.
    /// </summary>
    Task<LoyaltyMembers> AddMemberAsync(LoyaltyMembers member);

    /// <summary>
    /// Cập nhật thông tin hồ sơ thành viên.
    /// </summary>
    Task UpdateMemberAsync(LoyaltyMembers member);

    /// <summary>
    /// Thêm bản ghi giao dịch biến động điểm.
    /// </summary>
    Task AddTransactionAsync(LoyaltyPointTransactions transaction);

    /// <summary>
    /// Tìm giao dịch biến động điểm theo mã thành viên, đơn hàng và loại giao dịch (kiểm tra chống trùng lặp).
    /// </summary>
    Task<LoyaltyPointTransactions?> GetTransactionByOrderAndTypeAsync(Guid memberId, Guid orderId, string transactionType);

    /// <summary>
    /// Lấy danh sách giao dịch điểm gần nhất của thành viên.
    /// </summary>
    Task<IEnumerable<LoyaltyPointTransactions>> GetTransactionsByMemberIdAsync(Guid memberId, int limit = 20);

    /// <summary>
    /// Lưu các thay đổi vào cơ sở dữ liệu.
    /// </summary>
    Task SaveChangesAsync();
}

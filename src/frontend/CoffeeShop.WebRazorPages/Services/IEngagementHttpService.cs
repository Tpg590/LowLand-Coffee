using CoffeeShop.WebRazorPages.ViewModels.Common;
using CoffeeShop.WebRazorPages.ViewModels.Engagement;

namespace CoffeeShop.WebRazorPages.Services;

public interface IEngagementHttpService
{
    // Loyalty APIs
    Task<ApiResponse<IEnumerable<LoyaltyTierViewModel>>> GetTiersAsync();
    Task<ApiResponse<LoyaltyMemberProfileViewModel>> GetMemberProfileAsync(string? token = null);
    Task<ApiResponse<LoyaltyMemberProfileViewModel>> GetMemberProfileByUserIdAsync(Guid userId, string? token = null);
    Task<ApiResponse<IEnumerable<PointTransactionViewModel>>> GetPointTransactionsAsync(string? token = null, int limit = 20);
    Task<ApiResponse<IEnumerable<PointTransactionViewModel>>> GetUserPointTransactionsAsync(Guid userId, string? token = null, int limit = 20);
    Task<ApiResponse<AddPointsResponseViewModel>> AddPointsAsync(AddPointsRequestViewModel request, string? token = null);
    Task<ApiResponse<RedeemPointsResponseViewModel>> RedeemPointsAsync(RedeemPointsRequestViewModel request, string? token = null);

    // Voucher APIs
    Task<ApiResponse<IEnumerable<VoucherViewModel>>> GetActiveVouchersAsync();
    Task<ApiResponse<IEnumerable<VoucherViewModel>>> GetAllVouchersForAdminAsync(string? token = null);
    Task<ApiResponse<VoucherViewModel>> GetVoucherByIdAsync(Guid id, string? token = null);
    Task<ApiResponse<VoucherViewModel>> CreateVoucherAsync(CreateVoucherRequestViewModel request, string? token = null);
    Task<ApiResponse<VoucherViewModel>> UpdateVoucherAsync(Guid id, UpdateVoucherRequestViewModel request, string? token = null);
    Task<ApiResponse<bool>> DeleteVoucherAsync(Guid id, string? token = null);
    Task<ApiResponse<ValidateVoucherResponseViewModel>> ValidateVoucherAsync(ValidateVoucherRequestViewModel request, string? token = null);
    Task<ApiResponse<bool>> ApplyVoucherUsageAsync(ApplyVoucherUsageRequestViewModel request, string? token = null);
    Task<ApiResponse<IEnumerable<Guid>>> GetMyUsedVoucherIdsAsync(string? token = null);
    Task<ApiResponse<IEnumerable<Guid>>> GetUserUsedVoucherIdsAsync(Guid userId, string? token = null);
}

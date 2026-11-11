using AutoMapper;
using Engagement.API.DTOs.Loyalty;
using Engagement.API.Entities;
using Engagement.API.Repositories;

namespace Engagement.API.Services;

/// <summary>
/// Service xử lý toàn bộ nghiệp vụ của hệ thống Khách hàng thân thiết (Loyalty Program):
/// - Quản lý thông tin hạng thành viên và tiến trình thăng hạng.
/// - Tích điểm từ đơn hàng dựa trên hệ số nhân điểm của từng hạng.
/// - Đổi điểm thưởng thành tiền giảm giá cho đơn hàng.
/// - Truy vấn lịch sử giao dịch biến động điểm.
/// </summary>
public class LoyaltyService : ILoyaltyService
{
    private readonly ILoyaltyRepository _loyaltyRepository;
    private readonly IMapper _mapper;

    /// <summary>
    /// Tỉ lệ quy đổi tích điểm chuẩn: Cứ mỗi 10.000 VNĐ thanh toán được quy đổi thành 1 điểm chuẩn.
    /// </summary>
    private const decimal VND_PER_STANDARD_POINT = 10000m;

    /// <summary>
    /// Tỉ lệ quy đổi tiêu điểm: Cứ mỗi 1 điểm tích lũy được giảm trừ tương đương 1.000 VNĐ vào hóa đơn.
    /// </summary>
    private const decimal REDEEM_VND_PER_POINT = 1000m;

    public LoyaltyService(ILoyaltyRepository loyaltyRepository, IMapper mapper)
    {
        _loyaltyRepository = loyaltyRepository;
        _mapper = mapper;
    }

    /// <summary>
    /// Lấy danh sách tất cả các hạng thành viên hiện có trong hệ thống (Đồng, Bạc, Vàng, Kim Cương...).
    /// </summary>
    /// <returns>Danh sách thông tin các hạng thẻ kèm quyền lợi và điểm yêu cầu.</returns>
    public async Task<IEnumerable<LoyaltyTierDto>> GetAllTiersAsync()
    {
        var tiers = await _loyaltyRepository.GetTiersAsync();
        return _mapper.Map<IEnumerable<LoyaltyTierDto>>(tiers);
    }

    /// <summary>
    /// Lấy thông tin hồ sơ khách hàng thân thiết của người dùng kèm tiến trình thăng hạng kế tiếp.
    /// </summary>
    /// <param name="userId">Mã định danh người dùng.</param>
    /// <returns>Hồ sơ thành viên gồm điểm khả dụng, điểm trọn đời và hạng thẻ.</returns>
    public async Task<LoyaltyMemberProfileDto> GetMemberProfileAsync(Guid userId)
    {
        var member = await GetOrCreateMemberAsync(userId);
        var tiers = (await _loyaltyRepository.GetTiersAsync()).OrderBy(t => t.MinPoints).ToList();

        var currentTier = member.Tier;
        var nextTier = tiers.FirstOrDefault(t => t.MinPoints > (currentTier?.MinPoints ?? 0));

        int? pointsNeeded = null;
        if (nextTier != null)
        {
            pointsNeeded = Math.Max(0, nextTier.MinPoints - member.TotalAccumulatedPoints);
        }

        var profile = _mapper.Map<LoyaltyMemberProfileDto>(member);
        profile.NextTierName = nextTier?.TierName;
        profile.PointsNeededForNextTier = pointsNeeded;

        return profile;
    }

    /// <summary>
    /// Tích điểm thành viên từ đơn hàng theo hệ số nhân của hạng thẻ hiện tại và tự động xét nâng hạng.
    /// </summary>
    /// <param name="request">Thông tin đơn hàng và số tiền thanh toán.</param>
    /// <returns>Kết quả tích điểm và trạng thái thăng hạng.</returns>
    public async Task<AddPointsResponse> AddPointsAsync(AddPointsRequest request)
    {
        var member = await GetOrCreateMemberAsync(request.UserId);
        var currentTier = member.Tier;

        // Kiểm tra chống tích trùng điểm cho cùng một đơn hàng
        if (request.OrderId != Guid.Empty)
        {
            var existingTx = await _loyaltyRepository.GetTransactionByOrderAndTypeAsync(member.Id, request.OrderId, "EARN");
            if (existingTx != null)
            {
                return new AddPointsResponse
                {
                    UserId = member.UserId,
                    PointsEarned = existingTx.PointsChanged,
                    CurrentPoints = member.CurrentPoints,
                    TotalAccumulatedPoints = member.TotalAccumulatedPoints,
                    TierName = member.Tier?.TierName ?? "Hạng Đồng",
                    DidTierUpgrade = false
                };
            }
        }

        decimal basePoints = request.OrderAmount / VND_PER_STANDARD_POINT;
        decimal multiplier = currentTier?.PointMultiplier ?? 1.0m;
        int pointsEarned = (int)Math.Round(basePoints * multiplier, 0, MidpointRounding.AwayFromZero);

        if (pointsEarned <= 0)
        {
            pointsEarned = 1;
        }

        var tierLabel = currentTier?.TierName ?? "Hạng Đồng";

        member.CurrentPoints += pointsEarned;
        member.TotalAccumulatedPoints += pointsEarned;
        member.UpdatedAt = DateTime.UtcNow;

        // Xét duyệt thăng hạng dựa trên tổng điểm tích lũy trọn đời
        var qualifyingTier = await _loyaltyRepository.GetTierForPointsAsync(member.TotalAccumulatedPoints);
        bool didUpgrade = false;

        if (qualifyingTier != null && qualifyingTier.Id != member.TierId && qualifyingTier.MinPoints > (currentTier?.MinPoints ?? 0))
        {
            member.TierId = qualifyingTier.Id;
            member.Tier = qualifyingTier;
            didUpgrade = true;
        }

        await _loyaltyRepository.UpdateMemberAsync(member);

        var baseReason = string.IsNullOrWhiteSpace(request.Reason)
            ? $"Tích {pointsEarned} điểm từ đơn hàng"
            : request.Reason.Trim();
        var detailedReason = baseReason.Contains("x", StringComparison.OrdinalIgnoreCase)
            ? baseReason
            : $"{baseReason} ({tierLabel} - Hệ số x{multiplier:0.##})";

        var transaction = new LoyaltyPointTransactions
        {
            Id = Guid.NewGuid(),
            MemberId = member.Id,
            OrderId = request.OrderId,
            TransactionType = "EARN",
            PointsChanged = pointsEarned,
            BalanceAfter = member.CurrentPoints,
            Reason = detailedReason,
            CreatedAt = DateTime.UtcNow
        };

        await _loyaltyRepository.AddTransactionAsync(transaction);
        await _loyaltyRepository.SaveChangesAsync();

        return new AddPointsResponse
        {
            UserId = member.UserId,
            PointsEarned = pointsEarned,
            CurrentPoints = member.CurrentPoints,
            TotalAccumulatedPoints = member.TotalAccumulatedPoints,
            TierName = member.Tier?.TierName ?? "Hạng Đồng",
            DidTierUpgrade = didUpgrade
        };
    }

    /// <summary>
    /// Quy đổi điểm tích lũy thành số tiền giảm giá cho đơn hàng.
    /// </summary>
    /// <param name="request">Thông tin yêu cầu quy đổi điểm.</param>
    /// <returns>Số điểm đã quy đổi và số tiền được giảm trừ.</returns>
    public async Task<RedeemPointsResponse> RedeemPointsAsync(RedeemPointsRequest request)
    {
        var member = await GetOrCreateMemberAsync(request.UserId);

        if (request.PointsToRedeem <= 0)
        {
            throw new ArgumentException("Số điểm quy đổi phải lớn hơn 0.");
        }

        if (member.CurrentPoints < request.PointsToRedeem)
        {
            throw new InvalidOperationException($"Số dư điểm không đủ. Bạn hiện có {member.CurrentPoints} điểm nhưng yêu cầu đổi {request.PointsToRedeem} điểm.");
        }

        member.CurrentPoints -= request.PointsToRedeem;
        member.UpdatedAt = DateTime.UtcNow;
        await _loyaltyRepository.UpdateMemberAsync(member);

        decimal discountAmount = request.PointsToRedeem * REDEEM_VND_PER_POINT;

        var transaction = new LoyaltyPointTransactions
        {
            Id = Guid.NewGuid(),
            MemberId = member.Id,
            OrderId = request.OrderId,
            TransactionType = "REDEEM",
            PointsChanged = -request.PointsToRedeem,
            BalanceAfter = member.CurrentPoints,
            Reason = $"Quy đổi {request.PointsToRedeem} điểm giảm {discountAmount:N0} VNĐ cho đơn hàng",
            CreatedAt = DateTime.UtcNow
        };

        await _loyaltyRepository.AddTransactionAsync(transaction);
        await _loyaltyRepository.SaveChangesAsync();

        return new RedeemPointsResponse
        {
            UserId = member.UserId,
            PointsRedeemed = request.PointsToRedeem,
            DiscountAmount = discountAmount,
            RemainingPoints = member.CurrentPoints
        };
    }

    /// <summary>
    /// Lấy danh sách lịch sử biến động điểm gần nhất của người dùng.
    /// </summary>
    /// <param name="userId">Mã định danh người dùng.</param>
    /// <param name="limit">Số lượng giao dịch tối đa cần lấy.</param>
    /// <returns>Danh sách lịch sử giao dịch điểm.</returns>
    public async Task<IEnumerable<PointTransactionDto>> GetPointTransactionsAsync(Guid userId, int limit = 20)
    {
        var member = await _loyaltyRepository.GetMemberByUserIdAsync(userId);
        if (member == null)
        {
            return Enumerable.Empty<PointTransactionDto>();
        }

        var transactions = await _loyaltyRepository.GetTransactionsByMemberIdAsync(member.Id, limit);
        return _mapper.Map<IEnumerable<PointTransactionDto>>(transactions);
    }

    private async Task<LoyaltyMembers> GetOrCreateMemberAsync(Guid userId)
    {
        var member = await _loyaltyRepository.GetMemberByUserIdAsync(userId);
        if (member != null)
        {
            return member;
        }

        var tiers = await _loyaltyRepository.GetTiersAsync();
        var bronzeTier = tiers.OrderBy(t => t.MinPoints).FirstOrDefault();

        if (bronzeTier == null)
        {
            bronzeTier = await _loyaltyRepository.GetTierByCodeAsync("BRONZE")
                ?? throw new InvalidOperationException("Chưa khởi tạo danh mục Hạng thành viên (Loyalty Tiers) trong database.");
        }

        member = new LoyaltyMembers
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TierId = bronzeTier.Id,
            Tier = bronzeTier,
            CurrentPoints = 0,
            TotalAccumulatedPoints = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _loyaltyRepository.AddMemberAsync(member);
        await _loyaltyRepository.SaveChangesAsync();

        return member;
    }
}

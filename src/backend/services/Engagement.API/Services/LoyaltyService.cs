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
    /// Lấy thông tin hồ sơ khách hàng thân thiết của người dùng:
    /// - Hạng hiện tại, điểm hiện có và tổng điểm tích lũy trọn đời.
    /// - Tính toán hạng thành viên kế tiếp và số điểm còn thiếu để được thăng hạng.
    /// - Tự động tạo hồ sơ mặc định nếu người dùng chưa từng tham gia.
    /// </summary>
    public async Task<LoyaltyMemberProfileDto> GetMemberProfileAsync(Guid userId)
    {
        // Lấy hoặc tự động khởi tạo hồ sơ hội viên nếu chưa có
        var member = await GetOrCreateMemberAsync(userId);

        // Lấy tất cả các hạng được sắp xếp theo mức điểm tối thiểu tăng dần
        var tiers = (await _loyaltyRepository.GetTiersAsync()).OrderBy(t => t.MinPoints).ToList();

        // Xác định hạng kế tiếp (hạng có điểm tối thiểu cao hơn hạng hiện tại)
        var currentTier = member.Tier;
        var nextTier = tiers.FirstOrDefault(t => t.MinPoints > (currentTier?.MinPoints ?? 0));

        // Tính số điểm cần tích lũy thêm để đạt hạng tiếp theo
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
    /// Tích điểm cho người dùng sau khi hoàn thành đơn hàng:
    /// 1. Tính điểm cơ bản: [Giá trị đơn hàng] / [Tỉ lệ chuẩn 10.000 VNĐ].
    /// 2. Áp dụng hệ số nhân điểm (Multiplier) theo hạng thẻ hiện tại của khách hàng.
    /// 3. Cộng dồn vào điểm khả dụng (CurrentPoints) và điểm tích lũy trọn đời (TotalAccumulatedPoints).
    /// 4. Kiểm tra điều kiện thăng hạng dựa trên tổng điểm trọn đời.
    /// 5. Ghi nhận giao dịch biến động điểm (EARN) vào bảng lịch sử.
    /// </summary>
    public async Task<AddPointsResponse> AddPointsAsync(AddPointsRequest request)
    {
        var member = await GetOrCreateMemberAsync(request.UserId);
        var currentTier = member.Tier;

        // Chống cộng trùng điểm cho cùng một đơn hàng (Idempotency check theo OrderId)
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

        // Tính số điểm tích được: (OrderAmount / 10.000) * hệ số nhân của hạng thẻ (Đồng x1.0, Bạc x1.1, Vàng x1.2, Kim Cương x1.3)
        decimal basePoints = request.OrderAmount / VND_PER_STANDARD_POINT;
        decimal multiplier = currentTier?.PointMultiplier ?? 1.0m;
        int pointsEarned = (int)Math.Round(basePoints * multiplier, 0, MidpointRounding.AwayFromZero);

        // Đảm bảo đơn hợp lệ luôn được ít nhất 1 điểm thưởng
        if (pointsEarned <= 0)
        {
            pointsEarned = 1;
        }

        var tierLabel = currentTier?.TierName ?? "Hạng Đồng";

        // Cập nhật số dư điểm khả dụng và tổng điểm tích lũy trọn đời
        member.CurrentPoints += pointsEarned;
        member.TotalAccumulatedPoints += pointsEarned;
        member.UpdatedAt = DateTime.UtcNow;

        // Kiểm tra xem có đủ điều kiện thăng hạng dựa vào tổng điểm trọn đời (TotalAccumulatedPoints) không
        var qualifyingTier = await _loyaltyRepository.GetTierForPointsAsync(member.TotalAccumulatedPoints);
        bool didUpgrade = false;

        // Chỉ nâng hạng khi hạng mới cao hơn hạng hiện tại
        if (qualifyingTier != null && qualifyingTier.Id != member.TierId && qualifyingTier.MinPoints > (currentTier?.MinPoints ?? 0))
        {
            member.TierId = qualifyingTier.Id;
            member.Tier = qualifyingTier;
            didUpgrade = true;
        }

        await _loyaltyRepository.UpdateMemberAsync(member);

        // Ghi lại lịch sử giao dịch tích điểm kèm thông tin hạng thẻ và hệ số nhân
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
    /// Đổi điểm tích lũy thành tiền giảm giá cho đơn hàng:
    /// 1. Kiểm tra tính hợp lệ của số điểm yêu cầu đổi và số dư hiện có.
    /// 2. Trừ điểm khỏi tài khoản thành viên (CurrentPoints).
    /// 3. Quy đổi điểm ra số tiền giảm giá (1 điểm = 1.000 VNĐ).
    /// 4. Ghi nhận giao dịch tiêu điểm (REDEEM) vào bảng lịch sử.
    /// </summary>
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

        // Trừ điểm khả dụng
        member.CurrentPoints -= request.PointsToRedeem;
        member.UpdatedAt = DateTime.UtcNow;
        await _loyaltyRepository.UpdateMemberAsync(member);

        // Tính số tiền giảm giá tương ứng
        decimal discountAmount = request.PointsToRedeem * REDEEM_VND_PER_POINT;

        // Ghi lại lịch sử giao dịch tiêu điểm
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

    /// <summary>
    /// Phương thức hỗ trợ (Private): Lấy thông tin hội viên theo UserId, hoặc tự động khởi tạo hồ sơ mới
    /// với hạng mặc định là "Đồng" (Bronze) nếu người dùng truy cập lần đầu (Lazy Initialization).
    /// </summary>
    private async Task<LoyaltyMembers> GetOrCreateMemberAsync(Guid userId)
    {
        var member = await _loyaltyRepository.GetMemberByUserIdAsync(userId);
        if (member != null)
        {
            return member;
        }

        // Lazy Init: Tự động khởi tạo thành viên với Tier Đồng (Bronze) khi người dùng truy cập lần đầu
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

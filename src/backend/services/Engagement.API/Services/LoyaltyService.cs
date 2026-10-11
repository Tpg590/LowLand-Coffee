using AutoMapper;
using Engagement.API.DTOs.Loyalty;
using Engagement.API.Entities;
using Engagement.API.Repositories;

namespace Engagement.API.Services;

public class LoyaltyService : ILoyaltyService
{
    private readonly ILoyaltyRepository _loyaltyRepository;
    private readonly IMapper _mapper;

    private const decimal VND_PER_STANDARD_POINT = 10000m; // 10.000 VNĐ = 1 điểm chuẩn
    private const decimal REDEEM_VND_PER_POINT = 1000m;    // 1 điểm đổi = 1.000 VNĐ giảm giá

    public LoyaltyService(ILoyaltyRepository loyaltyRepository, IMapper mapper)
    {
        _loyaltyRepository = loyaltyRepository;
        _mapper = mapper;
    }

    public async Task<IEnumerable<LoyaltyTierDto>> GetAllTiersAsync()
    {
        var tiers = await _loyaltyRepository.GetTiersAsync();
        return _mapper.Map<IEnumerable<LoyaltyTierDto>>(tiers);
    }

    public async Task<LoyaltyMemberProfileDto> GetMemberProfileAsync(Guid userId)
    {
        var member = await GetOrCreateMemberAsync(userId);
        var tiers = (await _loyaltyRepository.GetTiersAsync()).OrderBy(t => t.MinPoints).ToList();

        // Tìm hạng kế tiếp (nếu có)
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

    public async Task<AddPointsResponse> AddPointsAsync(AddPointsRequest request)
    {
        var member = await GetOrCreateMemberAsync(request.UserId);
        var currentTier = member.Tier;

        // Tính số điểm tích được: (OrderAmount / 10.000) * hệ số hạng thẻ
        decimal basePoints = request.OrderAmount / VND_PER_STANDARD_POINT;
        decimal multiplier = currentTier?.PointMultiplier ?? 1.0m;
        int pointsEarned = (int)Math.Floor(basePoints * multiplier);

        if (pointsEarned <= 0)
        {
            pointsEarned = 1; // Tối thiểu 1 điểm cho đơn hàng hợp lệ
        }

        member.CurrentPoints += pointsEarned;
        member.TotalAccumulatedPoints += pointsEarned;
        member.UpdatedAt = DateTime.UtcNow;

        // Kiểm tra xem có thăng hạng không dựa vào tổng điểm trọn đời
        var qualifyingTier = await _loyaltyRepository.GetTierForPointsAsync(member.TotalAccumulatedPoints);
        bool didUpgrade = false;

        if (qualifyingTier != null && qualifyingTier.Id != member.TierId && qualifyingTier.MinPoints > (currentTier?.MinPoints ?? 0))
        {
            member.TierId = qualifyingTier.Id;
            member.Tier = qualifyingTier;
            didUpgrade = true;
        }

        await _loyaltyRepository.UpdateMemberAsync(member);

        // Lưu vết lịch sử điểm
        var transaction = new LoyaltyPointTransactions
        {
            Id = Guid.NewGuid(),
            MemberId = member.Id,
            OrderId = request.OrderId,
            TransactionType = "EARN",
            PointsChanged = pointsEarned,
            BalanceAfter = member.CurrentPoints,
            Reason = request.Reason ?? $"Tích {pointsEarned} điểm từ đơn hàng",
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

        // Ghi nhận lịch sử giao dịch
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

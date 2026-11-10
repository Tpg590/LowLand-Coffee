using AutoMapper;
using Engagement.API.DTOs.Vouchers;
using Engagement.API.Entities;
using Engagement.API.Repositories;

namespace Engagement.API.Services;

/// <summary>
/// Service quản lý toàn bộ nghiệp vụ Mã giảm giá / Voucher khuyến mãi:
/// - Quản lý danh mục voucher (Xem, Thêm, Cập nhật, Xóa/Vô hiệu hóa).
/// - Kiểm tra và thẩm định tính hợp lệ của voucher trước khi đặt hàng (Validation).
/// - Tính toán số tiền chiết khấu theo phần trăm (%) hoặc số tiền cố định (Fixed Amount).
/// - Ghi nhận lịch sử sử dụng voucher và cập nhật số lượt dùng.
/// </summary>
public class VoucherService : IVoucherService
{
    private readonly IVoucherRepository _voucherRepository;
    private readonly IMapper _mapper;

    public VoucherService(IVoucherRepository voucherRepository, IMapper mapper)
    {
        _voucherRepository = voucherRepository;
        _mapper = mapper;
    }

    /// <summary>
    /// Lấy danh sách tất cả các voucher khuyến mãi trong hệ thống.
    /// </summary>
    public async Task<IEnumerable<VoucherDto>> GetAllVouchersAsync(bool? isActiveOnly = null)
    {
        var vouchers = await _voucherRepository.GetVouchersAsync(isActiveOnly);
        return _mapper.Map<IEnumerable<VoucherDto>>(vouchers);
    }

    /// <summary>
    /// Lấy chi tiết thông tin voucher theo ID duy nhất.
    /// </summary>
    public async Task<VoucherDto?> GetVoucherByIdAsync(Guid id)
    {
        var voucher = await _voucherRepository.GetVoucherByIdAsync(id);
        return voucher == null ? null : _mapper.Map<VoucherDto>(voucher);
    }

    /// <summary>
    /// Tạo mới một mã voucher khuyến mãi trong hệ thống.
    /// </summary>
    public async Task<VoucherDto> CreateVoucherAsync(CreateVoucherRequest request)
    {
        var existing = await _voucherRepository.GetVoucherByCodeAsync(request.Code);
        if (existing != null)
        {
            throw new InvalidOperationException($"Mã voucher '{request.Code}' đã tồn tại trong hệ thống.");
        }

        var voucher = _mapper.Map<PromoVouchers>(request);

        await _voucherRepository.AddVoucherAsync(voucher);
        await _voucherRepository.SaveChangesAsync();

        return _mapper.Map<VoucherDto>(voucher);
    }

    /// <summary>
    /// Cập nhật thông tin cấu hình của một voucher đã có.
    /// </summary>
    public async Task<VoucherDto?> UpdateVoucherAsync(Guid id, UpdateVoucherRequest request)
    {
        var voucher = await _voucherRepository.GetVoucherByIdAsync(id);
        if (voucher == null) return null;

        voucher.Title = request.Title.Trim();
        voucher.Description = request.Description?.Trim();
        voucher.DiscountType = request.DiscountType.ToUpper();
        voucher.DiscountValue = request.DiscountValue;
        voucher.MaxDiscountAmount = request.MaxDiscountAmount;
        voucher.MinOrderValue = request.MinOrderValue;
        voucher.TotalQuantity = request.TotalQuantity;
        voucher.StartDate = request.StartDate;
        voucher.EndDate = request.EndDate;
        voucher.IsActive = request.IsActive;
        voucher.UpdatedAt = DateTime.UtcNow;

        await _voucherRepository.UpdateVoucherAsync(voucher);
        await _voucherRepository.SaveChangesAsync();

        return _mapper.Map<VoucherDto>(voucher);
    }

    /// <summary>
    /// Xóa hoặc vô hiệu hóa voucher khỏi hệ thống.
    /// Nếu voucher đã có lượt sử dụng thì thực hiện vô hiệu hóa (IsActive = false) để bảo đảm toàn vẹn dữ liệu đơn hàng.
    /// Nếu voucher chưa từng sử dụng thì xóa cứng khỏi cơ sở dữ liệu.
    /// </summary>
    /// <param name="id">Mã định danh voucher cần xóa.</param>
    /// <returns>True nếu xóa/vô hiệu hóa thành công, ngược lại False.</returns>
    public async Task<bool> DeleteVoucherAsync(Guid id)
    {
        var voucher = await _voucherRepository.GetVoucherByIdAsync(id);
        if (voucher == null) return false;

        if (voucher.UsedQuantity > 0)
        {
            voucher.IsActive = false;
            voucher.UpdatedAt = DateTime.UtcNow;
            await _voucherRepository.UpdateVoucherAsync(voucher);
        }
        else
        {
            await _voucherRepository.DeleteVoucherAsync(voucher);
        }

        await _voucherRepository.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Kiểm tra tính hợp lệ và thẩm định mã voucher cho một đơn hàng cụ thể.
    /// </summary>
    /// <param name="request">Thông tin mã voucher, tổng tiền đơn hàng và khách hàng.</param>
    /// <returns>Kết quả thẩm định kèm số tiền được giảm giá.</returns>
    public async Task<ValidateVoucherResponse> ValidateVoucherAsync(ValidateVoucherRequest request)
    {
        var voucher = await _voucherRepository.GetVoucherByCodeAsync(request.VoucherCode);
        if (voucher == null)
        {
            return new ValidateVoucherResponse
            {
                IsValid = false,
                Message = "Mã voucher không tồn tại."
            };
        }

        if (!voucher.IsActive)
        {
            return new ValidateVoucherResponse
            {
                IsValid = false,
                Message = "Mã voucher hiện đã bị tạm ngưng hoặc khóa."
            };
        }

        var now = DateTime.UtcNow;
        if (now < voucher.StartDate)
        {
            return new ValidateVoucherResponse
            {
                IsValid = false,
                Message = $"Mã voucher chưa đến thời gian áp dụng (bắt đầu từ {voucher.StartDate:dd/MM/yyyy HH:mm})."
            };
        }

        if (now > voucher.EndDate)
        {
            return new ValidateVoucherResponse
            {
                IsValid = false,
                Message = "Mã voucher đã hết hạn sử dụng."
            };
        }

        if (voucher.UsedQuantity >= voucher.TotalQuantity)
        {
            return new ValidateVoucherResponse
            {
                IsValid = false,
                Message = "Mã voucher đã hết lượt sử dụng."
            };
        }

        if (request.OrderSubtotal < voucher.MinOrderValue)
        {
            return new ValidateVoucherResponse
            {
                IsValid = false,
                Message = $"Đơn hàng chưa đạt giá trị tối thiểu {voucher.MinOrderValue:N0} đ để áp dụng voucher này."
            };
        }

        if (request.UserId.HasValue)
        {
            var usageCount = await _voucherRepository.GetUserUsageCountAsync(voucher.Id, request.UserId.Value);
            if (usageCount > 0)
            {
                return new ValidateVoucherResponse
                {
                    IsValid = false,
                    Message = "Bạn đã sử dụng mã voucher này rồi. Mỗi khách hàng chỉ được dùng 1 lần."
                };
            }
        }

        decimal discountAmount = 0m;
        if (voucher.DiscountType.Equals("PERCENTAGE", StringComparison.OrdinalIgnoreCase))
        {
            discountAmount = request.OrderSubtotal * (voucher.DiscountValue / 100m);
            if (voucher.MaxDiscountAmount.HasValue && discountAmount > voucher.MaxDiscountAmount.Value)
            {
                discountAmount = voucher.MaxDiscountAmount.Value;
            }
        }
        else if (voucher.DiscountType.Equals("FIXED_AMOUNT", StringComparison.OrdinalIgnoreCase))
        {
            discountAmount = Math.Min(voucher.DiscountValue, request.OrderSubtotal);
        }

        var finalTotal = Math.Max(0, request.OrderSubtotal - discountAmount);

        return new ValidateVoucherResponse
        {
            IsValid = true,
            Message = "Áp dụng mã giảm giá thành công!",
            VoucherId = voucher.Id,
            Code = voucher.Code,
            Title = voucher.Title,
            DiscountType = voucher.DiscountType,
            DiscountValue = voucher.DiscountValue,
            DiscountAmount = discountAmount,
            FinalTotal = finalTotal
        };
    }

    /// <summary>
    /// Ghi nhận việc sử dụng voucher khi đơn hàng được tạo hoặc thanh toán thành công.
    /// </summary>
    /// <param name="request">Thông tin đơn hàng và mã voucher đã áp dụng.</param>
    /// <returns>True nếu ghi nhận thành công, ngược lại False.</returns>
    public async Task<bool> ApplyVoucherUsageAsync(ApplyVoucherUsageRequest request)
    {
        var voucher = await _voucherRepository.GetVoucherByIdAsync(request.VoucherId);
        if (voucher == null || !voucher.IsActive || voucher.UsedQuantity >= voucher.TotalQuantity)
        {
            return false;
        }

        var usage = new PromoVoucherUsages
        {
            Id = Guid.NewGuid(),
            VoucherId = voucher.Id,
            UserId = request.UserId,
            OrderId = request.OrderId,
            DiscountAmount = request.DiscountAmount,
            UsedAt = DateTime.UtcNow
        };

        await _voucherRepository.AddUsageAsync(usage);

        voucher.UsedQuantity += 1;
        voucher.UpdatedAt = DateTime.UtcNow;
        await _voucherRepository.UpdateVoucherAsync(voucher);
        await _voucherRepository.SaveChangesAsync();

        return true;
    }

    /// <summary>
    /// Lấy danh sách ID các voucher mà khách hàng đã từng sử dụng.
    /// </summary>
    /// <param name="userId">Mã định danh khách hàng.</param>
    /// <returns>Danh sách ID các voucher đã dùng.</returns>
    public async Task<IEnumerable<Guid>> GetUsedVoucherIdsByUserAsync(Guid userId)
    {
        return await _voucherRepository.GetUsedVoucherIdsByUserAsync(userId);
    }
}

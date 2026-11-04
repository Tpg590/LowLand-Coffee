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
    /// Xóa hoặc vô hiệu hóa voucher khỏi hệ thống:
    /// - Nếu voucher ĐÃ CÓ người sử dụng (UsedQuantity > 0): Thực hiện Soft-Delete bằng cách đặt IsActive = false để bảo toàn toàn vẹn dữ liệu đơn hàng cũ.
    /// - Nếu voucher CHƯA TỪNG được sử dụng: Xóa cứng hoàn toàn khỏi cơ sở dữ liệu.
    /// </summary>
    public async Task<bool> DeleteVoucherAsync(Guid id)
    {
        var voucher = await _voucherRepository.GetVoucherByIdAsync(id);
        if (voucher == null) return false;

        // Vô hiệu hóa (Soft-Delete) thay vì xóa cứng nếu đã có người sử dụng
        if (voucher.UsedQuantity > 0)
        {
            voucher.IsActive = false;
            voucher.UpdatedAt = DateTime.UtcNow;
            await _voucherRepository.UpdateVoucherAsync(voucher);
        }
        else
        {
            // Xóa cứng an toàn khi voucher chưa phát sinh lịch sử sử dụng
            await _voucherRepository.DeleteVoucherAsync(voucher);
        }

        await _voucherRepository.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Kiểm tra tính hợp lệ và thẩm định mã voucher cho một đơn hàng cụ thể:
    /// 1. Kiểm tra sự tồn tại của mã voucher.
    /// 2. Kiểm tra trạng thái hoạt động (IsActive).
    /// 3. Kiểm tra khung thời gian hiệu lực (StartDate <= Hiện tại <= EndDate).
    /// 4. Kiểm tra giới hạn số lượng phát hành toàn hệ thống (UsedQuantity &lt; TotalQuantity).
    /// 5. Kiểm tra giá trị đơn hàng tối thiểu (OrderSubtotal &gt;= MinOrderValue).
    /// 6. Kiểm tra giới hạn mỗi khách hàng chỉ được dùng voucher 1 lần (nếu đã đăng nhập).
    /// 7. Tính toán số tiền được giảm giá theo PERCENTAGE (kèm MaxDiscount) hoặc FIXED_AMOUNT.
    /// </summary>
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

        // 1. Kiểm tra trạng thái kích hoạt của voucher
        if (!voucher.IsActive)
        {
            return new ValidateVoucherResponse
            {
                IsValid = false,
                Message = "Mã voucher hiện đã bị tạm ngưng hoặc khóa."
            };
        }

        // 2. Kiểm tra ngày bắt đầu có hiệu lực
        var now = DateTime.UtcNow;
        if (now < voucher.StartDate)
        {
            return new ValidateVoucherResponse
            {
                IsValid = false,
                Message = $"Mã voucher chưa đến thời gian áp dụng (bắt đầu từ {voucher.StartDate:dd/MM/yyyy HH:mm})."
            };
        }

        // 3. Kiểm tra hạn sử dụng
        if (now > voucher.EndDate)
        {
            return new ValidateVoucherResponse
            {
                IsValid = false,
                Message = "Mã voucher đã hết hạn sử dụng."
            };
        }

        // 4. Kiểm tra tổng số lượt sử dụng còn lại
        if (voucher.UsedQuantity >= voucher.TotalQuantity)
        {
            return new ValidateVoucherResponse
            {
                IsValid = false,
                Message = "Mã voucher đã hết lượt sử dụng."
            };
        }

        // 5. Kiểm tra giá trị đơn hàng tối thiểu
        if (request.OrderSubtotal < voucher.MinOrderValue)
        {
            return new ValidateVoucherResponse
            {
                IsValid = false,
                Message = $"Đơn hàng chưa đạt giá trị tối thiểu {voucher.MinOrderValue:N0} đ để áp dụng voucher này."
            };
        }

        // 6. Kiểm tra giới hạn 1 lần dùng cho mỗi người dùng (nếu có thông tin UserId)
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

        // 7. Tính số tiền giảm giá theo loại voucher
        decimal discountAmount = 0m;
        if (voucher.DiscountType.Equals("PERCENTAGE", StringComparison.OrdinalIgnoreCase))
        {
            // Giảm theo % giá trị đơn
            discountAmount = request.OrderSubtotal * (voucher.DiscountValue / 100m);
            // Áp dụng mức giảm tối đa nếu có cấu hình
            if (voucher.MaxDiscountAmount.HasValue && discountAmount > voucher.MaxDiscountAmount.Value)
            {
                discountAmount = voucher.MaxDiscountAmount.Value;
            }
        }
        else if (voucher.DiscountType.Equals("FIXED_AMOUNT", StringComparison.OrdinalIgnoreCase))
        {
            // Giảm số tiền cố định (không vượt quá tổng tiền đơn)
            discountAmount = Math.Min(voucher.DiscountValue, request.OrderSubtotal);
        }

        // Tính tổng tiền cần thanh toán sau khi trừ giảm giá (không để âm)
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
    /// Ghi nhận việc sử dụng voucher khi đơn hàng được tạo/thanh toán thành công:
    /// - Thêm bản ghi lịch sử vào bảng <see cref="PromoVoucherUsages"/>.
    /// - Tăng số lượt đã sử dụng (UsedQuantity) của voucher lên 1.
    /// </summary>
    public async Task<bool> ApplyVoucherUsageAsync(ApplyVoucherUsageRequest request)
    {
        var voucher = await _voucherRepository.GetVoucherByIdAsync(request.VoucherId);
        if (voucher == null || !voucher.IsActive || voucher.UsedQuantity >= voucher.TotalQuantity)
        {
            return false;
        }

        // Khởi tạo bản ghi lịch sử áp dụng voucher
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

        // Tăng số lượt đã sử dụng của voucher
        voucher.UsedQuantity += 1;
        voucher.UpdatedAt = DateTime.UtcNow;
        await _voucherRepository.UpdateVoucherAsync(voucher);
        await _voucherRepository.SaveChangesAsync();

        return true;
    }

    /// <summary>
    /// Lấy danh sách ID các voucher mà khách hàng đã từng sử dụng.
    /// </summary>
    public async Task<IEnumerable<Guid>> GetUsedVoucherIdsByUserAsync(Guid userId)
    {
        return await _voucherRepository.GetUsedVoucherIdsByUserAsync(userId);
    }
}

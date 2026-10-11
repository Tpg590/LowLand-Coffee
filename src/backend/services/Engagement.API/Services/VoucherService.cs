using AutoMapper;
using Engagement.API.DTOs.Vouchers;
using Engagement.API.Entities;
using Engagement.API.Repositories;

namespace Engagement.API.Services;

public class VoucherService : IVoucherService
{
    private readonly IVoucherRepository _voucherRepository;
    private readonly IMapper _mapper;

    public VoucherService(IVoucherRepository voucherRepository, IMapper mapper)
    {
        _voucherRepository = voucherRepository;
        _mapper = mapper;
    }

    public async Task<IEnumerable<VoucherDto>> GetAllVouchersAsync(bool? isActiveOnly = null)
    {
        var vouchers = await _voucherRepository.GetVouchersAsync(isActiveOnly);
        return _mapper.Map<IEnumerable<VoucherDto>>(vouchers);
    }

    public async Task<VoucherDto?> GetVoucherByIdAsync(Guid id)
    {
        var voucher = await _voucherRepository.GetVoucherByIdAsync(id);
        return voucher == null ? null : _mapper.Map<VoucherDto>(voucher);
    }

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

    public async Task<bool> DeleteVoucherAsync(Guid id)
    {
        var voucher = await _voucherRepository.GetVoucherByIdAsync(id);
        if (voucher == null) return false;

        // Vô hiệu hóa thay vì xóa cứng nếu đã có người sử dụng
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

        // Kiểm tra giới hạn 1 lần dùng / user
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

        // Tính tiền giảm giá
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
}

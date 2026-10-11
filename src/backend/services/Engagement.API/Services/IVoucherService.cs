using Engagement.API.DTOs.Vouchers;

namespace Engagement.API.Services;

public interface IVoucherService
{
    Task<IEnumerable<VoucherDto>> GetAllVouchersAsync(bool? isActiveOnly = null);
    Task<VoucherDto?> GetVoucherByIdAsync(Guid id);
    Task<VoucherDto> CreateVoucherAsync(CreateVoucherRequest request);
    Task<VoucherDto?> UpdateVoucherAsync(Guid id, UpdateVoucherRequest request);
    Task<bool> DeleteVoucherAsync(Guid id);
    Task<ValidateVoucherResponse> ValidateVoucherAsync(ValidateVoucherRequest request);
    Task<bool> ApplyVoucherUsageAsync(ApplyVoucherUsageRequest request);
}

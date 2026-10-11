using Engagement.API.Entities;

namespace Engagement.API.Repositories;

public interface IVoucherRepository
{
    Task<IEnumerable<PromoVouchers>> GetVouchersAsync(bool? isActiveOnly = null);
    Task<PromoVouchers?> GetVoucherByIdAsync(Guid id);
    Task<PromoVouchers?> GetVoucherByCodeAsync(string code);
    Task<PromoVouchers> AddVoucherAsync(PromoVouchers voucher);
    Task UpdateVoucherAsync(PromoVouchers voucher);
    Task DeleteVoucherAsync(PromoVouchers voucher);
    Task AddUsageAsync(PromoVoucherUsages usage);
    Task<int> GetUserUsageCountAsync(Guid voucherId, Guid userId);
    Task SaveChangesAsync();
}

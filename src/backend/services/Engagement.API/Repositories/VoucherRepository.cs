using Engagement.API.Data;
using Engagement.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace Engagement.API.Repositories;

public class VoucherRepository : IVoucherRepository
{
    private readonly EngagementDbContext _context;

    public VoucherRepository(EngagementDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<PromoVouchers>> GetVouchersAsync(bool? isActiveOnly = null)
    {
        IQueryable<PromoVouchers> query = _context.PromoVouchers;

        if (isActiveOnly.HasValue && isActiveOnly.Value)
        {
            var now = DateTime.UtcNow;
            query = query.Where(v => v.IsActive && v.StartDate <= now && v.EndDate >= now && v.UsedQuantity < v.TotalQuantity);
        }

        return await query.OrderByDescending(v => v.CreatedAt).ToListAsync();
    }

    public async Task<PromoVouchers?> GetVoucherByIdAsync(Guid id)
    {
        return await _context.PromoVouchers
            .Include(v => v.PromoVoucherUsages)
            .FirstOrDefaultAsync(v => v.Id == id);
    }

    public async Task<PromoVouchers?> GetVoucherByCodeAsync(string code)
    {
        return await _context.PromoVouchers
            .FirstOrDefaultAsync(v => v.Code.ToUpper() == code.Trim().ToUpper());
    }

    public async Task<PromoVouchers> AddVoucherAsync(PromoVouchers voucher)
    {
        await _context.PromoVouchers.AddAsync(voucher);
        return voucher;
    }

    public Task UpdateVoucherAsync(PromoVouchers voucher)
    {
        voucher.UpdatedAt = DateTime.UtcNow;
        _context.PromoVouchers.Update(voucher);
        return Task.CompletedTask;
    }

    public Task DeleteVoucherAsync(PromoVouchers voucher)
    {
        _context.PromoVouchers.Remove(voucher);
        return Task.CompletedTask;
    }

    public async Task AddUsageAsync(PromoVoucherUsages usage)
    {
        await _context.PromoVoucherUsages.AddAsync(usage);
    }

    public async Task<int> GetUserUsageCountAsync(Guid voucherId, Guid userId)
    {
        return await _context.PromoVoucherUsages
            .CountAsync(u => u.VoucherId == voucherId && u.UserId == userId);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}

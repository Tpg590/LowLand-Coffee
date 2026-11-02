using Order.API.Data;
using Order.API.Entities;

namespace Order.API.Repositories;

/// <summary>
/// Repository thực thi các thao tác lưu trữ và cập nhật giao dịch Thanh toán (OrderPayment) thông qua OrderDbContext.
/// </summary>
public class PaymentRepository : IPaymentRepository
{
    private readonly OrderDbContext _context;

    public PaymentRepository(OrderDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Thêm mới một bản ghi thanh toán (OrderPayment) vào CSDL và lưu thay đổi.
    /// </summary>
    public async Task<OrderPayment> AddAsync(OrderPayment payment, CancellationToken cancellationToken = default)
    {
        await _context.OrderPayments.AddAsync(payment, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return payment;
    }

    /// <summary>
    /// Cập nhật bản ghi thanh toán (OrderPayment) vào CSDL và lưu thay đổi.
    /// </summary>
    public async Task UpdateAsync(OrderPayment payment, CancellationToken cancellationToken = default)
    {
        _context.OrderPayments.Update(payment);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

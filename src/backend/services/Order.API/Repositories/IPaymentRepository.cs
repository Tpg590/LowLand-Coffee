using Order.API.Entities;

namespace Order.API.Repositories;

/// <summary>
/// Interface Repository xử lý truy xuất dữ liệu Thanh toán (OrderPayment) từ Database.
/// </summary>
public interface IPaymentRepository
{
    Task<OrderPayment> AddAsync(OrderPayment payment, CancellationToken cancellationToken = default);
    Task UpdateAsync(OrderPayment payment, CancellationToken cancellationToken = default);
}


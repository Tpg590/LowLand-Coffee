using Order.API.DTOs;

namespace Order.API.Repositories;

/// <summary>
/// Interface Repository xử lý truy xuất dữ liệu Đơn hàng (Order) từ Database.
/// </summary>
public interface IOrderRepository
{
    Task<bool> ExistsByOrderCodeAsync(string orderCode, CancellationToken cancellationToken = default);
    Task<(List<Entities.Order> Items, int TotalCount)> GetFilteredAsync(Guid? userId, OrderFilterRequest filter, CancellationToken cancellationToken = default);
    Task<Entities.Order?> GetByIdWithDetailsAsync(Guid orderId, bool asNoTracking = false, CancellationToken cancellationToken = default);
    Task<Entities.Order?> GetByOrderCodeWithDetailsAsync(string orderCode, CancellationToken cancellationToken = default);
    Task<Entities.Order> AddAsync(Entities.Order order, CancellationToken cancellationToken = default);
    Task UpdateAsync(Entities.Order order, CancellationToken cancellationToken = default);
}


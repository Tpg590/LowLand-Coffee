using Microsoft.EntityFrameworkCore;
using Order.API.Data;
using Order.API.DTOs;
using Order.API.Entities;

namespace Order.API.Repositories;

/// <summary>
/// Repository thực thi các thao tác truy vấn và lưu trữ Đơn hàng (Order) thông qua OrderDbContext.
/// </summary>
public class OrderRepository : IOrderRepository
{
    private readonly OrderDbContext _context;

    public OrderRepository(OrderDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Kiểm tra xem mã đơn hàng (OrderCode) đã tồn tại trong bảng order_orders hay chưa.
    /// </summary>
    public async Task<bool> ExistsByOrderCodeAsync(string orderCode, CancellationToken cancellationToken = default)
    {
        return await _context.Orders.AnyAsync(o => o.OrderCode == orderCode, cancellationToken);
    }

    /// <summary>
    /// Truy vấn danh sách đơn hàng có phân trang, hỗ trợ lọc theo UserId (cho Customer),
    /// trạng thái đơn hàng (Status), từ khóa mã đơn/tên khách (OrderCode) và khoảng thời gian.
    /// </summary>
    public async Task<(List<Entities.Order> Items, int TotalCount)> GetFilteredAsync(
        Guid? userId,
        OrderFilterRequest filter,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Orders
            .Include(o => o.OrderItems)
            .Include(o => o.OrderPayments)
            .AsNoTracking()
            .AsQueryable();

        // Nếu có truyền userId (Customer xem đơn cá nhân), chỉ lọc đơn của chính userId đó
        if (userId.HasValue)
        {
            query = query.Where(o => o.UserId == userId.Value);
        }

        // Lọc theo trạng thái đơn hàng (PENDING, PROCESSING, SHIPPING, COMPLETED, CANCELLED)
        if (!string.IsNullOrWhiteSpace(filter.Status))
        {
            var statusNormalized = OrderStatus.Normalize(filter.Status);
            query = query.Where(o => o.Status == statusNormalized);
        }

        // Tìm kiếm theo mã đơn hàng (OrderCode), tên khách hàng (CustomerName) hoặc SĐT khách hàng trong Note
        if (!string.IsNullOrWhiteSpace(filter.OrderCode))
        {
            var keyword = filter.OrderCode.Trim();
            query = query.Where(o => o.OrderCode.Contains(keyword) ||
                                     (o.CustomerName != null && o.CustomerName.Contains(keyword)) ||
                                     (o.Note != null && o.Note.Contains(keyword)));
        }

        // Lọc theo khoảng ngày tạo đơn
        if (filter.FromDate.HasValue)
        {
            query = query.Where(o => o.CreatedAt >= filter.FromDate.Value);
        }

        if (filter.ToDate.HasValue)
        {
            query = query.Where(o => o.CreatedAt <= filter.ToDate.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var pageNumber = filter.PageNumber > 0 ? filter.PageNumber : 1;
        var pageSize = filter.PageSize > 0 ? filter.PageSize : 10;

        var items = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    /// <summary>
    /// Lấy thông tin chi tiết của một đơn hàng theo Id (kèm danh sách món OrderItems và thanh toán OrderPayments).
    /// </summary>
    public async Task<Entities.Order?> GetByIdWithDetailsAsync(
        Guid orderId,
        bool asNoTracking = false,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Orders
            .Include(o => o.OrderItems)
            .Include(o => o.OrderPayments)
            .AsQueryable();

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);
    }

    /// <summary>
    /// Lấy thông tin chi tiết của một đơn hàng theo mã OrderCode (phục vụ đối soát VNPay IPN / Return).
    /// </summary>
    public async Task<Entities.Order?> GetByOrderCodeWithDetailsAsync(
        string orderCode,
        CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .Include(o => o.OrderItems)
            .Include(o => o.OrderPayments)
            .FirstOrDefaultAsync(o => o.OrderCode == orderCode, cancellationToken);
    }

    /// <summary>
    /// Thêm mới đơn hàng vào Database và lưu thay đổi.
    /// </summary>
    public async Task<Entities.Order> AddAsync(Entities.Order order, CancellationToken cancellationToken = default)
    {
        await _context.Orders.AddAsync(order, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return order;
    }

    /// <summary>
    /// Cập nhật đơn hàng hiện có trong Database và lưu thay đổi.
    /// </summary>
    public async Task UpdateAsync(Entities.Order order, CancellationToken cancellationToken = default)
    {
        order.UpdatedAt = DateTime.UtcNow;
        _context.Orders.Update(order);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

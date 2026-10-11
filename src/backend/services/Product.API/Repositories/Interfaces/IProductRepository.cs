using Product.API.DTOs;
using Product.API.Entities;

namespace Product.API.Repositories.Interfaces;

public interface IProductRepository
{
    Task<(IEnumerable<Entities.Product> Items, int TotalCount)> GetFilteredAsync(ProductFilterRequest filter, CancellationToken cancellationToken = default);
    Task<Entities.Product?> GetByIdAsync(Guid id, bool includeDeleted = false, CancellationToken cancellationToken = default);
    Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task<Entities.Product> AddAsync(Entities.Product product, CancellationToken cancellationToken = default);
    Task UpdateAsync(Entities.Product product, CancellationToken cancellationToken = default);
    Task<bool> SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ToggleStockAsync(Guid id, bool? isAvailable = null, CancellationToken cancellationToken = default);
}

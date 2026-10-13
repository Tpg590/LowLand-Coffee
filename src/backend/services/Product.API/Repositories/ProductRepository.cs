using Microsoft.EntityFrameworkCore;
using Product.API.Data;
using Product.API.DTOs;
using Product.API.Entities;
using Product.API.Repositories.Interfaces;

namespace Product.API.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly ProductDbContext _context;

    public ProductRepository(ProductDbContext context)
    {
        _context = context;
    }

    public async Task<(IEnumerable<Entities.Product> Items, int TotalCount)> GetFilteredAsync(ProductFilterRequest filter, CancellationToken cancellationToken = default)
    {
        var query = _context.Products
            .Include(p => p.Category)
            .Include(p => p.Options)
            .Where(p => !p.IsDeleted)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            var keyword = filter.Keyword.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(keyword) || (p.Description != null && p.Description.ToLower().Contains(keyword)));
        }

        if (filter.CategoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == filter.CategoryId.Value);
        }

        if (filter.MinPrice.HasValue)
        {
            query = query.Where(p => p.BasePrice >= filter.MinPrice.Value);
        }

        if (filter.MaxPrice.HasValue)
        {
            query = query.Where(p => p.BasePrice <= filter.MaxPrice.Value);
        }

        if (filter.IsAvailable.HasValue)
        {
            query = query.Where(p => p.IsAvailable == filter.IsAvailable.Value);
        }

        // Sorting
        query = (filter.SortBy?.ToLower()) switch
        {
            "name" => filter.IsDescending ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name),
            "price" => filter.IsDescending ? query.OrderByDescending(p => p.BasePrice) : query.OrderBy(p => p.BasePrice),
            _ => filter.IsDescending ? query.OrderByDescending(p => p.CreatedAt) : query.OrderBy(p => p.CreatedAt)
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var pageNumber = filter.PageNumber > 0 ? filter.PageNumber : 1;
        var pageSize = filter.PageSize > 0 ? filter.PageSize : 10;
        var skip = (pageNumber - 1) * pageSize;

        var items = await query.Skip(skip).Take(pageSize).ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<Entities.Product?> GetByIdAsync(Guid id, bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        var query = _context.Products
            .Include(p => p.Category)
            .Include(p => p.Options)
            .AsQueryable();

        if (!includeDeleted)
        {
            query = query.Where(p => !p.IsDeleted);
        }

        return await query.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Products.Where(p => !p.IsDeleted);

        if (excludeId.HasValue)
        {
            query = query.Where(p => p.Id != excludeId.Value);
        }

        return await query.AnyAsync(p => p.Name.ToLower() == name.Trim().ToLower(), cancellationToken);
    }

    public async Task<Entities.Product> AddAsync(Entities.Product product, CancellationToken cancellationToken = default)
    {
        product.CreatedAt = DateTime.UtcNow;
        product.UpdatedAt = DateTime.UtcNow;

        await _context.Products.AddAsync(product, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return product;
    }

    public async Task UpdateAsync(Entities.Product product, CancellationToken cancellationToken = default)
    {
        product.UpdatedAt = DateTime.UtcNow;
        _context.Products.Update(product);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, cancellationToken);
        if (product == null)
        {
            return false;
        }

        product.IsDeleted = true;
        product.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> ToggleStockAsync(Guid id, bool? isAvailable = null, CancellationToken cancellationToken = default)
    {
        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, cancellationToken);
        if (product == null)
        {
            return false;
        }

        product.IsAvailable = isAvailable ?? !product.IsAvailable;
        product.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}

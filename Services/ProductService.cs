using Microsoft.EntityFrameworkCore;
using WMS.Data;
using WMS.Models;

namespace WMS.Services;

public class ProductService
{
    private readonly IDbContextFactory<WmsDbContext> _contextFactory;

    public ProductService(IDbContextFactory<WmsDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<Product>> GetAllAsync(string? search = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.Products.Include(p => p.Category).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(s) ||
                p.Article.ToLower().Contains(s) ||
                (p.Barcode != null && p.Barcode.ToLower().Contains(s)));
        }

        return await query.OrderBy(p => p.Name).ToListAsync();
    }

    public async Task<(bool Success, string? Error)> AddAsync(Product product)
    {
        var validationError = Validate(product);
        if (validationError != null)
        {
            return (false, validationError);
        }

        await using var context = await _contextFactory.CreateDbContextAsync();

        if (await context.Products.AnyAsync(p => p.Article == product.Article))
        {
            return (false, "Товар с таким артикулом уже существует.");
        }

        context.Products.Add(new Product
        {
            Name = product.Name.Trim(),
            Article = product.Article.Trim(),
            Barcode = string.IsNullOrWhiteSpace(product.Barcode) ? null : product.Barcode.Trim(),
            CategoryId = product.CategoryId,
            Unit = product.Unit.Trim(),
            Description = string.IsNullOrWhiteSpace(product.Description) ? null : product.Description.Trim()
        });

        await context.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> UpdateAsync(Product product)
    {
        var validationError = Validate(product);
        if (validationError != null)
        {
            return (false, validationError);
        }

        await using var context = await _contextFactory.CreateDbContextAsync();

        if (await context.Products.AnyAsync(p => p.Article == product.Article && p.Id != product.Id))
        {
            return (false, "Товар с таким артикулом уже существует.");
        }

        var existing = await context.Products.FindAsync(product.Id);
        if (existing == null)
        {
            return (false, "Товар не найден.");
        }

        existing.Name = product.Name.Trim();
        existing.Article = product.Article.Trim();
        existing.Barcode = string.IsNullOrWhiteSpace(product.Barcode) ? null : product.Barcode.Trim();
        existing.CategoryId = product.CategoryId;
        existing.Unit = product.Unit.Trim();
        existing.Description = string.IsNullOrWhiteSpace(product.Description) ? null : product.Description.Trim();

        await context.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var product = await context.Products.FindAsync(id);
        if (product == null)
        {
            return (false, "Товар не найден.");
        }

        context.Products.Remove(product);

        try
        {
            await context.SaveChangesAsync();
            return (true, null);
        }
        catch (DbUpdateException)
        {
            return (false, "Невозможно удалить товар: он используется в остатках или операциях.");
        }
    }

    private static string? Validate(Product product)
    {
        if (string.IsNullOrWhiteSpace(product.Name))
        {
            return "Название товара обязательно для заполнения.";
        }

        if (string.IsNullOrWhiteSpace(product.Article))
        {
            return "Артикул товара обязателен для заполнения.";
        }

        if (string.IsNullOrWhiteSpace(product.Unit))
        {
            return "Единица измерения обязательна для заполнения.";
        }

        if (product.CategoryId <= 0)
        {
            return "Необходимо выбрать категорию.";
        }

        return null;
    }
}
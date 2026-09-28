using Microsoft.EntityFrameworkCore;
using WMS.Data;
using WMS.Models;

namespace WMS.Services;

public class CategoryService
{
    private readonly IDbContextFactory<WmsDbContext> _contextFactory;

    public CategoryService(IDbContextFactory<WmsDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<Category>> GetAllAsync(string? search = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.Categories.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(c => c.Name.ToLower().Contains(search.ToLower()));
        }

        return await query.OrderBy(c => c.Name).ToListAsync();
    }

    public async Task<(bool Success, string? Error)> AddAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return (false, "Название категории обязательно для заполнения.");
        }

        await using var context = await _contextFactory.CreateDbContextAsync();
        context.Categories.Add(new Category { Name = name.Trim() });
        await context.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> UpdateAsync(int id, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return (false, "Название категории обязательно для заполнения.");
        }

        await using var context = await _contextFactory.CreateDbContextAsync();
        var category = await context.Categories.FindAsync(id);
        if (category == null)
        {
            return (false, "Категория не найдена.");
        }

        category.Name = name.Trim();
        await context.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var category = await context.Categories.FindAsync(id);
        if (category == null)
        {
            return (false, "Категория не найдена.");
        }

        context.Categories.Remove(category);

        try
        {
            await context.SaveChangesAsync();
            return (true, null);
        }
        catch (DbUpdateException)
        {
            return (false, "Невозможно удалить категорию: она используется в товарах.");
        }
    }
}
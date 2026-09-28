using Microsoft.EntityFrameworkCore;
using WMS.Data;
using WMS.Models;

namespace WMS.Services;

public class WarehouseService
{
    private readonly IDbContextFactory<WmsDbContext> _contextFactory;

    public WarehouseService(IDbContextFactory<WmsDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<Warehouse>> GetAllAsync(string? search = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.Warehouses.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            query = query.Where(w => w.Name.ToLower().Contains(s));
        }

        return await query.OrderBy(w => w.Name).ToListAsync();
    }

    public async Task<(bool Success, string? Error)> AddAsync(string name, string? address)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return (false, "Название склада обязательно для заполнения.");
        }

        await using var context = await _contextFactory.CreateDbContextAsync();
        context.Warehouses.Add(new Warehouse { Name = name.Trim(), Address = string.IsNullOrWhiteSpace(address) ? null : address.Trim() });
        await context.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> UpdateAsync(int id, string name, string? address)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return (false, "Название склада обязательно для заполнения.");
        }

        await using var context = await _contextFactory.CreateDbContextAsync();
        var warehouse = await context.Warehouses.FindAsync(id);
        if (warehouse == null)
        {
            return (false, "Склад не найден.");
        }

        warehouse.Name = name.Trim();
        warehouse.Address = string.IsNullOrWhiteSpace(address) ? null : address.Trim();
        await context.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var warehouse = await context.Warehouses.FindAsync(id);
        if (warehouse == null)
        {
            return (false, "Склад не найден.");
        }

        context.Warehouses.Remove(warehouse);

        try
        {
            await context.SaveChangesAsync();
            return (true, null);
        }
        catch (DbUpdateException)
        {
            return (false, "Невозможно удалить склад: он используется в ячейках или операциях.");
        }
    }
}
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
        var query = context.Warehouses.Include(w => w.Employees).AsQueryable();

        if (CurrentSession.WarehouseScopeId is int scopeId)
        {
            query = query.Where(w => w.Id == scopeId);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            query = query.Where(w =>
                w.Name.ToLower().Contains(s) ||
                (w.Address != null && w.Address.ToLower().Contains(s)));
        }

        return await query.OrderBy(w => w.Name).ToListAsync();
    }

    public async Task<(bool Success, string? Error)> AddAsync(Warehouse warehouse)
    {
        var validationError = Validate(warehouse);
        if (validationError != null)
        {
            return (false, validationError);
        }

        await using var context = await _contextFactory.CreateDbContextAsync();

        context.Warehouses.Add(new Warehouse
        {
            Name = warehouse.Name.Trim(),
            Address = Clean(warehouse.Address),
            Area = warehouse.Area,
            Purpose = Clean(warehouse.Purpose),
            WorkingHours = Clean(warehouse.WorkingHours),
            Phone = Clean(warehouse.Phone),
            Email = Clean(warehouse.Email)
        });

        await context.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> UpdateAsync(Warehouse warehouse)
    {
        var validationError = Validate(warehouse);
        if (validationError != null)
        {
            return (false, validationError);
        }

        await using var context = await _contextFactory.CreateDbContextAsync();
        var existing = await context.Warehouses.FindAsync(warehouse.Id);
        if (existing == null)
        {
            return (false, "Склад не найден.");
        }

        existing.Name = warehouse.Name.Trim();
        existing.Address = Clean(warehouse.Address);
        existing.Area = warehouse.Area;
        existing.Purpose = Clean(warehouse.Purpose);
        existing.WorkingHours = Clean(warehouse.WorkingHours);
        existing.Phone = Clean(warehouse.Phone);
        existing.Email = Clean(warehouse.Email);

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

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? Validate(Warehouse warehouse)
    {
        if (string.IsNullOrWhiteSpace(warehouse.Name))
        {
            return "Название склада обязательно для заполнения.";
        }

        if (warehouse.Area is <= 0)
        {
            return "Площадь склада должна быть больше нуля.";
        }

        if (!string.IsNullOrWhiteSpace(warehouse.Email) && !warehouse.Email.Contains('@'))
        {
            return "Email указан некорректно.";
        }

        return null;
    }
}
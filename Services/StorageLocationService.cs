using Microsoft.EntityFrameworkCore;
using WMS.Data;
using WMS.Models;

namespace WMS.Services;

public class StorageLocationService
{
    private readonly IDbContextFactory<WmsDbContext> _contextFactory;

    public StorageLocationService(IDbContextFactory<WmsDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<StorageLocation>> GetAllAsync(string? search = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.StorageLocations.Include(l => l.Warehouse).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            query = query.Where(l => l.Code.ToLower().Contains(s));
        }

        return await query.OrderBy(l => l.Warehouse!.Name).ThenBy(l => l.Code).ToListAsync();
    }

    public async Task<(bool Success, string? Error)> AddAsync(int warehouseId, string code, int capacity)
    {
        var validationError = Validate(warehouseId, code, capacity);
        if (validationError != null)
        {
            return (false, validationError);
        }

        await using var context = await _contextFactory.CreateDbContextAsync();

        if (await context.StorageLocations.AnyAsync(l => l.WarehouseId == warehouseId && l.Code == code.Trim()))
        {
            return (false, "Ячейка с таким кодом уже существует на этом складе.");
        }

        context.StorageLocations.Add(new StorageLocation
        {
            WarehouseId = warehouseId,
            Code = code.Trim(),
            Capacity = capacity
        });

        await context.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> UpdateAsync(int id, int warehouseId, string code, int capacity)
    {
        var validationError = Validate(warehouseId, code, capacity);
        if (validationError != null)
        {
            return (false, validationError);
        }

        await using var context = await _contextFactory.CreateDbContextAsync();

        if (await context.StorageLocations.AnyAsync(l => l.WarehouseId == warehouseId && l.Code == code.Trim() && l.Id != id))
        {
            return (false, "Ячейка с таким кодом уже существует на этом складе.");
        }

        var existing = await context.StorageLocations.FindAsync(id);
        if (existing == null)
        {
            return (false, "Ячейка не найдена.");
        }

        existing.WarehouseId = warehouseId;
        existing.Code = code.Trim();
        existing.Capacity = capacity;

        await context.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var location = await context.StorageLocations.FindAsync(id);
        if (location == null)
        {
            return (false, "Ячейка не найдена.");
        }

        context.StorageLocations.Remove(location);

        try
        {
            await context.SaveChangesAsync();
            return (true, null);
        }
        catch (DbUpdateException)
        {
            return (false, "Невозможно удалить ячейку: в ней есть остатки товаров или связанные операции.");
        }
    }

    private static string? Validate(int warehouseId, string code, int capacity)
    {
        if (warehouseId <= 0)
        {
            return "Необходимо выбрать склад.";
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            return "Код ячейки обязателен для заполнения.";
        }

        if (capacity < 0)
        {
            return "Вместимость ячейки не может быть отрицательной.";
        }

        return null;
    }
}
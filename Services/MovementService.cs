using Microsoft.EntityFrameworkCore;
using WMS.Data;
using WMS.Models;

namespace WMS.Services;

public class MovementService
{
    private readonly IDbContextFactory<WmsDbContext> _contextFactory;
    private readonly InventoryService _inventoryService;

    public MovementService(IDbContextFactory<WmsDbContext> contextFactory, InventoryService inventoryService)
    {
        _contextFactory = contextFactory;
        _inventoryService = inventoryService;
    }

    /// Товары, у которых есть хотя бы один остаток больше нуля
    public async Task<List<ProductStockOption>> GetProductsWithStockAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context.Inventories
            .Where(i => i.Quantity > 0)
            .Select(i => i.Product!)
            .Distinct()
            .OrderBy(p => p.Name)
            .Select(p => new ProductStockOption { ProductId = p.Id, Name = p.Name, Article = p.Article })
            .ToListAsync();
    }

    /// Ячейки, где у конкретного товара есть остаток
    public async Task<List<SourceLocationOption>> GetSourceLocationsAsync(int productId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context.Inventories
            .Where(i => i.ProductId == productId && i.Quantity > 0)
            .OrderBy(i => i.StorageLocation!.Code)
            .Select(i => new SourceLocationOption
            {
                LocationId = i.StorageLocationId,
                Code = i.StorageLocation!.Code,
                Available = i.Quantity
            })
            .ToListAsync();
    }

    /// Все ячейки того же склада, что и исходная ячейка (кроме самой исходной), с заполненностью
    public async Task<List<LocationOption>> GetTargetLocationsAsync(int fromLocationId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var fromLocation = await context.StorageLocations.FindAsync(fromLocationId);
        if (fromLocation == null)
        {
            return new List<LocationOption>();
        }

        var locations = await context.StorageLocations
            .Where(l => l.WarehouseId == fromLocation.WarehouseId && l.Id != fromLocationId)
            .OrderBy(l => l.Code)
            .ToListAsync();

        var usage = await context.Inventories
            .Where(i => i.StorageLocation!.WarehouseId == fromLocation.WarehouseId)
            .GroupBy(i => i.StorageLocationId)
            .Select(g => new { LocationId = g.Key, Total = g.Sum(x => x.Quantity) })
            .ToDictionaryAsync(x => x.LocationId, x => x.Total);

        return locations.Select(l => new LocationOption
        {
            Id = l.Id,
            Code = l.Code,
            Capacity = l.Capacity,
            Used = usage.GetValueOrDefault(l.Id)
        }).ToList();
    }

    public async Task<(bool Success, string? Error)> MoveAsync(int productId, int fromLocationId, int toLocationId, int quantity)
    {
        if (quantity <= 0)
        {
            return (false, "Количество должно быть больше нуля.");
        }

        if (fromLocationId <= 0 || toLocationId <= 0)
        {
            return (false, "Необходимо выбрать исходную и целевую ячейку.");
        }

        if (fromLocationId == toLocationId)
        {
            return (false, "Исходная и целевая ячейка не могут совпадать.");
        }

        await using var context = await _contextFactory.CreateDbContextAsync();

        var fromLocation = await context.StorageLocations.FindAsync(fromLocationId);
        var toLocation = await context.StorageLocations.FindAsync(toLocationId);

        if (fromLocation == null || toLocation == null)
        {
            return (false, "Ячейка не найдена.");
        }

        if (fromLocation.WarehouseId != toLocation.WarehouseId)
        {
            return (false, "Перемещение возможно только в пределах одного склада.");
        }

        var available = await _inventoryService.GetQuantityAsync(context, productId, fromLocationId);
        if (quantity > available)
        {
            return (false, $"Нельзя переместить больше, чем есть в ячейке {fromLocation.Code}: {available}.");
        }

        var usedInTarget = await context.Inventories
            .Where(i => i.StorageLocationId == toLocationId)
            .SumAsync(i => i.Quantity);

        if (usedInTarget + quantity > toLocation.Capacity)
        {
            var free = Math.Max(0, toLocation.Capacity - usedInTarget);
            return (false, $"В ячейке {toLocation.Code} недостаточно места: свободно {free}.");
        }

        // Списание, зачисление и запись в историю - одной транзакцией
        await _inventoryService.DecreaseAsync(context, productId, fromLocationId, quantity);
        await _inventoryService.IncreaseAsync(context, productId, toLocationId, quantity);

        context.StockMovements.Add(new StockMovement
        {
            ProductId = productId,
            FromLocationId = fromLocationId,
            ToLocationId = toLocationId,
            Quantity = quantity,
            OperationType = OperationType.Movement,
            Date = DateTime.UtcNow
        });

        await context.SaveChangesAsync();
        return (true, null);
    }
}
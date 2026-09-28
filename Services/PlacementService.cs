using Microsoft.EntityFrameworkCore;
using WMS.Data;
using WMS.Models;

namespace WMS.Services;

public class PlacementService
{
    private readonly IDbContextFactory<WmsDbContext> _contextFactory;
    private readonly InventoryService _inventoryService;

    public PlacementService(IDbContextFactory<WmsDbContext> contextFactory, InventoryService inventoryService)
    {
        _contextFactory = contextFactory;
        _inventoryService = inventoryService;
    }

    /// Позиции подтверждённых приёмок, которые ещё не полностью размещены
    public async Task<List<PendingPlacementItem>> GetPendingAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context.ReceiptItems
            .Where(i => i.Receipt!.Status == ReceiptStatus.Confirmed && i.Quantity > i.PlacedQuantity)
            .OrderBy(i => i.Receipt!.Date)
            .Select(i => new PendingPlacementItem
            {
                ReceiptItemId = i.Id,
                ReceiptNumber = i.Receipt!.DocumentNumber,
                ReceiptDate = i.Receipt!.Date,
                WarehouseId = i.Receipt!.WarehouseId,
                WarehouseName = i.Receipt!.Warehouse!.Name,
                ProductId = i.ProductId,
                ProductName = i.Product!.Name,
                Article = i.Product!.Article,
                Quantity = i.Quantity,
                PlacedQuantity = i.PlacedQuantity
            })
            .ToListAsync();
    }

    /// Ячейки склада вместе с текущей заполненностью
    public async Task<List<LocationOption>> GetLocationsAsync(int warehouseId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var locations = await context.StorageLocations
            .Where(l => l.WarehouseId == warehouseId)
            .OrderBy(l => l.Code)
            .ToListAsync();

        var usage = await context.Inventories
            .Where(i => i.StorageLocation!.WarehouseId == warehouseId)
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

    public async Task<(bool Success, string? Error)> PlaceAsync(int receiptItemId, int locationId, int quantity)
    {
        if (quantity <= 0)
        {
            return (false, "Количество должно быть больше нуля.");
        }

        if (locationId <= 0)
        {
            return (false, "Выберите ячейку для размещения.");
        }

        await using var context = await _contextFactory.CreateDbContextAsync();

        var item = await context.ReceiptItems
            .Include(i => i.Receipt)
            .FirstOrDefaultAsync(i => i.Id == receiptItemId);

        if (item == null)
        {
            return (false, "Позиция приёмки не найдена.");
        }

        var remaining = item.Quantity - item.PlacedQuantity;
        if (quantity > remaining)
        {
            return (false, $"Нельзя разместить больше, чем осталось: {remaining}.");
        }

        var location = await context.StorageLocations.FindAsync(locationId);
        if (location == null)
        {
            return (false, "Ячейка не найдена.");
        }

        if (location.WarehouseId != item.Receipt!.WarehouseId)
        {
            return (false, "Ячейка находится на другом складе, чем документ приёмки.");
        }

        var used = await context.Inventories
            .Where(i => i.StorageLocationId == locationId)
            .SumAsync(i => i.Quantity);

        if (used + quantity > location.Capacity)
        {
            var free = Math.Max(0, location.Capacity - used);
            return (false, $"В ячейке {location.Code} недостаточно места: свободно {free}.");
        }

        // Все три изменения сохраняются одним SaveChanges - то есть в одной транзакции
        await _inventoryService.IncreaseAsync(context, item.ProductId, locationId, quantity);

        item.PlacedQuantity += quantity;

        context.StockMovements.Add(new StockMovement
        {
            ProductId = item.ProductId,
            FromLocationId = null,
            ToLocationId = locationId,
            Quantity = quantity,
            OperationType = OperationType.Placement,
            Date = DateTime.UtcNow
        });

        await context.SaveChangesAsync();
        return (true, null);
    }
}
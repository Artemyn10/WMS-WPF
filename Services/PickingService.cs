using Microsoft.EntityFrameworkCore;
using WMS.Data;
using WMS.Models;

namespace WMS.Services;

public class PickingService
{
    private readonly IDbContextFactory<WmsDbContext> _contextFactory;
    private readonly InventoryService _inventoryService;

    public PickingService(IDbContextFactory<WmsDbContext> contextFactory, InventoryService inventoryService)
    {
        _contextFactory = contextFactory;
        _inventoryService = inventoryService;
    }

    /// Заказы, у которых остались несобранные позиции (New или Assembling)
    public async Task<List<Order>> GetOrdersPendingPickingAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context.Orders
            .Include(o => o.Customer)
            .Include(o => o.Warehouse)
            .Where(o => (o.Status == OrderStatus.New || o.Status == OrderStatus.Assembling)
                        && o.Items.Any(i => i.Quantity > i.PickedQuantity))
            .OrderBy(o => o.Date)
            .ToListAsync();
    }

    /// Несобранные позиции конкретного заказа
    public async Task<List<PendingPickingItem>> GetPendingItemsAsync(int orderId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context.OrderItems
            .Where(i => i.OrderId == orderId && i.Quantity > i.PickedQuantity)
            .Select(i => new PendingPickingItem
            {
                OrderItemId = i.Id,
                ProductId = i.ProductId,
                ProductName = i.Product!.Name,
                Article = i.Product!.Article,
                Quantity = i.Quantity,
                PickedQuantity = i.PickedQuantity
            })
            .ToListAsync();
    }

    /// Ячейки склада заказа, где есть остаток конкретного товара
    public async Task<List<SourceLocationOption>> GetSourceLocationsAsync(int warehouseId, int productId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context.Inventories
            .Where(i => i.ProductId == productId && i.Quantity > 0 && i.StorageLocation!.WarehouseId == warehouseId)
            .OrderBy(i => i.StorageLocation!.Code)
            .Select(i => new SourceLocationOption
            {
                LocationId = i.StorageLocationId,
                Code = i.StorageLocation!.Code,
                Available = i.Quantity
            })
            .ToListAsync();
    }

    public async Task<(bool Success, string? Error, bool OrderCompleted)> PickAsync(int orderItemId, int locationId, int quantity)
    {
        if (quantity <= 0)
        {
            return (false, "Количество должно быть больше нуля.", false);
        }

        if (locationId <= 0)
        {
            return (false, "Выберите ячейку для сборки.", false);
        }

        await using var context = await _contextFactory.CreateDbContextAsync();

        var item = await context.OrderItems
            .Include(i => i.Order)
                .ThenInclude(o => o!.Items)
            .FirstOrDefaultAsync(i => i.Id == orderItemId);

        if (item == null)
        {
            return (false, "Позиция заказа не найдена.", false);
        }

        var remaining = item.Quantity - item.PickedQuantity;
        if (quantity > remaining)
        {
            return (false, $"Нельзя собрать больше, чем осталось: {remaining}.", false);
        }

        var location = await context.StorageLocations.FindAsync(locationId);
        if (location == null)
        {
            return (false, "Ячейка не найдена.", false);
        }

        if (location.WarehouseId != item.Order!.WarehouseId)
        {
            return (false, "Ячейка находится на другом складе, чем склад заказа.", false);
        }

        var available = await _inventoryService.GetQuantityAsync(context, item.ProductId, locationId);
        if (quantity > available)
        {
            return (false, $"В ячейке {location.Code} недостаточно товара: доступно {available}.", false);
        }

        // Списание, счётчик собранного и история - одной транзакцией
        await _inventoryService.DecreaseAsync(context, item.ProductId, locationId, quantity);

        item.PickedQuantity += quantity;

        context.StockMovements.Add(new StockMovement
        {
            ProductId = item.ProductId,
            FromLocationId = locationId,
            ToLocationId = null,
            Quantity = quantity,
            OperationType = OperationType.Picking,
            Date = DateTime.UtcNow
        });

        // Если это первая собранная позиция заказа - переводим его в статус "в сборке"
        if (item.Order.Status == OrderStatus.New)
        {
            item.Order.Status = OrderStatus.Assembling;
        }

        // Проверяем, все ли позиции заказа теперь полностью собраны
        var orderCompleted = item.Order.Items.All(i => i.Id == item.Id
            ? item.PickedQuantity >= item.Quantity
            : i.PickedQuantity >= i.Quantity);

        if (orderCompleted)
        {
            item.Order.Status = OrderStatus.Assembled;
        }

        await context.SaveChangesAsync();
        return (true, null, orderCompleted);
    }
}
using Microsoft.EntityFrameworkCore;
using WMS.Data;
using WMS.Models;

namespace WMS.Services;

public class InventoryService
{
    private readonly IDbContextFactory<WmsDbContext> _contextFactory;

    public InventoryService(IDbContextFactory<WmsDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    /// Остатки для раздела "Остатки": товар в ячейках плюс товар из подтверждённых
    /// поступлений, который ещё не размещён (он показывается в строке "Зона приёмки").
    public async Task<List<InventoryRow>> GetRowsAsync(string? productSearch = null, string? locationSearch = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var placedQuery = context.Inventories
            .Include(i => i.Product)
            .Include(i => i.StorageLocation)
                .ThenInclude(l => l!.Warehouse)
            .Where(i => i.Quantity > 0)
            .AsQueryable();

        var pendingQuery = context.ReceiptItems
            .Include(i => i.Product)
            .Include(i => i.Receipt)
                .ThenInclude(r => r!.Warehouse)
            .Where(i => i.Receipt!.Status == ReceiptStatus.Confirmed && i.Quantity > i.PlacedQuantity)
            .AsQueryable();

        if (CurrentSession.WarehouseScopeId is int scopeId)
        {
            placedQuery = placedQuery.Where(i => i.StorageLocation!.WarehouseId == scopeId);
            pendingQuery = pendingQuery.Where(i => i.Receipt!.WarehouseId == scopeId);
        }

        if (!string.IsNullOrWhiteSpace(productSearch))
        {
            var search = productSearch.Trim().ToLowerInvariant();
            placedQuery = placedQuery.Where(i =>
                i.Product!.Name.ToLower().Contains(search) ||
                i.Product.Article.ToLower().Contains(search));
            pendingQuery = pendingQuery.Where(i =>
                i.Product!.Name.ToLower().Contains(search) ||
                i.Product.Article.ToLower().Contains(search));
        }

        var includePending = true;
        if (!string.IsNullOrWhiteSpace(locationSearch))
        {
            var search = locationSearch.Trim().ToLowerInvariant();
            placedQuery = placedQuery.Where(i =>
                i.StorageLocation!.Code.ToLower().Contains(search));
            includePending = InventoryRow.PendingLocationName.ToLowerInvariant().Contains(search);
        }

        var placed = await placedQuery.ToListAsync();
        var pending = includePending
            ? await pendingQuery.ToListAsync()
            : new List<ReceiptItem>();

        var rows = placed.Select(i => new InventoryRow
        {
            ProductName = i.Product!.Name,
            Article = i.Product.Article,
            WarehouseName = i.StorageLocation!.Warehouse!.Name,
            LocationCode = i.StorageLocation.Code,
            Quantity = i.Quantity,
            IsPending = false
        }).ToList();

        rows.AddRange(pending
            .GroupBy(i => new { i.Receipt!.WarehouseId, i.ProductId })
            .Select(group =>
            {
                var first = group.First();
                return new InventoryRow
                {
                    ProductName = first.Product!.Name,
                    Article = first.Product.Article,
                    WarehouseName = first.Receipt!.Warehouse!.Name,
                    LocationCode = InventoryRow.PendingLocationName,
                    Quantity = group.Sum(i => i.Quantity - i.PlacedQuantity),
                    IsPending = true
                };
            }));

        return rows
            .OrderBy(row => row.ProductName)
            .ThenBy(row => row.IsPending)
            .ThenBy(row => row.WarehouseName)
            .ThenBy(row => row.LocationCode)
            .ToList();
    }

    /// Формирует ведомость остатков на конец выбранного локального календарного дня.
    public async Task<List<InventoryRow>> GetRowsOnDateAsync(
        DateTime selectedDate,
        string? productSearch = null,
        string? locationSearch = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var localEndOfDay = DateTime.SpecifyKind(
            selectedDate.Date.AddDays(1),
            DateTimeKind.Local);
        var endOfDayUtc = localEndOfDay.ToUniversalTime();

        var movementsQuery = context.StockMovements
            .Include(m => m.Product)
            .Include(m => m.Warehouse)
            .Include(m => m.FromLocation)
                .ThenInclude(location => location!.Warehouse)
            .Include(m => m.ToLocation)
                .ThenInclude(location => location!.Warehouse)
            .Where(m => m.Date < endOfDayUtc);

        var movements = await movementsQuery.ToListAsync();
        var warehouseScopeId = CurrentSession.WarehouseScopeId;

        var balances = new Dictionary<(int ProductId, int LocationId), int>();
        var pendingReceipts = new Dictionary<(int WarehouseId, int ProductId), int>();
        var pendingPlacements = new Dictionary<(int WarehouseId, int ProductId), int>();
        var products = new Dictionary<int, Product>();
        var locations = new Dictionary<int, StorageLocation>();
        var warehouses = new Dictionary<int, string>();

        int? GetWarehouseId(StockMovement movement)
        {
            return movement.WarehouseId
                ?? movement.ToLocation?.WarehouseId
                ?? movement.FromLocation?.WarehouseId;
        }

        void AddBalance(int productId, int locationId, int quantity)
        {
            var key = (productId, locationId);
            balances.TryGetValue(key, out var current);
            balances[key] = current + quantity;
        }

        void AddPending(
            Dictionary<(int WarehouseId, int ProductId), int> totals,
            int warehouseId,
            int productId,
            int quantity)
        {
            var key = (warehouseId, productId);
            totals.TryGetValue(key, out var current);
            totals[key] = current + quantity;
        }

        foreach (var movement in movements)
        {
            var warehouseId = GetWarehouseId(movement);

            if (warehouseScopeId is int scopeId && warehouseId != scopeId)
            {
                continue;
            }

            products[movement.ProductId] = movement.Product!;

            if (movement.Warehouse is not null)
            {
                warehouses[movement.WarehouseId ?? movement.Warehouse.Id] =
                    movement.Warehouse.Name;
            }

            if (movement.FromLocation is not null)
            {
                locations[movement.FromLocation.Id] = movement.FromLocation;
                if (movement.FromLocation.Warehouse is not null)
                {
                    warehouses[movement.FromLocation.WarehouseId] =
                        movement.FromLocation.Warehouse.Name;
                }
            }

            if (movement.ToLocation is not null)
            {
                locations[movement.ToLocation.Id] = movement.ToLocation;
                if (movement.ToLocation.Warehouse is not null)
                {
                    warehouses[movement.ToLocation.WarehouseId] =
                        movement.ToLocation.Warehouse.Name;
                }
            }

            switch (movement.OperationType)
            {
                case OperationType.Receipt:
                    if (movement.ToLocationId is int receivedLocationId)
                    {
                        // Поддержка старых записей, где первоначальное размещение
                        // было записано как поступление с указанной ячейкой.
                        AddBalance(
                            movement.ProductId,
                            receivedLocationId,
                            movement.Quantity);
                    }
                    else if (warehouseId is int receiptWarehouseId)
                    {
                        AddPending(
                            pendingReceipts,
                            receiptWarehouseId,
                            movement.ProductId,
                            movement.Quantity);
                    }

                    break;

                case OperationType.Placement:
                    if (movement.ToLocationId is int placedLocationId)
                    {
                        AddBalance(
                            movement.ProductId,
                            placedLocationId,
                            movement.Quantity);
                    }

                    if (warehouseId is int placementWarehouseId)
                    {
                        AddPending(
                            pendingPlacements,
                            placementWarehouseId,
                            movement.ProductId,
                            movement.Quantity);
                    }

                    break;

                case OperationType.Movement:
                    if (movement.FromLocationId is int fromLocationId)
                    {
                        AddBalance(
                            movement.ProductId,
                            fromLocationId,
                            -movement.Quantity);
                    }

                    if (movement.ToLocationId is int toLocationId)
                    {
                        AddBalance(
                            movement.ProductId,
                            toLocationId,
                            movement.Quantity);
                    }

                    break;

                case OperationType.Picking:
                case OperationType.WriteOff:
                    if (movement.FromLocationId is int decreasedLocationId)
                    {
                        AddBalance(
                            movement.ProductId,
                            decreasedLocationId,
                            -movement.Quantity);
                    }

                    break;

                case OperationType.StockTaking:
                    if (movement.FromLocationId is int shortageLocationId)
                    {
                        AddBalance(
                            movement.ProductId,
                            shortageLocationId,
                            -movement.Quantity);
                    }

                    if (movement.ToLocationId is int excessLocationId)
                    {
                        AddBalance(
                            movement.ProductId,
                            excessLocationId,
                            movement.Quantity);
                    }

                    break;

                case OperationType.Shipment:
                    // Товар уже вычтен из ячеек при комплектации.
                    break;
            }
        }

        var rows = new List<InventoryRow>();

        foreach (var balance in balances)
        {
            if (balance.Value == 0 ||
                !products.TryGetValue(balance.Key.ProductId, out var product) ||
                !locations.TryGetValue(balance.Key.LocationId, out var location))
            {
                continue;
            }

            rows.Add(new InventoryRow
            {
                ProductName = product.Name,
                Article = product.Article,
                WarehouseName = location.Warehouse?.Name ?? string.Empty,
                LocationCode = location.Code,
                Quantity = balance.Value,
                IsPending = false
            });
        }

        foreach (var receipt in pendingReceipts)
        {
            pendingPlacements.TryGetValue(receipt.Key, out var placedQuantity);
            var pendingQuantity = Math.Max(0, receipt.Value - placedQuantity);

            if (pendingQuantity == 0 ||
                !products.TryGetValue(receipt.Key.ProductId, out var product))
            {
                continue;
            }

            var warehouseName = warehouses.TryGetValue(
                receipt.Key.WarehouseId,
                out var name)
                ? name
                : string.Empty;

            rows.Add(new InventoryRow
            {
                ProductName = product.Name,
                Article = product.Article,
                WarehouseName = warehouseName,
                LocationCode = InventoryRow.PendingLocationName,
                Quantity = pendingQuantity,
                IsPending = true
            });
        }

        if (!string.IsNullOrWhiteSpace(productSearch))
        {
            var search = productSearch.Trim();
            rows = rows
                .Where(row =>
                    row.ProductName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    row.Article.Contains(search, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        if (!string.IsNullOrWhiteSpace(locationSearch))
        {
            var search = locationSearch.Trim();
            rows = rows
                .Where(row => row.LocationCode.Contains(
                    search,
                    StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        return rows
            .OrderBy(row => row.ProductName)
            .ThenBy(row => row.IsPending)
            .ThenBy(row => row.WarehouseName)
            .ThenBy(row => row.LocationCode)
            .ToList();
    }

    /// Получить текущий остаток товара в конкретной ячейке (0, если записи нет).
    public async Task<int> GetQuantityAsync(
        WmsDbContext context,
        int productId,
        int locationId)
    {
        var inventory = await context.Inventories
            .FirstOrDefaultAsync(i =>
                i.ProductId == productId &&
                i.StorageLocationId == locationId);

        return inventory?.Quantity ?? 0;
    }

    /// Увеличить остаток товара в ячейке.
    public async Task IncreaseAsync(
        WmsDbContext context,
        int productId,
        int locationId,
        int quantity)
    {
        var inventory = await context.Inventories
            .FirstOrDefaultAsync(i =>
                i.ProductId == productId &&
                i.StorageLocationId == locationId);

        if (inventory == null)
        {
            context.Inventories.Add(new Inventory
            {
                ProductId = productId,
                StorageLocationId = locationId,
                Quantity = quantity
            });
        }
        else
        {
            inventory.Quantity += quantity;
        }
    }

    /// Уменьшить остаток товара в ячейке.
    public async Task DecreaseAsync(
        WmsDbContext context,
        int productId,
        int locationId,
        int quantity)
    {
        var inventory = await context.Inventories
            .FirstOrDefaultAsync(i =>
                i.ProductId == productId &&
                i.StorageLocationId == locationId);

        if (inventory == null || inventory.Quantity < quantity)
        {
            throw new InvalidOperationException(
                "Недостаточно товара в ячейке для списания.");
        }

        inventory.Quantity -= quantity;
    }
}
using Microsoft.EntityFrameworkCore;
using WMS.Data;
using WMS.Models;

namespace WMS.Services;

public class WarehouseTransferService
{
    private readonly IDbContextFactory<WmsDbContext> _contextFactory;
    private readonly InventoryService _inventoryService;
    private readonly WarehouseEmployeeService _employeeService;

    public WarehouseTransferService(
        IDbContextFactory<WmsDbContext> contextFactory,
        InventoryService inventoryService,
        WarehouseEmployeeService employeeService)
    {
        _contextFactory = contextFactory;
        _inventoryService = inventoryService;
        _employeeService = employeeService;
    }

    public async Task<List<Warehouse>> GetWarehousesAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context.Warehouses
            .AsNoTracking()
            .OrderBy(warehouse => warehouse.Name)
            .ToListAsync();
    }

    public Task<List<WarehouseStaffOption>> GetStaffAsync(int warehouseId)
    {
        return _employeeService.GetStaffAsync(warehouseId: warehouseId);
    }

    public async Task<List<ProductStockOption>> GetProductsWithStockAsync(
        int warehouseId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var productIds = await context.Inventories
            .Where(inventory =>
                inventory.StorageLocation!.WarehouseId == warehouseId &&
                inventory.Quantity > 0)
            .Select(inventory => inventory.ProductId)
            .Distinct()
            .ToListAsync();

        return await context.Products
            .AsNoTracking()
            .Where(product => productIds.Contains(product.Id))
            .OrderBy(product => product.Name)
            .Select(product => new ProductStockOption
            {
                ProductId = product.Id,
                Name = product.Name,
                Article = product.Article
            })
            .ToListAsync();
    }

    public async Task<List<SourceLocationOption>> GetSourceLocationsAsync(
        int warehouseId,
        int productId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context.Inventories
            .AsNoTracking()
            .Where(inventory =>
                inventory.ProductId == productId &&
                inventory.StorageLocation!.WarehouseId == warehouseId &&
                inventory.Quantity > 0)
            .OrderBy(inventory => inventory.StorageLocation!.Code)
            .Select(inventory => new SourceLocationOption
            {
                LocationId = inventory.StorageLocationId,
                Code = inventory.StorageLocation!.Code,
                Available = inventory.Quantity
            })
            .ToListAsync();
    }

    public async Task<List<LocationOption>> GetDestinationLocationsAsync(
        int warehouseId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var locations = await context.StorageLocations
            .AsNoTracking()
            .Where(location => location.WarehouseId == warehouseId)
            .OrderBy(location => location.Code)
            .ToListAsync();

        var usage = await context.Inventories
            .Where(inventory =>
                inventory.StorageLocation!.WarehouseId == warehouseId)
            .GroupBy(inventory => inventory.StorageLocationId)
            .Select(group => new
            {
                LocationId = group.Key,
                Quantity = group.Sum(inventory => inventory.Quantity)
            })
            .ToDictionaryAsync(item => item.LocationId, item => item.Quantity);

        return locations.Select(location => new LocationOption
        {
            Id = location.Id,
            Code = location.Code,
            Capacity = location.Capacity,
            Used = usage.GetValueOrDefault(location.Id)
        }).ToList();
    }

    public async Task<List<WarehouseTransfer>> GetTransfersAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var query = context.Set<WarehouseTransfer>()
            .AsNoTracking()
            .Include(transfer => transfer.SourceWarehouse)
            .Include(transfer => transfer.DestinationWarehouse)
            .Include(transfer => transfer.SourceResponsibleEmployee)
            .Include(transfer => transfer.SourceResponsibleUser)
            .Include(transfer => transfer.DestinationResponsibleEmployee)
            .Include(transfer => transfer.DestinationResponsibleUser)
            .Include(transfer => transfer.CreatedByUser)
            .Include(transfer => transfer.Items)
                .ThenInclude(item => item.Product)
            .Include(transfer => transfer.Items)
                .ThenInclude(item => item.SourceStorageLocation)
            .Include(transfer => transfer.Items)
                .ThenInclude(item => item.DestinationStorageLocation)
            .AsSplitQuery();

        if (CurrentSession.WarehouseScopeId is int scopeId)
        {
            query = query.Where(transfer =>
                transfer.SourceWarehouseId == scopeId ||
                transfer.DestinationWarehouseId == scopeId);
        }

        return await query
            .OrderByDescending(transfer => transfer.CreatedAt)
            .ToListAsync();
    }

    public async Task<(
        bool Success,
        string? Error,
        WarehouseTransfer? Transfer)> CreateAsync(
        int sourceWarehouseId,
        int destinationWarehouseId,
        WarehouseStaffOption sourceResponsible,
        WarehouseStaffOption destinationResponsible,
        DateTime plannedDeliveryDate,
        IReadOnlyCollection<WarehouseTransferItemInput> items)
    {
        if (CurrentSession.CurrentUser == null)
        {
            return (false, "Сначала войдите в систему.", null);
        }

        if (sourceWarehouseId <= 0 || destinationWarehouseId <= 0)
        {
            return (false, "Выберите склады отправителя и получателя.", null);
        }

        if (sourceWarehouseId == destinationWarehouseId)
        {
            return (false, "Склад отправителя и склад получателя должны различаться.", null);
        }

        if (!CurrentSession.CanAccessWarehouse(sourceWarehouseId))
        {
            return (false, "У вас нет доступа к складу-отправителю.", null);
        }

        if (plannedDeliveryDate.Date < DateTime.Today)
        {
            return (false, "Планируемая дата доставки не может быть в прошлом.", null);
        }

        if (items.Count == 0)
        {
            return (false, "Добавьте в документ хотя бы одну позицию.", null);
        }

        if (items.Any(item =>
                item.ProductId <= 0 ||
                item.SourceStorageLocationId <= 0 ||
                item.DestinationStorageLocationId <= 0 ||
                item.Quantity <= 0))
        {
            return (
                false,
                "Проверьте товар, ячейки и количество в каждой позиции.",
                null);
        }

        await using var context = await _contextFactory.CreateDbContextAsync();

        var warehousesExist =
            await context.Warehouses.AnyAsync(
                warehouse => warehouse.Id == sourceWarehouseId) &&
            await context.Warehouses.AnyAsync(
                warehouse => warehouse.Id == destinationWarehouseId);

        if (!warehousesExist)
        {
            return (false, "Один из выбранных складов не найден.", null);
        }

        if (!await IsStaffMemberForWarehouseAsync(
                context,
                sourceResponsible,
                sourceWarehouseId))
        {
            return (
                false,
                "Выберите ответственного, прикреплённого к складу-отправителю.",
                null);
        }

        if (!await IsStaffMemberForWarehouseAsync(
                context,
                destinationResponsible,
                destinationWarehouseId))
        {
            return (
                false,
                "Выберите ответственного, прикреплённого к складу-получателю.",
                null);
        }

        var sourceLocationIds = items
            .Select(item => item.SourceStorageLocationId)
            .Distinct()
            .ToList();

        var destinationLocationIds = items
            .Select(item => item.DestinationStorageLocationId)
            .Distinct()
            .ToList();

        var sourceLocations = await context.StorageLocations
            .Where(location =>
                sourceLocationIds.Contains(location.Id) &&
                location.WarehouseId == sourceWarehouseId)
            .Select(location => location.Id)
            .ToListAsync();

        if (sourceLocations.Count != sourceLocationIds.Count)
        {
            return (
                false,
                "Одна из исходных ячеек не принадлежит складу-отправителю.",
                null);
        }

        var destinationLocations = await context.StorageLocations
            .Where(location =>
                destinationLocationIds.Contains(location.Id) &&
                location.WarehouseId == destinationWarehouseId)
            .Select(location => location.Id)
            .ToListAsync();

        if (destinationLocations.Count != destinationLocationIds.Count)
        {
            return (
                false,
                "Одна из целевых ячеек не принадлежит складу-получателю.",
                null);
        }

        var productIds = items
            .Select(item => item.ProductId)
            .Distinct()
            .ToList();

        var existingProductIds = await context.Products
            .Where(product => productIds.Contains(product.Id))
            .Select(product => product.Id)
            .ToListAsync();

        if (existingProductIds.Count != productIds.Count)
        {
            return (false, "Один из выбранных товаров не найден.", null);
        }

        foreach (var group in items.GroupBy(item =>
                     new { item.ProductId, item.SourceStorageLocationId }))
        {
            var available = await _inventoryService.GetQuantityAsync(
                context,
                group.Key.ProductId,
                group.Key.SourceStorageLocationId);

            var requested = group.Sum(item => item.Quantity);
            if (requested > available)
            {
                return (
                    false,
                    $"Недостаточно товара в исходной ячейке. Доступно: {available}, требуется: {requested}.",
                    null);
            }
        }

        var now = DateTime.UtcNow;
        var suffix = Guid.NewGuid().ToString("N")[..6];

        var transfer = new WarehouseTransfer
        {
            DocumentNumber = $"TR-{now:yyyyMMdd-HHmmss}-{suffix}",
            SourceWarehouseId = sourceWarehouseId,
            DestinationWarehouseId = destinationWarehouseId,

            SourceResponsibleEmployeeId =
                sourceResponsible.Type == WarehouseStaffType.Employee
                    ? sourceResponsible.Id
                    : null,
            SourceResponsibleUserId =
                sourceResponsible.Type == WarehouseStaffType.User
                    ? sourceResponsible.Id
                    : null,

            DestinationResponsibleEmployeeId =
                destinationResponsible.Type == WarehouseStaffType.Employee
                    ? destinationResponsible.Id
                    : null,
            DestinationResponsibleUserId =
                destinationResponsible.Type == WarehouseStaffType.User
                    ? destinationResponsible.Id
                    : null,

            CreatedByUserId = CurrentSession.CurrentUser.Id,
            CreatedAt = now,
            PlannedDeliveryDate =
                DateOnly.FromDateTime(plannedDeliveryDate.Date),
            Status = WarehouseTransferStatus.Created,
            Items = items.Select(item => new WarehouseTransferItem
            {
                ProductId = item.ProductId,
                SourceStorageLocationId = item.SourceStorageLocationId,
                DestinationStorageLocationId = item.DestinationStorageLocationId,
                Quantity = item.Quantity
            }).ToList()
        };

        context.Add(transfer);
        await context.SaveChangesAsync();

        return (true, null, transfer);
    }

    private static Task<bool> IsStaffMemberForWarehouseAsync(
        WmsDbContext context,
        WarehouseStaffOption staff,
        int warehouseId)
    {
        if (staff.Id <= 0 || staff.WarehouseId != warehouseId)
        {
            return Task.FromResult(false);
        }

        return staff.Type switch
        {
            WarehouseStaffType.Employee =>
                context.WarehouseEmployees.AnyAsync(employee =>
                    employee.Id == staff.Id &&
                    employee.WarehouseId == warehouseId),

            WarehouseStaffType.User =>
                context.Users.AnyAsync(user =>
                    user.Id == staff.Id &&
                    user.WarehouseId == warehouseId),

            _ => Task.FromResult(false)
        };
    }

    public async Task<(bool Success, string? Error)> ConfirmShipmentAsync(
        int transferId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var transfer = await GetTransferForUpdateAsync(context, transferId);
        if (transfer == null)
        {
            return (false, "Документ перемещения не найден.");
        }

        if (!CurrentSession.CanAccessWarehouse(transfer.SourceWarehouseId))
        {
            return (
                false,
                "Подтвердить отправку может пользователь склада-отправителя.");
        }

        if (transfer.Status != WarehouseTransferStatus.Created)
        {
            return (false, "Отправить можно только созданный документ.");
        }

        foreach (var group in transfer.Items.GroupBy(item =>
                     new { item.ProductId, item.SourceStorageLocationId }))
        {
            var available = await _inventoryService.GetQuantityAsync(
                context,
                group.Key.ProductId,
                group.Key.SourceStorageLocationId);

            var requested = group.Sum(item => item.Quantity);
            if (requested > available)
            {
                return (
                    false,
                    $"Недостаточно товара в исходной ячейке. Доступно: {available}, требуется: {requested}.");
            }
        }

        var now = DateTime.UtcNow;

        foreach (var item in transfer.Items)
        {
            await _inventoryService.DecreaseAsync(
                context,
                item.ProductId,
                item.SourceStorageLocationId,
                item.Quantity);

            context.StockMovements.Add(new StockMovement
            {
                ProductId = item.ProductId,
                FromLocationId = item.SourceStorageLocationId,
                ToLocationId = null,
                WarehouseId = transfer.SourceWarehouseId,
                Quantity = item.Quantity,
                OperationType = OperationType.InterWarehouseTransfer,
                Date = now
            });
        }

        transfer.Status = WarehouseTransferStatus.Shipped;
        transfer.ShippedAt = now;

        await context.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> ConfirmReceiptAsync(
        int transferId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var transfer = await GetTransferForUpdateAsync(context, transferId);
        if (transfer == null)
        {
            return (false, "Документ перемещения не найден.");
        }

        if (!CurrentSession.CanAccessWarehouse(transfer.DestinationWarehouseId))
        {
            return (
                false,
                "Подтвердить получение может пользователь склада-получателя.");
        }

        if (transfer.Status != WarehouseTransferStatus.Shipped)
        {
            return (false, "Получить можно только уже отправленный документ.");
        }

        var targetLocationIds = transfer.Items
            .Select(item => item.DestinationStorageLocationId)
            .Distinct()
            .ToList();

        var targetLocations = await context.StorageLocations
            .Where(location =>
                targetLocationIds.Contains(location.Id) &&
                location.WarehouseId == transfer.DestinationWarehouseId)
            .ToDictionaryAsync(location => location.Id);

        if (targetLocations.Count != targetLocationIds.Count)
        {
            return (
                false,
                "Одна из целевых ячеек больше не принадлежит складу-получателю.");
        }

        var usedQuantities = await context.Inventories
            .Where(inventory =>
                targetLocationIds.Contains(inventory.StorageLocationId))
            .GroupBy(inventory => inventory.StorageLocationId)
            .Select(group => new
            {
                LocationId = group.Key,
                Quantity = group.Sum(inventory => inventory.Quantity)
            })
            .ToDictionaryAsync(item => item.LocationId, item => item.Quantity);

        foreach (var group in transfer.Items.GroupBy(
                     item => item.DestinationStorageLocationId))
        {
            var location = targetLocations[group.Key];
            var used = usedQuantities.GetValueOrDefault(group.Key);
            var incoming = group.Sum(item => item.Quantity);

            if (used + incoming > location.Capacity)
            {
                var free = Math.Max(0, location.Capacity - used);
                return (
                    false,
                    $"В ячейке {location.Code} недостаточно места. Свободно: {free}, требуется: {incoming}.");
            }
        }

        var now = DateTime.UtcNow;

        foreach (var item in transfer.Items)
        {
            await _inventoryService.IncreaseAsync(
                context,
                item.ProductId,
                item.DestinationStorageLocationId,
                item.Quantity);

            context.StockMovements.Add(new StockMovement
            {
                ProductId = item.ProductId,
                FromLocationId = null,
                ToLocationId = item.DestinationStorageLocationId,
                WarehouseId = transfer.DestinationWarehouseId,
                Quantity = item.Quantity,
                OperationType = OperationType.InterWarehouseTransfer,
                Date = now
            });
        }

        transfer.Status = WarehouseTransferStatus.Received;
        transfer.ReceivedAt = now;

        await context.SaveChangesAsync();
        return (true, null);
    }

    private static Task<WarehouseTransfer?> GetTransferForUpdateAsync(
        WmsDbContext context,
        int transferId)
    {
        return context.Set<WarehouseTransfer>()
            .Include(transfer => transfer.Items)
            .FirstOrDefaultAsync(transfer => transfer.Id == transferId);
    }
}
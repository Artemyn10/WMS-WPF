using Microsoft.EntityFrameworkCore;
using WMS.Data;
using WMS.Models;

namespace WMS.Services;

public class ReportingService
{
    private readonly IDbContextFactory<WmsDbContext> _contextFactory;

    public ReportingService(
        IDbContextFactory<WmsDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<Warehouse>> GetWarehousesAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var query = context.Warehouses.AsNoTracking().AsQueryable();

        if (CurrentSession.WarehouseScopeId is int scopeId)
            query = query.Where(warehouse => warehouse.Id == scopeId);

        return await query
            .OrderBy(warehouse => warehouse.Name)
            .ToListAsync();
    }

    public async Task<List<ReportingStockRow>> GetStockAsync(
        int? warehouseId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var query = context.Inventories
            .AsNoTracking()
            .Include(inventory => inventory.Product)
            .Include(inventory => inventory.StorageLocation)
                .ThenInclude(location => location!.Warehouse)
            .Where(inventory => inventory.Quantity != 0)
            .AsQueryable();

        if (CurrentSession.WarehouseScopeId is int scopeId)
            query = query.Where(inventory =>
                inventory.StorageLocation!.WarehouseId == scopeId);

        if (warehouseId is int selectedWarehouseId &&
            selectedWarehouseId > 0)
        {
            query = query.Where(inventory =>
                inventory.StorageLocation!.WarehouseId ==
                selectedWarehouseId);
        }

        var records = await query.ToListAsync();

        return records
            .GroupBy(inventory => new
            {
                WarehouseId =
                    inventory.StorageLocation!.WarehouseId,
                WarehouseName =
                    inventory.StorageLocation.Warehouse!.Name,
                inventory.ProductId,
                ProductName = inventory.Product!.Name,
                inventory.Product.Article
            })
            .Select(group => new ReportingStockRow
            {
                WarehouseId = group.Key.WarehouseId,
                WarehouseName = group.Key.WarehouseName,
                ProductId = group.Key.ProductId,
                Article = group.Key.Article,
                ProductName = group.Key.ProductName,
                Quantity = group.Sum(inventory => inventory.Quantity)
            })
            .OrderBy(row => row.WarehouseName)
            .ThenBy(row => row.ProductName)
            .ToList();
    }

    public async Task<List<ReportingActivityRow>> GetActivityAsync(
        DateTime from,
        DateTime to,
        int? warehouseId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var start = DateTime.SpecifyKind(from.Date, DateTimeKind.Utc);
        var end = DateTime.SpecifyKind(to.Date.AddDays(1), DateTimeKind.Utc);
        var rows = new List<ReportingActivityRow>();

        var receiptQuery = context.Receipts
            .AsNoTracking()
            .Include(receipt => receipt.Supplier)
            .Include(receipt => receipt.Warehouse)
            .Include(receipt => receipt.Items)
            .Where(receipt =>
                receipt.Status == ReceiptStatus.Confirmed &&
                receipt.ArrivalDate >= DateOnly.FromDateTime(start) &&
                receipt.ArrivalDate < DateOnly.FromDateTime(end))
            .AsQueryable();

        if (CurrentSession.WarehouseScopeId is int scopeId)
            receiptQuery = receiptQuery.Where(receipt =>
                receipt.WarehouseId == scopeId);

        if (warehouseId is int selectedWarehouseId &&
            selectedWarehouseId > 0)
        {
            receiptQuery = receiptQuery.Where(receipt =>
                receipt.WarehouseId == selectedWarehouseId);
        }

        foreach (var receipt in await receiptQuery.ToListAsync())
        {
            rows.Add(new ReportingActivityRow
            {
                Operation = "Поступление",
                WarehouseName = receipt.Warehouse?.Name ?? "—",
                PartnerName = receipt.Supplier?.Name ?? "—",
                DocumentNumber = receipt.DocumentNumber,
                Date = receipt.ArrivalDate.ToDateTime(TimeOnly.MinValue),
                Quantity = receipt.Items.Sum(item => item.Quantity)
            });
        }

        var shipmentQuery = context.Shipments
            .AsNoTracking()
            .Include(shipment => shipment.Order)
                .ThenInclude(order => order!.Customer)
            .Include(shipment => shipment.Order)
                .ThenInclude(order => order!.Warehouse)
            .Include(shipment => shipment.Items)
            .Where(shipment =>
                shipment.Status == ShipmentStatus.Completed &&
                shipment.Date >= start &&
                shipment.Date < end)
            .AsQueryable();

        if (CurrentSession.WarehouseScopeId is int activityScopeId)
        {
            shipmentQuery = shipmentQuery.Where(shipment =>
                shipment.Order!.WarehouseId == activityScopeId);
        }

        if (warehouseId is int activityWarehouseId &&
            activityWarehouseId > 0)
        {
            shipmentQuery = shipmentQuery.Where(shipment =>
                shipment.Order!.WarehouseId == activityWarehouseId);
        }

        foreach (var shipment in await shipmentQuery.ToListAsync())
        {
            rows.Add(new ReportingActivityRow
            {
                Operation = "Отгрузка",
                WarehouseName =
                    shipment.Order?.Warehouse?.Name ?? "—",
                PartnerName =
                    shipment.Order?.Customer?.Name ?? "—",
                DocumentNumber =
                    shipment.Order?.OrderNumber ?? $"#{shipment.Id}",
                Date = shipment.Date,
                Quantity = shipment.Items.Sum(item => item.Quantity)
            });
        }

        return rows
            .OrderByDescending(row => row.Date)
            .ThenBy(row => row.Operation)
            .ToList();
    }

    public async Task<List<ReportingTransferRow>> GetTransfersAsync(
        DateTime from,
        DateTime to,
        int? warehouseId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var start = DateTime.SpecifyKind(from.Date, DateTimeKind.Utc);
        var end = DateTime.SpecifyKind(to.Date.AddDays(1), DateTimeKind.Utc);

        var query = context.WarehouseTransfers
            .AsNoTracking()
            .Include(transfer => transfer.SourceWarehouse)
            .Include(transfer => transfer.DestinationWarehouse)
            .Include(transfer => transfer.Items)
            .Where(transfer =>
                transfer.CreatedAt >= start &&
                transfer.CreatedAt < end)
            .AsQueryable();

        if (CurrentSession.WarehouseScopeId is int scopeId)
        {
            query = query.Where(transfer =>
                transfer.SourceWarehouseId == scopeId ||
                transfer.DestinationWarehouseId == scopeId);
        }

        if (warehouseId is int selectedWarehouseId &&
            selectedWarehouseId > 0)
        {
            query = query.Where(transfer =>
                transfer.SourceWarehouseId == selectedWarehouseId ||
                transfer.DestinationWarehouseId == selectedWarehouseId);
        }

        return await query
            .OrderByDescending(transfer => transfer.CreatedAt)
            .Select(transfer => new ReportingTransferRow
            {
                DocumentNumber = transfer.DocumentNumber,
                Date = transfer.CreatedAt,
                SourceWarehouse =
                    transfer.SourceWarehouse!.Name,
                DestinationWarehouse =
                    transfer.DestinationWarehouse!.Name,
                Quantity =
                    transfer.Items.Sum(item => item.Quantity),
                Status =
                    transfer.Status == WarehouseTransferStatus.Created
                        ? "Создано"
                        : transfer.Status == WarehouseTransferStatus.Shipped
                            ? "Отправлено"
                            : "Получено"
            })
            .ToListAsync();
    }

    public async Task<List<ReportingPartnerRow>> GetPartnersAsync(
        DateTime from,
        DateTime to,
        int? warehouseId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var start = DateTime.SpecifyKind(from.Date, DateTimeKind.Utc);
        var end = DateTime.SpecifyKind(to.Date.AddDays(1), DateTimeKind.Utc);
        var result = new List<ReportingPartnerRow>();

        var receipts = context.Receipts
            .AsNoTracking()
            .Include(receipt => receipt.Supplier)
            .Include(receipt => receipt.Items)
            .Where(receipt =>
                receipt.Status == ReceiptStatus.Confirmed &&
                receipt.ArrivalDate >= DateOnly.FromDateTime(start) &&
                receipt.ArrivalDate < DateOnly.FromDateTime(end))
            .AsQueryable();

        if (CurrentSession.WarehouseScopeId is int scopeId)
            receipts = receipts.Where(receipt =>
                receipt.WarehouseId == scopeId);

        if (warehouseId is int selectedWarehouseId &&
            selectedWarehouseId > 0)
        {
            receipts = receipts.Where(receipt =>
                receipt.WarehouseId == selectedWarehouseId);
        }

        var receiptList = await receipts.ToListAsync();

        result.AddRange(receiptList
            .GroupBy(receipt => receipt.Supplier?.Name ?? "Не указан")
            .Select(group => new ReportingPartnerRow
            {
                Type = "Поставщик",
                Name = group.Key,
                DocumentsCount = group.Count(),
                Quantity = group.Sum(receipt =>
                    receipt.Items.Sum(item => item.Quantity))
            }));

        var shipments = context.Shipments
            .AsNoTracking()
            .Include(shipment => shipment.Order)
                .ThenInclude(order => order!.Customer)
            .Include(shipment => shipment.Order)
                .ThenInclude(order => order!.Warehouse)
            .Include(shipment => shipment.Items)
            .Where(shipment =>
                shipment.Status == ShipmentStatus.Completed &&
                shipment.Date >= start &&
                shipment.Date < end)
            .AsQueryable();

        if (CurrentSession.WarehouseScopeId is int partnerScopeId)
        {
            shipments = shipments.Where(shipment =>
                shipment.Order!.WarehouseId == partnerScopeId);
        }

        if (warehouseId is int partnerWarehouseId &&
            partnerWarehouseId > 0)
        {
            shipments = shipments.Where(shipment =>
                shipment.Order!.WarehouseId == partnerWarehouseId);
        }

        var shipmentList = await shipments.ToListAsync();

        result.AddRange(shipmentList
            .GroupBy(shipment =>
                shipment.Order?.Customer?.Name ?? "Не указан")
            .Select(group => new ReportingPartnerRow
            {
                Type = "Покупатель",
                Name = group.Key,
                DocumentsCount = group.Count(),
                Quantity = group.Sum(shipment =>
                    shipment.Items.Sum(item => item.Quantity))
            }));

        return result
            .OrderBy(row => row.Type)
            .ThenBy(row => row.Name)
            .ToList();
    }

    public async Task<List<ReportingStockTakingRow>> GetStockTakingAsync(
        DateTime from,
        DateTime to,
        int? warehouseId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var start = DateTime.SpecifyKind(from.Date, DateTimeKind.Utc);
        var end = DateTime.SpecifyKind(to.Date.AddDays(1), DateTimeKind.Utc);

        var query = context.StockTakings
            .AsNoTracking()
            .Include(stockTaking => stockTaking.Warehouse)
            .Include(stockTaking => stockTaking.Items)
            .Where(stockTaking =>
                stockTaking.StartDate >= start &&
                stockTaking.StartDate < end)
            .AsQueryable();

        if (CurrentSession.WarehouseScopeId is int scopeId)
            query = query.Where(stockTaking =>
                stockTaking.WarehouseId == scopeId);

        if (warehouseId is int selectedWarehouseId &&
            selectedWarehouseId > 0)
        {
            query = query.Where(stockTaking =>
                stockTaking.WarehouseId == selectedWarehouseId);
        }

        var stockTakings = await query.ToListAsync();

        return stockTakings
            .OrderByDescending(stockTaking => stockTaking.StartDate)
            .Select(stockTaking =>
            {
                var countedItems = stockTaking.Items
                    .Where(item => item.ActualQuantity != null)
                    .ToList();

                var differences = countedItems
                    .Where(item =>
                        item.ActualQuantity != item.ExpectedQuantity)
                    .ToList();

                return new ReportingStockTakingRow
                {
                    DocumentNumber = stockTaking.DocumentNumber,
                    WarehouseName =
                        stockTaking.Warehouse?.Name ?? "—",
                    StartDate = stockTaking.StartDate,
                    CompletedDate = stockTaking.CompletedDate,
                    PositionsCount = stockTaking.Items.Count,
                    DifferencesCount = differences.Count,
                    ExcessQuantity = differences.Sum(item =>
                        Math.Max(
                            0,
                            item.ActualQuantity!.Value -
                            item.ExpectedQuantity)),
                    DeficitQuantity = differences.Sum(item =>
                        Math.Max(
                            0,
                            item.ExpectedQuantity -
                            item.ActualQuantity!.Value)),
                    Status = stockTaking.Status ==
                        StockTakingStatus.Completed
                        ? "Завершена"
                        : "В процессе"
                };
            })
            .ToList();
    }

    public async Task<List<ReportingCapacityRow>> GetCapacityAsync(
        int? warehouseId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var warehousesQuery = context.Warehouses
            .AsNoTracking()
            .AsQueryable();

        if (CurrentSession.WarehouseScopeId is int scopeId)
            warehousesQuery = warehousesQuery.Where(warehouse =>
                warehouse.Id == scopeId);

        if (warehouseId is int selectedWarehouseId &&
            selectedWarehouseId > 0)
        {
            warehousesQuery = warehousesQuery.Where(warehouse =>
                warehouse.Id == selectedWarehouseId);
        }

        var warehouses = await warehousesQuery
            .OrderBy(warehouse => warehouse.Name)
            .ToListAsync();

        var warehouseIds = warehouses.Select(warehouse => warehouse.Id).ToList();

        var locationStats = await context.StorageLocations
            .Where(location => warehouseIds.Contains(location.WarehouseId))
            .GroupBy(location => location.WarehouseId)
            .Select(group => new
            {
                WarehouseId = group.Key,
                LocationsCount = group.Count(),
                Capacity = group.Sum(location => location.Capacity)
            })
            .ToDictionaryAsync(row => row.WarehouseId);

        var storedStats = await context.Inventories
            .Where(inventory =>
                warehouseIds.Contains(
                    inventory.StorageLocation!.WarehouseId))
            .GroupBy(inventory =>
                inventory.StorageLocation!.WarehouseId)
            .Select(group => new
            {
                WarehouseId = group.Key,
                Stored = group.Sum(inventory => inventory.Quantity)
            })
            .ToDictionaryAsync(row => row.WarehouseId);

        return warehouses.Select(warehouse =>
        {
            locationStats.TryGetValue(
                warehouse.Id,
                out var locationStatsRow);

            var stored =
                storedStats.TryGetValue(warehouse.Id, out var storedRow)
                    ? storedRow.Stored
                    : 0;

            return new ReportingCapacityRow
            {
                WarehouseId = warehouse.Id,
                WarehouseName = warehouse.Name,
                Area = warehouse.Area ?? 0,
                LocationsCount =
                    locationStatsRow?.LocationsCount ?? 0,
                Capacity =
                    locationStatsRow?.Capacity ?? 0,
                Stored = stored
            };
        }).ToList();
    }
}
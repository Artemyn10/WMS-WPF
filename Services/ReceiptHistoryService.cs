using Microsoft.EntityFrameworkCore;
using WMS.Data;
using WMS.Models;

namespace WMS.Services;

public class ReceiptHistoryService
{
    private readonly IDbContextFactory<WmsDbContext> _contextFactory;

    public ReceiptHistoryService(IDbContextFactory<WmsDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    /// Подтверждённые поставки и полученные межскладские перемещения.
    /// warehouseId = 0 и supplierId = 0 означают отсутствие фильтра.
    public async Task<List<ReceiptHistoryDocumentRow>> GetAsync(
        int warehouseId,
        DateOnly? dateFrom,
        DateOnly? dateTo,
        int supplierId,
        string? search)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var receiptQuery = context.Receipts
            .AsNoTracking()
            .Include(receipt => receipt.Supplier)
            .Include(receipt => receipt.Warehouse)
            .Include(receipt => receipt.PurchaseOrder)
            .Include(receipt => receipt.CreatedBy)
            .Include(receipt => receipt.Items)
                .ThenInclude(item => item.Product)
            .Where(receipt => receipt.Status == ReceiptStatus.Confirmed)
            .AsSplitQuery();

        if (CurrentSession.WarehouseScopeId is int scopeId)
            receiptQuery = receiptQuery.Where(receipt => receipt.WarehouseId == scopeId);

        if (warehouseId > 0)
            receiptQuery = receiptQuery.Where(receipt => receipt.WarehouseId == warehouseId);

        if (supplierId > 0)
            receiptQuery = receiptQuery.Where(receipt => receipt.SupplierId == supplierId);

        if (dateFrom is DateOnly fromDate)
            receiptQuery = receiptQuery.Where(receipt => receipt.ArrivalDate >= fromDate);

        if (dateTo is DateOnly toDate)
            receiptQuery = receiptQuery.Where(receipt => receipt.ArrivalDate <= toDate);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var text = search.Trim().ToLowerInvariant();
            receiptQuery = receiptQuery.Where(receipt =>
                receipt.DocumentNumber.ToLower().Contains(text) ||
                receipt.Supplier!.Name.ToLower().Contains(text) ||
                receipt.Items.Any(item =>
                    item.Product!.Name.ToLower().Contains(text)));
        }

        var receipts = await receiptQuery.ToListAsync();

        var documents = receipts.Select(receipt => new ReceiptHistoryDocumentRow
        {
            WarehouseId = receipt.WarehouseId,
            DocumentNumber = receipt.DocumentNumber,
            ArrivalDate = receipt.ArrivalDate,
            DateLocal = receipt.DateLocal,
            WarehouseName = receipt.Warehouse?.Name ?? string.Empty,
            SourceDisplay = receipt.Supplier?.Name ?? string.Empty,
            PurchaseOrderNumber = receipt.PurchaseOrder?.OrderNumber ?? string.Empty,
            ItemsCount = receipt.Items.Count,
            TotalQuantity = receipt.Items.Sum(item => item.Quantity),
            ReceivedBy = receipt.CreatedBy?.FullName ?? string.Empty,
            StatusDisplay = receipt.StatusDisplay,
            IsInterWarehouseTransfer = false,
            Items = receipt.Items.Select(item => new ReceiptHistoryItemRow
            {
                ProductName = item.Product?.Name ?? string.Empty,
                Article = item.Product?.Article ?? string.Empty,
                Quantity = item.Quantity,
                PlacedQuantity = item.PlacedQuantity,
                Remaining = item.Remaining,
                DestinationLocationCode = string.Empty
            }).ToList()
        }).ToList();

        // Межскладское перемещение не имеет поставщика; включаем его,
        // только когда выбран фильтр «Все поставщики».
        if (supplierId == 0)
        {
            var transferQuery = context.Set<WarehouseTransfer>()
                .AsNoTracking()
                .Include(transfer => transfer.SourceWarehouse)
                .Include(transfer => transfer.DestinationWarehouse)
                .Include(transfer => transfer.DestinationResponsibleEmployee)
                .Include(transfer => transfer.Items)
                    .ThenInclude(item => item.Product)
                .Include(transfer => transfer.Items)
                    .ThenInclude(item => item.DestinationStorageLocation)
                .Where(transfer =>
                    transfer.Status == WarehouseTransferStatus.Received &&
                    transfer.ReceivedAt != null)
                .AsSplitQuery();

            if (CurrentSession.WarehouseScopeId is int transferScopeId)
            {
                transferQuery = transferQuery.Where(transfer =>
                    transfer.DestinationWarehouseId == transferScopeId);
            }

            if (warehouseId > 0)
            {
                transferQuery = transferQuery.Where(transfer =>
                    transfer.DestinationWarehouseId == warehouseId);
            }

            if (dateFrom is DateOnly transferFromDate)
            {
                var fromUtc = transferFromDate
                    .ToDateTime(TimeOnly.MinValue, DateTimeKind.Local)
                    .ToUniversalTime();

                transferQuery = transferQuery.Where(transfer =>
                    transfer.ReceivedAt >= fromUtc);
            }

            if (dateTo is DateOnly transferToDate)
            {
                var toUtcExclusive = transferToDate
                    .AddDays(1)
                    .ToDateTime(TimeOnly.MinValue, DateTimeKind.Local)
                    .ToUniversalTime();

                transferQuery = transferQuery.Where(transfer =>
                    transfer.ReceivedAt < toUtcExclusive);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var text = search.Trim().ToLowerInvariant();
                transferQuery = transferQuery.Where(transfer =>
                    transfer.DocumentNumber.ToLower().Contains(text) ||
                    transfer.SourceWarehouse!.Name.ToLower().Contains(text) ||
                    transfer.Items.Any(item =>
                        item.Product!.Name.ToLower().Contains(text)));
            }

            var transfers = await transferQuery.ToListAsync();

            documents.AddRange(transfers.Select(transfer =>
            {
                var receivedAtLocal = transfer.ReceivedAt!.Value.ToLocalTime();

                return new ReceiptHistoryDocumentRow
                {
                    WarehouseId = transfer.DestinationWarehouseId,
                    DocumentNumber = transfer.DocumentNumber,
                    ArrivalDate = DateOnly.FromDateTime(receivedAtLocal),
                    DateLocal = receivedAtLocal,
                    WarehouseName = transfer.DestinationWarehouse?.Name ?? string.Empty,
                    SourceDisplay =
                        $"Межскладское перемещение из «{transfer.SourceWarehouse?.Name}»",
                    PurchaseOrderNumber = string.Empty,
                    ItemsCount = transfer.Items.Count,
                    TotalQuantity = transfer.Items.Sum(item => item.Quantity),
                    ReceivedBy =
                        transfer.DestinationResponsibleEmployee?.FullName ?? string.Empty,
                    StatusDisplay = "Получено (межскладское перемещение)",
                    IsInterWarehouseTransfer = true,
                    Items = transfer.Items.Select(item => new ReceiptHistoryItemRow
                    {
                        ProductName = item.Product?.Name ?? string.Empty,
                        Article = item.Product?.Article ?? string.Empty,
                        Quantity = item.Quantity,
                        PlacedQuantity = item.Quantity,
                        Remaining = 0,
                        DestinationLocationCode =
                            item.DestinationStorageLocation?.Code ?? string.Empty
                    }).ToList()
                };
            }));
        }

        return documents
            .OrderByDescending(document => document.ArrivalDate)
            .ThenByDescending(document => document.DateLocal)
            .ToList();
    }
}
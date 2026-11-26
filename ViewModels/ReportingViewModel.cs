using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using Microsoft.Win32;
using WMS.Helpers;
using WMS.Models;
using System.IO;
using WMS.Services;

namespace WMS.ViewModels;

public class ReportingViewModel : ViewModelBase
{
    private readonly ReportingService _reportingService;

    public ObservableCollection<Warehouse> Warehouses { get; } = new();
    public ObservableCollection<ReportingStockRow> StockRows { get; } = new();
    public ObservableCollection<ReportingActivityRow> ActivityRows { get; } = new();
    public ObservableCollection<ReportingTransferRow> TransferRows { get; } = new();
    public ObservableCollection<ReportingPartnerRow> PartnerRows { get; } = new();
    public ObservableCollection<ReportingStockTakingRow> StockTakingRows { get; } = new();
    public ObservableCollection<ReportingCapacityRow> CapacityRows { get; } = new();

    private Warehouse? _selectedWarehouse;
    public Warehouse? SelectedWarehouse
    {
        get => _selectedWarehouse;
        set
        {
            if (SetField(ref _selectedWarehouse, value))
                _ = LoadAsync();
        }
    }

    private DateTime? _fromDate = DateTime.Today.AddMonths(-1);
    public DateTime? FromDate
    {
        get => _fromDate;
        set
        {
            if (SetField(ref _fromDate, value))
                _ = LoadAsync();
        }
    }

    private DateTime? _toDate = DateTime.Today;
    public DateTime? ToDate
    {
        get => _toDate;
        set
        {
            if (SetField(ref _toDate, value))
                _ = LoadAsync();
        }
    }

    private int _selectedReportIndex;
    public int SelectedReportIndex
    {
        get => _selectedReportIndex;
        set => SetField(ref _selectedReportIndex, value);
    }

    private string _statusMessage = string.Empty;
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetField(ref _statusMessage, value);
    }

    private string _receiptQuantity = "0";
    public string ReceiptQuantity
    {
        get => _receiptQuantity;
        set => SetField(ref _receiptQuantity, value);
    }

    private string _shipmentQuantity = "0";
    public string ShipmentQuantity
    {
        get => _shipmentQuantity;
        set => SetField(ref _shipmentQuantity, value);
    }

    private string _transferQuantity = "0";
    public string TransferQuantity
    {
        get => _transferQuantity;
        set => SetField(ref _transferQuantity, value);
    }

    private string _stockQuantity = "0";
    public string StockQuantity
    {
        get => _stockQuantity;
        set => SetField(ref _stockQuantity, value);
    }

    public ICommand RefreshCommand { get; }
    public ICommand ExportCsvCommand { get; }

    public ReportingViewModel(ReportingService reportingService)
    {
        _reportingService = reportingService;
        RefreshCommand = new RelayCommand(async _ => await LoadAsync());
        ExportCsvCommand = new RelayCommand(_ => ExportCsv());

        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        var warehouses = await _reportingService.GetWarehousesAsync();

        Warehouses.Clear();
        Warehouses.Add(new Warehouse
        {
            Id = 0,
            Name = "Все склады"
        });

        foreach (var warehouse in warehouses)
            Warehouses.Add(warehouse);

        SelectedWarehouse = Warehouses.FirstOrDefault();
    }

    private async Task LoadAsync()
    {
        if (FromDate is not DateTime from ||
            ToDate is not DateTime to)
        {
            StatusMessage = "Укажите начальную и конечную дату.";
            return;
        }

        if (from.Date > to.Date)
        {
            StatusMessage =
                "Начальная дата не может быть позже конечной.";
            return;
        }

        StatusMessage = string.Empty;
        int? warehouseId = SelectedWarehouse != null &&
                            SelectedWarehouse.Id > 0
                    ? SelectedWarehouse.Id
                    : null;

        var stock =
            await _reportingService.GetStockAsync(warehouseId);
        StockRows.Clear();
        foreach (var row in stock)
            StockRows.Add(row);

        StockQuantity =
            stock.Sum(row => row.Quantity).ToString();

        var activity =
            await _reportingService.GetActivityAsync(
                from,
                to,
                warehouseId);
        ActivityRows.Clear();
        foreach (var row in activity)
            ActivityRows.Add(row);

        ReceiptQuantity = activity
            .Where(row => row.Operation == "Поступление")
            .Sum(row => row.Quantity)
            .ToString();

        ShipmentQuantity = activity
            .Where(row => row.Operation == "Отгрузка")
            .Sum(row => row.Quantity)
            .ToString();

        var transfers =
            await _reportingService.GetTransfersAsync(
                from,
                to,
                warehouseId);
        TransferRows.Clear();
        foreach (var row in transfers)
            TransferRows.Add(row);

        TransferQuantity = transfers
            .Sum(row => row.Quantity)
            .ToString();

        var partners =
            await _reportingService.GetPartnersAsync(
                from,
                to,
                warehouseId);
        PartnerRows.Clear();
        foreach (var row in partners)
            PartnerRows.Add(row);

        var stockTaking =
            await _reportingService.GetStockTakingAsync(
                from,
                to,
                warehouseId);
        StockTakingRows.Clear();
        foreach (var row in stockTaking)
            StockTakingRows.Add(row);

        var capacity =
            await _reportingService.GetCapacityAsync(warehouseId);
        CapacityRows.Clear();
        foreach (var row in capacity)
            CapacityRows.Add(row);
    }

    private void ExportCsv()
    {
        var dialog = new SaveFileDialog
        {
            Title = "Экспорт отчёта в CSV",
            Filter = "CSV для Microsoft Excel (*.csv)|*.csv",
            FileName = $"WMS-отчёт-{DateTime.Today:yyyy-MM-dd}.csv"
        };

        if (dialog.ShowDialog() != true)
            return;

        var csv = SelectedReportIndex switch
        {
            0 => StockToCsv(),
            1 => ActivityToCsv(),
            2 => TransferToCsv(),
            3 => PartnerToCsv(),
            4 => StockTakingToCsv(),
            _ => CapacityToCsv()
        };

        File.WriteAllText(
            dialog.FileName,
            csv,
            new UTF8Encoding(true));

        StatusMessage =
            $"Отчёт сохранён: {dialog.FileName}";
    }

    private string StockToCsv()
    {
        var lines = new List<string>
        {
            "Склад;Артикул;Товар;Количество"
        };

        lines.AddRange(StockRows.Select(row =>
            Escape(row.WarehouseName) + ";" +
            Escape(row.Article) + ";" +
            Escape(row.ProductName) + ";" +
            row.Quantity));

        return string.Join(Environment.NewLine, lines);
    }

    private string ActivityToCsv()
    {
        var lines = new List<string>
        {
            "Операция;Склад;Контрагент;Документ;Дата;Количество"
        };

        lines.AddRange(ActivityRows.Select(row =>
            Escape(row.Operation) + ";" +
            Escape(row.WarehouseName) + ";" +
            Escape(row.PartnerName) + ";" +
            Escape(row.DocumentNumber) + ";" +
            row.Date.ToString("dd.MM.yyyy") + ";" +
            row.Quantity));

        return string.Join(Environment.NewLine, lines);
    }

    private string TransferToCsv()
    {
        var lines = new List<string>
        {
            "Документ;Дата;Отправитель;Получатель;Количество;Статус"
        };

        lines.AddRange(TransferRows.Select(row =>
            Escape(row.DocumentNumber) + ";" +
            row.Date.ToString("dd.MM.yyyy") + ";" +
            Escape(row.SourceWarehouse) + ";" +
            Escape(row.DestinationWarehouse) + ";" +
            row.Quantity + ";" +
            Escape(row.Status)));

        return string.Join(Environment.NewLine, lines);
    }

    private string PartnerToCsv()
    {
        var lines = new List<string>
        {
            "Тип;Контрагент;Документов;Количество"
        };

        lines.AddRange(PartnerRows.Select(row =>
            Escape(row.Type) + ";" +
            Escape(row.Name) + ";" +
            row.DocumentsCount + ";" +
            row.Quantity));

        return string.Join(Environment.NewLine, lines);
    }

    private string StockTakingToCsv()
    {
        var lines = new List<string>
        {
            "Документ;Склад;Начало;Завершена;Позиций;Расхождений;" +
            "Излишки;Недостача;Статус"
        };

        lines.AddRange(StockTakingRows.Select(row =>
            Escape(row.DocumentNumber) + ";" +
            Escape(row.WarehouseName) + ";" +
            row.StartDate.ToString("dd.MM.yyyy") + ";" +
            (row.CompletedDate?.ToString("dd.MM.yyyy") ?? "—") + ";" +
            row.PositionsCount + ";" +
            row.DifferencesCount + ";" +
            row.ExcessQuantity + ";" +
            row.DeficitQuantity + ";" +
            Escape(row.Status)));

        return string.Join(Environment.NewLine, lines);
    }

    private string CapacityToCsv()
    {
        var lines = new List<string>
        {
            "Склад;Площадь;Ячеек;Вместимость;Хранится;Свободно;Загруженность"
        };

        lines.AddRange(CapacityRows.Select(row =>
            Escape(row.WarehouseName) + ";" +
            row.Area.ToString("0.##") + ";" +
            row.LocationsCount + ";" +
            row.Capacity + ";" +
            row.Stored + ";" +
            row.Free + ";" +
            $"{row.LoadPercent:0.#}%"));

        return string.Join(Environment.NewLine, lines);
    }

    private static string Escape(string value)
    {
        var result = value.Replace("\"", "\"\"");
        return result.Contains(';') ||
               result.Contains('"') ||
               result.Contains('\n')
            ? $"\"{result}\""
            : result;
    }
}
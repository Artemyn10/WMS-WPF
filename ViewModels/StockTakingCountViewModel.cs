using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using WMS.Helpers;
using WMS.Models;
using WMS.Services;

namespace WMS.ViewModels;

public class StockTakingCountViewModel : ViewModelBase
{
    private readonly StockTakingService _stockTakingService;
    private int _stockTakingId;

    public ObservableCollection<StockTakingRowViewModel> Rows { get; } = new();

    public string Title { get; private set; } = string.Empty;

    public string ProgressText =>
        $"Пересчитано {Rows.Count(r => r.Actual != null)} из {Rows.Count} позиций";

    private string _errorMessage = string.Empty;
    public string ErrorMessage { get => _errorMessage; set => SetField(ref _errorMessage, value); }

    public event Action<bool?>? RequestClose;

    public ICommand SaveCommand { get; }
    public ICommand CompleteCommand { get; }
    public ICommand CancelCommand { get; }

    public StockTakingCountViewModel(StockTakingService stockTakingService)
    {
        _stockTakingService = stockTakingService;

        SaveCommand = new RelayCommand(async _ => await SaveAsync());
        CompleteCommand = new RelayCommand(async _ => await CompleteAsync());
        CancelCommand = new RelayCommand(_ => RequestClose?.Invoke(false));
    }

    /// false - документ не найден, завершён или нет доступа
    public async Task<bool> InitializeAsync(int id)
    {
        var document = await _stockTakingService.GetForCountAsync(id);
        if (document == null || document.Status != StockTakingStatus.InProgress)
        {
            return false;
        }

        _stockTakingId = id;
        Title = $"Инвентаризация {document.DocumentNumber} · {document.Warehouse?.Name}";
        OnPropertyChanged(nameof(Title));
        ErrorMessage = string.Empty;

        Rows.Clear();
        foreach (var item in document.Items
                     .OrderBy(i => i.StorageLocation!.Code)
                     .ThenBy(i => i.Product!.Name))
        {
            var row = new StockTakingRowViewModel(item);
            row.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(StockTakingRowViewModel.ActualText))
                {
                    OnPropertyChanged(nameof(ProgressText));
                }
            };
            Rows.Add(row);
        }

        OnPropertyChanged(nameof(ProgressText));
        return true;
    }

    // Собирает введённые значения и проверяет, что это целые числа не меньше нуля
    private bool TryCollectActuals(out Dictionary<int, int?> actuals)
    {
        ErrorMessage = string.Empty;
        actuals = new Dictionary<int, int?>();

        foreach (var row in Rows)
        {
            if (string.IsNullOrWhiteSpace(row.ActualText))
            {
                actuals[row.ItemId] = null;
                continue;
            }

            if (!int.TryParse(row.ActualText.Trim(), out var value) || value < 0)
            {
                ErrorMessage = $"Неверное количество в строке: {row.ProductName} ({row.LocationCode}). " +
                               "Введите целое число не меньше нуля.";
                return false;
            }

            actuals[row.ItemId] = value;
        }

        return true;
    }

    private async Task SaveAsync()
    {
        if (!TryCollectActuals(out var actuals)) return;

        var result = await _stockTakingService.SaveCountAsync(_stockTakingId, actuals);

        if (result.Success)
        {
            RequestClose?.Invoke(true);
        }
        else
        {
            ErrorMessage = result.Error ?? "Не удалось сохранить пересчёт.";
        }
    }

    private async Task CompleteAsync()
    {
        if (!TryCollectActuals(out var actuals)) return;

        var answer = MessageBox.Show(
            "Завершить инвентаризацию?\n\nУчётные остатки будут исправлены по введённым фактическим данным. " +
            "Отменить это действие нельзя.",
            "Завершение инвентаризации",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (answer != MessageBoxResult.Yes) return;

        var result = await _stockTakingService.CompleteAsync(_stockTakingId, actuals);

        if (result.Success)
        {
            RequestClose?.Invoke(true);
        }
        else
        {
            ErrorMessage = result.Error ?? "Не удалось завершить инвентаризацию.";
        }
    }
}
using System.Collections.ObjectModel;
using WMS.Helpers;
using WMS.Models;
using WMS.Services;

namespace WMS.ViewModels;

public class StockMovementHistoryViewModel : ViewModelBase
{
    private readonly StockMovementService _movementService;

    public ObservableCollection<StockMovementRow> Movements { get; } = new();

    // Первый элемент "Все операции" не входит в enum, поэтому список готовим отдельно
    public List<string> OperationTypeOptions { get; } = new()
    {
        "Все операции",
        OperationTypeDisplay.ToRussian(OperationType.Receipt),
        OperationTypeDisplay.ToRussian(OperationType.Placement),
        OperationTypeDisplay.ToRussian(OperationType.Movement),
        OperationTypeDisplay.ToRussian(OperationType.Picking),
        OperationTypeDisplay.ToRussian(OperationType.Shipment)
    };

    private string _productSearch = string.Empty;
    public string ProductSearch
    {
        get => _productSearch;
        set { if (SetField(ref _productSearch, value)) _ = LoadAsync(); }
    }

    private string _selectedOperationType;
    public string SelectedOperationType
    {
        get => _selectedOperationType;
        set { if (SetField(ref _selectedOperationType, value)) _ = LoadAsync(); }
    }

    public StockMovementHistoryViewModel(StockMovementService movementService)
    {
        _movementService = movementService;
        _selectedOperationType = OperationTypeOptions[0];

        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        OperationType? filter = SelectedOperationType == OperationTypeOptions[0]
            ? null
            : Enum.GetValues<OperationType>().First(t => OperationTypeDisplay.ToRussian(t) == SelectedOperationType);

        var items = await _movementService.GetHistoryAsync(ProductSearch, filter);
        Movements.Clear();
        foreach (var item in items) Movements.Add(item);
    }
}
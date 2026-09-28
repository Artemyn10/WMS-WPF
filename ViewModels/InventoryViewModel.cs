using System.Collections.ObjectModel;
using WMS.Models;
using WMS.Services;

namespace WMS.ViewModels;

public class InventoryViewModel : ViewModelBase
{
    private readonly InventoryService _inventoryService;

    public ObservableCollection<Inventory> InventoryRecords { get; } = new();

    private string _productSearch = string.Empty;
    public string ProductSearch
    {
        get => _productSearch;
        set { if (SetField(ref _productSearch, value)) _ = LoadAsync(); }
    }

    private string _locationSearch = string.Empty;
    public string LocationSearch
    {
        get => _locationSearch;
        set { if (SetField(ref _locationSearch, value)) _ = LoadAsync(); }
    }

    public InventoryViewModel(InventoryService inventoryService)
    {
        _inventoryService = inventoryService;
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        var items = await _inventoryService.GetAllAsync(ProductSearch, LocationSearch);
        InventoryRecords.Clear();
        foreach (var item in items)
        {
            InventoryRecords.Add(item);
        }
    }
}
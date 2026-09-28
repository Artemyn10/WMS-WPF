using System.Collections.ObjectModel;
using System.Windows.Input;
using WMS.Helpers;
using WMS.Services;

namespace WMS.ViewModels;

public class PlacementViewModel : ViewModelBase
{
    private readonly PlacementService _placementService;

    public ObservableCollection<PendingPlacementItem> PendingItems { get; } = new();
    public ObservableCollection<LocationOption> Locations { get; } = new();

    private PendingPlacementItem? _selectedItem;
    public PendingPlacementItem? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (SetField(ref _selectedItem, value))
            {
                _ = OnSelectedItemChangedAsync();
            }
        }
    }

    private LocationOption? _selectedLocation;
    public LocationOption? SelectedLocation
    {
        get => _selectedLocation;
        set => SetField(ref _selectedLocation, value);
    }

    private int _quantity;
    public int Quantity
    {
        get => _quantity;
        set => SetField(ref _quantity, value);
    }

    private string _errorMessage = string.Empty;
    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetField(ref _errorMessage, value);
    }

    private string _successMessage = string.Empty;
    public string SuccessMessage
    {
        get => _successMessage;
        set => SetField(ref _successMessage, value);
    }

    public bool IsEmpty => PendingItems.Count == 0;

    public ICommand PlaceCommand { get; }

    public PlacementViewModel(PlacementService placementService)
    {
        _placementService = placementService;

        PlaceCommand = new RelayCommand(async _ => await PlaceAsync(), _ => SelectedItem != null);

        _ = LoadAsync();
    }

    private async Task LoadAsync(int? reselectReceiptItemId = null)
    {
        var items = await _placementService.GetPendingAsync();

        PendingItems.Clear();
        foreach (var item in items)
        {
            PendingItems.Add(item);
        }

        OnPropertyChanged(nameof(IsEmpty));

        if (reselectReceiptItemId != null)
        {
            SelectedItem = PendingItems.FirstOrDefault(i => i.ReceiptItemId == reselectReceiptItemId);
        }
    }

    private async Task OnSelectedItemChangedAsync()
    {
        Locations.Clear();
        SelectedLocation = null;
        ErrorMessage = string.Empty;

        var item = SelectedItem;
        if (item == null)
        {
            Quantity = 0;
            return;
        }

        Quantity = item.Remaining;

        var options = await _placementService.GetLocationsAsync(item.WarehouseId);

        // Пока грузились ячейки, пользователь мог выбрать другую строку
        if (SelectedItem?.ReceiptItemId != item.ReceiptItemId)
        {
            return;
        }

        foreach (var option in options)
        {
            Locations.Add(option);
        }

        if (Locations.Count == 0)
        {
            ErrorMessage = $"На складе «{item.WarehouseName}» нет ячеек. " +
                           "Администратор должен создать их в разделе «Ячейки».";
        }
    }

    private async Task PlaceAsync()
    {
        SuccessMessage = string.Empty;
        ErrorMessage = string.Empty;

        var item = SelectedItem;
        if (item == null)
        {
            return;
        }

        var location = SelectedLocation;
        var quantity = Quantity;

        var result = await _placementService.PlaceAsync(item.ReceiptItemId, location?.Id ?? 0, quantity);

        if (!result.Success)
        {
            ErrorMessage = result.Error ?? "Ошибка размещения.";
            return;
        }

        SuccessMessage = $"Размещено {quantity} ед. товара «{item.ProductName}» в ячейку {location!.Code}.";
        await LoadAsync(item.ReceiptItemId);
    }
}
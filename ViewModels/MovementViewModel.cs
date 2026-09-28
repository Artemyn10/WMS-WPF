using System.Collections.ObjectModel;
using System.Windows.Input;
using WMS.Helpers;
using WMS.Services;

namespace WMS.ViewModels;

public class MovementViewModel : ViewModelBase
{
    private readonly MovementService _movementService;

    public ObservableCollection<ProductStockOption> Products { get; } = new();
    public ObservableCollection<SourceLocationOption> SourceLocations { get; } = new();
    public ObservableCollection<LocationOption> TargetLocations { get; } = new();

    private ProductStockOption? _selectedProduct;
    public ProductStockOption? SelectedProduct
    {
        get => _selectedProduct;
        set
        {
            if (SetField(ref _selectedProduct, value))
            {
                _ = OnProductChangedAsync();
            }
        }
    }

    private SourceLocationOption? _selectedSourceLocation;
    public SourceLocationOption? SelectedSourceLocation
    {
        get => _selectedSourceLocation;
        set
        {
            if (SetField(ref _selectedSourceLocation, value))
            {
                _ = OnSourceLocationChangedAsync();
            }
        }
    }

    private LocationOption? _selectedTargetLocation;
    public LocationOption? SelectedTargetLocation
    {
        get => _selectedTargetLocation;
        set => SetField(ref _selectedTargetLocation, value);
    }

    private int _quantity = 1;
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

    public ICommand MoveCommand { get; }

    public MovementViewModel(MovementService movementService)
    {
        _movementService = movementService;

        MoveCommand = new RelayCommand(async _ => await MoveAsync(),
            _ => SelectedProduct != null && SelectedSourceLocation != null && SelectedTargetLocation != null);

        _ = LoadProductsAsync();
    }

    private async Task LoadProductsAsync()
    {
        var items = await _movementService.GetProductsWithStockAsync();
        Products.Clear();
        foreach (var item in items) Products.Add(item);
    }

    private async Task OnProductChangedAsync()
    {
        SourceLocations.Clear();
        TargetLocations.Clear();
        SelectedSourceLocation = null;
        SelectedTargetLocation = null;
        ErrorMessage = string.Empty;

        var product = SelectedProduct;
        if (product == null) return;

        var options = await _movementService.GetSourceLocationsAsync(product.ProductId);

        if (SelectedProduct?.ProductId != product.ProductId) return;

        foreach (var option in options) SourceLocations.Add(option);
    }

    private async Task OnSourceLocationChangedAsync()
    {
        TargetLocations.Clear();
        SelectedTargetLocation = null;
        ErrorMessage = string.Empty;

        var source = SelectedSourceLocation;
        if (source == null)
        {
            Quantity = 1;
            return;
        }

        Quantity = source.Available;

        var options = await _movementService.GetTargetLocationsAsync(source.LocationId);

        if (SelectedSourceLocation?.LocationId != source.LocationId) return;

        foreach (var option in options) TargetLocations.Add(option);
    }

    private async Task MoveAsync()
    {
        SuccessMessage = string.Empty;
        ErrorMessage = string.Empty;

        var product = SelectedProduct;
        var source = SelectedSourceLocation;
        var target = SelectedTargetLocation;

        if (product == null || source == null || target == null) return;

        var quantity = Quantity;

        var result = await _movementService.MoveAsync(product.ProductId, source.LocationId, target.Id, quantity);

        if (!result.Success)
        {
            ErrorMessage = result.Error ?? "Ошибка перемещения.";
            return;
        }

        SuccessMessage = $"Перемещено {quantity} ед. товара «{product.Name}» из {source.Code} в {target.Code}.";

        // Обновляем списки, так как остатки изменились
        await LoadProductsAsync();
        SelectedProduct = Products.FirstOrDefault(p => p.ProductId == product.ProductId);
    }
}
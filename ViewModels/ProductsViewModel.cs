using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Extensions.DependencyInjection;
using WMS.Helpers;
using WMS.Models;
using WMS.Services;
using WMS.Views;

namespace WMS.ViewModels;

public class ProductsViewModel : ViewModelBase
{
    private readonly ProductService _productService;
    private readonly CategoryService _categoryService;
    private readonly IServiceProvider _serviceProvider;

    public ObservableCollection<Product> Products { get; } = new();

    // Первый элемент - "Все категории" (Id = 0), дальше реальные категории
    public ObservableCollection<Category> CategoryFilters { get; } = new();

    // Наличие выбранного товара по складам
    public ObservableCollection<ProductStockRow> StockByWarehouse { get; } = new();

    // Права текущего пользователя в этом разделе
    public bool CanEdit => CurrentSession.CanAccess(AppSection.ProductsEdit);
    public bool CanSeePrices => CurrentSession.CanAccess(AppSection.ProductPrices);

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set { if (SetField(ref _searchText, value)) _ = LoadAsync(); }
    }

    private Category? _selectedCategoryFilter;
    public Category? SelectedCategoryFilter
    {
        get => _selectedCategoryFilter;
        set { if (SetField(ref _selectedCategoryFilter, value)) _ = LoadAsync(); }
    }

    private Product? _selectedProduct;
    public Product? SelectedProduct
    {
        get => _selectedProduct;
        set
        {
            if (SetField(ref _selectedProduct, value))
            {
                OnPropertyChanged(nameof(HasNoPhoto));
                _ = LoadDetailsAsync();
            }
        }
    }

    private BitmapImage? _selectedPhoto;
    public BitmapImage? SelectedPhoto
    {
        get => _selectedPhoto;
        set
        {
            if (SetField(ref _selectedPhoto, value))
            {
                OnPropertyChanged(nameof(HasNoPhoto));
            }
        }
    }

    // Надпись "Нет фото" показывается, когда товар выбран, а фотографий у него нет
    public bool HasNoPhoto => SelectedProduct != null && SelectedPhoto == null;

    public int TotalStock => StockByWarehouse.Sum(r => r.Quantity);

    private string _statusMessage = string.Empty;
    public string StatusMessage { get => _statusMessage; set => SetField(ref _statusMessage, value); }

    public ICommand AddCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand DeleteCommand { get; }

    public ProductsViewModel(
        ProductService productService,
        CategoryService categoryService,
        IServiceProvider serviceProvider)
    {
        _productService = productService;
        _categoryService = categoryService;
        _serviceProvider = serviceProvider;

        AddCommand = new RelayCommand(async _ => await OpenEditDialogAsync(null), _ => CanEdit);
        EditCommand = new RelayCommand(async _ => await OpenEditDialogAsync(SelectedProduct), _ => CanEdit && SelectedProduct != null);
        DeleteCommand = new RelayCommand(async _ => await DeleteAsync(), _ => CanEdit && SelectedProduct != null);

        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        var categories = await _categoryService.GetAllAsync();

        CategoryFilters.Clear();
        CategoryFilters.Add(new Category { Id = 0, Name = "Все категории" });
        foreach (var category in categories) CategoryFilters.Add(category);

        // Установка значения сама запускает загрузку списка товаров
        SelectedCategoryFilter = CategoryFilters[0];
    }

    private async Task LoadAsync(int? reselectId = null)
    {
        var items = await _productService.GetCatalogAsync(SearchText, SelectedCategoryFilter?.Id ?? 0);

        Products.Clear();
        foreach (var item in items) Products.Add(item);

        SelectedProduct = reselectId != null
            ? Products.FirstOrDefault(p => p.Id == reselectId)
            : null;
    }

    private async Task LoadDetailsAsync()
    {
        var product = SelectedProduct;

        StockByWarehouse.Clear();
        SelectedPhoto = null;
        OnPropertyChanged(nameof(TotalStock));

        if (product == null)
        {
            return;
        }

        var photoTask = _productService.GetMainPhotoAsync(product.Id);
        var stockTask = _productService.GetStockByWarehouseAsync(product.Id);

        var photoData = await photoTask;
        var stock = await stockTask;

        // Пока данные грузились, пользователь мог выбрать другой товар
        if (SelectedProduct?.Id != product.Id)
        {
            return;
        }

        SelectedPhoto = ImageHelper.FromBytes(photoData);

        foreach (var row in stock) StockByWarehouse.Add(row);
        OnPropertyChanged(nameof(TotalStock));
    }

    private async Task OpenEditDialogAsync(Product? product)
    {
        StatusMessage = string.Empty;

        var window = _serviceProvider.GetRequiredService<ProductEditWindow>();
        var viewModel = (ProductEditViewModel)window.DataContext;

        if (product == null)
        {
            await viewModel.InitializeForAddAsync();
        }
        else
        {
            await viewModel.InitializeForEditAsync(product);
        }

        if (window.ShowDialog() == true)
        {
            await LoadAsync(product?.Id);
        }
    }

    private async Task DeleteAsync()
    {
        StatusMessage = string.Empty;

        var product = SelectedProduct;
        if (product == null) return;

        var answer = MessageBox.Show(
            $"Удалить «{product.Name}» из номенклатуры? Прикреплённые к товару файлы тоже будут удалены.",
            "Удаление",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (answer != MessageBoxResult.Yes) return;

        var result = await _productService.DeleteAsync(product.Id);
        StatusMessage = result.Success ? string.Empty : (result.Error ?? string.Empty);

        if (result.Success) await LoadAsync();
    }
}
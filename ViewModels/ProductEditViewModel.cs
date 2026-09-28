using System.Collections.ObjectModel;
using System.Windows.Input;
using WMS.Helpers;
using WMS.Models;
using WMS.Services;

namespace WMS.ViewModels;

public class ProductEditViewModel : ViewModelBase
{
    private readonly ProductService _productService;
    private readonly CategoryService _categoryService;
    private int _id;

    public ObservableCollection<Category> Categories { get; } = new();

    private string _name = string.Empty;
    public string Name { get => _name; set => SetField(ref _name, value); }

    private string _article = string.Empty;
    public string Article { get => _article; set => SetField(ref _article, value); }

    private string _barcode = string.Empty;
    public string Barcode { get => _barcode; set => SetField(ref _barcode, value); }

    private string _unit = string.Empty;
    public string Unit { get => _unit; set => SetField(ref _unit, value); }

    private string _description = string.Empty;
    public string Description { get => _description; set => SetField(ref _description, value); }

    private Category? _selectedCategory;
    public Category? SelectedCategory { get => _selectedCategory; set => SetField(ref _selectedCategory, value); }

    private string _errorMessage = string.Empty;
    public string ErrorMessage { get => _errorMessage; set => SetField(ref _errorMessage, value); }

    public string WindowTitle { get; private set; } = "Добавить товар";

    public event Action<bool?>? RequestClose;

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    public ProductEditViewModel(ProductService productService, CategoryService categoryService)
    {
        _productService = productService;
        _categoryService = categoryService;

        SaveCommand = new RelayCommand(async _ => await SaveAsync());
        CancelCommand = new RelayCommand(_ => RequestClose?.Invoke(false));
    }

    public async Task InitializeForAddAsync()
    {
        await LoadCategoriesAsync();
        _id = 0;
        Name = Article = Barcode = Unit = Description = string.Empty;
        SelectedCategory = null;
        WindowTitle = "Добавить товар";
        OnPropertyChanged(nameof(WindowTitle));
    }

    public async Task InitializeForEditAsync(Product product)
    {
        await LoadCategoriesAsync();
        _id = product.Id;
        Name = product.Name;
        Article = product.Article;
        Barcode = product.Barcode ?? string.Empty;
        Unit = product.Unit;
        Description = product.Description ?? string.Empty;
        SelectedCategory = Categories.FirstOrDefault(c => c.Id == product.CategoryId);
        WindowTitle = "Изменить товар";
        OnPropertyChanged(nameof(WindowTitle));
    }

    private async Task LoadCategoriesAsync()
    {
        var items = await _categoryService.GetAllAsync();
        Categories.Clear();
        foreach (var item in items)
        {
            Categories.Add(item);
        }
    }

    private async Task SaveAsync()
    {
        var product = new Product
        {
            Id = _id,
            Name = Name,
            Article = Article,
            Barcode = Barcode,
            CategoryId = SelectedCategory?.Id ?? 0,
            Unit = Unit,
            Description = Description
        };

        var result = _id == 0
            ? await _productService.AddAsync(product)
            : await _productService.UpdateAsync(product);

        if (result.Success)
        {
            ErrorMessage = string.Empty;
            RequestClose?.Invoke(true);
        }
        else
        {
            ErrorMessage = result.Error ?? "Ошибка сохранения.";
        }
    }
}
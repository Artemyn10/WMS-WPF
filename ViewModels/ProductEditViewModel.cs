using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using WMS.Helpers;
using WMS.Models;
using WMS.Services;

namespace WMS.ViewModels;

public class ProductEditViewModel : ViewModelBase
{
    private readonly ProductService _productService;
    private readonly CategoryService _categoryService;
    private readonly SupplierService _supplierService;
    private int _id;

    // Файлы, удалённые в окне: из базы они удаляются только при сохранении товара
    private readonly List<int> _removedFileIds = new();

    public ObservableCollection<Category> Categories { get; } = new();

    // Первый элемент - "не указан" (Id = 0), дальше реальные поставщики
    public ObservableCollection<Supplier> Suppliers { get; } = new();

    public ObservableCollection<ProductTypeOption> ProductTypes { get; } = new(ProductTypeOption.All);
    public ObservableCollection<ProductFileRowViewModel> Files { get; } = new();

    private string _name = string.Empty;
    public string Name { get => _name; set => SetField(ref _name, value); }

    private string _article = string.Empty;
    public string Article { get => _article; set => SetField(ref _article, value); }

    private string _barcode = string.Empty;
    public string Barcode { get => _barcode; set => SetField(ref _barcode, value); }

    private string _unit = string.Empty;
    public string Unit { get => _unit; set => SetField(ref _unit, value); }

    private string _releaseYear = string.Empty;
    public string ReleaseYear { get => _releaseYear; set => SetField(ref _releaseYear, value); }

    private string _manufacturer = string.Empty;
    public string Manufacturer { get => _manufacturer; set => SetField(ref _manufacturer, value); }

    private string _purchasePriceText = string.Empty;
    public string PurchasePriceText { get => _purchasePriceText; set => SetField(ref _purchasePriceText, value); }

    private string _accountingPriceText = string.Empty;
    public string AccountingPriceText { get => _accountingPriceText; set => SetField(ref _accountingPriceText, value); }

    private string _characteristics = string.Empty;
    public string Characteristics { get => _characteristics; set => SetField(ref _characteristics, value); }

    private string _description = string.Empty;
    public string Description { get => _description; set => SetField(ref _description, value); }

    private Category? _selectedCategory;
    public Category? SelectedCategory { get => _selectedCategory; set => SetField(ref _selectedCategory, value); }

    private Supplier? _selectedSupplier;
    public Supplier? SelectedSupplier { get => _selectedSupplier; set => SetField(ref _selectedSupplier, value); }

    private ProductTypeOption? _selectedProductType;
    public ProductTypeOption? SelectedProductType { get => _selectedProductType; set => SetField(ref _selectedProductType, value); }

    private ProductFileRowViewModel? _selectedFile;
    public ProductFileRowViewModel? SelectedFile
    {
        get => _selectedFile;
        set
        {
            if (SetField(ref _selectedFile, value))
            {
                _ = LoadPreviewAsync();
            }
        }
    }

    private BitmapImage? _preview;
    public BitmapImage? Preview { get => _preview; set => SetField(ref _preview, value); }

    private string _errorMessage = string.Empty;
    public string ErrorMessage { get => _errorMessage; set => SetField(ref _errorMessage, value); }

    public string WindowTitle { get; private set; } = "Добавить в номенклатуру";

    public event Action<bool?>? RequestClose;

    public ICommand AddPhotoCommand { get; }
    public ICommand AddDocumentCommand { get; }
    public ICommand OpenFileCommand { get; }
    public ICommand RemoveFileCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    public ProductEditViewModel(
        ProductService productService,
        CategoryService categoryService,
        SupplierService supplierService)
    {
        _productService = productService;
        _categoryService = categoryService;
        _supplierService = supplierService;

        AddPhotoCommand = new RelayCommand(_ => AddFiles(ProductFileKind.Photo));
        AddDocumentCommand = new RelayCommand(_ => AddFiles(ProductFileKind.Document));
        OpenFileCommand = new RelayCommand(async _ => await OpenFileAsync(), _ => SelectedFile != null);
        RemoveFileCommand = new RelayCommand(_ => RemoveSelectedFile(), _ => SelectedFile != null);
        SaveCommand = new RelayCommand(async _ => await SaveAsync());
        CancelCommand = new RelayCommand(_ => RequestClose?.Invoke(false));
    }

    public async Task InitializeForAddAsync()
    {
        await LoadListsAsync();

        _id = 0;
        _removedFileIds.Clear();
        Name = Article = Barcode = Unit = ReleaseYear = Manufacturer = string.Empty;
        PurchasePriceText = AccountingPriceText = Characteristics = Description = string.Empty;
        SelectedCategory = null;
        SelectedSupplier = Suppliers[0];
        SelectedProductType = ProductTypes[0];

        Files.Clear();
        SelectedFile = null;
        Preview = null;
        ErrorMessage = string.Empty;
        WindowTitle = "Добавить в номенклатуру";
        OnPropertyChanged(nameof(WindowTitle));
    }

    public async Task InitializeForEditAsync(Product product)
    {
        await LoadListsAsync();

        _id = product.Id;
        _removedFileIds.Clear();
        Name = product.Name;
        Article = product.Article;
        Barcode = product.Barcode ?? string.Empty;
        Unit = product.Unit;
        ReleaseYear = product.ReleaseYear?.ToString() ?? string.Empty;
        Manufacturer = product.Manufacturer ?? string.Empty;
        PurchasePriceText = product.PurchasePrice?.ToString("0.##", CultureInfo.CurrentCulture) ?? string.Empty;
        AccountingPriceText = product.AccountingPrice?.ToString("0.##", CultureInfo.CurrentCulture) ?? string.Empty;
        Characteristics = product.Characteristics ?? string.Empty;
        Description = product.Description ?? string.Empty;
        SelectedCategory = Categories.FirstOrDefault(c => c.Id == product.CategoryId);
        SelectedSupplier = Suppliers.FirstOrDefault(s => s.Id == (product.SupplierId ?? 0)) ?? Suppliers[0];
        SelectedProductType = ProductTypes.First(t => t.Value == product.ItemType);

        Files.Clear();
        SelectedFile = null;
        Preview = null;
        foreach (var file in await _productService.GetFilesAsync(product.Id))
        {
            Files.Add(new ProductFileRowViewModel(file.Id, file.FileName, file.Kind, file.Size, null));
        }

        ErrorMessage = string.Empty;
        WindowTitle = "Изменить номенклатуру";
        OnPropertyChanged(nameof(WindowTitle));
    }

    private async Task LoadListsAsync()
    {
        var categories = await _categoryService.GetAllAsync();
        Categories.Clear();
        foreach (var category in categories) Categories.Add(category);

        var suppliers = await _supplierService.GetAllAsync();
        Suppliers.Clear();
        Suppliers.Add(new Supplier { Id = 0, Name = "— не указан —" });
        foreach (var supplier in suppliers) Suppliers.Add(supplier);
    }

    // Выбор файлов на диске. Файл сразу читается в память и сохранится вместе с товаром.
    private void AddFiles(ProductFileKind kind)
    {
        var isPhoto = kind == ProductFileKind.Photo;

        var dialog = new OpenFileDialog
        {
            Title = isPhoto ? "Выберите фотографии" : "Выберите документы",
            Filter = isPhoto
                ? "Изображения|*.jpg;*.jpeg;*.png;*.bmp;*.gif|Все файлы|*.*"
                : "Документы|*.pdf;*.doc;*.docx;*.xls;*.xlsx;*.txt|Все файлы|*.*",
            Multiselect = true
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        ErrorMessage = string.Empty;

        foreach (var path in dialog.FileNames)
        {
            var info = new FileInfo(path);

            if (info.Length == 0)
            {
                ErrorMessage = $"Файл «{info.Name}» пустой.";
                continue;
            }

            if (info.Length > ProductService.MaxFileSize)
            {
                ErrorMessage = $"Файл «{info.Name}» слишком большой (максимум 10 МБ).";
                continue;
            }

            var data = File.ReadAllBytes(path);

            if (isPhoto && ImageHelper.FromBytes(data) == null)
            {
                ErrorMessage = $"Файл «{info.Name}» не удалось открыть как изображение.";
                continue;
            }

            Files.Add(new ProductFileRowViewModel(0, info.Name, kind, data.Length, data));
        }
    }

    private async Task LoadPreviewAsync()
    {
        Preview = null;

        var file = SelectedFile;
        if (file == null || file.Kind != ProductFileKind.Photo)
        {
            return;
        }

        var data = file.Data ?? await _productService.GetFileDataAsync(file.Id);

        // Пока файл загружался, пользователь мог выбрать другую строку
        if (SelectedFile != file)
        {
            return;
        }

        Preview = ImageHelper.FromBytes(data);
    }

    // Файл сохраняется во временную папку и открывается программой, назначенной для его типа
    private async Task OpenFileAsync()
    {
        var file = SelectedFile;
        if (file == null)
        {
            return;
        }

        var data = file.Data ?? await _productService.GetFileDataAsync(file.Id);
        if (data == null)
        {
            ErrorMessage = "Файл не найден в базе данных.";
            return;
        }

        var folder = Path.Combine(Path.GetTempPath(), "WMS");
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, $"{Guid.NewGuid():N}_{file.FileName}");

        await File.WriteAllBytesAsync(path, data);

        try
        {
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch (Exception)
        {
            ErrorMessage = "Не удалось открыть файл: для этого типа файлов нет подходящей программы.";
        }
    }

    private void RemoveSelectedFile()
    {
        var file = SelectedFile;
        if (file == null)
        {
            return;
        }

        // Сохранённый файл удалится из базы только при нажатии "Сохранить"
        if (file.Id > 0)
        {
            _removedFileIds.Add(file.Id);
        }

        Files.Remove(file);
        SelectedFile = null;
    }

    private static bool TryParseMoney(string text, out decimal? value)
    {
        value = null;

        if (string.IsNullOrWhiteSpace(text))
        {
            return true;
        }

        // Принимаем и запятую, и точку: "1500,5" и "1500.5"
        var normalized = text.Trim().Replace(',', '.');
        if (!decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
        {
            return false;
        }

        value = parsed;
        return true;
    }

    private async Task SaveAsync()
    {
        ErrorMessage = string.Empty;

        int? releaseYear = null;
        if (!string.IsNullOrWhiteSpace(ReleaseYear))
        {
            if (!int.TryParse(ReleaseYear.Trim(), out var parsedYear))
            {
                ErrorMessage = "Год выпуска должен быть числом.";
                return;
            }

            releaseYear = parsedYear;
        }

        if (!TryParseMoney(PurchasePriceText, out var purchasePrice))
        {
            ErrorMessage = "Закупочная стоимость должна быть числом.";
            return;
        }

        if (!TryParseMoney(AccountingPriceText, out var accountingPrice))
        {
            ErrorMessage = "Учётная стоимость должна быть числом.";
            return;
        }

        int? supplierId = SelectedSupplier != null && SelectedSupplier.Id > 0 ? SelectedSupplier.Id : null;

        var product = new Product
        {
            Id = _id,
            Name = Name,
            Article = Article,
            Barcode = Barcode,
            CategoryId = SelectedCategory?.Id ?? 0,
            Unit = Unit,
            Description = Description,
            ReleaseYear = releaseYear,
            ItemType = SelectedProductType?.Value ?? ProductType.Goods,
            Manufacturer = Manufacturer,
            SupplierId = supplierId,
            Characteristics = Characteristics,
            PurchasePrice = purchasePrice,
            AccountingPrice = accountingPrice
        };

        var newFiles = Files
            .Where(f => f.Id == 0 && f.Data != null)
            .Select(f => new ProductFileInput { FileName = f.FileName, Kind = f.Kind, Data = f.Data! })
            .ToList();

        var result = _id == 0
            ? await _productService.AddAsync(product, newFiles)
            : await _productService.UpdateAsync(product, newFiles, _removedFileIds);

        if (result.Success)
        {
            RequestClose?.Invoke(true);
        }
        else
        {
            ErrorMessage = result.Error ?? "Ошибка сохранения.";
        }
    }
}
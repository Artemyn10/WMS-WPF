using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using WMS.Helpers;
using WMS.Models;
using WMS.Services;
using WMS.Views;

namespace WMS.ViewModels;

public class CategoriesViewModel : ViewModelBase
{
    private readonly CategoryService _categoryService;
    private readonly IServiceProvider _serviceProvider;

    public ObservableCollection<Category> Categories { get; } = new();

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetField(ref _searchText, value))
            {
                _ = LoadAsync();
            }
        }
    }

    private Category? _selectedCategory;
    public Category? SelectedCategory
    {
        get => _selectedCategory;
        set => SetField(ref _selectedCategory, value);
    }

    private string _statusMessage = string.Empty;
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetField(ref _statusMessage, value);
    }

    public ICommand AddCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand DeleteCommand { get; }

    public CategoriesViewModel(CategoryService categoryService, IServiceProvider serviceProvider)
    {
        _categoryService = categoryService;
        _serviceProvider = serviceProvider;

        AddCommand = new RelayCommand(async _ => await OpenEditDialogAsync(null));
        EditCommand = new RelayCommand(async _ => await OpenEditDialogAsync(SelectedCategory), _ => SelectedCategory != null);
        DeleteCommand = new RelayCommand(async _ => await DeleteAsync(), _ => SelectedCategory != null);

        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        var items = await _categoryService.GetAllAsync(SearchText);
        Categories.Clear();
        foreach (var item in items)
        {
            Categories.Add(item);
        }
    }

    private async Task OpenEditDialogAsync(Category? category)
    {
        var window = _serviceProvider.GetRequiredService<CategoryEditWindow>();
        var viewModel = (CategoryEditViewModel)window.DataContext;

        if (category == null)
        {
            viewModel.InitializeForAdd();
        }
        else
        {
            viewModel.InitializeForEdit(category);
        }

        var result = window.ShowDialog();
        if (result == true)
        {
            await LoadAsync();
        }
    }

    private async Task DeleteAsync()
    {
        if (SelectedCategory == null)
        {
            return;
        }

        var result = await _categoryService.DeleteAsync(SelectedCategory.Id);
        StatusMessage = result.Success ? string.Empty : (result.Error ?? string.Empty);

        if (result.Success)
        {
            await LoadAsync();
        }
    }
}
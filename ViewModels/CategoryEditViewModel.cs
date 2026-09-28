using System.Windows.Input;
using WMS.Helpers;
using WMS.Models;
using WMS.Services;

namespace WMS.ViewModels;

public class CategoryEditViewModel : ViewModelBase
{
    private readonly CategoryService _categoryService;
    private int _id;

    private string _name = string.Empty;
    public string Name
    {
        get => _name;
        set => SetField(ref _name, value);
    }

    private string _errorMessage = string.Empty;
    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetField(ref _errorMessage, value);
    }

    public string WindowTitle { get; private set; } = "Добавить категорию";

    public event Action<bool?>? RequestClose;

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    public CategoryEditViewModel(CategoryService categoryService)
    {
        _categoryService = categoryService;
        SaveCommand = new RelayCommand(async _ => await SaveAsync());
        CancelCommand = new RelayCommand(_ => RequestClose?.Invoke(false));
    }

    public void InitializeForAdd()
    {
        _id = 0;
        Name = string.Empty;
        WindowTitle = "Добавить категорию";
        OnPropertyChanged(nameof(WindowTitle));
    }

    public void InitializeForEdit(Category category)
    {
        _id = category.Id;
        Name = category.Name;
        WindowTitle = "Изменить категорию";
        OnPropertyChanged(nameof(WindowTitle));
    }

    private async Task SaveAsync()
    {
        var result = _id == 0
            ? await _categoryService.AddAsync(Name)
            : await _categoryService.UpdateAsync(_id, Name);

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
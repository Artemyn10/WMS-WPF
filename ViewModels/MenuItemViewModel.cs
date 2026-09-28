using System.Windows.Input;
using WMS.Helpers;

namespace WMS.ViewModels;

public class MenuItemViewModel : ViewModelBase
{
    public string Title { get; }
    public bool AdminOnly { get; }
    public ICommand NavigateCommand { get; }

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetField(ref _isSelected, value);
    }

    public MenuItemViewModel(string title, bool adminOnly, Action onSelected)
    {
        Title = title;
        AdminOnly = adminOnly;
        NavigateCommand = new RelayCommand(_ => onSelected());
    }
}
using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using WMS.Helpers;
using WMS.Models;
using WMS.Services;
using WMS.Views;

namespace WMS.ViewModels;

public class UsersViewModel : ViewModelBase
{
    private readonly UserService _userService;
    private readonly IServiceProvider _serviceProvider;

    public ObservableCollection<User> Users { get; } = new();

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set { if (SetField(ref _searchText, value)) _ = LoadAsync(); }
    }

    private User? _selectedUser;
    public User? SelectedUser
    {
        get => _selectedUser;
        set => SetField(ref _selectedUser, value);
    }

    private string _statusMessage = string.Empty;
    public string StatusMessage { get => _statusMessage; set => SetField(ref _statusMessage, value); }

    public ICommand AddCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand DeleteCommand { get; }

    public UsersViewModel(UserService userService, IServiceProvider serviceProvider)
    {
        _userService = userService;
        _serviceProvider = serviceProvider;

        AddCommand = new RelayCommand(async _ => await OpenEditDialogAsync(null));
        EditCommand = new RelayCommand(async _ => await OpenEditDialogAsync(SelectedUser), _ => SelectedUser != null);
        DeleteCommand = new RelayCommand(async _ => await DeleteAsync(), _ => SelectedUser != null);

        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        var items = await _userService.GetAllAsync(SearchText);
        Users.Clear();
        foreach (var item in items) Users.Add(item);
    }

    private async Task OpenEditDialogAsync(User? user)
    {
        var window = _serviceProvider.GetRequiredService<UserEditWindow>();
        var viewModel = (UserEditViewModel)window.DataContext;

        if (user == null) viewModel.InitializeForAdd();
        else viewModel.InitializeForEdit(user);

        if (window.ShowDialog() == true)
        {
            await LoadAsync();
        }
    }

    private async Task DeleteAsync()
    {
        if (SelectedUser == null) return;

        var result = await _userService.DeleteAsync(SelectedUser.Id);
        StatusMessage = result.Success ? string.Empty : (result.Error ?? string.Empty);

        if (result.Success) await LoadAsync();
    }
}
using System.Collections.ObjectModel;
using System.Windows.Input;
using WMS.Helpers;
using WMS.Models;
using WMS.Services;

namespace WMS.ViewModels;

public class UserEditViewModel : ViewModelBase
{
    private readonly UserService _userService;
    private readonly WarehouseService _warehouseService;
    private int _id;

    public ObservableCollection<UserRoleOption> Roles { get; } = new(UserRoleOption.All);

    // Первый элемент - "не выбран" (Id = 0), дальше реальные склады
    public ObservableCollection<Warehouse> Warehouses { get; } = new();

    private string _login = string.Empty;
    public string Login { get => _login; set => SetField(ref _login, value); }

    private string _password = string.Empty;
    public string Password { get => _password; set => SetField(ref _password, value); }

    private string _fullName = string.Empty;
    public string FullName { get => _fullName; set => SetField(ref _fullName, value); }

    private UserRoleOption? _selectedRole;
    public UserRoleOption? SelectedRole { get => _selectedRole; set => SetField(ref _selectedRole, value); }

    private Warehouse? _selectedWarehouse;
    public Warehouse? SelectedWarehouse { get => _selectedWarehouse; set => SetField(ref _selectedWarehouse, value); }

    private string _passwordHint = string.Empty;
    public string PasswordHint { get => _passwordHint; set => SetField(ref _passwordHint, value); }

    private string _errorMessage = string.Empty;
    public string ErrorMessage { get => _errorMessage; set => SetField(ref _errorMessage, value); }

    public string WindowTitle { get; private set; } = "Добавить пользователя";

    public event Action<bool?>? RequestClose;

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    public UserEditViewModel(UserService userService, WarehouseService warehouseService)
    {
        _userService = userService;
        _warehouseService = warehouseService;

        SaveCommand = new RelayCommand(async _ => await SaveAsync());
        CancelCommand = new RelayCommand(_ => RequestClose?.Invoke(false));
    }

    public async Task InitializeForAddAsync()
    {
        await LoadWarehousesAsync();
        _id = 0;
        Login = string.Empty;
        Password = string.Empty;
        FullName = string.Empty;
        SelectedRole = Roles.First(r => r.Value == UserRole.Storekeeper);
        SelectedWarehouse = Warehouses[0];
        PasswordHint = "Пароль обязателен для нового пользователя.";
        ErrorMessage = string.Empty;
        WindowTitle = "Добавить пользователя";
        OnPropertyChanged(nameof(WindowTitle));
    }

    public async Task InitializeForEditAsync(User user)
    {
        await LoadWarehousesAsync();
        _id = user.Id;
        Login = user.Login;
        Password = string.Empty;
        FullName = user.FullName;
        SelectedRole = Roles.First(r => r.Value == user.Role);
        SelectedWarehouse = Warehouses.FirstOrDefault(w => w.Id == (user.WarehouseId ?? 0)) ?? Warehouses[0];
        PasswordHint = "Оставьте поле пустым, чтобы не менять текущий пароль.";
        ErrorMessage = string.Empty;
        WindowTitle = "Изменить пользователя";
        OnPropertyChanged(nameof(WindowTitle));
    }

    private async Task LoadWarehousesAsync()
    {
        var items = await _warehouseService.GetAllAsync();
        Warehouses.Clear();
        Warehouses.Add(new Warehouse { Id = 0, Name = "— не выбран —" });
        foreach (var item in items) Warehouses.Add(item);
    }

    private async Task SaveAsync()
    {
        var role = SelectedRole?.Value ?? UserRole.Storekeeper;
        int? warehouseId = SelectedWarehouse != null && SelectedWarehouse.Id > 0 ? SelectedWarehouse.Id : null;

        var result = _id == 0
            ? await _userService.AddAsync(Login, Password, FullName, role, warehouseId)
            : await _userService.UpdateAsync(_id, Login, Password, FullName, role, warehouseId);

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
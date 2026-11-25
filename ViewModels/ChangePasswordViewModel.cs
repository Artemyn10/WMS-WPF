using System.Windows.Input;
using WMS.Helpers;
using WMS.Services;

namespace WMS.ViewModels;

public class ChangePasswordViewModel : ViewModelBase
{
    private readonly UserService _userService;

    // Пароли заполняются из code-behind, потому что PasswordBox нельзя привязать через Binding
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;

    private string _errorMessage = string.Empty;
    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetField(ref _errorMessage, value);
    }

    public event Action<bool?>? RequestClose;

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    public ChangePasswordViewModel(UserService userService)
    {
        _userService = userService;

        SaveCommand = new RelayCommand(async _ => await SaveAsync());
        CancelCommand = new RelayCommand(_ => RequestClose?.Invoke(false));
    }

    private async Task SaveAsync()
    {
        var user = CurrentSession.CurrentUser;
        if (user == null)
        {
            ErrorMessage = "Пользователь не определён.";
            return;
        }

        var result = await _userService.ChangePasswordAsync(
            user.Id, CurrentPassword, NewPassword, ConfirmPassword);

        if (result.Success)
        {
            ErrorMessage = string.Empty;
            RequestClose?.Invoke(true);
        }
        else
        {
            ErrorMessage = result.Error ?? "Не удалось изменить пароль.";
        }
    }
}
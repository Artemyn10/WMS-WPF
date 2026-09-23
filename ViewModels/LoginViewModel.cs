using System.Windows;
using System.Windows.Input;
using WMS.Helpers;
using WMS.Services;

namespace WMS.ViewModels;

public class LoginViewModel : ViewModelBase
{
    private readonly AuthService _authService;

    private string _login = string.Empty;
    public string Login
    {
        get => _login;
        set => SetField(ref _login, value);
    }

    private string _errorMessage = string.Empty;
    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetField(ref _errorMessage, value);
    }

    public bool IsSignedIn { get; private set; }

    public ICommand SignInCommand { get; }

    // Пароль передаётся отдельно из code-behind, т.к. PasswordBox нельзя биндить напрямую (небезопасно)
    public LoginViewModel(AuthService authService)
    {
        _authService = authService;
        SignInCommand = new RelayCommand(async _ => await SignInAsync());
    }

    public Func<string>? GetPassword { get; set; }

    private async Task SignInAsync()
    {
        var password = GetPassword?.Invoke() ?? string.Empty;
        var result = await _authService.SignInAsync(Login, password);

        if (result.Success)
        {
            ErrorMessage = string.Empty;
            IsSignedIn = true;
            OnPropertyChanged(nameof(IsSignedIn));
        }
        else
        {
            ErrorMessage = result.ErrorMessage ?? "Ошибка входа.";
        }
    }
}
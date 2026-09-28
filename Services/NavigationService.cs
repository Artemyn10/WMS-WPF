using WMS.ViewModels;

namespace WMS.Services;

public class NavigationService
{
    public event Action<ViewModelBase>? CurrentViewChanged;

    public void NavigateTo(ViewModelBase viewModel)
    {
        CurrentViewChanged?.Invoke(viewModel);
    }
}
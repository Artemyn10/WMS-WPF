namespace WMS.ViewModels;

public class DashboardCardViewModel
{
    public string Title { get; }
    public int Value { get; }

    public DashboardCardViewModel(string title, int value)
    {
        Title = title;
        Value = value;
    }
}
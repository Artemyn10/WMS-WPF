namespace WMS.ViewModels;

public class SummaryCardViewModel
{
    public string Title { get; }
    public string Value { get; }

    public SummaryCardViewModel(string title, string value)
    {
        Title = title;
        Value = value;
    }
}
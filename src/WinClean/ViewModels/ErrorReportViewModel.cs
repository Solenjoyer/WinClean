using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinClean.Services.Shell;

namespace WinClean.ViewModels;

public sealed partial class ErrorReportViewModel : ObservableObject
{
    private readonly string _logDirectory;

    private readonly ShellLinks _links;

    public ErrorReportViewModel(string message, string details, string logDirectory, ShellLinks links)
    {
        Message = message;
        Details = details;
        _logDirectory = logDirectory;
        _links = links;
    }

    public string Message { get; }

    public string Details { get; }

    [RelayCommand]
    private void OpenLogFolder() => _links.OpenFolder(_logDirectory);

    [RelayCommand]
    private void Report() => _links.OpenUrl(new Uri(AppInfo.IssuesUrl));
}

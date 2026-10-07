namespace WinClean.Dialogs;

/// <summary>Modal questions, behind an interface so view models stay free of windows.</summary>
public interface IDialogService
{
    bool ConfirmEndProcess(EndProcessPrompt prompt);
}

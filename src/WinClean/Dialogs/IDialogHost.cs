namespace WinClean.Dialogs;

/// <summary>The main window dims its content while a dialog is open, so the dialog reads as part of it.</summary>
public interface IDialogHost
{
    void SetDialogOpen(bool open);
}

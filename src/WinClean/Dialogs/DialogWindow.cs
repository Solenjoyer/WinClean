using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using WinClean.Native;

namespace WinClean.Dialogs;

/// <summary>
/// Base for every dialog: an owned, centred, fixed-size window. Real windows give modality, focus
/// trapping, Esc/Enter handling and Narrator announcements for free; an in-window overlay would not.
/// </summary>
public class DialogWindow : Window
{
    public DialogWindow()
    {
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false;
        SizeToContent = SizeToContent.Height;
        FontFamily = new FontFamily("Segoe UI Variable Text, Segoe UI");
        UseLayoutRounding = true;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        // Dialogs have no minimise box; NoResize already drops the maximise box.
        var handle = new WindowInteropHelper(this).Handle;
        var style = User32.GetWindowLongPtrW(handle, User32.GWL_STYLE);
        User32.SetWindowLongPtrW(handle, User32.GWL_STYLE, style & ~(nint)User32.WS_MINIMIZEBOX);
    }
}

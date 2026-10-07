using System.ComponentModel;
using System.Runtime.InteropServices;

namespace WinClean.Services.Monitoring;

internal static class Win32Reason
{
    /// <summary>"Access is denied (5)" for the last Win32 error of the calling thread.</summary>
    public static string LastError()
    {
        var code = Marshal.GetLastPInvokeError();
        return $"{new Win32Exception(code).Message.TrimEnd('.')} ({code})";
    }
}

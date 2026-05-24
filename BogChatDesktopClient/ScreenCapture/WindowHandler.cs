using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;

namespace BogChatDesktopClient.ScreenCapture;

public static class WindowHandler
{
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr hWnd, ref Rectangle lpRect);

    public static Rectangle GetProcessWindowBounds(string processName)
    {
        var processToRecord = GetProcessByName(processName);
        if (processToRecord == null) return new Rectangle();

        // 2. Get the handle to the main window
        var hWnd = processToRecord.MainWindowHandle;
        if (hWnd == IntPtr.Zero) throw new Exception("Process has no main window.");

        // 3. Retrieve the bounds
        var rect = new Rectangle();
        if (GetWindowRect(hWnd, ref rect))
        {
            rect.Width -= rect.Left;
            rect.Height -= rect.Top;
            return rect;
        }

        throw new Exception("Could not get window bounds.");
    }

    private static Process? GetProcessByName(string processName)
    {
        var processes = Process.GetProcesses().Where(process => !string.IsNullOrEmpty(process.MainWindowTitle))
            .ToList();
        return processes.FirstOrDefault(process => process.ProcessName.Contains(processName));
    }

    private static Process? GetProcessById(int processId)
    {
        var processes = Process.GetProcesses().Where(process => !string.IsNullOrEmpty(process.MainWindowTitle))
            .ToList();
        return processes.FirstOrDefault(process => process.Id == processId);
    }

    private static Process? GetSpotifyProcess()
    {
        return GetProcessByName("Spotify");
    }
}
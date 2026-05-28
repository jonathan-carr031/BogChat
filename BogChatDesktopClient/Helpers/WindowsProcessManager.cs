using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace BogChatDesktopClient.Helpers;

public static class WindowsProcessManager {
    public static List<Process> GetStreamableProcesses() {
        var processes = Process.GetProcesses().Where(process =>
            !string.IsNullOrEmpty(process.MainWindowTitle) && process.Id != GetOwnProcess());

        return processes.ToList();
    }

    public static uint GetOwnProcess() {
        return (uint)Environment.ProcessId;
    }
}
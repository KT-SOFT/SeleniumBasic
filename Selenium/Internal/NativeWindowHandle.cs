using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace Selenium.Internal {
    internal static class NativeWindowHandle {
        private delegate bool EnumWindowProc(IntPtr window, IntPtr parameter);
        [DllImport("user32.dll")] private static extern bool EnumWindows(EnumWindowProc callback, IntPtr parameter);
        [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr window);
        [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr window, out uint process);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowText(IntPtr window, StringBuilder text, int length);

        internal static string Find(int driverProcessId, string title) {
            if (driverProcessId <= 0) return "0";
            HashSet<int> owned = new HashSet<int>();
            owned.Add(driverProcessId);
            ProcessExt.ProcessRelationship[] relationships = ProcessExt.GetProcessRelationships();
            bool added;
            do {
                added = false;
                foreach (ProcessExt.ProcessRelationship item in relationships)
                    if (owned.Contains(item.ParentPid) && owned.Add(item.Pid)) added = true;
            } while (added);
            for (int attempt = 0; attempt < 6; attempt++) {
                System.Collections.Generic.List<IntPtr> windows = new System.Collections.Generic.List<IntPtr>();
                System.Collections.Generic.List<IntPtr> matches = new System.Collections.Generic.List<IntPtr>();
                EnumWindows(delegate(IntPtr window, IntPtr ignored) {
                    uint pid;
                    GetWindowThreadProcessId(window, out pid);
                    if (!owned.Contains((int)pid) || !IsWindowVisible(window)) return true;
                    StringBuilder caption = new StringBuilder(4096);
                    if (GetWindowText(window, caption, caption.Capacity) == 0) return true;
                    windows.Add(window);
                    string value = caption.ToString();
                    if (!String.IsNullOrEmpty(title) && (value == title || value.StartsWith(title + " - ", StringComparison.Ordinal) || value.StartsWith(title + " — ", StringComparison.Ordinal))) matches.Add(window);
                    return true;
                }, IntPtr.Zero);
                if (matches.Count == 1) return matches[0].ToInt64().ToString(CultureInfo.InvariantCulture);
                if (windows.Count == 1) return windows[0].ToInt64().ToString(CultureInfo.InvariantCulture);
                if (attempt < 5) Thread.Sleep(50);
            }
            // Never guess another user's browser window from a similar title.
            return "0";
        }
    }
}

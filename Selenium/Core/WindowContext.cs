using System;

namespace Selenium.Core {
    // A window object is an identity, not proof that the remote browser still
    // focuses that window. Popups can close themselves outside this client.
    class WindowContext {
        private readonly RemoteSession _session;
        private List _cachedWindows = new List(10);
        internal Window _previousWindow;
        internal Window _currentWindow;

        public WindowContext(RemoteSession session) {
            _session = session;
            _currentWindow = new Window(session, this, null);
            _previousWindow = _currentWindow;
            _cachedWindows.Add(_currentWindow);
        }

        internal static List GetWindowsHandles(RemoteSession session) {
            return (List)session.Send(RequestMethod.GET, "/window_handles");
        }
        internal static string GetCurrentTitle(RemoteSession session) {
            return (string)session.Send(RequestMethod.GET, "/title");
        }
        internal static string ActivateWindow(RemoteSession session, string name) {
            session.Send(RequestMethod.POST, "/window", "name", name);
            return (string)session.Send(RequestMethod.GET, "/window_handle");
        }
        private static bool ContainsHandle(List handles, string handle) {
            if (handle == null) return false;
            foreach (string item in handles)
                if (String.Equals(item, handle, StringComparison.Ordinal)) return true;
            return false;
        }
        private Window Remember(string handle) {
            foreach (Window item in _cachedWindows)
                if (String.Equals(item.KnownHandle, handle, StringComparison.Ordinal)) return item;
            if (_currentWindow != null && _currentWindow.KnownHandle == null) {
                _currentWindow.BindHandle(handle);
                return _currentWindow;
            }
            Window window = new Window(_session, this, handle);
            _cachedWindows.Add(window);
            return window;
        }
        private void SetCurrent(Window window) {
            if (!Object.ReferenceEquals(_currentWindow, window)) {
                _previousWindow = _currentWindow;
                _currentWindow = window;
            }
        }
        public void ListWindows(out List windows, out List handles) {
            handles = GetWindowsHandles(_session);
            windows = new List(handles.Count);
            foreach (string handle in handles) windows.Add(Remember(handle));
            _cachedWindows = windows;
        }
        public Window CurrentWindow {
            get {
                string actual = (string)_session.Send(RequestMethod.GET, "/window_handle");
                SetCurrent(Remember(actual));
                return _currentWindow;
            }
        }
        internal bool IsAlive(Window window) {
            try { return ContainsHandle(GetWindowsHandles(_session), window.KnownHandle); }
            catch (Errors.NoSuchDriverError) { return false; }
            catch (Errors.NoSuchWindowError) { return false; }
        }
        internal bool TryRecoverClosedWindow() {
            List handles;
            try {
                handles = (List)_session.SendRaw(RequestMethod.GET, "/window_handles", null);
            } catch (Errors.NoSuchDriverError) { return false; }
            if (handles == null || handles.Count == 0) return false;
            string preferred = _previousWindow == null ? null : _previousWindow.KnownHandle;
            if (!ContainsHandle(handles, preferred)) {
                preferred = _currentWindow == null ? null : _currentWindow.KnownHandle;
                if (!ContainsHandle(handles, preferred)) preferred = (string)handles[0];
            }
            var data = new Dictionary();
            data.Add("name", preferred);
            _session.SendRaw(RequestMethod.POST, "/window", data);
            Window survivor = Remember(preferred);
            _currentWindow = survivor;
            if (_previousWindow == null || !ContainsHandle(handles, _previousWindow.KnownHandle))
                _previousWindow = survivor;
            List live = new List(handles.Count);
            foreach (string handle in handles) live.Add(Remember(handle));
            _cachedWindows = live;
            return true;
        }
        internal void WindowClosed(Window closed) {
            // DELETE of the last window can also terminate the driver session.
            try { TryRecoverClosedWindow(); }
            catch (Errors.NoSuchDriverError) { }
            catch (Errors.NoSuchWindowError) { }
        }
        public Window RecoverWindow() {
            try { return CurrentWindow; }
            catch (Errors.NoSuchWindowError) {
                if (TryRecoverClosedWindow()) return _currentWindow;
                throw;
            }
        }
        public Window SwitchToWindowByTitle(string title, int timeout = -1) {
            DateTime endTime = _session.GetEndTime(timeout);
            Window original = CurrentWindow;
            while (true) {
                List windows, handles;
                ListWindows(out windows, out handles);
                foreach (Window win in windows) {
                    try {
                        ActivateWindow(win);
                        if (String.Equals(GetCurrentTitle(_session), title, StringComparison.Ordinal)) return win;
                    } catch (Errors.NoSuchWindowError) { }
                }
                // A failed title search must not leave the remote browser on
                // an arbitrary scanned window while the local cache says otherwise.
                if (ContainsHandle(handles, original.KnownHandle)) ActivateWindow(original);
                else TryRecoverClosedWindow();
                if (DateTime.UtcNow >= endTime) throw new Errors.NoSuchWindowError(title);
                SysWaiter.Wait();
            }
        }
        public Window SwitchToWindowByName(string name, int timeout = -1) {
            DateTime endTime = _session.GetEndTime(timeout);
            Window before = CurrentWindow;
            while (true) {
                try {
                    string actual = ActivateWindow(_session, name);
                    Window window = Remember(actual);
                    _previousWindow = before;
                    _currentWindow = window;
                    return window;
                } catch (Errors.NoSuchWindowError) { }
                if (DateTime.UtcNow >= endTime) throw new Errors.NoSuchWindowError(name);
                SysWaiter.Wait();
            }
        }
        public Window SwitchToNextWindow(int timeout = -1) {
            DateTime endTime = _session.GetEndTime(timeout);
            Window current = CurrentWindow;
            while (true) {
                List windows, handles;
                ListWindows(out windows, out handles);
                int index = -1;
                for (int i = 0; i < handles.Count; i++)
                    if (String.Equals((string)handles[i], current.KnownHandle, StringComparison.Ordinal)) { index = i; break; }
                // The previous window is a valid next target when two windows
                // exist. Excluding it made the original implementation time out.
                for (int offset = 1; offset <= handles.Count; offset++) {
                    int next = (index + offset) % handles.Count;
                    Window candidate = (Window)windows[next];
                    if (!String.Equals(candidate.KnownHandle, current.KnownHandle, StringComparison.Ordinal))
                        return ActivateWindow(candidate);
                }
                if (DateTime.UtcNow >= endTime) throw new Errors.NoSuchWindowError();
                SysWaiter.Wait();
            }
        }
        public Window SwitchToPreviousWindow() {
            List windows, handles;
            ListWindows(out windows, out handles);
            if (_previousWindow != null && ContainsHandle(handles, _previousWindow.KnownHandle))
                return ActivateWindow(_previousWindow);
            if (TryRecoverClosedWindow()) return _currentWindow;
            throw new Errors.NoSuchWindowError();
        }
        internal Window ActivateWindow(Window window) {
            // Always tell the remote browser. Equality with the local cached
            // object does not imply focus after a self-closing popup.
            ActivateWindow(_session, window.Handle);
            SetCurrent(window);
            return window;
        }
    }
}

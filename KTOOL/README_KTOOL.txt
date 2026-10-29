SeleniumBasic KTOOL 2.0.9.1
Upstream: florentbr/SeleniumBasic 2.0.9.0, BSD license (LICENSE.txt).
Changes: recover the live parent after self-closing popups; synchronize actual window focus; fix two-window switching and failed title-search restoration; expose browser Handle, WindowHandles, IsAlive and NativeHandle to VBA; apply configured transport timeout to headers/body and existing sessions.
The original COM GUIDs, ProgIDs, assembly identity and signing key are retained. Windows 10/11 and .NET Framework 4.8 are supported.
Run SeleniumBasic-KTOOL-2.0.9.1.exe: no component selection or batch file is required. Required library and registered browser drivers are prepared automatically. Existing user-created profiles and files are retained.
VBA example:
  Dim home As Selenium.Window
  Set home = driver.Window
  Debug.Print home.Handle
  Debug.Print home.NativeHandle
  Debug.Print driver.WindowHandles.Count
  'After a popup closes:
  driver.SwitchToWindowByHandle home.Handle, 5000
  Set item = driver.FindElementById("your-element-id")
A browser Handle identifies a tab/window for WebDriver. NativeHandle is the decimal Win32 HWND; tabs in the same OS window can share it. NativeHandle returns "0" for remote or ambiguous native windows.
After a page reload, find page elements again instead of reusing a stale WebElement.
Transport timeout can be set after Start: driver.Timeouts.Server = 15000
A click or script is never automatically replayed after recovery.
Logs: KTOOL_DriverSetup.log and KTOOL_chrome_driver.json / KTOOL_edge_driver.json inside the SeleniumBasic install directory.
Source and releases: https://github.com/KT-SOFT/SeleniumBasic
VBA error handling (no update dialog is opened):
  On Error GoTo Failed
  driver.Start "chrome"
  Exit Sub
Failed:
  Debug.Print driver.LastErrorCode
  Debug.Print driver.LastErrorMessage
  Debug.Print driver.LastErrorStage
  Debug.Print driver.LastErrorTime
  Debug.Print driver.LastErrorJson
  driver.ClearLastError
Code 1001 identifies automatic driver-preparation failure. Standard WebDriver errors retain their protocol codes (for example 23: closed/missing window, 26: open alert). Err.Number and Err.Description remain available.
Successful automatic recovery does not create an error. Stored diagnostics remain readable until ClearLastError.
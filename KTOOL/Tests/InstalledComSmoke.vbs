Option Explicit
Dim driver, mainWindow, savedHandle, testRoot, browser, fso, page, popup
testRoot = WScript.Arguments(0)
browser = WScript.Arguments(1)
Set fso = CreateObject("Scripting.FileSystemObject")
If Not fso.FolderExists(testRoot) Then fso.CreateFolder testRoot
Set page = fso.CreateTextFile(testRoot & "\main.html", True)
page.Write "<html><title>KTOOL COM Main</title><body><div id='value'>main-data</div><button id='open' onclick=""window.open('popup.html','child','width=450,height=350')"">Open</button></body></html>"
page.Close
Set popup = fso.CreateTextFile(testRoot & "\popup.html", True)
popup.Write "<html><title>KTOOL COM Popup</title><body>popup<script>setTimeout(function(){window.close()},2500);</script></body></html>"
popup.Close
If browser = "edge" Then
    Set driver = CreateObject("Selenium.EdgeDriver")
Else
    Set driver = CreateObject("Selenium.ChromeDriver")
End If
If driver.LastErrorCode <> 0 Then Fail "Initial error state"
driver.ClearLastError
driver.AddArgument "--user-data-dir=" & testRoot & "\profile"
driver.AddArgument "--no-first-run"
driver.AddArgument "--no-default-browser-check"
driver.AddArgument "--disable-popup-blocking"
driver.Start
driver.Get "file:///" & Replace(testRoot, "\", "/") & "/main.html"
Set mainWindow = driver.Window
savedHandle = mainWindow.Handle
If savedHandle = "" Or mainWindow.NativeHandle = "0" Then Fail "COM handles"
WScript.Echo "PASS: COM exposes browser handle and native HWND"
driver.FindElementById("open").Click
Set popup = driver.SwitchToNextWindow(3000)
WScript.Sleep 2900
If driver.Title <> "KTOOL COM Main" Then Fail "Popup recovery title"
If driver.FindElementById("value").Text <> "main-data" Then Fail "Popup recovery element"
If driver.WindowHandle <> savedHandle Or driver.WindowHandles.Count <> 1 Then Fail "Live handles"
If driver.LastErrorCode <> 0 Then Fail "Recovery incorrectly recorded as error"
WScript.Echo "PASS: COM popup closes and original main information remains readable"
driver.SwitchToWindowByHandle savedHandle, 0
If Not mainWindow.IsAlive Then Fail "Main IsAlive"
WScript.Echo "PASS: COM handle switch and retained error JSON: " & driver.LastErrorJson
driver.Quit
WScript.Echo "INSTALLED_COM_RESULT=PASS " & browser
WScript.Quit 0

Sub Fail(message)
    WScript.Echo "FAIL: " & message
    On Error Resume Next
    driver.Quit
    WScript.Quit 1
End Sub

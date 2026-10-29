using System;
using System.IO;
using System.Threading;
using Selenium;
class RealPopupTest {
 static void Check(bool condition,string label){if(!condition)throw new Exception(label);Console.WriteLine("PASS: "+label);}
 static int Main(string[] args){
  string root=Path.GetFullPath(args[0]);Directory.CreateDirectory(root);
  File.WriteAllText(Path.Combine(root,"main.html"),"<html><title>KTOOL Main Test</title><body><div id='value'>main-data</div><button id='open' onclick=\"window.open('popup.html','child','width=450,height=350')\">Open popup</button></body></html>");
  File.WriteAllText(Path.Combine(root,"popup.html"),"<html><title>KTOOL Popup Test</title><body>popup<script>setTimeout(function(){window.close()},2500);</script></body></html>");
  WebDriver d=args.Length>1 && args[1]=="edge" ? (WebDriver)new EdgeDriver() : new ChromeDriver();
  try {
   d.AddArgument("--user-data-dir="+Path.Combine(root,"profile"));d.AddArgument("--no-first-run");d.AddArgument("--no-default-browser-check");d.AddArgument("--disable-popup-blocking");
   d.Timeouts.Server=15000;d.Start();d.Get(new Uri(Path.Combine(root,"main.html")).AbsoluteUri);
   Window main=d.Window;string original=main.Handle;
   Check(d.Title=="KTOOL Main Test","main loaded with current Chrome");
   string hwnd=main.NativeHandle;Check(hwnd!="0","owned native HWND exposed");Console.WriteLine("OWN_NATIVE_HWND="+hwnd);
   d.FindElementById("open").Click();Window popup=d.SwitchToNextWindow(3000);
   Check(popup.Handle!=original,"popup has a different browser handle");
   Thread.Sleep(2900);
   Check(d.Title=="KTOOL Main Test","self-closing popup restores main without title activation");
   Check(d.FindElementById("value").Text()=="main-data","main information can be read after popup closes");
   Check(d.WindowHandle==original && d.WindowHandles.Count==1,"closed popup handle is pruned");
   d.FindElementById("open").Click();popup=d.SwitchToNextWindow(3000);d.Close();
   Check(d.Title=="KTOOL Main Test","explicit Close restores main");
   d.FindElementById("open").Click();popup=d.SwitchToNextWindow(3000);d.SwitchToNextWindow(0);
   Check(d.WindowHandle==original,"next switches back with exactly two windows");
   d.SwitchToWindowByHandle(popup.Handle,0);Thread.Sleep(2900);main.Activate();
   Check(d.Title=="KTOOL Main Test","saved main activation works after external close");
   Console.WriteLine("REAL_BROWSER_RESULT=PASS");return 0;
  }catch(Exception e){Console.WriteLine(e.ToString());return 1;}
  finally{try{d.Quit();}catch{}}
 }
}
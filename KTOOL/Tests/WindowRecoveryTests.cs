using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;
using Selenium;

class FakeDriver : IDisposable {
    readonly TcpListener listener;
    readonly Thread thread;
    readonly object gate = new object();
    readonly System.Collections.Generic.List<string> live = new System.Collections.Generic.List<string>();
    volatile bool stopped;
    public string Current = "A";
    public int Clicks;
    public int HeaderDelay, BodyDelay, ScriptTimeout;
    public string Address { get; private set; }
    public FakeDriver() {
        live.Add("A"); live.Add("B");
        listener = new TcpListener(IPAddress.Loopback, 0); listener.Start();
        Address = "http://127.0.0.1:" + ((IPEndPoint)listener.LocalEndpoint).Port;
        thread = new Thread(Loop); thread.IsBackground = true; thread.Start();
    }
    public void CloseExternally(string handle) { lock(gate) live.Remove(handle); }
    public void SelectExternally(string handle) { lock(gate) Current = handle; }
    object Result(int status, object value) { return new Dictionary<string,object>{{"status",status},{"value",value}}; }
    object Route(string method, string path, Dictionary<string,object> data) {
        lock(gate) {
            if(path == "/session" && method == "POST") return new Dictionary<string,object>{{"status",0},{"sessionId","test"},{"value",new Dictionary<string,object>{{"browserName","chrome"}}}};
            if(path == "/status") return Result(0,new Dictionary<string,object>{{"ready",true}});
            if(path.EndsWith("/timeouts")) { if(data.ContainsKey("type") && (string)data["type"] == "script") ScriptTimeout = Convert.ToInt32(data["ms"]); return Result(0,null); }
            if(path == "/session/test" && method == "DELETE") return Result(0,null);
            if(path.EndsWith("/window_handles")) return Result(0,live.ToArray());
            if(path.EndsWith("/window") && method == "POST") {
                string handle = (string)data["name"];
                if(!live.Contains(handle)) return Result(23,new Dictionary<string,object>{{"message","No such window: "+handle}});
                Current = handle; return Result(0,null);
            }
            if(path.EndsWith("/click")) Clicks++;
            if(!live.Contains(Current)) return Result(23,new Dictionary<string,object>{{"message","Current popup has closed"}});
            if(path.EndsWith("/window_handle")) return Result(0,Current);
            if(path.EndsWith("/title")) return Result(0,Current == "A" ? "Main" : "Popup");
            if(path.EndsWith("/url")) return Result(0,"about:blank");
            if(path.EndsWith("/source")) return Result(0,"<html>test</html>");
            if(path.EndsWith("/window") && method == "DELETE") { live.Remove(Current); return Result(0,null); }
            if(path.EndsWith("/element")) return Result(0,new Dictionary<string,object>{{"ELEMENT","e"}});
            return Result(0,null);
        }
    }
    void Loop() {
        while(!stopped) {
            try {
                using(TcpClient client = listener.AcceptTcpClient())
                using(NetworkStream stream = client.GetStream()) {
                    StreamReader reader = new StreamReader(stream, Encoding.UTF8, false, 4096);
                    string line = reader.ReadLine(); if(line == null) continue;
                    string[] head = line.Split(' '); int count = 0;
                    while((line = reader.ReadLine()) != null && line.Length != 0)
                        if(line.StartsWith("Content-Length:",StringComparison.OrdinalIgnoreCase)) count = Int32.Parse(line.Substring(15).Trim());
                    char[] chars = new char[count]; int read = 0;
                    while(read < count) { int got = reader.Read(chars,read,count-read); if(got==0) break; read += got; }
                    JavaScriptSerializer json = new JavaScriptSerializer();
                    Dictionary<string,object> data = count == 0 ? new Dictionary<string,object>() : json.Deserialize<Dictionary<string,object>>(new string(chars,0,read));
                    string path = head[1]; object value = Route(head[0],path,data);
                    byte[] body = Encoding.UTF8.GetBytes(json.Serialize(value));
                    if(path.EndsWith("/source") && HeaderDelay > 0) Thread.Sleep(HeaderDelay);
                    byte[] headers = Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\nContent-Type: application/json; charset=utf-8\r\nContent-Length: "+body.Length+"\r\nConnection: close\r\n\r\n");
                    stream.Write(headers,0,headers.Length); stream.Flush();
                    if(path.EndsWith("/source") && BodyDelay > 0) Thread.Sleep(BodyDelay);
                    stream.Write(body,0,body.Length); stream.Flush();
                }
            } catch { if(stopped) break; }
        }
    }
    public WebDriver Connect() { WebDriver driver = new WebDriver(); driver.StartRemotely(Address,"chrome"); return driver; }
    public void Dispose() { stopped = true; listener.Stop(); thread.Join(2500); }
}

class WindowRecoveryTests {
    static int passed, failed;
    static void Assert(bool condition,string message) { if(!condition) throw new Exception(message); }
    static void Run(string name, Action test) {
        try { test(); passed++; Console.WriteLine("PASS: "+name); }
        catch(Exception e) { failed++; Console.WriteLine("FAIL: "+name+" => "+e.GetType().Name+": "+e.Message); }
    }
    static int Main() {
        Run("saved main object must actually switch remote focus", delegate {
            using(FakeDriver server = new FakeDriver()) { WebDriver d=server.Connect(); Window main=d.Window;
                server.SelectExternally("B"); main.Activate(); Assert(server.Current=="A","cached object skipped the switch"); }
        });
        Run("two-window next can return to previous main", delegate {
            using(FakeDriver server = new FakeDriver()) { WebDriver d=server.Connect(); Window main=d.Window;
                d.SwitchToWindowByName("B",0); Window back=d.SwitchToNextWindow(0); Assert(back.Handle==main.Handle,"did not return to main"); }
        });
        Run("self-closing popup recovers a title read", delegate {
            using(FakeDriver server = new FakeDriver()) { WebDriver d=server.Connect(); Window main=d.Window;
                d.SwitchToWindowByName("B",0); server.CloseExternally("B"); Assert(d.Title=="Main","title read did not recover"); }
        });
        Run("direct read also recovers without touching Window property", delegate {
            using(FakeDriver server = new FakeDriver()) { WebDriver d=server.Connect(); Window main=d.Window;
                d.SwitchToWindowByName("B",0); server.CloseExternally("B"); Assert((string)d.Send("GET","/title")=="Main","direct read did not recover"); }
        });
        Run("Close popup restores surviving main", delegate {
            using(FakeDriver server = new FakeDriver()) { WebDriver d=server.Connect(); Window main=d.Window;
                d.SwitchToWindowByName("B",0); d.Close(); Assert(server.Current=="A" && d.Title=="Main","close left a dead focus"); }
        });
        Run("failed title search restores remote and cached original", delegate {
            using(FakeDriver server = new FakeDriver()) { WebDriver d=server.Connect(); Window main=d.Window;
                try {d.SwitchToWindowByTitle("missing",0);} catch(Selenium.Errors.NoSuchWindowError){}
                Assert(server.Current=="A","failed title lookup changed the browser window"); Assert(d.Title=="Main","cache and remote disagree"); }
        });
        Run("click is never automatically replayed after popup disappears", delegate {
            using(FakeDriver server = new FakeDriver()) { WebDriver d=server.Connect(); Window main=d.Window;
                d.SwitchToWindowByName("B",0); server.CloseExternally("B"); bool raised=false;
                try{d.Send("POST","/element/e/click");}catch(Selenium.Errors.NoSuchWindowError){raised=true;}
                Assert(raised && server.Clicks==1,"non-idempotent click was replayed"); Assert(server.Current=="A","surviving main was not restored"); }
        });
        Run("VBA Window handle is exposed without changing interface GUID", delegate {
            Type type=typeof(Selenium.ComInterfaces._Window); Assert(type.GetProperty("Handle")!=null,"Handle missing in COM interface");
            Assert(type.GUID.ToString()=="0277fc34-fd1b-4616-bb19-fbda3a91c82b","existing GUID changed");
        });
        Run("handle list and explicit handle switching", delegate {
            using(FakeDriver server = new FakeDriver()) { WebDriver d=server.Connect(); Assert(d.WindowHandles.Count==2,"missing handle");
                d.SwitchToWindowByHandle("B",0); Assert(d.WindowHandle=="B","wrong handle"); }
        });
        Run("closed window removed from cache and can be checked", delegate {
            using(FakeDriver server = new FakeDriver()) { WebDriver d=server.Connect(); Window main=d.Window;
                Window popup=d.SwitchToWindowByName("B",0); server.CloseExternally("B"); Assert(!popup.IsAlive,"closed handle still alive"); Assert(d.Windows.Count==1,"dead cache retained"); }
        });
        Run("script timeout is script setting, not page-load setting", delegate {
            using(FakeDriver server = new FakeDriver()) { server.Connect(); Assert(server.ScriptTimeout==15000,"incorrect script timeout "+server.ScriptTimeout); }
        });
        Run("changing server timeout applies to existing session headers", delegate {
            using(FakeDriver server = new FakeDriver()) { WebDriver d=server.Connect(); d.Timeouts.Server=250; server.HeaderDelay=1800;
                Stopwatch watch=Stopwatch.StartNew(); bool timed=false;
                try{d.Send("GET","/source");}catch(Selenium.Errors.WebRequestTimeout){timed=true;}
                Assert(timed && watch.ElapsedMilliseconds<1400,"timeout update ignored; elapsed="+watch.ElapsedMilliseconds); }
        });
        Run("response body read has the configured timeout too", delegate {
            using(FakeDriver server = new FakeDriver()) { WebDriver d=server.Connect(); d.Timeouts.Server=250; server.BodyDelay=1800;
                Stopwatch watch=Stopwatch.StartNew(); bool timed=false;
                try{d.Send("GET","/source");}catch(Selenium.Errors.WebRequestTimeout){timed=true;}
                Assert(timed && watch.ElapsedMilliseconds<1400,"body read blocked beyond configured timeout; elapsed="+watch.ElapsedMilliseconds); }
        });
        Run("VBA can read retained error details and explicitly clear them", delegate {
            using(FakeDriver server = new FakeDriver()) { WebDriver d=server.Connect();
                CheckNoWindow(d);
                Assert(d.LastErrorCode==23 && d.LastErrorHResult!=0,"missing error code");
                Assert(d.LastErrorMessage.Contains("missing") && d.LastErrorStage=="SwitchToWindowByName" && d.LastErrorTime.Length>0,"missing error context");
                Assert(d.LastErrorJson.Contains("\"code\":23"),"missing machine-readable error");
                string title=d.Title; Assert(d.LastErrorCode==23,"success erased diagnostics before VBA could read them");
                d.ClearLastError();Assert(d.LastErrorCode==0 && d.LastErrorMessage=="","clear did not reset diagnostics");
            }
        });
        Run("successful automatic popup recovery is not a VBA error", delegate {
            using(FakeDriver server = new FakeDriver()) {WebDriver d=server.Connect();Window main=d.Window;d.SwitchToWindowByName("B",0);server.CloseExternally("B");string title=d.Title;Assert(title=="Main" && d.LastErrorCode==0,"recovered read reported a false error");}
        });
        Console.WriteLine("RESULT: "+passed+" passed; "+failed+" failed");
        return failed==0?0:1;
    }
    static void CheckNoWindow(WebDriver d) {Assert(d.SwitchToWindowByHandle("missing",0,false)==null,"unexpected window");}
}

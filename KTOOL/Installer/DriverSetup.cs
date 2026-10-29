using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using Microsoft.Win32;

class DriverSetup {
 static readonly string Root=AppDomain.CurrentDomain.BaseDirectory;
 static readonly JavaScriptSerializer Json=new JavaScriptSerializer {MaxJsonLength=16777216};
 static readonly string Log=Path.Combine(Root,"KTOOL_DriverSetup.log");
 static void Write(string text){Console.WriteLine(text);File.AppendAllText(Log,DateTime.Now.ToString("s")+" "+text+Environment.NewLine,Encoding.UTF8);}
 static string Browser(string name){
  string exe=name=="chrome"?"chrome.exe":"msedge.exe";
  foreach(RegistryHive hive in new[]{RegistryHive.CurrentUser,RegistryHive.LocalMachine})
   foreach(RegistryView view in new[]{RegistryView.Registry64,RegistryView.Registry32}){
    using(RegistryKey key=RegistryKey.OpenBaseKey(hive,view).OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\"+exe)){
     string value=key==null?null:key.GetValue("") as string;if(!String.IsNullOrEmpty(value)&&File.Exists(value.Trim('"')))return value.Trim('"');
    }
   }
  foreach(string folder in new[]{Environment.GetEnvironmentVariable("ProgramW6432"),Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}){
   if(String.IsNullOrEmpty(folder))continue;string file=Path.Combine(folder,name=="chrome"?@"Google\Chrome\Application\chrome.exe":@"Microsoft\Edge\Application\msedge.exe");if(File.Exists(file))return file;
  }
  return null;
 }
 static Version VersionOf(string file){
  if(!File.Exists(file))return null;string text=FileVersionInfo.GetVersionInfo(file).FileVersion??"";Match match=Regex.Match(text,@"\d+\.\d+\.\d+\.\d+");
  if(!match.Success){
   try { using(Process p=Process.Start(new ProcessStartInfo(file,"--version"){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true})){
    if(!p.WaitForExit(5000))throw new IOException("Version check timed out: "+Path.GetFileName(file));text=p.StandardOutput.ReadToEnd();match=Regex.Match(text,@"\d+\.\d+\.\d+\.\d+");
   }} catch(System.ComponentModel.Win32Exception) { return null; }
  }
  return match.Success?new Version(match.Value):null;
 }
 static HttpWebRequest Request(string address){
  Uri uri=new Uri(address);if(uri.Scheme!="https")throw new IOException("HTTPS is required for driver download.");
  HttpWebRequest request=(HttpWebRequest)WebRequest.Create(uri);request.UserAgent="KT-SOFT-SeleniumBasic/2.0.9.1";request.Timeout=120000;request.ReadWriteTimeout=120000;return request;
 }
 static string Text(string url){using(WebResponse response=Request(url).GetResponse())using(StreamReader r=new StreamReader(response.GetResponseStream(),Encoding.UTF8,true))return r.ReadToEnd();}
 static string Hash(string file){using(Stream input=File.OpenRead(file))using(SHA256 hash=SHA256.Create())return BitConverter.ToString(hash.ComputeHash(input)).Replace("-","");}
 static string DriverUrl(string browser,Version version){
  string platform=Environment.Is64BitOperatingSystem?"win64":"win32";
  if(browser=="chrome"){
   Dictionary<string,object> root=(Dictionary<string,object>)Json.DeserializeObject(Text("https://googlechromelabs.github.io/chrome-for-testing/latest-patch-versions-per-build-with-downloads.json"));
   Dictionary<string,object> builds=(Dictionary<string,object>)root["builds"];object entry;
   if(!builds.TryGetValue(version.ToString(3),out entry))throw new IOException("No ChromeDriver release for browser build "+version.ToString(3));
   Dictionary<string,object> release=(Dictionary<string,object>)entry;Dictionary<string,object> downloads=(Dictionary<string,object>)release["downloads"];
   foreach(object item in (IEnumerable)downloads["chromedriver"]){Dictionary<string,object> asset=(Dictionary<string,object>)item;if((string)asset["platform"]==platform)return (string)asset["url"];}
   throw new IOException("No matching ChromeDriver platform.");
  }
  string edge=Text("https://msedgedriver.microsoft.com/LATEST_RELEASE_"+version.Major+"_WINDOWS").Trim('\uFEFF','\0','\r','\n',' ');Version releaseVersion;
  if(!Version.TryParse(edge,out releaseVersion)||releaseVersion.ToString(3)!=version.ToString(3))throw new IOException("No EdgeDriver release for browser build "+version.ToString(3));
  return "https://msedgedriver.microsoft.com/"+releaseVersion+"/edgedriver_"+platform+".zip";
 }
 static void Ensure(string browser){
  string installed=Browser(browser);if(installed==null){Write(browser+": browser not installed; skipped");return;}
  Version required=VersionOf(installed);if(required==null)throw new IOException("Cannot read "+browser+" version.");
  string target=Path.Combine(Root,browser=="chrome"?"chromedriver.exe":"edgedriver.exe");Version current=VersionOf(target);
  if(current!=null&&current.ToString(3)==required.ToString(3)){Write(browser+": matching driver already installed ("+current+")");return;}
  string url=DriverUrl(browser,required);Uri source=new Uri(url);
  if(!((source.Host=="storage.googleapis.com"&&source.AbsolutePath.StartsWith("/chrome-for-testing-public/",StringComparison.Ordinal))||source.Host=="msedgedriver.microsoft.com"))throw new IOException("Unexpected driver download origin.");
  string cache=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"KTOOL","DriverDownloads");Directory.CreateDirectory(cache);
  string zip=Path.Combine(cache,Guid.NewGuid().ToString("N")+".zip");string staging=target+"."+Guid.NewGuid().ToString("N")+".download";
  try{
   Write(browser+": downloading matching driver for "+required);
   using(WebResponse response=Request(url).GetResponse())using(Stream input=response.GetResponseStream())using(FileStream output=File.Create(zip)){
    byte[] buffer=new byte[65536];long total=0;int count;while((count=input.Read(buffer,0,buffer.Length))>0){total+=count;if(total>100000000)throw new IOException("Driver archive exceeds expected size.");output.Write(buffer,0,count);}
   }
   string expected=browser=="chrome"?"chromedriver.exe":"msedgedriver.exe";bool found=false;
   using(ZipArchive archive=ZipFile.OpenRead(zip))foreach(ZipArchiveEntry entry in archive.Entries){
    if(entry.Name!=expected)continue;if(found)throw new IOException("Multiple driver executables in archive.");if(entry.Length<=0||entry.Length>150000000)throw new IOException("Unexpected driver size.");
    using(Stream input=entry.Open())using(FileStream output=File.Create(staging))input.CopyTo(output);found=true;
   }
   if(!found)throw new IOException("Driver executable is missing from archive.");Version received=VersionOf(staging);
   if(received==null||received.ToString(3)!=required.ToString(3))throw new IOException("Downloaded driver version does not match browser.");
   // Keep the previous working file until a fully downloaded binary is validated.
   if(File.Exists(target))File.Replace(staging,target,target+".previous",true);else File.Move(staging,target);
   File.WriteAllText(Path.Combine(Root,"KTOOL_"+browser+"_driver.json"),Json.Serialize(new{Browser=browser,BrowserVersion=required.ToString(),DriverVersion=received.ToString(),Source=url,ArchiveSHA256=Hash(zip),DriverSHA256=Hash(target),UpdatedAt=DateTimeOffset.Now.ToString("o")}),Encoding.UTF8);
   Write(browser+": installed matching driver "+received);
  }finally{if(File.Exists(zip))File.Delete(zip);if(File.Exists(staging))File.Delete(staging);}
 }
 static int Main(string[] args){
  ServicePointManager.SecurityProtocol|=SecurityProtocolType.Tls12;bool failed=false;
  string[] targets=args.Length>0&&args[0].StartsWith("/ensure:",StringComparison.OrdinalIgnoreCase)?new[]{args[0].Substring(8).ToLowerInvariant()}:new[]{"chrome","edge"};
  foreach(string browser in targets)try{if(browser!="chrome"&&browser!="edge")throw new ArgumentException("Unsupported browser");Ensure(browser);}catch(Exception e){failed=true;Write(browser+": FAILED - "+e.Message);}
  return failed?1:0;
 }
}

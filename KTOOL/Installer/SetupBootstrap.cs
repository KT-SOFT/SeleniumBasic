using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using Microsoft.Win32;
[assembly:AssemblyTitle("SeleniumBasic KTOOL automatic setup")]
[assembly:AssemblyVersion("2.0.9.1")]
[assembly:AssemblyFileVersion("2.0.9.1")]
class SetupBootstrap {
 static readonly string Logs=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"KTOOL","SetupLogs");
 static void Log(string text){Directory.CreateDirectory(Logs);File.AppendAllText(Path.Combine(Logs,"SeleniumBasic_KTOOL_setup.log"),DateTime.Now.ToString("s")+" "+text+Environment.NewLine);}
 static bool RuntimeInstalled(){
  foreach(RegistryView view in new[]{RegistryView.Registry64,RegistryView.Registry32})using(RegistryKey key=RegistryKey.OpenBaseKey(RegistryHive.LocalMachine,view).OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full"))
   if(key!=null&&Convert.ToInt32(key.GetValue("Release",0),CultureInfo.InvariantCulture)>=528040)return true;
  return false;
 }
 static int EnsureRuntime(){
  if(RuntimeInstalled()){Log(".NET Framework 4.8 or later already installed.");return 0;}
  string cache=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"KTOOL","Downloads");Directory.CreateDirectory(cache);
  string setup=Path.Combine(cache,"ndp48-x86-x64-allos-enu.exe");
  if(!MicrosoftSignature(setup)){
   string part=setup+"."+Guid.NewGuid().ToString("N")+".download";
   try{
    ServicePointManager.SecurityProtocol|=(SecurityProtocolType)3072;
    HttpWebRequest request=(HttpWebRequest)WebRequest.Create("https://go.microsoft.com/fwlink/?linkid=2088631");request.Timeout=120000;request.ReadWriteTimeout=120000;
    using(WebResponse response=request.GetResponse())using(Stream input=response.GetResponseStream())using(FileStream output=File.Create(part)){
     byte[] buffer=new byte[65536];long total=0;int count;while((count=input.Read(buffer,0,buffer.Length))>0){total+=count;if(total>200000000)throw new IOException("Unexpected .NET installer size.");output.Write(buffer,0,count);}
    }
    if(!MicrosoftSignature(part))throw new IOException("The .NET installer does not have a valid Microsoft signature.");File.Copy(part,setup,true);
   }finally{if(File.Exists(part))File.Delete(part);}
  }
  bool admin=new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
  ProcessStartInfo start=new ProcessStartInfo(setup,"/q /norestart"){UseShellExecute=!admin,CreateNoWindow=admin,WindowStyle=ProcessWindowStyle.Hidden};if(!admin)start.Verb="runas";
  using(Process p=Process.Start(start)){if(!p.WaitForExit(900000))throw new IOException(".NET installation is still running.");if(p.ExitCode!=0&&p.ExitCode!=3010)throw new IOException(".NET installer exit code "+p.ExitCode);return p.ExitCode;}
 }
 static string Quote(string value){if(value.IndexOf('"')>=0)throw new ArgumentException("Invalid log path.");return "\""+value+"\"";}
 [STAThread]static int Main(string[] args){
  string work=null;
  try{
   using(System.Threading.Mutex mutex=new System.Threading.Mutex(false,@"Local\SeleniumBasic_KTOOL_Setup")){
    bool acquired=false;try{try{acquired=mutex.WaitOne(0);}catch(System.Threading.AbandonedMutexException){acquired=true;}if(!acquired){Log("Another installation is running.");return 1618;}
     int runtime=EnsureRuntime();
     string baseDir=Path.Combine(Path.GetTempPath(),"SeleniumBasic_KTOOL");work=Path.Combine(baseDir,Guid.NewGuid().ToString("N"));Directory.CreateDirectory(work);
     string core=Path.Combine(work,"SeleniumBasic-Core.exe");using(Stream input=Assembly.GetExecutingAssembly().GetManifestResourceStream("KTOOL.CoreInstaller"))using(FileStream output=File.Create(core)){if(input==null)throw new IOException("Embedded installer is missing.");input.CopyTo(output);}
     string log=Path.Combine(Logs,"SeleniumBasic_core_install.log");foreach(string arg in args)if(arg.StartsWith("/LOG=",StringComparison.OrdinalIgnoreCase))log=arg.Substring(5).Trim('"');Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(log)));
     string command="/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP- /TYPE=full /NOICONS /LOG="+Quote(log);
     Log("Installing all SeleniumBasic components automatically.");
     using(Process p=Process.Start(new ProcessStartInfo(core,command){UseShellExecute=false,CreateNoWindow=true,WorkingDirectory=work})){
      if(!p.WaitForExit(900000))throw new IOException("SeleniumBasic installation is still running. See "+log);
      Log("Core installer exit code "+p.ExitCode+". Log: "+log);
      return p.ExitCode==0?runtime:p.ExitCode;
     }
    }finally{if(acquired)mutex.ReleaseMutex();}
   }
  }catch(Exception e){Log("FAILED: "+e.ToString());return 1;}
  finally{
   if(work!=null){string allowed=Path.GetFullPath(Path.Combine(Path.GetTempPath(),"SeleniumBasic_KTOOL"))+Path.DirectorySeparatorChar;string actual=Path.GetFullPath(work);if(actual.StartsWith(allowed,StringComparison.OrdinalIgnoreCase))try{Directory.Delete(actual,true);}catch{}}
  }
 }
 static bool MicrosoftSignature(string file){
  if(!File.Exists(file))return false;IntPtr path=IntPtr.Zero,info=IntPtr.Zero;
  try{
   path=Marshal.StringToCoTaskMemUni(Path.GetFullPath(file));TrustFile f=new TrustFile{Size=(uint)Marshal.SizeOf(typeof(TrustFile)),Path=path};info=Marshal.AllocHGlobal(Marshal.SizeOf(typeof(TrustFile)));Marshal.StructureToPtr(f,info,false);
   TrustData data=new TrustData{Size=(uint)Marshal.SizeOf(typeof(TrustData)),UI=2,Choice=1,File=info};Guid policy=new Guid("00AAC56B-CD44-11D0-8CC2-00C04FC295EE");if(WinVerifyTrust(new IntPtr(-1),ref policy,ref data)!=0)return false;
   X509Certificate2 signer=new X509Certificate2(X509Certificate.CreateFromSignedFile(file));return signer.GetNameInfo(X509NameType.SimpleName,false)=="Microsoft Corporation";
  }catch{return false;}finally{if(info!=IntPtr.Zero)Marshal.FreeHGlobal(info);if(path!=IntPtr.Zero)Marshal.FreeCoTaskMem(path);}
 }
 [StructLayout(LayoutKind.Sequential)]struct TrustFile{internal uint Size;internal IntPtr Path,Handle,KnownSubject;}
 [StructLayout(LayoutKind.Sequential)]struct TrustData{internal uint Size;internal IntPtr Policy,Sip;internal uint UI,Revocation,Choice;internal IntPtr File;internal uint StateAction;internal IntPtr State,Url;internal uint Flags,UIContext;internal IntPtr Signature;}
 [DllImport("wintrust.dll",ExactSpelling=true,PreserveSig=true)]static extern int WinVerifyTrust(IntPtr window,ref Guid action,ref TrustData data);
}
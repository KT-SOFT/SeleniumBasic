using System;
using System.Reflection;
using System.Runtime.InteropServices;
class ExportTypeLibrary : ITypeLibExporterNotifySink {
 public void ReportEvent(ExporterEventKind kind,int code,string message){if(kind==ExporterEventKind.ERROR_REFTOINVALIDASSEMBLY)throw new InvalidOperationException(message);}
 public object ResolveRef(Assembly assembly){return null;}
 static int Main(string[] args){
  try{Assembly asm=Assembly.LoadFrom(args[0]);TypeLibExporterFlags flags=args[2]=="32"?TypeLibExporterFlags.ExportAs32Bit:TypeLibExporterFlags.ExportAs64Bit;
   object result=new TypeLibConverter().ConvertAssemblyToTypeLib(asm,args[1],flags,new ExportTypeLibrary());
   ((ICreateTypeLib2)result).SaveAllChanges();Console.WriteLine(args[1]);return 0;
  }catch(Exception e){Console.Error.WriteLine(e);return 1;}
 }
 [ComImport,Guid("0002040F-0000-0000-C000-000000000046"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
 interface ICreateTypeLib2 {
  void CreateTypeInfo();void SetName();void SetVersion();void SetGuid();void SetDocString();void SetHelpFileName();void SetHelpContext();void SetLcid();void SetLibFlags();void SaveAllChanges();
 }
}
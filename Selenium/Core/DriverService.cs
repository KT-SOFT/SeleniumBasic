using Selenium.Internal;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;

namespace Selenium.Core {

    class DriverService : IDriverService {

        const string TEMP_FOLDER = @"Selenium";

        internal static string GetTempFolder() {
            var tmp_dir = Path.Combine(Path.GetTempPath(), TEMP_FOLDER);
            Directory.CreateDirectory(tmp_dir);
            return tmp_dir;
        }


        private string _library_dir;
        private ProcessExt _process;
        private List<string> _arguments;
        protected EndPointExt _endpoint;
        private string _temp_folder;

        public DriverService() {
            _library_dir = IOExt.GetAssemblyDirectory();
            _arguments = new List<string>();
            _temp_folder = GetTempFolder();
            _endpoint = EndPointExt.Create(IPAddress.Loopback, false);
        }

        /// <summary>
        /// Releases all resources.
        /// </summary>
        public void Dispose() {
            _endpoint.Dispose();

            if (_process != null) {
                if (!_process.HasExited) {
                    _process.Kill();
                }
                _process.Dispose();
                _process = null;
            }
        }

        /// <summary>
        /// Stops the service.
        /// </summary>
        public void Quit(RemoteServer server) {
            if (_process == null || _process.HasExited)
                return;

            server.ShutDown();

            if (_process.WaitForExit(5000)) {
                _process.Dispose();
                _process = null;
            }
        }

        public virtual string Uri {
            get {
                return "http://localhost:" + _endpoint.IPEndPoint.Port.ToString();
            }
        }

        public IPEndPoint IPEndPoint {
            get {
                return _endpoint.IPEndPoint;
            }
        }
        internal int ProcessId { get { return _process == null ? 0 : _process.Id; } }

        public void AddArgument(string argument) {
            _arguments.Add(argument);
        }

        public void Start(string filename, bool hide = true) {
            if (filename == "chromedriver.exe" || filename == "edgedriver.exe") {
                string helper = Path.Combine(_library_dir, "SeleniumBasic.DriverSetup.exe");
                if (File.Exists(helper)) {
                    string browser = filename == "chromedriver.exe" ? "chrome" : "edge";
                    var start = new System.Diagnostics.ProcessStartInfo(helper, "/ensure:" + browser) {
                        UseShellExecute = false, CreateNoWindow = true, WorkingDirectory = _library_dir,
                        RedirectStandardOutput = true, RedirectStandardError = true
                    };
                    using (var setup = System.Diagnostics.Process.Start(start)) {
                        var output = setup.StandardOutput.ReadToEndAsync();
                        var errors = setup.StandardError.ReadToEndAsync();
                        if (!setup.WaitForExit(180000)) throw new DriverPreparationError("브라우저 드라이버 자동 업데이트 시간이 초과되었습니다. 로그: {0}", Path.Combine(_library_dir,"KTOOL_DriverSetup.log"));
                        if (setup.ExitCode != 0) throw new DriverPreparationError("{0} 드라이버 자동 업데이트 실패. {1} {2}\n로그: {3}", browser, output.Result, errors.Result, Path.Combine(_library_dir, "KTOOL_DriverSetup.log"));
                    }
                }
            }
            Hashtable env = ProcessExt.GetStdEnvironmentVariables();
            env["TEMP"] = _temp_folder;
            env["TMP"] = _temp_folder;

            string servicePath = Path.Combine(_library_dir, filename);
            if (!File.Exists(servicePath))
                throw new Errors.FileNotFoundError(servicePath);

            //Start the process
            _process = ProcessExt.Start(servicePath, _arguments, null, env, true, true);

            //Waits for the port to be listening
            if (!_endpoint.WaitForListening(10000, 150))
                throw new Errors.TimeoutError("The driver failed to open the listening port {0} within 10s", _endpoint);

        }

    }

    internal sealed class DriverPreparationError : SeleniumError {
        internal DriverPreparationError(string message, params object[] args) : base(1001,message,args) { }
    }

}

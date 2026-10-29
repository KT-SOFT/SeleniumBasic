using System;
using System.Web.Script.Serialization;
using System.Collections.Generic;
using Selenium;

// Run in an isolated folder with Selenium.dll and a test helper that exits 1.
class DriverFailureTest {
    static int Main() {
        using (var driver = new ChromeDriver()) {
            bool failed = false;
            try { driver.Start(); }
            catch (Exception) { failed = true; }
            var data = new JavaScriptSerializer().Deserialize<Dictionary<string,object>>(driver.LastErrorJson);
            if (!failed || driver.LastErrorCode != 1001 || driver.LastErrorStage != "driver-update"
                || !driver.LastErrorMessage.Contains("KTOOL_TEST_PREPARATION_FAILURE")
                || Convert.ToInt32(data["code"]) != 1001 || driver.LastErrorTime.Length == 0) {
                Console.WriteLine("FAIL: driver preparation error is not readable: " + driver.LastErrorJson);
                return 1;
            }
            Console.WriteLine("PASS: hidden driver preparation failure is retained for VBA (1001, driver-update, message, JSON, time)");
            driver.ClearLastError();
            if (driver.LastErrorCode != 0 || driver.LastErrorMessage != "") return 1;
            Console.WriteLine("PASS: VBA can explicitly clear a driver preparation error");
            return 0;
        }
    }
}

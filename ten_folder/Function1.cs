using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

// Services/Function1.cs

namespace AgentForMe.Services
{
    // Tuy chon: Ban nen tao mot Interface cho Service (Vi du: IFunction1)
    public interface IFunction1
    {
        string ExecuteFunction1(string target);
    }

    public class Function1 : IFunction1
    {
        public string ExecuteFunction1(string target)
        {
            // Logic cua chuc nang 1 (vi du: Liet ke, chay, dung mot ung dung)
            return $"Function 1 dang thuc thi lenh cho: {target}";
        }
    }
}


namespace AgentForMe.Services
{
    /// <summary>
    /// Lop quan ly cac tien trinh (ung dung) tren he thong.
    /// </summary>
    public class ApplicationManager
    {
        // --- 1. DATA TRANSFER OBJECT (DTO) ---

        /// <summary>
        /// Thong tin co ban ve mot tien trinh dang chay.
        /// </summary>
        public class ProcessInfo
        {
            public int Id { get; set; }
            public string Name { get; set; } // Ten hien thi (Window Title)
            public string ProcessName { get; set; } // Ten tien trinh (Executable Name)
            public long MemoryUsageMB { get; set; }
            public string FullPath { get; set; }
        }

        // ----------------------------------------------------------------------------------

        /// <summary>
        /// LIET KE: Lay danh sach cac ung dung dang chay (tien trinh).
        /// </summary>
        /// <returns>Danh sach cac doi tuong ProcessInfo.</returns>
        public List<ProcessInfo> ListRunningApplications()
        {
            List<ProcessInfo> runningApps = new List<ProcessInfo>();

            // Lay tat ca cac tien trinh.
            Process[] localProcesses = Process.GetProcesses();

            foreach (Process p in localProcesses)
            {
                string appName = string.Empty;
                string processPath = string.Empty;

                try
                {
                    // Uu tien lay ten cua so chinh (thuong la ten ung dung nguoi dung thay)
                    appName = p.MainWindowTitle;

                    // Neu khong co ten cua so, lay ten tien trinh (vi du: "notepad")
                    if (string.IsNullOrEmpty(appName))
                    {
                        appName = p.ProcessName;
                    }

                    // Lay duong dan file thuc thi
                    processPath = p.MainModule.FileName;
                }
                catch (System.ComponentModel.Win32Exception)
                {
                    // Loi truy cap bi tu choi (Access Denied) voi tien trinh he thong
                    appName = $"[{p.ProcessName} - System Process]";
                    processPath = "[Access Denied]";
                }
                catch (Exception)
                {
                    // Cac loi khac khi truy cap thong tin tien trinh
                    continue;
                }

                // Chi them cac tien trinh co ten cua so hoac khong phai he thong an danh
                if (!string.IsNullOrEmpty(appName) && !appName.Contains("[System Process]"))
                {
                    runningApps.Add(new ProcessInfo
                    {
                        Id = p.Id,
                        Name = appName.Trim(),
                        ProcessName = p.ProcessName,
                        MemoryUsageMB = p.WorkingSet64 / (1024 * 1024), // Chuyen sang MB
                        FullPath = processPath
                    });
                }
            }
            // Sap xep theo ten tien trinh
            return runningApps.OrderBy(a => a.Name).ToList();
        }

        // ----------------------------------------------------------------------------------

        /// <summary>
        /// CHAY: Khoi dong mot ung dung moi.
        /// </summary>
        /// <param name="executablePath">Duong dan day du den file thuc thi (.exe).</param>
        /// <param name="arguments">Cac doi so dong lenh (neu co).</param>
        /// <returns>True neu khoi dong thanh cong.</returns>
        public bool StartApplication(string executablePath, string arguments = "")
        {
            if (string.IsNullOrWhiteSpace(executablePath))
            {
                Console.WriteLine("Loi: Duong dan file thuc thi khong duoc de trong.");
                return false;
            }

            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo(executablePath);
                startInfo.Arguments = arguments;

                // Su dung ShellExecute de khoi chay nhu nguoi dung thong thuong
                startInfo.UseShellExecute = true;

                Process.Start(startInfo);

                Console.WriteLine($"[START] Da khoi dong: {executablePath}");
                return true;
            }
            catch (System.IO.FileNotFoundException)
            {
                Console.WriteLine($"[START] Loi: Khong tim thay file tai duong dan: {executablePath}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[START] Loi khi khoi dong ung dung {executablePath}: {ex.Message}");
                return false;
            }
        }

        // ----------------------------------------------------------------------------------

        /// <summary>
        /// DUNG: Dung mot ung dung bang Process ID.
        /// </summary>
        /// <param name="processId">ID cua tien trinh can dung.</param>
        /// <returns>True neu tien trinh da dung (Kill hoac Close) hoac khong ton tai.</returns>
        public bool StopApplication(int processId)
        {
            try
            {
                Process processToStop = Process.GetProcessById(processId);

                if (processToStop == null)
                {
                    Console.WriteLine($"[STOP] Khong tim thay tien trinh ID {processId}.");
                    return true;
                }

                // Kiem tra xem tien trinh da thoat chua
                if (processToStop.HasExited)
                {
                    Console.WriteLine($"[STOP] Tien trinh ID {processId} da thoat truoc do.");
                    return true;
                }

                // Co gang dong cua so chinh
                if (processToStop.CloseMainWindow())
                {
                    if (processToStop.WaitForExit(5000))
                    {
                        Console.WriteLine($"[STOP] Dong cua so tien trinh ID {processId} thanh cong.");
                        return true;
                    }
                }

                // Neu khong dong duoc -> Kill
                if (!processToStop.HasExited)
                {
                    processToStop.Kill();
                    Console.WriteLine($"[STOP] Buoc dung (Kill) tien trinh ID {processId}.");
                    return true;
                }
            }
            catch (ArgumentException)
            {
                Console.WriteLine($"[STOP] Tien trinh ID {processId} khong ton tai.");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[STOP] Loi khong xac dinh khi dung ID {processId}: {ex.Message}");
                return false;
            }

            return false;
        }
    }
}

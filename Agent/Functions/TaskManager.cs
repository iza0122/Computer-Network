using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Agent.Functions
{
    /// <summary>
    /// Lớp quản lý các tiến trình (Processes) trên hệ thống qua Name và ID.
    /// </summary>
    public class TaskManager
    {
        // --- 1. DATA TRANSFER OBJECT (DTO) ---
        public class ProcessTaskInfo
        {
            public int Id { get; set; }      // Process ID (PID)
            public string Name { get; set; }    // Ten tien trinh (vi du: "chrome")
        }

        // ----------------------------------------------------------------------------------

        /// <summary>
        /// LIET KE: Lay danh sach tat ca cac tien trinh dang chay.
        /// </summary>
        public List<ProcessTaskInfo> ListProcesses()
        {
            List<ProcessTaskInfo> processList = new List<ProcessTaskInfo>();

            try
            {
                // Lay tat ca process va sap xep theo ten
                Process[] processes = Process.GetProcesses();

                foreach (Process p in processes.OrderBy(x => x.ProcessName))
                {
                    processList.Add(new ProcessTaskInfo
                    {
                        Id = p.Id,
                        Name = p.ProcessName
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LIST] Loi: {ex.Message}");
            }

            return processList;
        }

        // ----------------------------------------------------------------------------------

        /// <summary>
        /// DUNG: Dong mot tien trinh dua tren ID (PID).
        /// </summary>
        public bool StopProcessById(int pid)
        {
            try
            {
                using (Process p = Process.GetProcessById(pid))
                {
                    p.Kill(); // Cuong che dung
                    p.WaitForExit(5000); // Cho toi da 5 giay de dong han
                    Console.WriteLine($"[STOP] Da dung Process ID: {pid}");
                    return true;
                }
            }
            catch (ArgumentException)
            {
                Console.WriteLine($"[STOP] Khong tim thay Process voi ID: {pid}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[STOP] Loi: {ex.Message}");
                return false;
            }
        }

        // ----------------------------------------------------------------------------------

        /// <summary>
        /// CHAY: Khoi chay mot ung dung moi bang ten/duong dan.
        /// </summary>
        public bool StartProcessByName(string processPath)
        {
            if (string.IsNullOrWhiteSpace(processPath)) return false;

            try
            {
                Process.Start(processPath);
                Console.WriteLine($"[START] Da khoi chay: {processPath}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[START] Khong the chay '{processPath}': {ex.Message}");
                return false;
            }
        }
    }
}
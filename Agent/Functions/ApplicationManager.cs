using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Agent.Functions
{
    public class ApplicationManager
    {
        public class RunningAppInfo
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Path { get; set; } = string.Empty;
            public List<RunningAppInfo> ListRunningApplications()
            {
                var result = new List<RunningAppInfo>();

                foreach (var p in Process.GetProcesses())
                {
                    try
                    {
                        if (p.MainWindowHandle == IntPtr.Zero)
                            continue;

                        string name = p.MainWindowTitle;
                        if (string.IsNullOrWhiteSpace(name))
                            name = p.ProcessName;

                        if (string.IsNullOrWhiteSpace(name))
                            continue;

                        string path = p.MainModule.FileName;

                        result.Add(new RunningAppInfo
                        {
                            Id = p.Id,
                            Name = name.Trim(),
                            Path = path
                        });
                    }
                    catch
                    {
                    }
                }

                return result
                    .OrderBy(a => a.Name, StringComparer.CurrentCultureIgnoreCase)
                    .ToList();
            }
        }

        public class InstalledAppInfo
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string ExecutablePath { get; set; } = string.Empty;

            public static List<InstalledAppInfo> ListInstalledApps()
            {
                var result = new List<InstalledAppInfo>();
                int id = 1;

                string[] roots =
                {
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
                @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
                };

                foreach (string root in roots)
                {
                    using var key = Registry.LocalMachine.OpenSubKey(root);
                    if (key == null) continue;

                    foreach (var subName in key.GetSubKeyNames())
                    {
                        using var sub = key.OpenSubKey(subName);

                        string? name = sub?.GetValue("DisplayName") as string;
                        if (string.IsNullOrWhiteSpace(name))
                            continue;

                        string? installDir = sub?.GetValue("InstallLocation") as string;
                        string? exePath = null;

                        // 1. InstallLocation
                        if (!string.IsNullOrWhiteSpace(installDir)
                            && Directory.Exists(installDir))
                        {
                            exePath = Directory
                                .GetFiles(installDir, "*.exe", SearchOption.TopDirectoryOnly)
                                .FirstOrDefault();
                        }

                        // 2. UninstallString
                        if (exePath == null)
                        {
                            string? uninstall = sub?.GetValue("UninstallString") as string;
                            exePath = ExtractExePath(uninstall);
                        }

                        // 3. Validate exe
                        if (exePath == null || !File.Exists(exePath))
                            continue;

                        result.Add(new InstalledAppInfo
                        {
                            Id = id++,
                            Name = name,
                            ExecutablePath = exePath
                        });
                    }
                }

                return result;
            }

            static string? ExtractExePath(string? text)
            {
                if (string.IsNullOrWhiteSpace(text))
                    return null;

                int i = text.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
                if (i == -1)
                    return null;

                int start = text.LastIndexOf('"', i);
                if (start >= 0)
                    return text.Substring(start + 1, i - start + 4);

                return text.Substring(0, i + 4);
            }

        }

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
                //Tránh đóng app hệ thống
                if (processToStop.MainWindowHandle == IntPtr.Zero)
                {
                    Console.WriteLine("[STOP] Tu choi dung process khong phai App.");
                    return false;
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
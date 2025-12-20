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
            public string ExecutablePath { get; set; } = string.Empty;

            public List<RunningAppInfo> ListRunningApplications()
            {
                var result = new List<RunningAppInfo>();

                foreach (var process in Process.GetProcesses())
                {
                    try
                    {
                        if (process.MainWindowHandle == IntPtr.Zero)
                            continue;

                        string name = process.MainWindowTitle;
                        if (string.IsNullOrWhiteSpace(name))
                            name = process.ProcessName;

                        if (string.IsNullOrWhiteSpace(name))
                            continue;

                        string path = TryGetProcessPath(process);

                        result.Add(new RunningAppInfo
                        {
                            Id = process.Id,
                            Name = name.Trim(),
                            ExecutablePath = path
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

            private static string TryGetProcessPath(Process process)
            {
                try
                {
                    return process.MainModule?.FileName ?? string.Empty;
                }
                catch
                {
                    return string.Empty;
                }
            }
        }

        public class InstalledAppInfo
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string ExecutablePath { get; set; } = string.Empty;

            public string Source { get; set; } = string.Empty;

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
                    using var rootKey = Registry.LocalMachine.OpenSubKey(root);
                    if (rootKey == null) continue;

                    foreach (string subName in rootKey.GetSubKeyNames())
                    {
                        using var sub = rootKey.OpenSubKey(subName);
                        if (sub == null) continue;

                        string? name = sub.GetValue("DisplayName") as string;
                        if (string.IsNullOrWhiteSpace(name))
                            continue;

                        string? exePath = null;
                        string source = "";

                        string? displayIcon = sub.GetValue("DisplayIcon") as string;
                        exePath = ExtractExePath(displayIcon);
                        if (IsValidExe(exePath))
                            source = "DisplayIcon";

                        if (exePath == null)
                        {
                            string? installDir = sub.GetValue("InstallLocation") as string;
                            exePath = FindExeInDirectory(installDir);
                            if (IsValidExe(exePath))
                                source = "InstallLocation";
                        }

                        if (!IsValidExe(exePath))
                            continue;

                        result.Add(new InstalledAppInfo
                        {
                            Id = id++,
                            Name = name,
                            ExecutablePath = exePath!,
                            Source = source
                        });
                    }
                }

                return result;
            }

            // ================== Helpers ==================

            private static bool IsValidExe(string? path)
            {
                return !string.IsNullOrWhiteSpace(path)
                       && path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
                       && File.Exists(path);
            }

            private static string? FindExeInDirectory(string? directory)
            {
                if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
                    return null;

                try
                {
                    return Directory.GetFiles(directory, "*.exe", SearchOption.TopDirectoryOnly)
                        .OrderByDescending(f => new FileInfo(f).Length) // exe chính thường lớn nhất
                        .FirstOrDefault();
                }
                catch
                {
                    return null;
                }
            }

            private static string? ExtractExePath(string? text)
            {
                if (string.IsNullOrWhiteSpace(text))
                    return null;

                int exeIndex = text.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
                if (exeIndex < 0)
                    return null;

                int startQuote = text.LastIndexOf('"', exeIndex);
                if (startQuote >= 0)
                    return text.Substring(startQuote + 1, exeIndex - startQuote + 4);

                return text.Substring(0, exeIndex + 4);
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
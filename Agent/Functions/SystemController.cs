using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Agent.Functions
{
    public static class SystemController
    {
        /// <summary>
        /// Thực hiện lệnh Shutdown (Tắt máy).
        /// Sử dụng: shutdown /s /t 0
        /// </summary>
        public static void Shutdown()
        {
            try
            {
                // /s: Tắt máy
                // /t 0: Thời gian chờ là 10 giây)
                Process.Start("shutdown", "/s /t 10");
                Console.WriteLine("Máy tính đang tắt...");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi thực hiện Shutdown: {ex.Message}");
                // Gợi ý cho người dùng nếu cần quyền Admin
                if (ex.Message.Contains("denied") || ex.HResult == -2147467259)
                {
                    Console.WriteLine("Lưu ý: Bạn có thể cần chạy ứng dụng với quyền Quản trị (Administrator).");
                }
            }
        }

        /// <summary>
        /// Thực hiện lệnh Restart (Khởi động lại).
        /// Sử dụng: shutdown /r /t 0
        /// </summary>
        public static void Restart()
        {
            try
            {
                // /r: Khởi động lại
                // /t 0: Thời gian chờ là 10 giây
                Process.Start("shutdown", "/r /t 10");
                Console.WriteLine("Máy tính đang khởi động lại...");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi thực hiện Restart: {ex.Message}");
                // Gợi ý cho người dùng nếu cần quyền Admin
                if (ex.Message.Contains("denied") || ex.HResult == -2147467259)
                {
                    Console.WriteLine("Lưu ý: Bạn có thể cần chạy ứng dụng với quyền Quản trị (Administrator).");
                }
            }
        }
    }
}
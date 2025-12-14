/*using System;
using AgentForMe.Services;

using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices; // Cần thiết cho IntPtr
using System.Threading;
using System.Web.Services.Description;
using System.Threading.Tasks;*/

/*
Console.WriteLine("\n=============================================");
Console.WriteLine("TEST 4: CHỤP MÀN HÌNH (SCREENSHOT)");
Console.WriteLine("=============================================");

var captureManager = new ScreenCaptureManager();
string fileName = $"screenshot_{DateTime.Now.Ticks}.png";
string filePath = Path.Combine(Environment.CurrentDirectory, fileName);


// Thử chụp và lưu file
bool saveResult = captureManager.CaptureScreenToFile(filePath);

if (saveResult)
{
    Console.WriteLine($"[RESULT] Chụp và lưu file thành công tại: {filePath}");
}
else
{
    Console.WriteLine("[RESULT] Chụp màn hình thất bại. Vui lòng kiểm tra tham chiếu và quyền truy cập.");
}

// (Tùy chọn: Test chuyển đổi Base64)
// string base64String = captureManager.CaptureScreenToBase64();
// if (!string.IsNullOrEmpty(base64String))
// {
//     Console.WriteLine($"[RESULT] Chuỗi Base64 (dài {base64String.Length} ký tự) đã được tạo.");
// }

Console.WriteLine("\n--- KẾT THÚC CHƯƠNG TRÌNH THỬ NGHIỆM ---");
// ...-");

*/


// Program.cs (thêm đoạn này vào)



// ... (Các đoạn code Test 1, 2, 3, 4 trước đó)

// --- TEST 5: CHỨC NĂNG KEYLOGGER ---

// Program.cs (Thêm đoạn này vào)
// Program.cs (Thêm đoạn này vào)


// Program.cs (Thêm đoạn này vào)


// Program.cs (Thêm đoạn này vào)
// Program.cs (Đảm bảo cấu hình và chạy thử đúng)
/*
using System;
using System.Windows.Forms;
using AgentForMe.Services; // Thêm namespace của bạn

namespace AgentForMe
{
    internal static class Program
    {
        // Khai báo một đối tượng tĩnh của lớp Keylogger
        private static KeyboardHook _keylogger;

        [STAThread]
        static void Main()
        {
            // 1. Khởi tạo đối tượng Keylogger
            _keylogger = new KeyboardHook();

            // Khai báo delegate (ủy quyền) để trỏ đến phương thức HookCallback
            KeyboardHook.LowLevelKeyboardProc proc = _keylogger.HookCallback;

            // 2. Thiết lập Hook và lưu ID
            // SetHook trả về IntPtr (là ID của hook), chúng ta đã lưu nó vào _hookID bên trong lớp KeyboardHook
            _keylogger.SetHook(proc);

            // 3. Tạo Message Loop
            // Hàm này bắt buộc để giữ cho ứng dụng không thoát và cho phép hook hoạt động.
            // Nếu không có nó, ứng dụng sẽ chạy và thoát ngay lập tức.
            Application.Run();

            // Lưu ý: Application.Run() thường là điểm cuối của chương trình.
            // Nếu bạn muốn gỡ hook, bạn sẽ cần một cách để thoát Message Loop (ví dụ: nhấn phím Esc) 
            // và gọi _keylogger.UnhookWindowsHookEx(_keylogger._hookID); 
        }
    }
}*/
/* Này test code của Function 6

// File: Program.cs
using System;
using System.Windows.Forms;

// SỬA: Đặt trong cùng Namespace với WebcamAgent và WebcamViewerForm
namespace AgentForMe
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Khởi chạy Form xem Webcam
            // (WebcamViewerForm hiện đã nằm trong Namespace AgentForMe)
            Application.Run(new WebcamViewerForm());
        }
    }

}
*/

// New Test 6 by JD
using System;
using System.Windows.Forms;
using AgentForMe; // Đảm bảo đúng Namespace nơi WebcamViewerForm được định nghĩa

namespace AgentForMe
{
    internal static class Program
    {
        /// <summary>
        /// Điểm vào chính của ứng dụng.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Cần thiết cho các ứng dụng Windows Forms để đảm bảo 
            // các điều khiển được hiển thị chính xác.
            Application.EnableVisualStyles();

            // Đặt chế độ kết xuất văn bản tương thích.
            Application.SetCompatibleTextRenderingDefault(false);

            // Chạy Form hiển thị Webcam.
            // Điều này khởi tạo WebcamViewerForm (Function6_2.cs), 
            // tải các thiết bị, và bắt đầu lắng nghe sự kiện.
            Application.Run(new WebcamViewerForm());
        }
    }
}

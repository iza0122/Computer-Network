using System;
// Yêu cầu cài đặt gói NuGet System.Drawing.Common
using System.IO;
using System.Drawing;
using System.Windows.Forms; // Yêu cầu tham chiếu/gói NuGet nếu không phải WinForms

namespace AgentForMe.Services
{
    /// <summary>
    /// Lớp quản lý chức năng chụp màn hình (Screenshot).
    /// </summary>
    public class ScreenCaptureManager
    {
        /// <summary>
        /// Chụp toàn bộ màn hình và chuyển đổi thành chuỗi Base64.
        /// </summary>
        /// <returns>Chuỗi Base64 của ảnh chụp (dạng PNG) hoặc null nếu lỗi.</returns>
        public string CaptureScreenToBase64()
        {
            try
            {
                // 1. Xác định kích thước màn hình chính
                // Lưu ý: Cần thêm tham chiếu đến System.Windows.Forms để sử dụng Screen.
                // Đối với dự án Console/Worker, có thể cần cài đặt thêm gói NuGet Microsoft.Windows.Compatibility
                Rectangle bounds = Screen.PrimaryScreen.Bounds;

                // 2. Tạo đối tượng Bitmap với kích thước màn hình
                using (Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height))
                {
                    // 3. Tạo đối tượng Graphics từ Bitmap
                    using (Graphics g = Graphics.FromImage(bitmap))
                    {
                        // 4. Copy nội dung màn hình vào Bitmap
                        g.CopyFromScreen(
                            bounds.Location, // Điểm bắt đầu trên màn hình (0,0)
                            Point.Empty,     // Điểm bắt đầu trên Bitmap (0,0)
                            bounds.Size      // Kích thước copy
                        );
                    }

                    // 5. Chuyển Bitmap thành Base64 string
                    using (MemoryStream ms = new MemoryStream())
                    {
                        // Lưu Bitmap vào MemoryStream dưới định dạng PNG (hoặc JPEG)
                        // PNG thường tốt hơn cho ảnh chụp màn hình do nén không mất mát
                        bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);

                        // Chuyển mảng byte thành chuỗi Base64
                        byte[] byteImage = ms.ToArray();
                        return Convert.ToBase64String(byteImage);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SCREENSHOT ERROR] Lỗi khi chụp màn hình: {ex.Message}");
                Console.WriteLine("Lưu ý: Có thể cần chạy với quyền quản trị hoặc kiểm tra System.Drawing.Common đã được tham chiếu.");
                return null;
            }
        }

        /// <summary>
        /// Chụp màn hình và lưu trực tiếp thành file (ví dụ minh họa).
        /// </summary>
        /// <param name="filePath">Đường dẫn đầy đủ để lưu file.</param>
        public bool CaptureScreenToFile(string filePath)
        {
            try
            {
                Rectangle bounds = Screen.PrimaryScreen.Bounds;
                using (Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height))
                {
                    using (Graphics g = Graphics.FromImage(bitmap))
                    {
                        g.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
                    }
                    bitmap.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);
                    Console.WriteLine($"[SCREENSHOT] Đã lưu ảnh chụp tại: {filePath}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SCREENSHOT ERROR] Lỗi khi lưu file: {ex.Message}");
                return false;
            }
        }
    }
}
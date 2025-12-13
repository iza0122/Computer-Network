using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Agent.Functions
{
    public static class ScreenCapture
    {
        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        public static byte[]? CaptureScreenToBytes()
        {
            try
            {
                SetProcessDPIAware();

                Rectangle bounds = SystemInformation.VirtualScreen;

                using Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height);
                using Graphics g = Graphics.FromImage(bitmap);

                g.CopyFromScreen(
                    bounds.Left,
                    bounds.Top,
                    0,
                    0,
                    bounds.Size,
                    CopyPixelOperation.SourceCopy
                );

                using MemoryStream ms = new MemoryStream();
                bitmap.Save(ms, ImageFormat.Png);
                return ms.ToArray();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SCREENSHOT ERROR] {ex.Message}");
                return null;
            }
        }
    }
}

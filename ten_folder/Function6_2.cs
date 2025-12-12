using System;
using System.Drawing;
using System.Windows.Forms;
using AForge.Video;
using AForge.Video.DirectShow;

// SỬA: Đặt trong cùng Namespace với WebcamAgent để tránh lỗi CS0246
namespace AgentForMe
{
    public class WebcamViewerForm : Form
    {
        private WebcamAgent _agent;
        private PictureBox _videoBox;
        private ComboBox _deviceComboBox;
        private Button _toggleButton;
        private Label _statusLabel;

        public WebcamViewerForm()
        {
            InitializeComponent();
            _agent = new WebcamAgent();

            _agent.NewFrameReceived += Agent_NewFrameReceived;
            _agent.VideoErrorOccurred += Agent_VideoErrorOccurred;

            LoadWebcamDevices();
        }

        // Cấu hình các Control trên Form
        private void InitializeComponent()
        {
            this.Text = "Webcam Viewer (C# Agent)";
            this.Size = new Size(1280, 1120);
            this.FormClosing += WebcamViewerForm_FormClosing;

            // 1. Vùng hiển thị video (PictureBox)
            _videoBox = new PictureBox
            {
                Location = new Point(30, 30),
                Size = new Size(1200, 800),
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Zoom
            };
            this.Controls.Add(_videoBox);

            // 2. ComboBox chọn thiết bị
            _deviceComboBox = new ComboBox
            {
                Location = new Point(20, 840),
                Width = 400,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            this.Controls.Add(_deviceComboBox);

            // 3. Nút Bật/Tắt
            _toggleButton = new Button
            {
                Text = "Bật Webcam",
                Location = new Point(1300, 820),
                Width = 190
            };
            _toggleButton.Click += ToggleButton_Click;
            this.Controls.Add(_toggleButton);

            // 4. Label trạng thái
            _statusLabel = new Label
            {
                Text = "Trạng thái: Chưa kết nối.",
                Location = new Point(10, 820),
                AutoSize = true
            };
            this.Controls.Add(_statusLabel);
        }

        // --- HÀM TẢI THIẾT BỊ ---
        private void LoadWebcamDevices()
        {
            int deviceCount = _agent.GetDeviceCount();
            if (deviceCount == 0)
            {
                _statusLabel.Text = "Trạng thái: LỖI - Không tìm thấy Webcam.";
                _toggleButton.Enabled = false;
                return;
            }

            for (int i = 0; i < deviceCount; i++)
            {
                _deviceComboBox.Items.Add($"[{i}] {_agent.GetDeviceName(i)}");
            }
            _deviceComboBox.SelectedIndex = 0;
        }

        // --- HÀM BẬT/TẮT ---
        private void ToggleButton_Click(object sender, EventArgs e)
        {
            if (_agent.IsRunning)
            {
                _agent.TurnOffWebcam();
                _toggleButton.Text = "Bật Webcam";
                _statusLabel.Text = "Trạng thái: Đã TẮT.";
                _videoBox.Image = null;
                _deviceComboBox.Enabled = true;
            }
            else
            {
                try
                {
                    int selectedIndex = _deviceComboBox.SelectedIndex;
                    _agent.TurnOnWebcam(selectedIndex);
                    _toggleButton.Text = "Tắt Webcam";
                    _statusLabel.Text = $"Trạng thái: Đang CHẠY ({_agent.GetDeviceName(selectedIndex)}).";
                    _deviceComboBox.Enabled = false;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi khi bật Webcam: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        // --- XỬ LÝ SỰ KIỆN KHUNG HÌNH (ĐÃ SỬA LỖI CS0123) ---
        private void Agent_NewFrameReceived(object sender, NewFrameEventArgs e)
        {
            // Kiểm tra xem có đang trên luồng UI không
            if (this.InvokeRequired)
            {
                // KHẮC PHỤC LỖI CS0123: Dùng MethodInvoker delegate an toàn
                this.Invoke((MethodInvoker)delegate
                {
                    // Gọi lại hàm này trên luồng UI
                    Agent_NewFrameReceived(sender, e);
                });
            }
            else
            {
                // Luồng UI: Cập nhật PictureBox
                // Giải phóng hình ảnh cũ và gán hình ảnh mới
                _videoBox.Image?.Dispose();
                _videoBox.Image = (Bitmap)e.Frame.Clone();
            }
        }

        // --- XỬ LÝ LỖI ---
        private void Agent_VideoErrorOccurred(object sender, VideoSourceErrorEventArgs e)
        {
            // Xử lý lỗi trên luồng UI
            this.Invoke((MethodInvoker)delegate
            {
                MessageBox.Show($"Lỗi xảy ra với nguồn video: {e.Description}", "Lỗi Video", MessageBoxButtons.OK, MessageBoxIcon.Error);
                // Gọi ToggleButton_Click để Tắt Webcam và cập nhật trạng thái
                ToggleButton_Click(null, null);
            });
        }

        // --- XỬ LÝ KHI ĐÓNG FORM ---
        private void WebcamViewerForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            _agent.TurnOffWebcam();
        }
    }
}
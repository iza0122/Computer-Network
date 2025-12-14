
// Phiên bản Update tiếp theo ( Mục đích tạo window Form Để test chương trình thử (Không liên quan nhưng mà để tets thử chức năng)
// Bản cập nhập ngày 14/12/2025

using System;
using System.Drawing;
using System.Windows.Forms;
using AForge.Video;
using AForge.Video.DirectShow;
using System.Collections.Generic;
using System.Linq; // Cần thiết cho .Sum()
using System.IO; // Cần thiết cho việc lưu trữ File/Directory

namespace AgentForMe
{
    public class WebcamViewerForm : Form
    {
        // Hằng số cho đường dẫn lưu trữ
        private const string SAVE_PATH = @"C:\Users\Admin\source\repos\OOPLT\Ex6.1\Test_VIDEO";

        private WebcamAgent _agent;
        private PictureBox _videoBox;
        private ComboBox _deviceComboBox;
        private Button _toggleButton;
        private Button _checkBytesButton;
        private Label _statusLabel;

        public WebcamViewerForm()
        {
            InitializeComponent();
            _agent = new WebcamAgent();

            _agent.NewFrameReceived += Agent_NewFrameReceived;
            _agent.VideoErrorOccurred += Agent_VideoErrorOccurred;

            LoadWebcamDevices();
            UpdateCheckBytesButtonState();
        }

        // Cấu hình các Control trên Form
        private void InitializeComponent()
        {
            this.Text = "Webcam Video Recorder (C# Agent)";
            this.Size = new Size(800, 700);
            this.FormClosing += WebcamViewerForm_FormClosing;

            // 1. Vùng hiển thị video (PictureBox)
            _videoBox = new PictureBox
            {
                Location = new Point(30, 30),
                Size = new Size(720, 480),
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Zoom
            };
            this.Controls.Add(_videoBox);

            // 2. Label trạng thái
            _statusLabel = new Label
            {
                Text = "Trạng thái: Chưa kết nối.",
                Location = new Point(30, 530),
                AutoSize = true
            };
            this.Controls.Add(_statusLabel);

            // 3. ComboBox chọn thiết bị
            _deviceComboBox = new ComboBox
            {
                Location = new Point(30, 560),
                Width = 300,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            this.Controls.Add(_deviceComboBox);

            // 4. Nút Bật/Tắt
            _toggleButton = new Button
            {
                Text = "Bật Webcam",
                Location = new Point(340, 560),
                Width = 120
            };
            _toggleButton.Click += ToggleButton_Click;
            this.Controls.Add(_toggleButton);

            // 5. NÚT: Kiểm tra Video Bytes
            _checkBytesButton = new Button
            {
                Text = "Kiểm tra Video Bytes",
                Location = new Point(470, 560),
                Width = 160,
                Enabled = false
            };
            _checkBytesButton.Click += CheckBytesButton_Click;
            this.Controls.Add(_checkBytesButton);
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

        // --- CẬP NHẬT TRẠNG THÁI NÚT KIỂM TRA BYTES ---
        private void UpdateCheckBytesButtonState()
        {
            // Cho phép kiểm tra khi Webcam ĐÃ TẮT và có dữ liệu (số khung hình > 0)
            _checkBytesButton.Enabled = !_agent.IsRunning && _agent.GetRecordedVideoBytes().Count > 0;
        }

        // --- HÀM BẬT/TẮT ---
        private void ToggleButton_Click(object sender, EventArgs e)
        {
            if (_agent.IsRunning)
            {
                // TẮT WEB CAM (và lưu trữ video bytes)
                _agent.TurnOffWebcam();
                int recordedFrames = _agent.GetRecordedVideoBytes().Count;

                _toggleButton.Text = "Bật Webcam";
                _statusLabel.Text = $"Trạng thái: Đã TẮT. Đã ghi được {recordedFrames} khung hình.";
                _videoBox.Image = null;
                _deviceComboBox.Enabled = true;

                // THÊM: Lưu các byte ra thư mục
                if (recordedFrames > 0)
                {
                    SaveRecordedVideoBytes();
                }
            }
            else
            {
                try
                {
                    // BẬT WEB CAM
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
            UpdateCheckBytesButtonState(); // Cập nhật trạng thái nút sau khi Bật/Tắt
        }

        // --- HÀM MỚI: LƯU VIDEO BYTES RA ĐƯỜNG DẪN ---
        private void SaveRecordedVideoBytes()
        {
            try
            {
                List<List<byte>> videoBytes = _agent.GetRecordedVideoBytes();
                int frameCount = videoBytes.Count;

                if (frameCount == 0) return;

                // 1. Tạo thư mục nếu chưa tồn tại
                Directory.CreateDirectory(SAVE_PATH);

                // 2. Xóa các tệp cũ trong thư mục (để tránh quá tải)
                foreach (string file in Directory.GetFiles(SAVE_PATH, "*.jpg"))
                {
                    File.Delete(file);
                }

                // 3. Lưu từng khung hình List<byte> thành tệp .jpg
                for (int i = 0; i < frameCount; i++)
                {
                    // Chuyển List<byte> thành byte[]
                    byte[] frameByteArray = videoBytes[i].ToArray();
                    string filePath = Path.Combine(SAVE_PATH, $"frame_{i:D4}.jpg");

                    // Ghi byte[] vào tệp
                    File.WriteAllBytes(filePath, frameByteArray);
                }

                MessageBox.Show(
                    $"Đã lưu trữ {frameCount} khung hình (dạng JPEG) vào:\n{SAVE_PATH}",
                    "Lưu Trữ Thành Công",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi lưu trữ video bytes: {ex.Message}", "Lỗi Lưu Trữ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // --- HÀM KIỂM TRA VIDEO BYTES (Không đổi) ---
        private void CheckBytesButton_Click(object sender, EventArgs e)
        {
            List<List<byte>> videoBytes = _agent.GetRecordedVideoBytes();
            int frameCount = videoBytes.Count;

            if (frameCount == 0)
            {
                MessageBox.Show("Không có khung hình nào được ghi.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Tính tổng kích thước của tất cả các mảng byte (tổng kích thước video)
            long totalSizeBytes = videoBytes.Sum(frame => (long)frame.Count);
            double totalSizeMB = totalSizeBytes / 1024.0 / 1024.0;

            MessageBox.Show(
                $"Đã lưu trữ thành công video gồm {frameCount} khung hình.\n" +
                $"Tổng kích thước dữ liệu List<List<byte>>: {totalSizeMB:F2} MB.\n\n" +
                "Dữ liệu này hiện đã sẵn sàng trong bộ nhớ để trả về cho API/Socket.",
                "Xác nhận Ghi Video",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        // --- XỬ LÝ SỰ KIỆN KHUNG HÌNH (Không đổi) ---
        private void Agent_NewFrameReceived(object sender, NewFrameEventArgs e)
        {
            if (this.InvokeRequired)
            {
                this.Invoke((MethodInvoker)delegate
                {
                    Agent_NewFrameReceived(sender, e);
                });
            }
            else
            {
                // Luồng UI: Cập nhật PictureBox
                _videoBox.Image?.Dispose();
                _videoBox.Image = (Bitmap)e.Frame.Clone();
            }
        }

        // --- XỬ LÝ LỖI (Không đổi) ---
        private void Agent_VideoErrorOccurred(object sender, VideoSourceErrorEventArgs e)
        {
            this.Invoke((MethodInvoker)delegate
            {
                MessageBox.Show($"Lỗi xảy ra với nguồn video: {e.Description}", "Lỗi Video", MessageBoxButtons.OK, MessageBoxIcon.Error);
                if (_agent.IsRunning) ToggleButton_Click(null, null);
            });
        }

        // --- XỬ LÝ KHI ĐÓNG FORM (Không đổi) ---
        private void WebcamViewerForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            _agent.TurnOffWebcam();
        }
    }
}

// Phiên bản cũ
/*﻿using System;
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

}*/

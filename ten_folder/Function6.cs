using System;
using AForge.Video.DirectShow; // Cần thiết để giao tiếp với Webcam (DirectShow)
using AForge.Video;             // Cần thiết cho các đối tượng xử lý video
using System.Drawing;           // Cần thiết để xử lý đối tượng Bitmap (khung hình)
using System.Threading;
using System;


namespace AgentForMe
{
    /// <summary>
    /// Lớp quản lý và điều khiển chức năng Bật/Tắt Webcam.
    /// </summary>
    public class WebcamAgent
    {
        // Sự kiện thông báo khi một khung hình mới được nhận
        public event EventHandler<NewFrameEventArgs> NewFrameReceived;

        // Sự kiện thông báo khi có lỗi từ nguồn video
        public event EventHandler<VideoSourceErrorEventArgs> VideoErrorOccurred;

        // Thiết bị Webcam được chọn
        private VideoCaptureDevice _videoSource;

        // Danh sách các thiết bị Webcam có sẵn
        private FilterInfoCollection _videoDevices;

        // Trạng thái hiện tại của Webcam
        public bool IsRunning => _videoSource != null && _videoSource.IsRunning;

        /// <summary>
        /// Khởi tạo Agent và quét các thiết bị Webcam có sẵn.
        /// </summary>
        public WebcamAgent()
        {
            // Quét tất cả các thiết bị video input (Webcam)
            _videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
        }

        /// <summary>
        /// Trả về số lượng Webcam được tìm thấy trên hệ thống.
        /// </summary>
        public int GetDeviceCount()
        {
            return _videoDevices.Count;
        }

        /// <summary>
        /// Trả về tên của Webcam tại chỉ số được chỉ định.
        /// </summary>
        /// <param name="index">Chỉ số của Webcam (bắt đầu từ 0).</param>
        /// <returns>Tên Webcam hoặc null nếu chỉ số không hợp lệ.</returns>
        public string GetDeviceName(int index)
        {
            if (index >= 0 && index < _videoDevices.Count)
            {
                return _videoDevices[index].Name;
            }
            return null;
        }

        /// <summary>
        /// Bật Webcam được chọn.
        /// </summary>
        /// <param name="deviceIndex">Chỉ số của Webcam cần Bật.</param>
        /// <exception cref="InvalidOperationException">Ném ngoại lệ nếu không tìm thấy thiết bị hoặc thiết bị đang chạy.</exception>
        public void TurnOnWebcam(int deviceIndex = 0)
        {
            if (_videoDevices.Count == 0)
            {
                throw new InvalidOperationException("Không tìm thấy bất kỳ thiết bị Webcam nào.");
            }

            if (IsRunning)
            {
                throw new InvalidOperationException("Webcam đã đang chạy.");
            }

            if (deviceIndex < 0 || deviceIndex >= _videoDevices.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(deviceIndex), "Chỉ số thiết bị không hợp lệ.");
            }

            // Khởi tạo nguồn video với MonikerString của thiết bị được chọn
            _videoSource = new VideoCaptureDevice(_videoDevices[deviceIndex].MonikerString);

            // Đăng ký các sự kiện xử lý khung hình và lỗi
            _videoSource.NewFrame += VideoSource_NewFrame;
            _videoSource.VideoSourceError += VideoSource_VideoSourceError;

            // Bắt đầu truyền video (BẬT Webcam)
            _videoSource.Start();
        }

        /// <summary>
        /// Tắt Webcam hiện tại.
        /// </summary>
        public void TurnOffWebcam()
        {
            if (_videoSource != null && _videoSource.IsRunning)
            {
                // Ra lệnh dừng stream
                _videoSource.SignalToStop();
                // Đợi cho stream dừng hẳn (quan trọng để giải phóng tài nguyên)
                _videoSource.WaitForStop();

                // Hủy đăng ký sự kiện để dọn dẹp bộ nhớ
                _videoSource.NewFrame -= VideoSource_NewFrame;
                _videoSource.VideoSourceError -= VideoSource_VideoSourceError;
                _videoSource = null;
            }
        }

        /// <summary>
        /// Xử lý sự kiện nhận khung hình mới từ Webcam.
        /// </summary>
        private void VideoSource_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            // Sử dụng Invoke để kích hoạt sự kiện công khai (public event)
            NewFrameReceived?.Invoke(this, eventArgs);

            // LƯU Ý: Khung hình là Bitmap, cần clone nếu muốn sử dụng ngoài sự kiện này
            // Ví dụ: Bitmap currentFrame = (Bitmap)eventArgs.Frame.Clone();
        }

        /// <summary>
        /// Xử lý sự kiện lỗi nguồn video.
        /// </summary>
        private void VideoSource_VideoSourceError(object sender, VideoSourceErrorEventArgs eventArgs)
        {
            VideoErrorOccurred?.Invoke(this, eventArgs);
            // Trong trường hợp lỗi, nên TẮT Webcam để tránh kẹt
            TurnOffWebcam();
        }
    }
}




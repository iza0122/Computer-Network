using System;
using AForge.Video.DirectShow;
using AForge.Video;
using System.Drawing;
using System.Drawing.Imaging; // Cần thiết để lưu Bitmap thành Stream
using System.IO;              // Cần thiết để xử lý Stream/MemoryStream
using System.Collections.Generic; // Cần thiết cho List<List<byte>>
using System.Threading;

namespace Agent.Functions
{
    /// <summary>
    /// Lớp quản lý và điều khiển chức năng Bật/Tắt Webcam.
    /// Đã thêm chức năng ghi video dưới dạng danh sách mảng byte.
    /// </summary>
    public class Webcam
    {
        // Sự kiện thông báo khi một khung hình mới được nhận
        public event EventHandler<NewFrameEventArgs> NewFrameReceived;

        // Sự kiện thông báo khi có lỗi từ nguồn video
        public event EventHandler<VideoSourceErrorEventArgs> VideoErrorOccurred;

        // Thiết bị Webcam được chọn
        private VideoCaptureDevice _videoSource;

        // Danh sách các thiết bị Webcam có sẵn
        private FilterInfoCollection _videoDevices;

        // TRƯỜNG MỚI: Danh sách các mảng byte của khung hình video đã ghi
        // Mỗi List<byte> là dữ liệu (JPEG) của một khung hình
        private List<List<byte>> _videoFramesBytes;

        // Trạng thái hiện tại của Webcam
        public bool IsRunning => _videoSource != null && _videoSource.IsRunning;

        /// <summary>
        /// Khởi tạo Agent và quét các thiết bị Webcam có sẵn.
        /// </summary>
        public Webcam()
        {
            // Quét tất cả các thiết bị video input (Webcam)
            _videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            _videoFramesBytes = new List<List<byte>>(); // Khởi tạo List<List<byte>>
        }

        /// <summary>
        /// TRUY CẬP CÔNG KHAI: Trả về danh sách các mảng byte của tất cả khung hình video đã ghi.
        /// </summary>
        /// <returns>Danh sách các List<byte> (List<List<byte>>) đại diện cho video đã ghi.</returns>
        public List<List<byte>> GetRecordedVideoBytes()
        {
            // Trả về một bản sao để đảm bảo an toàn luồng
            return new List<List<byte>>(_videoFramesBytes);
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

            // Xóa dữ liệu video cũ khi BẬT Webcam mới (bắt đầu ghi mới)
            _videoFramesBytes.Clear();

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
                // LƯU Ý: Dữ liệu video đã được giữ lại trong _videoFramesBytes
            }
        }

        /// <summary>
        /// Chuyển đổi đối tượng Bitmap (khung hình) thành List<byte> (định dạng JPEG).
        /// </summary>
        private List<byte> ConvertBitmapToBytes(Bitmap frame)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                // Lưu trữ dưới dạng JPEG để nén và giảm kích thước mảng byte
                frame.Save(ms, ImageFormat.Jpeg);
                // Chuyển MemoryStream thành mảng byte, sau đó là List<byte>
                return new List<byte>(ms.ToArray());
            }
        }

        /// <summary>
        /// Xử lý sự kiện nhận khung hình mới từ Webcam và lưu trữ nó.
        /// </summary>
        private void VideoSource_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            // 1. Lưu trữ khung hình dưới dạng List<byte> và thêm vào danh sách video
            // Cần CLONE khung hình trước khi xử lý vì eventArgs.Frame sẽ bị hủy sau khi sự kiện kết thúc
            using (Bitmap currentFrame = (Bitmap)eventArgs.Frame.Clone())
            {
                List<byte> frameBytes = ConvertBitmapToBytes(currentFrame);

                // Thêm mảng byte của khung hình vào danh sách video
                // Đây là nơi tạo ra List<List<byte>> theo yêu cầu của bạn.
                _videoFramesBytes.Add(frameBytes);
            }

            // 2. Kích hoạt sự kiện công khai để hiển thị
            NewFrameReceived?.Invoke(this, eventArgs);
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
using System.Text;
using Windows.Devices.Enumeration;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage;

namespace Agent.Functions
{
    public class WebcamManager
    {
        public async Task<byte[]> RecordWebcamVideoAsync(string videoDeviceId, int durationSeconds)
        {
            MediaCapture capture = null;
            StorageFile tempFile = null;
            string physicalPath = null;

            try
            {
                var settings = new MediaCaptureInitializationSettings
                {
                    VideoDeviceId = videoDeviceId,
                    StreamingCaptureMode = StreamingCaptureMode.Video
                };

                capture = new MediaCapture();
                await capture.InitializeAsync(settings);

                //file tạm trong thư mục Videos (có quyền truy cập)
                StorageFolder videosFolder = KnownFolders.VideosLibrary;
                string fileName = $"webcam_temp_{Guid.NewGuid()}.mp4";
                tempFile = await videosFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
                physicalPath = tempFile.Path;

                var profile = MediaEncodingProfile.CreateMp4(VideoEncodingQuality.Auto);

                await capture.StartRecordToStorageFileAsync(profile, tempFile);
                await Task.Delay(TimeSpan.FromSeconds(durationSeconds));
                await capture.StopRecordAsync();
                await Task.Delay(500);
                byte[] videoData = File.ReadAllBytes(physicalPath);

                Console.WriteLine($"Đã đọc {videoData.Length} bytes");

                return videoData;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Chi tiết lỗi: {ex}");
                throw;
            }
            finally
            {
                capture?.Dispose();

                // Xóa file tạm
                if (!string.IsNullOrEmpty(physicalPath) && File.Exists(physicalPath))
                {
                    try
                    {
                        File.Delete(physicalPath);
                        Console.WriteLine("Đã xóa file tạm.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Không thể xóa file tạm: {ex.Message}");
                    }
                }
            }
        }

        public class WebcamInfo
        {
            public string? Name { get; set; }
            public string? Id { get; set; }
            public bool IsEnabled { get; set; }
        }

        public async Task<List<WebcamInfo>> GetWebcamsListAsync()
        {
            var webcamList = new List<WebcamInfo>();
            DeviceInformationCollection devices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);

            foreach (var device in devices)
            {
                webcamList.Add(new WebcamInfo
                {
                    Name = device.Name,
                    Id = device.Id,
                    IsEnabled = device.IsEnabled
                });
            }

            return webcamList;
        }
    }
}
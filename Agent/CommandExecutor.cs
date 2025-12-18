using Agent.Functions;
using Shared;
using System.Text.Json;

namespace Agent
{
    public class CommandExecutor
    {
        private IResponseSender _responseSender;
        private ApplicationManager _appManager;
        private TaskManager _taskManager;
        private WebcamManager _webcamManager;

        public CommandExecutor(IResponseSender responseSender)
        {
            _responseSender = responseSender;
            _appManager = new ApplicationManager();
            _taskManager = new TaskManager();
            _webcamManager = new WebcamManager();
        }

        public async Task Execute(RemoteCommand command, CancellationToken cancellationToken)
        {
            if (!Enum.TryParse(command.Name, true, out AgentCommandType commandType))
            {
                Console.WriteLine($"[EXECUTOR] Lệnh không hợp lệ: {command.Name}");
                return;
            }

            switch (commandType)
            {
                case AgentCommandType.Shutdown:
                    ExecuteShutdown();
                    break;
                case AgentCommandType.Restart:
                    ExecuteRestart();
                    break;
                case AgentCommandType.Capture:
                    await ExecuteCapture(cancellationToken);
                    break;
                case AgentCommandType.ListInstalledApp:
                    await ExecuteListInstalledApp(cancellationToken);
                    break;
                case AgentCommandType.StartApp:
                    await ExecuteStartApp(command.Data, cancellationToken);
                    break;
                case AgentCommandType.StopApp:
                    await ExecuteStopApp(command.Data, cancellationToken);
                    break;
                case AgentCommandType.StartTask:
                    await ExecuteStartTask(command.Data, cancellationToken);
                    break;
                case AgentCommandType.ListRunningTask:
                    await ExecuteListTask(cancellationToken);
                    break;
                case AgentCommandType.StopTask:
                    await ExecuteStopTask(command.Data, cancellationToken);
                    break;
                case AgentCommandType.ListWebcam:
                    await ExecuteListWebcam(cancellationToken);
                    break;
                case AgentCommandType.RecordingWebcam:
                    await ExecuteRecordingWebcam(command.Data, cancellationToken);
                    break;

                default:
                    Console.WriteLine($"[EXECUTOR] Cảnh báo: Lệnh '{commandType}' hiện chưa được hỗ trợ hệ thống.");
                    break;
            }
        }

        private void ExecuteShutdown()
        {
            Console.WriteLine("[SYSTEM] Đang thực hiện lệnh tắt máy...");
            Functions.SystemController.Shutdown();
        }

        private void ExecuteRestart()
        {
            Console.WriteLine("[SYSTEM] Đang thực hiện lệnh khởi động lại máy...");
            Functions.SystemController.Restart();
        }

        private async Task ExecuteCapture(CancellationToken cancellationToken)
        {
            Console.WriteLine("[ACTION] Đang tiến hành chụp màn hình...");
            byte[]? imageBuffer = Functions.ScreenCapture.CaptureScreenToBytes();

            if (imageBuffer == null || imageBuffer.Length == 0)
            {
                string errorMsg = "Lỗi: Không thể chụp ảnh màn hình (Kiểm tra quyền truy cập hoặc thư viện đồ họa).";
                await _responseSender.SendStatus(false, errorMsg, cancellationToken);
                Console.WriteLine($"[ERROR] {errorMsg}");
                return;
            }

            try
            {
                await _responseSender.SendData(MessageType.Image, imageBuffer, cancellationToken);
                string successMsg = "Đã gửi ảnh chụp màn hình thành công.";
                await _responseSender.SendStatus(true, successMsg, cancellationToken);
                Console.WriteLine($"[SUCCESS] {successMsg} ({imageBuffer.Length} bytes)");
            }
            catch (Exception ex)
            {
                await _responseSender.SendStatus(false, $"Lỗi truyền tải ảnh: {ex.Message}", cancellationToken);
            }
        }

        private async Task ExecuteListInstalledApp(CancellationToken cancellationToken)
        {
            try
            {
                Console.WriteLine("[QUERY] Đang lấy danh sách ứng dụng đã cài đặt...");
                var tempList = Functions.ApplicationManager.InstalledAppInfo.ListInstalledApps();

                if (tempList == null || tempList.Count == 0)
                {
                    string msg = "Không tìm thấy dữ liệu ứng dụng nào trên hệ thống.";
                    Console.WriteLine($"[INFO] {msg}");
                    await _responseSender.SendStatus(false, msg, cancellationToken);
                    return;
                }

                string jsonData = JsonSerializer.Serialize(new
                {
                    Type = "INSTALLED_APP_LIST",
                    Timestamp = DateTime.Now,
                    Data = tempList
                });

                await _responseSender.SendText(jsonData, cancellationToken);
                await _responseSender.SendStatus(true, $"Đã gửi danh sách {tempList.Count} ứng dụng.", cancellationToken);
                Console.WriteLine($"[SUCCESS] Đã gửi thông tin của {tempList.Count} ứng dụng.");
            }
            catch (Exception ex)
            {
                string msg = $"Lỗi khi truy xuất danh sách ứng dụng: {ex.Message}";
                await _responseSender.SendStatus(false, msg, cancellationToken);
                Console.WriteLine($"[ERROR] {msg}");
            }
        }

        private async Task ExecuteStartApp(object data, CancellationToken cancellationToken)
        {
            if (data is JsonElement element)
            {
                try
                {
                    string? executablePath = element.GetProperty("Path").GetString();
                    string arguments = element.TryGetProperty("Arguments", out JsonElement arg) ? arg.GetString() ?? "" : "";

                    if (string.IsNullOrEmpty(executablePath))
                    {
                        await _responseSender.SendStatus(false, "Đường dẫn ứng dụng không được để trống.", cancellationToken);
                        return;
                    }

                    Console.WriteLine($"[ACTION] Đang khởi chạy ứng dụng: {executablePath}");
                    bool success = _appManager.StartApplication(executablePath, arguments);

                    string statusMsg = success ? "Khởi chạy ứng dụng thành công." : "Không thể khởi chạy ứng dụng (Kiểm tra đường dẫn).";
                    await _responseSender.SendStatus(success, statusMsg, cancellationToken);
                    Console.WriteLine($"[{(success ? "SUCCESS" : "FAIL")}] {statusMsg}");
                }
                catch (Exception ex)
                {
                    await _responseSender.SendStatus(false, $"Lỗi xử lý lệnh StartApp: {ex.Message}", cancellationToken);
                }
            }
        }

        private async Task ExecuteStopApp(object data, CancellationToken ct)
        {
            if (data is JsonElement element)
            {
                try
                {
                    if (!element.TryGetProperty("ID", out JsonElement idElement))
                    {
                        await _responseSender.SendStatus(false, "Thiếu mã định danh (ID) ứng dụng để đóng.", ct);
                        return;
                    }

                    int id = idElement.GetInt32();
                    Console.WriteLine($"[ACTION] Đang đóng ứng dụng ID: {id}");
                    bool success = _appManager.StopApplication(id);

                    string msg = success ? $"Đã đóng thành công ứng dụng (PID: {id})." : $"Thất bại khi đóng ứng dụng (PID: {id}).";
                    await _responseSender.SendStatus(success, msg, ct);
                    Console.WriteLine($"[{(success ? "SUCCESS" : "FAIL")}] {msg}");
                }
                catch (Exception ex)
                {
                    await _responseSender.SendStatus(false, $"Lỗi hệ thống khi đóng ứng dụng: {ex.Message}", ct);
                }
            }
        }

        private async Task ExecuteListTask(CancellationToken cancellationToken)
        {
            try
            {
                Console.WriteLine("[QUERY] Đang lấy danh sách các tác vụ đang chạy...");
                var processes = _taskManager.ListProcesses();

                string jsonData = JsonSerializer.Serialize(new
                {
                    Type = "TASK_LIST",
                    Timestamp = DateTime.Now,
                    Data = processes
                });

                await _responseSender.SendText(jsonData, cancellationToken);
                await _responseSender.SendStatus(true, $"Đã gửi danh sách {processes.Count} tác vụ.", cancellationToken);
                Console.WriteLine($"[SUCCESS] Tìm thấy {processes.Count} tác vụ đang hoạt động.");
            }
            catch (Exception ex)
            {
                await _responseSender.SendStatus(false, $"Lỗi khi lấy danh sách tác vụ: {ex.Message}", cancellationToken);
            }
        }

        private async Task ExecuteStartTask(object data, CancellationToken cancellationToken)
        {
            if (data is JsonElement element)
            {
                try
                {
                    string? path = element.TryGetProperty("Path", out var p) ? p.GetString() : null;
                    if (string.IsNullOrEmpty(path))
                    {
                        await _responseSender.SendStatus(false, "Cần đường dẫn (Path) để khởi chạy tác vụ.", cancellationToken);
                        return;
                    }

                    Console.WriteLine($"[ACTION] Đang tạo tác vụ mới: {path}");
                    bool success = _taskManager.StartProcessByName(path);

                    string msg = success ? "Khởi chạy tác vụ thành công." : "Khởi chạy tác vụ thất bại.";
                    await _responseSender.SendStatus(success, msg, cancellationToken);
                    Console.WriteLine($"[{(success ? "SUCCESS" : "FAIL")}] {msg}");
                }
                catch (Exception ex)
                {
                    await _responseSender.SendStatus(false, $"Lỗi khởi chạy tác vụ: {ex.Message}", cancellationToken);
                }
            }
        }

        private async Task ExecuteStopTask(object data, CancellationToken cancellationToken)
        {
            if (data is JsonElement element)
            {
                try
                {
                    if (element.TryGetProperty("ID", out JsonElement idElement))
                    {
                        int pid = idElement.GetInt32();
                        Console.WriteLine($"[ACTION] Đang dừng tác vụ ID: {pid}");
                        bool success = _taskManager.StopProcessById(pid);

                        string msg = success ? $"Đã dừng tác vụ {pid} thành công." : $"Không thể dừng tác vụ {pid} (Có thể tiến trình không tồn tại).";
                        await _responseSender.SendStatus(success, msg, cancellationToken);
                        Console.WriteLine($"[{(success ? "SUCCESS" : "FAIL")}] {msg}");
                    }
                    else
                    {
                        await _responseSender.SendStatus(false, "Vui lòng cung cấp ID tác vụ.", cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    await _responseSender.SendStatus(false, $"Lỗi khi dừng tác vụ: {ex.Message}", cancellationToken);
                }
            }
        }
    
    
        private async Task ExecuteListWebcam(CancellationToken cancellationToken)
        {
            try
            {
                Console.WriteLine("[QUERY] Đang lấy danh sách các tác vụ đang chạy...");
                List<WebcamManager.WebcamInfo> processes = await _webcamManager.GetWebcamsListAsync();

                string jsonData = JsonSerializer.Serialize(new
                {
                    Type = "WEBCAM_LIST",
                    Data = processes
                });

                await _responseSender.SendText(jsonData, cancellationToken);
                await _responseSender.SendStatus(true, $"Đã gửi danh sách {processes.Count} webcam.", cancellationToken);
                Console.WriteLine($"[SUCCESS] Tìm thấy {processes.Count} webcam đang hoạt động.");
            }
            catch (Exception ex)
            {
                await _responseSender.SendStatus(false, $"Lỗi khi lấy danh sách webcam: {ex.Message}", cancellationToken);
            }
        }

        public async Task ExecuteRecordingWebcam(object data, CancellationToken cancellationToken)
        {
            string? videoDeviceId = data switch
            {
                string s => s,
                JsonElement e => e.ValueKind == JsonValueKind.String ? e.GetString() : null,
                _ => null
            };

            if (string.IsNullOrEmpty(videoDeviceId))
            {
                string errorMsg = "Lỗi: Device ID không hợp lệ hoặc bị trống.";
                await _responseSender.SendStatus(false, errorMsg, cancellationToken);
                Console.WriteLine($"[WEBCAM] {errorMsg}");
                return;
            }

            try
            {
                Console.WriteLine($"[ACTION] Bắt đầu ghi hình 10 giây trên thiết bị: {videoDeviceId}");
                //await _responseSender.SendStatus(true, "Đang khởi tạo webcam và bắt đầu ghi hình...", cancellationToken);

                byte[] videoData = await _webcamManager.RecordWebcamVideoAsync(videoDeviceId, 10);

                if (videoData != null && videoData.Length > 0)
                {
                    Console.WriteLine($"[SUCCESS] Ghi hình hoàn tất. Dung lượng: {videoData.Length} bytes.");
                    await _responseSender.SendData(MessageType.Video, videoData, cancellationToken);
                    await _responseSender.SendStatus(true, "Đã gửi tệp video thành công.", cancellationToken);
                }
                else
                {
                    string failMsg = "Lỗi: Dữ liệu video thu được bị trống.";
                    await _responseSender.SendStatus(false, failMsg, cancellationToken);
                    Console.WriteLine($"[ERROR] {failMsg}");
                }
            }
            catch (OperationCanceledException)
            {
                string cancelMsg = "Tác vụ ghi hình đã bị hủy bởi hệ thống/người dùng.";
                await _responseSender.SendStatus(false, cancelMsg, cancellationToken);
                Console.WriteLine($"[CANCEL] {cancelMsg}");
            }
            catch (Exception ex)
            {
                string errorMsg = $"Lỗi thực thi quay webcam: {ex.Message}";
                await _responseSender.SendStatus(false, errorMsg, cancellationToken);
                Console.WriteLine($"[FATAL] {errorMsg}");
            }
        }
    }
}
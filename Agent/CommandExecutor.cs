using Agent.Functions;
using Shared;
using System.Text.Json;

namespace Agent
{
    public class CommandExecutor
    {
        private IResponseSender _responseSender;
        public CommandExecutor(IResponseSender responseSender) {  _responseSender = responseSender; }
        public async Task Execute(RemoteCommand command , CancellationToken cancellationToken)
        {
            // 1. Chuyển chuỗi Name (string) sang enum an toàn
            if (!Enum.TryParse(command.Name, true, out AgentCommandType commandType))
            {
                Console.WriteLine($"[Executor] Lệnh không hợp lệ: {command.Name}");
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
                    ExecuteStartApp(command.Data);
                    break;
                case AgentCommandType.StopApp:
                    ExecuteStopApp(command.Data);
                    break;
                case AgentCommandType.StartTask:
                    ExecuteStartTask();
                    break;
                case AgentCommandType.Webcam:
                    ExecuteWebcam();
                    break;

                default:
                    Console.WriteLine($"[Executor] Lệnh '{commandType}' không được hỗ trợ.");
                    break;
            }
        }

        // Hàm xử lý cụ thể
        private void ExecuteShutdown()
        {
            Console.WriteLine("[Executor] Thực thi: Tắt máy");
            Functions.SystemController.Shutdown();
        }  

        private void ExecuteRestart()
        {
            Console.WriteLine("[Executor] Thực thi: Bật lại máy");
            Functions.SystemController.Restart();
        }

        private async Task ExecuteCapture(CancellationToken cancellationToken)
        {
            Console.WriteLine("[Executor] Thực thi: Chụp màn hình");
            byte[]? imageBuffer = Functions.ScreenCapture.CaptureScreenToBytes();

            if (imageBuffer == null || imageBuffer.Length == 0)
            {
                string errorMsg = "[ERROR] Chụp ảnh thất bại. Kiểm tra quyền truy cập/thư viện System.Drawing.";
                await _responseSender.SendData(errorMsg, cancellationToken);
                Console.WriteLine(errorMsg);
            }

            try
            {
                await _responseSender.SendData(imageBuffer, cancellationToken);
                string successMsg = $"[OK] Chụp ảnh thành công. Kích thước: {imageBuffer.Length} bytes.";
                await _responseSender.SendData(successMsg, cancellationToken);

                Console.WriteLine(successMsg);
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine("[Executor] Tác vụ gửi phản hồi đã bị hủy.");
            }
            catch (Exception ex)
            {
                await _responseSender.SendData($"[ERROR] Lỗi gửi ảnh: {ex.Message}", cancellationToken);
            }
        }

        private async Task ExecuteListInstalledApp(CancellationToken cancellationToken)
        {
            try
            {
                Console.WriteLine("[Executor] Thực thi: Gửi danh sách ứng dụng đã cài đặt");
                var tempList = Functions.ApplicationManager.InstalledAppInfo.ListInstalledApps();

                if (tempList == null || tempList.Count == 0)
                {
                    Console.WriteLine("[Executor] Không tìm thấy ứng dụng nào.");
                    return;
                }

                string jsonData = System.Text.Json.JsonSerializer.Serialize(new
                {
                    Type = "InstalledAppList",
                    Timestamp = DateTime.Now,
                    Data = tempList
                });

                await _responseSender.SendData(jsonData, cancellationToken);

                Console.WriteLine($"[Executor] Đã gửi thông tin của {tempList.Count} ứng dụng.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Executor] Lỗi khi gửi dữ liệu: {ex.Message}");
            }
        }

        private void ExecuteStartApp(object data)
        {
            Console.WriteLine("[Executor] Thực thi: Chạy ứng dụng");

            if (data is JsonElement element)
            {
                try
                {
                    // Lấy đường dẫn
                    string? executablePath = element.GetProperty("Path").GetString();
                    string arguments = "";

                    // Lấy tham số
                    if (element.TryGetProperty("Arguments", out JsonElement argElement))
                    {
                        arguments = argElement.GetString() ?? "";
                    }

                    if (!string.IsNullOrEmpty(executablePath))
                    {
                        var appManager = new ApplicationManager();
                        bool success = appManager.StartApplication(executablePath, arguments);

                        if (success)
                            Console.WriteLine($"[Executor] Đã khởi chạy thành công: {executablePath}");
                        else
                            Console.WriteLine($"[Executor] Khởi chạy thất bại: {executablePath}");
                    }
                }
                catch (KeyNotFoundException)
                {
                    Console.WriteLine("[Executor] Lỗi: Thiếu thuộc tính 'Path' trong dữ liệu.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Executor] Lỗi xử lý dữ liệu: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine("[Executor] Lỗi: Dữ liệu đầu vào không phải là JsonElement hợp lệ");
            }
        }

        private void ExecuteStopApp(object data)
        {
            Console.WriteLine("[Executor] Thực thi: Đóng ứng dụng");

            if (data is JsonElement element)
            {
                try
                {
                    
                    //Lấy ID ứng dụng
                    int ID = element.GetProperty("ID").GetInt32();
                    var appManager = new ApplicationManager();
                    bool success = appManager.StopApplication(ID);

                    if (success)
                            Console.WriteLine($"[Executor] Đã dừng thành công");
                    else
                            Console.WriteLine($"[Executor] Dừng thất bại");
                }
                catch (KeyNotFoundException)
                {
                    Console.WriteLine("[Executor] Lỗi: Thiếu thuộc tính 'ID' trong dữ liệu.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Executor] Lỗi xử lý dữ liệu: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine("[Executor] Lỗi: Dữ liệu đầu vào không phải là JsonElement hợp lệ");
            }
        }

        private void ExecuteStartTask()
        {
            Console.WriteLine("[Executor] Thực thi: Chạy tác vụ");
        }

        private void ExecuteWebcam()
        {
            Console.WriteLine("[Executor] Thực thi: Đã bật Webcam");
        }
    }
}

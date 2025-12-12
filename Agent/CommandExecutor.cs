using Shared;

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
                case AgentCommandType.StartApp:
                    ExecuteStartApp();
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
            // System.Diagnostics.Process.Start("shutdown", "/s /t 0");
        }  

        private void ExecuteRestart()
        {
            Console.WriteLine("[Executor] Thực thi: Bật lại máy");
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

        private void ExecuteStartApp()
        {
            Console.WriteLine("[Executor] Thực thi: Mở ...");
        }

        private void ExecuteStartTask()
        {
            Console.WriteLine("[Executor] Thực thi: Mở ...");
        }

        private void ExecuteWebcam()
        {
            Console.WriteLine("[Executor] Thực thi: Đã bật Webcam");
        }
    }
}

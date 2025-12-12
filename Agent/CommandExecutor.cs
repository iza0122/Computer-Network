using Shared;

namespace Agent
{
    using Shared;

    public class CommandExecutor
    {
        public void Execute(RemoteCommand command)
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
                    ExecuteCapture();
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

        private void ExecuteCapture()
        {
            Console.WriteLine("[Executor] Thực thi: Chụp màn hình");
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

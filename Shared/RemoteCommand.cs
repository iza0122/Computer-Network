using System.Net.WebSockets;
using System.Text.Json;

namespace Shared
{
    //Cấu trúc của gói tin (JSON) 
    public class RemoteCommand
    {
        //Tên lệnh
        public string? Name { get; set; }
        //Dữ liệu kèm theo (Optional)
        public object? Data { get; set; }
        //Dịch dữ liệu nếu nó là 1 chuỗi json (Phân biệt json/binary)
        public T? GetData<T>()
        {
            if (Data is JsonElement element)
            {
                return element.Deserialize<T>(CommandJson.Options);
            }
            if (Data is T tData) return tData;
            return default;
        }
    }

    public static class CommandJson
    {
        //Mode không phân biệt hoa thường
        public static readonly JsonSerializerOptions Options =
            new()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };
        //Chuyển lệnh thành chuỗi Json
        public static string ToJson(RemoteCommand command)
        {
            return JsonSerializer.Serialize(command, Options);
        }
        //Dịch chuỗi json thành lệnh
        public static RemoteCommand? FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;

            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

            return JsonSerializer.Deserialize<RemoteCommand>(json, options);
            }
            catch (JsonException ex)
            {
                // Ghi log nhẹ nhàng thay vì throw
                Console.WriteLine($"[JSON ERROR] Dữ liệu không phải JSON hợp lệ: {ex.Message}");
                return null;
            }
        }
    }

    public enum AgentCommandType
    {
        None = 0,

        // ===== SYSTEM =====
        Exit,           // Agent tự thoát
        Shutdown,       // Tắt máy
        Restart,        // Khởi động lại

        // ===== SCREEN =====
        Screenshot,     // Chụp màn hình

        // ===== APPLICATION =====
        ListRunningApp,
        ListInstalledApp,        // App đã cài
        StartApp,       // Mở app theo Id
        StopApp,        // (optional – nếu có)

        // ===== TASK / PROCESS =====
        ListTask,       // Process đang chạy
        StartTask,      // (hiếm dùng)
        StopTask,       // Kill process theo PID

        // ===== WEBCAM =====
        WebcamList,     // Liệt kê webcam
        WebcamRecord,   // Quay webcam

        // ===== INPUT =====
        Keylogger
    }


    public interface IResponseSender
    {
        Task SendData(MessageType header, byte[] data, CancellationToken ct);
        Task SendText(string message, CancellationToken ct);
        Task SendStatus(bool isSuccess, string message, CancellationToken ct);

    }

    public enum MessageType : byte
    {
        Image = 0,
        Text = 1,
        Video = 2,
        Json = 3,
        Status = 4
    }
}

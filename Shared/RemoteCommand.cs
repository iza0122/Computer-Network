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
        public static RemoteCommand FromJson(string json)
        {
            return JsonSerializer.Deserialize<RemoteCommand>(json, Options)
        ?? throw new InvalidOperationException("Chuỗi json không hợp lệ!\n");
        }
    }

    public enum AgentCommandType
    {
       None,
       Exit,
       Shutdown,
       Restart,
       Capture,
       StartApp,
       ListRunningApp,
       ListInstalledApp,
       StopApp,
       ListRunningTask,
       StartTask,
       StopTask,
       Webcam
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

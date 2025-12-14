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
       StartTask,
       Webcam
    }

    public interface IResponseSender
    {
        Task SendData(object data, CancellationToken cancellationToken);
    }
}

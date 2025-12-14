
using Agent;
using Shared;
using System.Text;

Console.OutputEncoding = Encoding.UTF8;

string URL = "ws://localhost:5000/agent";

var cts = new CancellationTokenSource();
var agent = new Agent.AgentNetworkClient(URL, cts);
var executor = new CommandExecutor(agent);
agent.SetupExecutor(executor);

// Đăng ký xử lý sự kiện Ctrl+C
Console.CancelKeyPress += (s, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

Task task = agent.ConnectAndListenAsync();

// Program.cs - Sửa lỗi bằng cách chỉ giữ lại OperationCanceledException

try
{
    // Chờ vô hạn cho đến khi cts.Cancel() được gọi
    await Task.Delay(Timeout.InfiniteTimeSpan, cts.Token);
}
catch (OperationCanceledException) // CHỈ CẦN DÒNG NÀY LÀ ĐỦ
{
    Console.WriteLine("[MAIN] Nhận tín hiệu hủy từ Console. Đang tiến hành đóng Server...");
}
await task;
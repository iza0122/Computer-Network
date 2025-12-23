using Agent;
using Shared;
using System.Text;

Console.OutputEncoding = Encoding.UTF8;

// --- TỰ ĐỘNG TÌM IP SERVER QUA UDP ---
string serverIp = await DiscoveryClient.FindServerIP();

if (string.IsNullOrEmpty(serverIp))
{
    Console.WriteLine("[ERROR] Không tìm thấy Server trong mạng LAN. Vui lòng kiểm tra lại Firewall!");
    // Bạn có thể chọn dừng lại hoặc dùng IP mặc định để thử lại
    // serverIp = "127.0.0.1"; 
    return;
}

string URL = $"ws://{serverIp}:5000/agent";
// -------------------------------------

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

try
{
    // Chờ vô hạn cho đến khi cts.Cancel() được gọi
    await Task.Delay(Timeout.InfiniteTimeSpan, cts.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine("[MAIN] Nhận tín hiệu hủy từ Console. Đang đóng Agent...");
}

await task;
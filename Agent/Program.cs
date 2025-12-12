
using Agent;
using Shared;
using System.Text;

Console.OutputEncoding = Encoding.UTF8;

string URL = "ws://localhost:5000/ws";

var cts = new CancellationTokenSource();
var agent = new Agent.AgentNetworkClient(URL);
var executor = new CommandExecutor(agent);
agent.SetupExecutor(executor);

// Đăng ký xử lý sự kiện Ctrl+C
Console.CancelKeyPress += (s, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

Task task = agent.ConnectAndListenAsync(cts.Token);

// Chờ vô hạn cho đến khi cts.Cancel() được gọi
await Task.Delay(Timeout.InfiniteTimeSpan, cts.Token);

await task;

Console.WriteLine("Agent đã tắt an toàn.");
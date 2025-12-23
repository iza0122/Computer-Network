using Shared;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Agent
{
    public static class DiscoveryClient
    {
        public static async Task<string> FindServerIP(int port = 8888, int timeoutMs = 5000)
        {
            using var udpClient = new UdpClient();
            udpClient.EnableBroadcast = true;

            // Cấu hình Timeout để không chờ vô hạn nếu không thấy Server
            udpClient.Client.ReceiveTimeout = timeoutMs;

            byte[] requestData = Encoding.UTF8.GetBytes("WHERE_IS_AMONGUS_SERVER");
            var endPoint = new IPEndPoint(IPAddress.Broadcast, port);

            Console.WriteLine($"[UDP] Đang quét Server trong mạng LAN (Port {port})...");

            try
            {
                // Gửi tín hiệu Broadcast
                await udpClient.SendAsync(requestData, requestData.Length, endPoint);

                // Chờ phản hồi
                var receiveTask = udpClient.ReceiveAsync();
                if (await Task.WhenAny(receiveTask, Task.Delay(timeoutMs)) == receiveTask)
                {
                    var result = await receiveTask;
                    string response = Encoding.UTF8.GetString(result.Buffer);

                    if (response == "I_AM_SERVER")
                    {
                        string serverIp = result.RemoteEndPoint.Address.ToString();
                        Console.WriteLine($"[UDP] Đã tìm thấy Server tại IP: {serverIp}");
                        return serverIp;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UDP Error] {ex.Message}");
            }

            return null;
        }
    }
    public class AgentNetworkClient : IResponseSender
    {
        private readonly Uri _serverUri;
        private ClientWebSocket? _client;
        private CommandExecutor? _executor;

        private readonly CancellationTokenSource _appCts;
        private readonly SemaphoreSlim _sendLock = new(1, 1);

        public AgentNetworkClient(string serverUrl, CancellationTokenSource appCts)
        {
            _serverUri = new Uri(
                serverUrl
                    .Replace("http://", "ws://")
                    .Replace("https://", "wss://")
            );

            _appCts = appCts;
        }

        public void SetupExecutor(CommandExecutor executor)
        {
            _executor = executor;
        }

        public async Task ConnectAndListenAsync()
        {
            CancellationToken cancellationToken = _appCts.Token;

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    Console.WriteLine("[AGENT] Đang cố gắng kết nối...");

                    _client = new ClientWebSocket();
                    await _client.ConnectAsync(_serverUri, cancellationToken);

                    Console.WriteLine($"[AGENT] Đã kết nối tới {_serverUri}");

                    await WarmUpNetworkBuffer();

                    byte[] buffer = new byte[4096];

                    while (_client.State == WebSocketState.Open &&
                           !cancellationToken.IsCancellationRequested)
                    {
                        WebSocketReceiveResult result;
                        using var ms = new MemoryStream();

                        try
                        {
                            do
                            {
                                result = await _client.ReceiveAsync(
                                    new ArraySegment<byte>(buffer),
                                    cancellationToken
                                );

                                if (result.MessageType == WebSocketMessageType.Close)
                                {
                                    if (result.CloseStatus == WebSocketCloseStatus.PolicyViolation)
                                    {
                                        Console.WriteLine("[AGENT] Server đã kết nối tới 1 Agent r");
                                        await CloseConnectionAsync();
                                        Environment.Exit(0);
                                        return;
                                    }
                                    Console.WriteLine("[AGENT] Server yêu cầu đóng kết nối.");
                                    await CloseConnectionAsync();
                                    break;
                                }

                                ms.Write(buffer, 0, result.Count);
                            }
                            while (!result.EndOfMessage);
                        }
                        catch (OperationCanceledException)
                        {
                            break;
                        }
                        catch (WebSocketException)
                        {
                            break;
                        }

                        if (result.MessageType != WebSocketMessageType.Text)
                            continue;

                        if (_executor == null)
                        {
                            Console.WriteLine("[AGENT] Executor chưa được khởi tạo.");
                            continue;
                        }

                        string message = Encoding.UTF8.GetString(ms.ToArray());

                        try
                        {
                            RemoteCommand command = CommandJson.FromJson(message);
                            Console.WriteLine($"[AGENT] Nhận lệnh: {command.Name}");

                            await _executor.Execute(command, cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[AGENT] Lỗi xử lý lệnh: {ex.Message}");
                        }
                    }

                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await CloseConnectionAsync();
                        Console.WriteLine("[AGENT] Mất kết nối. Thử lại sau 5 giây...");
                        await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("[AGENT] Nhận tín hiệu hủy. Dừng agent.");
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[AGENT ERROR] {ex.Message}");
                    await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                }
                finally
                {
                    await CloseConnectionAsync();
                }
            }
        }

        // ================== SEND ==================

        public async Task SendData(MessageType type, byte[] data, CancellationToken ct)
        {
            if (_client == null || _client.State != WebSocketState.Open)
            {
                Console.WriteLine($"[AGENT] Không thể gửi {type}: Socket không sẵn sàng.");
                return;
            }

            data ??= Array.Empty<byte>();

            byte[] payload = new byte[data.Length + 1];
            payload[0] = (byte)type;
            Buffer.BlockCopy(data, 0, payload, 1, data.Length);

            await _sendLock.WaitAsync(ct);
            try
            {
                await _client.SendAsync(
                    new ArraySegment<byte>(payload),
                    WebSocketMessageType.Binary,
                    true,
                    ct
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AGENT SEND ERROR] {type}: {ex.Message}");
            }
            finally
            {
                _sendLock.Release();
            }
        }

        public Task SendText(string message, CancellationToken ct)
        {
            byte[] data = Encoding.UTF8.GetBytes(message);
            return SendData(MessageType.Text, data, ct);
        }

        public Task SendStatus(bool success, string message, CancellationToken ct)
        {
            byte[] msg = Encoding.UTF8.GetBytes(message);
            byte[] buffer = new byte[msg.Length + 1];
            buffer[0] = (byte)(success ? 1 : 0);
            Buffer.BlockCopy(msg, 0, buffer, 1, msg.Length);

            return SendData(MessageType.Status, buffer, ct);
        }

        // ================== UTIL ==================

        private async Task WarmUpNetworkBuffer()
        {
            if (_client?.State == WebSocketState.Open)
            {
                try
                {
                    await SendText("READY", CancellationToken.None);
                }
                catch { }
            }
        }

        public void RequestShutdown()
        {
            if (!_appCts.IsCancellationRequested)
            {
                Console.WriteLine("[AGENT] Yêu cầu shutdown.");
                _appCts.Cancel();
            }
        }

        private async Task CloseConnectionAsync()
        {
            if (_client == null)
                return;

            try
            {
                if (_client.State == WebSocketState.Open ||
                    _client.State == WebSocketState.CloseSent)
                {
                    await _client.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Agent shutting down",
                        CancellationToken.None
                    );
                }
            }
            catch (WebSocketException ex)
            {
                Console.WriteLine($"[AGENT CLOSE ERROR] {ex.Message}");
            }
            finally
            {
                _client.Dispose();
                _client = null;
            }
        }
    }
}

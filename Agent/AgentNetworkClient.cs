using Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Agent
{
    public class AgentNetworkClient : IResponseSender
    {
        private readonly Uri _serverUri;
        private ClientWebSocket? _client;
        private CommandExecutor? _executor;

        private readonly CancellationTokenSource _appCts;
        public AgentNetworkClient(string serverUrl, CancellationTokenSource appCts)
        {
            _serverUri = new Uri(serverUrl.Replace("http://", "ws://").Replace("https://", "wss://"));
            _appCts = appCts;
        }

        public void SetupExecutor(CommandExecutor executor)
        {
            _executor = executor;
        }

        public async Task ConnectAndListenAsync()
        {
            CancellationToken cancellationToken = _appCts.Token;

            // Vòng lặp chính: Đảm bảo Agent luôn cố gắng kết nối lại nếu bị rớt mạng
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    Console.WriteLine("[AGENT] Đang cố gắng kết nối...");

                    //Khởi tạo Client WebSocket
                    _client = new ClientWebSocket();

                    //Kết nối tới Server (Sử dụng token nội bộ)
                    await _client.ConnectAsync(_serverUri, cancellationToken);

                    Console.WriteLine($"[AGENT] Đã kết nối thành công tới {_serverUri}");

                    //Làm ấm bộ đệm mạng
                    await WarmUpNetworkBuffer();

                    // Khởi tạo buffer nhận dữ liệu
                    byte[] buffer = new byte[1024 * 4];

                    // 4. Vòng lặp lắng nghe chính
                    while (_client.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
                    {
                        WebSocketReceiveResult result;

                        try
                        {
                            // Chờ nhận dữ liệu từ Server
                            result = await _client.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                        }
                        catch (OperationCanceledException)
                        {
                            break;
                        }
                        catch (WebSocketException)
                        {
                            break;
                        }

                        // Xử lý loại tin nhắn đóng
                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            Console.WriteLine($"[AGENT] Server đã gửi lệnh đóng kết nối: {result.CloseStatusDescription}");
                            await CloseConnectionAsync();
                            break;
                        }

                        // Xử lý tin nhắn văn bản (Lệnh)
                        if (result.MessageType == WebSocketMessageType.Text)
                        {
                            string receivedMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);

                            try
                            {
                                RemoteCommand command = CommandJson.FromJson(receivedMessage);
                                Console.WriteLine($"[AGENT] Nhận lệnh từ Server: {command.Name}");

                                await _executor.Execute(command, cancellationToken);
                            }
                            catch (InvalidOperationException ex)
                            {
                                Console.WriteLine($"[AGENT] Lỗi dịch lệnh JSON: {ex.Message}");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[AGENT] Lỗi xử lý lệnh: {ex.Message}");
                            }
                        }
                    }

                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await CloseConnectionAsync();

                        Console.WriteLine("[AGENT] Kết nối bị mất. Thử lại sau 5 giây...");
                        await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("[AGENT] Nhận tín hiệu hủy. Thoát vòng lặp chính.");
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Lỗi kết nối: {ex.Message}. Thử lại sau 5 giây...");
                    await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                }
                finally
                {
                    if (_client != null)
                    {
                        await CloseConnectionAsync();
                    }
                }
            }
        }

        public async Task SendData(MessageType type, byte[] data, CancellationToken cancellationToken)
        {
            //Kiểm tra trạng thái kết nối
            if (_client == null || _client.State != WebSocketState.Open)
            {
                Console.WriteLine($"[AGENT] Gửi thất bại ({type}): Socket không sẵn sàng.");
                return;
            }

            //Kiểm tra dữ liệu rỗng
            data ??= Array.Empty<byte>();

            try
            {
                //Đóng gói dữ liệu
                byte[] payload = new byte[data.Length + 1];
                payload[0] = (byte)type;
                Buffer.BlockCopy(data, 0, payload, 1, data.Length);

                //Gửi dữ liệu
                await _client.SendAsync(
                    new ArraySegment<byte>(payload),
                    WebSocketMessageType.Binary,
                    endOfMessage: true,
                    cancellationToken: cancellationToken
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AGENT SEND ERROR] {type}: {ex.Message}");
            }
        }

        public async Task SendText(string message, CancellationToken ct)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(message);
            await SendData(MessageType.Text, buffer, ct);
        }

        public async Task SendStatus(bool isSuccess, string message, CancellationToken ct)
        {
            byte[] msgBytes = Encoding.UTF8.GetBytes(message);
            byte[] buffer = new byte[msgBytes.Length + 1];
            buffer[0] = (byte)(isSuccess ? 1 : 0);
            Buffer.BlockCopy(msgBytes, 0, buffer, 1, msgBytes.Length);
            await SendData(MessageType.Status, buffer, ct);
        }

        public async Task WarmUpNetworkBuffer()
        {
            // Gửi một gói tin nhỏ để khởi tạo và làm ấm bộ đệm mạng
            if (_client.State == WebSocketState.Open)
            {
                try
                {
                    byte[] data = Encoding.UTF8.GetBytes("READY");
                    await SendData(MessageType.Text, data, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Warm-up Error] {ex.Message}");
                }
            }
        }
        public void RequestShutdown()
        {
            if (!_appCts.IsCancellationRequested)
            {
                Console.WriteLine("[AGENT] Nhận lệnh Shutdown");
                _appCts.Cancel(); // Kích hoạt lệnh hủy toàn ứng dụng
            }
        }
        private async Task CloseConnectionAsync()
        {
            if (_client != null &&
                (_client.State == WebSocketState.Open || _client.State == WebSocketState.CloseSent))
            {
                try
                {
                    // Đóng kết nối WebSocket
                    await _client.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Agent shutting down",
                        CancellationToken.None
                    );
                    Console.WriteLine("[AGENT] Kết nối WebSocket đã đóng an toàn.");
                }
                catch (WebSocketException ex)
                {
                    Console.WriteLine($"[AGENT CLOSE ERROR] Lỗi khi đóng kết nối: {ex.Message}");
                }
                finally
                {
                    _client.Dispose();
                    _client = null;
                }
            }
            else if (_client != null && _client.State != WebSocketState.None)
            {
                // Nếu ở trạng thái đóng/lỗi, vẫn giải phóng tài nguyên
                _client.Dispose();
                _client = null;
            }
        }
    }

}

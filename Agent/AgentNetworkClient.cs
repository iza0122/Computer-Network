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
            // Lấy Token Hủy từ field đã lưu trữ (để kiểm soát vòng đời)
            CancellationToken cancellationToken = _appCts.Token;

            // Vòng lặp chính: Đảm bảo Agent luôn cố gắng kết nối lại nếu bị rớt mạng
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    Console.WriteLine("[AGENT] Đang cố gắng kết nối...");

                    // 1. Khởi tạo Client WebSocket
                    _client = new ClientWebSocket();

                    // 2. Kết nối tới Server (Sử dụng token nội bộ)
                    await _client.ConnectAsync(_serverUri, cancellationToken);

                    Console.WriteLine($"[AGENT] Đã kết nối thành công tới {_serverUri}");

                    // 3. Làm ấm bộ đệm mạng
                    await WarmUpNetworkBuffer();

                    // Khởi tạo buffer nhận dữ liệu
                    byte[] buffer = new byte[1024 * 4];

                    // 4. Vòng lặp lắng nghe chính
                    while (_client.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
                    {
                        WebSocketReceiveResult result;

                        try
                        {
                            // Chờ nhận dữ liệu từ Server (Sử dụng token nội bộ)
                            result = await _client.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                        }
                        catch (OperationCanceledException)
                        {
                            // Token bị hủy, thoát vòng lặp lắng nghe
                            break;
                        }
                        catch (WebSocketException)
                        {
                            // Lỗi mạng, thoát vòng lặp
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

                                // Thực thi lệnh (Sử dụng token nội bộ)
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
                    } // Kết thúc vòng lặp lắng nghe

                    // 5. Nếu vòng lặp thoát mà KHÔNG phải do yêu cầu hủy ứng dụng (Agent bị rớt mạng)
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await CloseConnectionAsync();

                        Console.WriteLine("[AGENT] Kết nối bị mất. Thử lại sau 5 giây...");
                        // Dùng Task.Delay với token để có thể hủy trong lúc chờ
                        await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Bắt ngoại lệ khi vòng lặp ngoài cùng bị hủy
                    Console.WriteLine("[AGENT] Nhận tín hiệu hủy. Thoát vòng lặp chính.");
                    break;
                }
                catch (Exception ex)
                {
                    // Lỗi kết nối ban đầu (VD: Server chưa chạy)
                    Console.WriteLine($"Lỗi kết nối: {ex.Message}. Thử lại sau 5 giây...");
                    // Dùng Task.Delay với token
                    await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                }
                finally
                {
                    // 6. Đảm bảo giải phóng tài nguyên sau cùng
                    if (_client != null)
                    {
                        await CloseConnectionAsync();
                    }
                }
            } // Kết thúc vòng lặp while (!cancellationToken.IsCancellationRequested)
        }

        public async Task SendData(object data, CancellationToken cancellation)
        {
            if (_client == null || _client.State != WebSocketState.Open)
            {
                Console.WriteLine("[AGENT] Không thể gửi phản hồi: Mất kết nối.");
                return;
            }

            byte[] buffer;
            WebSocketMessageType messageType;

            if (data is byte[] binarydata) //Nếu vật cần gửi là dữ liệu nhị phân VD: Hình, video, ...
            {
                messageType = WebSocketMessageType.Binary;
                buffer = binarydata;
            }
            else //Nếu vật cần gửi là text
            {
                string json = CommandJson.ToJson(new RemoteCommand { Name = "Response", Data = data });
                buffer = Encoding.UTF8.GetBytes(json);
                messageType = WebSocketMessageType.Text;
            }

            await _client.SendAsync(new ArraySegment<byte>(buffer), messageType, true, cancellation);
        }
        public async Task WarmUpNetworkBuffer()
        {
            // Gửi một gói tin nhỏ để khởi tạo và làm ấm bộ đệm mạng
            byte[] data = System.Text.Encoding.UTF8.GetBytes("READY");
            if (_client.State == WebSocketState.Open)
            {
                try
                {
                    await _client.SendAsync(
                        new ArraySegment<byte>(data),
                        System.Net.WebSockets.WebSocketMessageType.Text,
                        endOfMessage: true,
                        CancellationToken.None
                    );
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
                Console.WriteLine("[AGENT] Nhận lệnh Shutdown. Kích hoạt hủy ứng dụng...");
                _appCts.Cancel(); // Kích hoạt lệnh hủy toàn ứng dụng
            }
        }

        // AgentNetworkClient.cs (Thêm vào lớp)

        private async Task CloseConnectionAsync()
        {
            if (_client != null &&
                (_client.State == WebSocketState.Open || _client.State == WebSocketState.CloseSent))
            {
                try
                {
                    // Đóng kết nối WebSocket một cách duyên dáng (Dùng CancellationToken.None để đảm bảo nó hoàn thành)
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
                    // Đảm bảo đối tượng được giải phóng
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

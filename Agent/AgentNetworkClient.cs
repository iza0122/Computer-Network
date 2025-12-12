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
        public AgentNetworkClient(string serverUrl)
        {
            // Chuyển URL string sang đối tượng Uri, thay ws:// bằng wss:// nếu có bảo mật
            _serverUri = new Uri(serverUrl.Replace("http://", "ws://").Replace("https://", "wss://"));
        }

        public void SetupExecutor(CommandExecutor executor)
        {
            _executor = executor;
        }

        public async Task ConnectAndListenAsync(CancellationToken cancellationToken)
        {
            // Vòng lặp chính: Đảm bảo Agent luôn cố gắng kết nối lại nếu bị rớt mạng
            while (!cancellationToken.IsCancellationRequested)
            {
                {
                    // Cố gắng kết nối
                    try
                    {
                        _client = new ClientWebSocket();
                        await _client.ConnectAsync(_serverUri, cancellationToken);
                        Console.WriteLine($"[AGENT] Đã kết nối thành công tới {_serverUri}");

                        byte[] buffer = new byte[1024 * 4];

                        if (_client == null) throw new Exception("Kết nối rỗng");
                        while (_client.State == WebSocketState.Open)
                        {
                            var result = await _client.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

                            if (result.MessageType == WebSocketMessageType.Close)
                            {
                                await _client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Được yêu cầu đóng kết nối", cancellationToken);
                                _client = null;
                            }

                            if (result.MessageType == WebSocketMessageType.Text)
                            {
                                string receivedMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);
                                //Gửi lệnh cho executor
                                try
                                {
                                    RemoteCommand command = CommandJson.FromJson(receivedMessage);
                                    await _executor.Execute(command, cancellationToken);
                                }
                                catch (InvalidOperationException ex)
                                {
                                    Console.WriteLine($"[AGENT] Lỗi dịch lệnh JSON: Chuỗi không hợp lệ: {ex.Message}");
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"[AGENT] Lỗi dịch lệnh JSON: {ex.Message}");
                                }
                                //
                                Console.WriteLine($"[AGENT] Nhận lệnh từ Server: {receivedMessage}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Nếu lỗi, log và chờ 5 giây trước khi thử lại
                        Console.WriteLine($"Lỗi kết nối: {ex.Message}. Thử lại sau 5 giây...");
                        await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                    }
                    //Nhận dữ liệu

                }
            }
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
    }
}

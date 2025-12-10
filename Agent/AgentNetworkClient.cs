using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace Agent
{
    public class AgentNetworkClient
    {
        private readonly Uri _serverUri;
        private ClientWebSocket? _client;

        public AgentNetworkClient(string serverUrl)
        {
            // Chuyển URL string sang đối tượng Uri, thay ws:// bằng wss:// nếu có bảo mật
            _serverUri = new Uri(serverUrl.Replace("http://", "ws://").Replace("https://", "wss://"));
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
                                //Gửi lệnh này cho executor...
                                //
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
    }
}

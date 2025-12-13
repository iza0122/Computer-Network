using System.Net.WebSockets;
using System.Text;
using Shared;

namespace RemoteComputerController.Core
{
    public class Server
    {
        private WebSocket? _currentAgentSocket = null;
        private WebSocket? _webUISocket = null;

        public async Task ConnectAgent(WebSocket newSocket)
        {
            if (_currentAgentSocket != null && _currentAgentSocket.State == WebSocketState.Open)
            {
                throw new InvalidOperationException("Đã mở kết nối rồi!");
            }
            else
            {
                if (newSocket == null) throw new InvalidOperationException("Kết nối không hợp lệ");
                _currentAgentSocket = newSocket;
                Console.WriteLine($"[SERVER] Đã thiết lập kết nối tới agent");
                byte[] buffer = new byte[1024 * 4];
                while (_currentAgentSocket.State == WebSocketState.Open)
                {
                    WebSocketReceiveResult result = await _currentAgentSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    using var ms = new MemoryStream();

                    do // Vòng lặp này xử lý các mảnh của một tin nhắn duy nhất
                    {
                        result = await _currentAgentSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            await _currentAgentSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Agent yêu cầu đóng kết nối", CancellationToken.None);
                            _currentAgentSocket = null;
                            return;
                        }

                        if (result.Count > 0)
                        {
                            // Ghi mảnh dữ liệu vừa nhận được vào MemoryStream
                            ms.Write(buffer, 0, result.Count);
                        }

                    } while (!result.EndOfMessage); // Lặp cho đến khi nhận được mảnh cuối cùng

                    switch (result.MessageType)
                    {
                        case WebSocketMessageType.Text:
                            byte[] textBuffer = ms.ToArray();
                            string jsonResponse = Encoding.UTF8.GetString(textBuffer);
                            try
                            {
                                RemoteCommand response = CommandJson.FromJson(jsonResponse);
                                Console.WriteLine($"\n[AGENT RESPONSE] {response.Data}");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[SERVER ERROR] Lỗi dịch JSON phản hồi: {ex.Message}");
                            }
                            break;


                        //Dữ liệu nhị phân
                        case WebSocketMessageType.Binary:
                            byte[] imageBuffer = ms.ToArray();
                            string fileName = $"screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                            string savePath = Path.Combine("AgentData", fileName);

                            try
                            {
                                // 2. Đảm bảo thư mục tồn tại
                                Directory.CreateDirectory("AgentData");

                                // 3. Ghi mảng byte ra file
                                await File.WriteAllBytesAsync(savePath, imageBuffer);

                                Console.WriteLine($"\n[SERVER] Đã nhận và lưu ảnh: {fileName} ({imageBuffer.Length} bytes)");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[SERVER ERROR] Lỗi khi lưu ảnh: {ex.Message}");
                            }

                            break;
                    }
                }
            }
        }

        public async Task SendCommandAsync(RemoteCommand command)
        {
            try
            {
                if (_currentAgentSocket == null || _currentAgentSocket.State != WebSocketState.Open)
                    throw new Exception("[Server] Agent socket đã đóng — gửi thất bại");

                string json = CommandJson.ToJson(command);
                byte[] buffer = Encoding.UTF8.GetBytes(json);

                await _currentAgentSocket.SendAsync(
                    new ArraySegment<byte>(buffer),
                    WebSocketMessageType.Text,
                    true,
                    CancellationToken.None
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine("[SERVER SEND ERROR] " + ex.Message);

                // Nếu agent đã đóng → dọn socket
                if (_currentAgentSocket?.State != WebSocketState.Open)
                {
                    Console.WriteLine("[SERVER] Agent socket không còn hoạt động.");
                    _currentAgentSocket = null;
                }
            }
        }


        // Server.cs

        public async Task ExecuteAgentCommand(string commandName)
        {
            // 1. Kiểm tra kết nối Agent
            if (_currentAgentSocket != null && _currentAgentSocket.State == WebSocketState.Open)
            {
                try
                {
                    var command = new RemoteCommand { Name = commandName };
                    // Lỗi phải xảy ra ở đây
                    await SendCommandAsync(command);

                    Console.WriteLine($"[SERVER] Đã gửi lệnh: {commandName}");
                }
                catch (Exception ex)
                {
                    // Nếu có lỗi, chúng ta phải thấy log này!
                    Console.WriteLine($"[SERVER ERROR] Lỗi khi gửi lệnh {commandName} đến Agent: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine("[SERVER WARNING] Không có Agent nào đang kết nối.");
            }
        }

        private async Task ListenForControlCommandsAsync()
        {
            try // <--- BẮT ĐẦU khối try
            {
                byte[] buffer = new byte[1024 * 4];
                if (_webUISocket == null) throw new Exception("Socket điều khiển chưa được gán.");

                while (_webUISocket.State == WebSocketState.Open)
                {
                    WebSocketReceiveResult result = await _webUISocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        break; // Thoát vòng lặp while, đi đến khối catch WebSocketException
                    }
                    Console.WriteLine(buffer.ToString());
                    string commandString = Encoding.UTF8.GetString(buffer, 0, result.Count).Trim();
                    Console.WriteLine(commandString);

                    if (result.Count == 0) continue;

                    if (commandString.Length > 0)
                    {
                        Console.WriteLine($"[CONTROL] Đã nhận lệnh từ WebUI: {commandString}"); // <-- Dòng kiểm tra
                        await ExecuteAgentCommand(commandString);
                    }
                }
            }
            catch (WebSocketException)
            {
                Console.WriteLine("[SERVER] WebUI đóng kết nối.");
            }
            catch (IOException ioEx) when (ioEx.Message.Contains("send loop completed gracefully"))
            {
                Console.WriteLine("[SERVER] WebUI đóng kết nối (send loop kết thúc).");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SERVER CRASH ERROR] Lỗi không lường trước: {ex.Message}");
            }

        }

        public async Task ConnectWebUI(WebSocket newSocket)
        {
            if (_webUISocket != null && _webUISocket.State == WebSocketState.Open)
            {
                throw new InvalidOperationException("Đã mở kết nối rồi!");
            }
            else
            {
                if (newSocket == null) throw new InvalidOperationException("Kết nối không hợp lệ");
                _webUISocket = newSocket;
                Console.WriteLine("[SERVER] Đã kết nối thành công tới WebUI");
                await ListenForControlCommandsAsync();
            }
        }
                    
    }
}

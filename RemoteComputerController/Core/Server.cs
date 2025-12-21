using Microsoft.AspNetCore.Hosting.Server;
using Shared;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;

namespace RemoteComputerController.Core
{
    public class Server
    {
        private WebSocket? _agentSocket;
        private WebSocket? _webUISocket;

        /* =========================
         * AGENT CONNECTION
         * ========================= */

        public async Task ConnectAgent(WebSocket socket)
        {
            if (_agentSocket != null && _agentSocket.State == WebSocketState.Open)
            {
                Console.WriteLine("Agent đã kết nối rồi.");
                try
                {
                    await socket.CloseAsync(WebSocketCloseStatus.PolicyViolation, "Another agent is already connected", CancellationToken.None);
                }
                catch (Exception)
                {
                    socket.Abort();
                }
                finally
                {
                    socket.Dispose();
                }
                return;
            }

            _agentSocket = socket;
            Console.WriteLine("[SERVER] Agent connected");

            byte[] buffer = new byte[4096];

            try
            {
                while (_agentSocket.State == WebSocketState.Open)
                {
                    using var ms = new MemoryStream();
                    WebSocketReceiveResult result;

                    // Nhận đầy đủ 1 message
                    do
                    {
                        result = await _agentSocket.ReceiveAsync(
                            new ArraySegment<byte>(buffer),
                            CancellationToken.None
                        );

                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            await CloseAgentAsync();
                            return;
                        }

                        ms.Write(buffer, 0, result.Count);

                    } while (!result.EndOfMessage);

                    byte[] message = ms.ToArray();
                    if (message.Length == 0) continue;

                    // ===== AGENT PROTOCOL ===== (Để test)
                    MessageType type = (MessageType)message[0];
                    byte[] payload = message.Skip(1).ToArray();

                    await HandleAgentMessageAsync(type, payload);
                }
            }
            catch (WebSocketException)
            {
                Console.WriteLine("[SERVER] Agent mất kết nối");
            }
            finally
            {
                await CloseAgentAsync();
            }
        }

        /* =========================
         * HANDLE AGENT MESSAGE
         * ========================= */

        private async Task HandleAgentMessageAsync(MessageType type, byte[] payload)
        {
            byte[] fullMessage = new byte[payload.Length + 1];
            fullMessage[0] = (byte)type;
            Buffer.BlockCopy(payload, 0, fullMessage, 1, payload.Length);
            //switch (type)
            //{
            //    case MessageType.Text:
            //        {
            //            string text = Encoding.UTF8.GetString(payload);
            //            Console.WriteLine("[AGENT TEXT] " + text);

            //            // (Optional) forward cho WebUI
            //            //await ForwardToWebUIAsync(text);
            //            break;
            //        }

            //    case MessageType.Status:
            //        {
            //            bool success = payload.Length > 0 && payload[0] == 1;
            //            string msg = Encoding.UTF8.GetString(payload, 1, payload.Length - 1);
            //            Console.WriteLine($"[AGENT STATUS] {(success ? "OK" : "FAIL")} - {msg}");

            //            //await ForwardToWebUIAsync(msg);
            //            break;
            //        }

            //    case MessageType.Image:
            //        await SaveBinaryAsync(payload, "png", "screenshot");
            //        //await ForwardToWebUIAsync(payload);
            //        break;

            //    case MessageType.Video:
            //        await SaveBinaryAsync(payload, "mp4", "webcam");
            //        //await ForwardToWebUIAsync(payload);
            //        break;

            //    default:
            //        Console.WriteLine($"[SERVER] Unknown MessageType: {type}");
            //        break;
            //}
            await ForwardToWebUIAsync(fullMessage);
        }

        private async Task SaveBinaryAsync(byte[] data, string ext, string prefix)
        {
            Directory.CreateDirectory("AgentData");

            string file = $"{prefix}_{DateTime.Now:yyyyMMdd_HHmmss}.{ext}";
            string path = Path.Combine("AgentData", file);

            await File.WriteAllBytesAsync(path, data);
            Console.WriteLine($"[SERVER] Đã lưu {file} ({data.Length} bytes)");
        }

        /* =========================
         * SEND COMMAND TO AGENT
         * ========================= */

        public async Task SendCommandAsync(RemoteCommand command)
        {
            if (_agentSocket == null || _agentSocket.State != WebSocketState.Open)
            {
                Console.WriteLine("[SERVER] Agent chưa kết nối");
                return;
            }

            string json = CommandJson.ToJson(command);
            byte[] buffer = Encoding.UTF8.GetBytes(json);

            await _agentSocket.SendAsync(
                new ArraySegment<byte>(buffer),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None
            );

            Console.WriteLine($"[SERVER] Đã gửi lệnh: {command.Name}");
        }

        /* =========================
         * WEB UI CONNECTION
         * ========================= */

        public async Task ConnectWebUI(WebSocket socket)
        {
            if (_webUISocket != null && _webUISocket.State == WebSocketState.Open)
                throw new InvalidOperationException("WebUI đã kết nối.");

            _webUISocket = socket;
            Console.WriteLine("[SERVER] WebUI connected");

            byte[] buffer = new byte[4096];

            try
            {
                while (_webUISocket.State == WebSocketState.Open)
                {
                    using var ms = new MemoryStream();
                    WebSocketReceiveResult result;

                    do
                    {
                        result = await _webUISocket.ReceiveAsync(
                            new ArraySegment<byte>(buffer),
                            CancellationToken.None
                        );

                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            Console.WriteLine("[SERVER] WebUI disconnected");
                            await _webUISocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client yêu cầu đóng kết nối", CancellationToken.None);
                            return;
                        }

                        ms.Write(buffer, 0, result.Count);

                    } while (!result.EndOfMessage);

                    string json = Encoding.UTF8.GetString(ms.ToArray());
                    if (string.IsNullOrWhiteSpace(json)) continue;

                    Console.WriteLine("[WEBUI] " + json);

                    // WebUI gửi RemoteCommand JSON
                    RemoteCommand cmd = CommandJson.FromJson(json)!;
                    if (cmd == null)
                    {
                        string msg = "[SERVER] Nhận được tin nhắn không hợp lệ từ WebUI";
                        Console.WriteLine(msg);
                        await ForwardToWebUIAsync(msg);
                        continue;
                    }
                    await SendCommandAsync(cmd);
                }
            }
            catch (WebSocketException)
            {
                Console.WriteLine("[SERVER] WebUI disconnected");
            }
            catch (Exception ex) {
                Console.WriteLine($"[SERVER] Lỗi trong ConnectWebUI: {ex.Message}");
            }
        }

        /* =========================
         * FORWARD (OPTIONAL)
         * ========================= */

        private async Task ForwardToWebUIAsync(object data)
        {
            if (_webUISocket == null || _webUISocket.State != WebSocketState.Open)
                return;

            switch (data)
            {
                case byte[] binary:
                    await _webUISocket.SendAsync(
                        new ArraySegment<byte>(binary),
                        WebSocketMessageType.Binary,
                        true,
                        CancellationToken.None
                    );
                    break;

                case string text:
                    byte[] textBytes = Encoding.UTF8.GetBytes(text);
                    await _webUISocket.SendAsync(
                        new ArraySegment<byte>(textBytes),
                        WebSocketMessageType.Text,
                        true,
                        CancellationToken.None
                    );
                    break;

                default:
                    Console.WriteLine($"[SERVER] Không hỗ trợ forward kiểu dữ liệu: {data.GetType().Name}");
                    break;
            }
        }



        /* =========================
         * CLEANUP
         * ========================= */

        private async Task CloseAgentAsync()
        {
            if (_agentSocket == null) return;

            try
            {
                if (_agentSocket.State == WebSocketState.Open)
                {
                    await _agentSocket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Server shutdown",
                        CancellationToken.None
                    );
                }
            }
            catch { }
            finally
            {
                _agentSocket = null;
                Console.WriteLine("[SERVER] Agent socket closed");
            }
        }
    }
}

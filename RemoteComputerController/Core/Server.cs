using System.Net.WebSockets;
using System.Text;

namespace RemoteComputerController.Core
{
    public class Server
    {
        private WebSocket? _currentAgentSocket = null;

        public async Task Connect(WebSocket newSocket)
        {
            if (_currentAgentSocket != null && _currentAgentSocket.State == WebSocketState.Open)
            {
                throw new InvalidOperationException("Đã mở kết nối rồi!");
            }
            else
            {
                if (newSocket == null) throw new InvalidOperationException("Kết nối không hợp lệ");
                _currentAgentSocket = newSocket;
                byte[] buffer = new byte[1024 * 4];
                while (_currentAgentSocket.State == WebSocketState.Open)
                {
                    WebSocketReceiveResult result = await _currentAgentSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await _currentAgentSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Agent yêu cầu đóng kết nối",CancellationToken.None);
                        _currentAgentSocket = null;
                        break;
                    }
                }
            }
        }

        public async Task SendMessage(string message)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(message);
            if (_currentAgentSocket == null || _currentAgentSocket.State != WebSocketState.Open) throw new Exception("Không có kết nối!");
            await _currentAgentSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
        }



    }
}

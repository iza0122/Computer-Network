using RemoteComputerController.Core;
using System.Net.WebSockets;
using System.Text;
Console.OutputEncoding = Encoding.UTF8;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<Server>();

var app = builder.Build();

var options = new WebSocketOptions { KeepAliveInterval = TimeSpan.FromMinutes(2) };

app.UseWebSockets(options);
app.Use(async (context, next) =>
{
    if (context.WebSockets.IsWebSocketRequest && context.Request.Path == "/ws")
    {
        var server = context.RequestServices.GetService<Server>();
        using var websocket = await context.WebSockets.AcceptWebSocketAsync();
        if (server == null) throw new Exception("Khởi tạo server thất bại"); 
        await server.Connect(websocket);
    }
    // else, nếu không phải là WebSocket, chuyển request cho Middleware tiếp theo
    else
    {
        await next(context);
    }
});


app.Run();
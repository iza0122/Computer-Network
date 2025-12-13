using Microsoft.AspNetCore.Hosting.Server;
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
    if (context.WebSockets.IsWebSocketRequest)
    {
        var server = context.RequestServices.GetService<Server>();
        if (server == null) throw new Exception("Khởi tạo server thất bại");

        if (context.Request.Path == "/agent")
        {
            var websocket = await context.WebSockets.AcceptWebSocketAsync();
            await server.ConnectAgent(websocket);
        }
        else if (context.Request.Path == "/control")
        {
            var websocket = await context.WebSockets.AcceptWebSocketAsync();
            await server.ConnectWebUI(websocket);
        }
        else
        {
            // Trả về lỗi 404 cho các đường dẫn WebSocket không hợp lệ
            context.Response.StatusCode = StatusCodes.Status404NotFound;
        }
    }
    else
    {
        await next(context);
    }
});

Task serverTask = app.RunAsync();

//var server = app.Services.GetRequiredService<Server>();

//while (true)
//{
//    Console.WriteLine("\n[SERVER CONSOLE] Nhập lệnh (ví dụ: SCREENSHOT):");
//    string? command = Console.ReadLine();

//    if (string.IsNullOrEmpty(command)) continue;

//    if (command.Equals("EXIT", StringComparison.OrdinalIgnoreCase)) break;

//    try
//    {
//        // Gửi lệnh trực tiếp đến Agent
//        await server.ExecuteAgentCommand(command);
//    }
//    catch (Exception ex)
//    {
//        Console.WriteLine($"[CONSOLE ERROR] Lỗi thực thi lệnh: {ex.Message}");
//    }
//}


await serverTask;
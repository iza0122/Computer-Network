using Microsoft.AspNetCore.Hosting.Server;
using RemoteComputerController.Core;
using System.Net.WebSockets;
using System.Text;
using System.Net;
using System.Net.Sockets;

Console.OutputEncoding = Encoding.UTF8;

var builder = WebApplication.CreateBuilder(args);

// ---THÊM DỊCH VỤ RAZOR PAGES ---
builder.Services.AddRazorPages().AddRazorRuntimeCompilation();
builder.Services.AddSingleton<Server>();

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenAnyIP(5000);
});

var app = builder.Build();

var options = new WebSocketOptions { KeepAliveInterval = TimeSpan.FromMinutes(2) };
app.UseWebSockets(options);

// ---CẤU HÌNH FILE TĨNH VÀ ROUTING ---
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

// ---WEBSOCKET---
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
            context.Response.StatusCode = StatusCodes.Status404NotFound;
        }
    }
    else
    {
        await next(context);
    }
});

// MAP RAZOR PAGES ---
app.MapRazorPages();

// --- KHỞI CHẠY UDP DISCOVERY TRƯỚC KHI RUN APP ---
StartDiscoveryServer();

await app.RunAsync();

static void StartDiscoveryServer()
{
    Task.Run(() =>
    {
        try
        {
            // Lắng nghe trên cổng 8888
            using var udpServer = new UdpClient(8888);
            Console.WriteLine("--------------------------------------------------");
            Console.WriteLine("[UDP] Dịch vụ nhận diện tự động đang chạy (Port 8888)");
            Console.WriteLine("[UDP] Đang chờ yêu cầu từ Agent...");
            Console.WriteLine("--------------------------------------------------");

            var remoteEP = new IPEndPoint(IPAddress.Any, 0);

            while (true)
            {
                byte[] data = udpServer.Receive(ref remoteEP);
                string message = Encoding.UTF8.GetString(data);

                // Kiểm tra mã bí mật từ Agent
                if (message == "WHERE_IS_AMONGUS_SERVER")
                {
                    byte[] response = Encoding.UTF8.GetBytes("I_AM_SERVER");
                    udpServer.Send(response, response.Length, remoteEP);
                    Console.WriteLine($"[UDP] Đã phản hồi 'I_AM_SERVER' tới Agent tại: {remoteEP.Address}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[UDP Error] Lỗi khởi tạo Discovery: {ex.Message}");
        }
    });
}
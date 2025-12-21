using Microsoft.AspNetCore.Hosting.Server;
using RemoteComputerController.Core;
using System.Net.WebSockets;
using System.Text;

Console.OutputEncoding = Encoding.UTF8;

var builder = WebApplication.CreateBuilder(args);

// ---THÊM DỊCH VỤ RAZOR PAGES ---
builder.Services.AddRazorPages().AddRazorRuntimeCompilation();
builder.Services.AddSingleton<Server>();

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

// ---MIDDLEWARE WEBSOCKET (GIỮ NGUYÊN) ---
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

//MAP RAZOR PAGES ---
app.MapRazorPages();

await app.RunAsync();
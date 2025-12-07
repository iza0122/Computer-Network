## Cấu trúc dự án
```
Solution 'ComputerRemoteControl'


	
 ├── 🟢 Shared (Nơi này chứa thư viện dùng chung cho 2 project dưới)
 |   ├── 📂 Models                <-- [DỮ LIỆU CHUNG]
 │   │    └── 📄 RemoteCommand.cs (Định nghĩa gói tin: { Action: "Shutdown", MachineID: "PC1" })
 │
 │
 ├── 🟢 1. PROJECT WEB: ComputerRemoteControl (ASP.NET Core Razor Pages)
 │    │    Re: Đóng vai trò Server trung tâm & Giao diện điều khiển
 │    │
 │    ├── 📂 Hubs                  <-- [TRẠM TRUNG CHUYỂN SIGNALR]
 │    │    └── 📄 RemoteHub.cs     (Nhận lệnh từ Web -> Bắn xuống Agent)
 │    │
 │    │
 │    ├── 📂 Pages                 <-- [GIAO DIỆN NGƯỜI DÙNG]
 │    │    ├── 📄 Index.cshtml     (Trang giới thiệu/Trạng thái)
 │    │    ├── 📄 Login.cshtml     (Bảo mật: Phải đăng nhập mới được vào)
 │    │    ├── 📄 Control.cshtml   (Bảng điều khiển chính: Các nút bấm)
 │    │    └── 📄 Control.cshtml.cs (Code xử lý logic giao diện)
 │    │
 │    ├── 📂 Services              <-- [LOGIC QUẢN LÝ]
 │    │    └── 📄 RemoteControlService.cs (Lưu log lịch sử, xác thực quyền truy cập)
 │    │
 │    ├── 📂 wwwroot               <-- [FRONTEND TĨNH]
 │    │    ├── 📂 css
 │    │    └── 📂 js
 │    │         └── 📄 remote.js   (Code JS kết nối SignalR từ trình duyệt)
 │    │
 │    ├── 📄 Program.cs            (Cấu hình SignalR Server)
 │    └── 📄 appsettings.json
 │
 │
 ├── 🔴 2. PROJECT AGENT: Agent (Console App / Worker Service)
 │    │    Re: Chạy ngầm trên máy tính, thực thi lệnh thật sự
 │    │
 │    ├── 📄 AgentClient.cs        (Kết nối tới RemoteHub, ngồi chờ lệnh)
 │    ├── 📄 CommandExecutor.cs    (Thợ máy: Gọi CMD shutdown, tăng giảm volume...)
 │    └── 📄 Program.cs            (Khởi động Agent, giữ kết nối liên tục)
 │
 └── 📄 README.md
```
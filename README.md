ComputerRemoteControl (Solution)
│
├── 1. RemoteServer (ASP.NET Core Web App)
│   ├── Properties
│   │   └── launchSettings.json    <-- (Cấu hình cổng cố định: 5000)
│   │
│   ├── Core
│   │   └── Server.cs  <-- (quản lý socket agent)
│   │
│   ├── Pages 
│   │   ├── Index.cshtml         <-- (Giao diện của ứng dụng ...)  
│   │   └── Index.cshtml.cs
│   │
│   ├── wwwroot
│   │   └── js
│   │       └── remote-control.js  <-- (JS nối WebSocket từ trình duyệt lên Server)
│   │
│   └── Program.cs                 <-- (Định nghĩa route /ws, đăng ký dịch vụ)
│
│
└── 2. RemoteAgent (Console App)
    │
    ├── AgentNetworkClient.cs  <-- (Vòng lặp kết nối và nhận lệnh)
    ├── CommandExecutor.cs     <-- (Xử lý lệnh: Process.Start, Chụp ảnh...)
    └── Program.cs                 <-- (Hàm Main: Cấu hình URL server và chạy Client)
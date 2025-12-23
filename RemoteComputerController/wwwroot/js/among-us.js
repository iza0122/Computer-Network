// ================================================================
// 1. CẤU HÌNH & TRẠNG THÁI
// ================================================================
const protocol = window.location.protocol === 'https:' ? 'wss:' : 'ws:';
const host = window.location.hostname; // Lấy IP hoặc Hostname từ thanh địa chỉ trình duyệt
const port = '5000'; // Cổng của RemoteServer
const WS_URL = `${protocol}//${host}:${port}/control`;
console.log(">>> Hệ thống đang kết nối tới:", WS_URL);
let socket = null;
let isConnected = false;
let lastImageUrl = null;

const MessageType = {
    Image: 0,
    Text: 1,
    Video: 2,
    Json: 3,
    Status: 4
};

// ================================================================
// 2. QUẢN LÝ KẾT NỐI
// ================================================================
function handleConnection() {
    const btn = document.getElementById('connectionStatus');
    const spinner = document.getElementById('loadingSpinner');

    if (socket && socket.readyState === WebSocket.CONNECTING) {
        showToast("Hệ thống đang kết nối...", false);
        return;
    }

    if (!isConnected) {
        if (socket) {
            socket.onopen = socket.onmessage = socket.onclose = socket.onerror = null;
            socket.close();
        }

        btn.className = 'alert-connecting';
        btn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> ĐANG THIẾT LẬP...';

        try {
            socket = new WebSocket(WS_URL);
            socket.binaryType = "arraybuffer";

            socket.onopen = () => {
                isConnected = true;
                btn.className = 'alert-success';
                btn.innerHTML = '<i class="fas fa-check-circle"></i> HỆ THỐNG TRỰC TUYẾN';
                showToast("Đã kết nối tới Server!", false);
            };

            socket.onmessage = (event) => {
                if (spinner) spinner.style.display = 'none';
                if (event.data instanceof ArrayBuffer) {
                    try {
                        const view = new Uint8Array(event.data);
                        const type = view[0];
                        const payload = event.data.slice(1);
                        handleIncomingData(type, payload, view);
                    } catch (err) {
                        console.error("Lỗi xử lý dữ liệu:", err);
                    }
                }
            };

            socket.onclose = () => {
                cleanupSocketState();
                showToast("Đã mất kết nối với Server.", true);
            };

            socket.onerror = (err) => {
                cleanupSocketState();
                showToast("Lỗi: Không thể kết nối Server!", true);
            };

        } catch (error) {
            cleanupSocketState();
            showToast("Khởi tạo Socket thất bại!", true);
        }
    } else {
        if (socket) socket.close();
    }
}

function cleanupSocketState() {
    isConnected = false;
    socket = null;
    const btn = document.getElementById('connectionStatus');
    const spinner = document.getElementById('loadingSpinner');

    if (spinner) spinner.style.display = 'none';

    btn.className = 'alert-danger';
    btn.innerHTML = '<i class="fas fa-plug"></i> ĐÃ NGẮT KẾT NỐI';

    setTimeout(() => {
        if (!isConnected) {
            btn.className = '';
            btn.innerHTML = '<i class="fas fa-plug"></i> KẾT NỐI HỆ THỐNG';
        }
    }, 2000);
}

// ================================================================
// 3. XỬ LÝ DỮ LIỆU NHẬN VỀ
// ================================================================
function handleIncomingData(type, payload, fullView) {
    const textScreen = document.getElementById('textResult');
    const rawData = new TextDecoder().decode(payload);

    switch (type) {
        case MessageType.Image:
            resetDisplay();

            const img = document.getElementById('imgResult');
            const blob = new Blob([payload], { type: 'image/jpeg' });
            const newUrl = URL.createObjectURL(blob);

            img.src = newUrl;
            img.style.display = 'block';

            if (lastImageUrl) URL.revokeObjectURL(lastImageUrl);
            lastImageUrl = newUrl;

            showToast("ĐÃ NHẬN ẢNH CHỤP MÀN HÌNH");
            break;

        case MessageType.Text: // 1
            const rawData = new TextDecoder().decode(payload);

            // Kiểm tra các loại JSON danh sách
            if (rawData.includes('"Name":"InstalledAppList"')) {
                renderInstalledAppList(JSON.parse(rawData));
                return;
            }
            if (rawData.includes('"RunningAppList"')) {
                renderGraphicList(JSON.parse(rawData), 'APP');
                return;
            }
            if (rawData.includes('"TaskList"')) {
                renderGraphicList(JSON.parse(rawData), 'TASK');
                return;
            }
            if (rawData.includes("WebcamList")) {
                processJsonResponse(JSON.parse(rawData));
                return;
            }

            // Nếu là Keylog: Cộng dồn vào Tablet
            const textScreen = document.getElementById('textResult');
            // Chỉ resetDisplay nếu tablet đang hiện đồ họa mà muốn chuyển sang text
            if (document.getElementById('gridResult').style.display !== 'none') {
                resetDisplay();
            }
            textScreen.style.display = 'block';
            textScreen.innerText += rawData;
            const displayArea = document.getElementById('displayArea');
            displayArea.scrollTop = displayArea.scrollHeight;
            break;

        case MessageType.Video: // 2
            // 1. Dọn dẹp toàn bộ màn hình Tablet (Ẩn Task, App, Text, Ảnh)
            resetDisplay();

            const video = document.getElementById('videoResult');
            if (video) {
                const videoBlob = new Blob([payload], { type: 'video/mp4' });
                const videoUrl = URL.createObjectURL(videoBlob);
                video.src = videoUrl;
                video.style.display = 'block';
                video.play().catch(e => console.warn("Trình duyệt chặn tự động phát:", e));
                video.onended = () => {
                    showToast("KẾT THÚC ĐOẠN PHIM WEBCAM");
                };
            }
            break;

        case MessageType.Json:
            const jsonStr = new TextDecoder().decode(payload);
            const obj = JSON.parse(jsonStr);

            if (obj.Name === "TaskList" || Array.isArray(obj)) {
                renderTaskList(obj);
            }
            else if (obj.Name === "WebcamList") {
                processJsonResponse(obj);
            }
            break;

        case MessageType.Status:
            const isSuccess = fullView[1] === 1;
            const msg = new TextDecoder().decode(fullView.slice(2));

            showToast(msg, !isSuccess);
            if (isSuccess && (msg.includes("dừng tác vụ") || msg.includes("thành công"))) {
                const match = msg.match(/\d+/);
                if (match) {
                    const pid = match[0];
                    removeTaskCard(pid);
                }
            }
            break;
    }
}

function processJsonResponse(data) {
    console.log("Đang xử lý mảng Data:", data.Data);

    const select = document.getElementById('webcamSelect');
    const textScreen = document.getElementById('textResult');

    if (!select) {
        console.error("LỖI: Không tìm thấy thẻ <select id='webcamSelect'>");
        return;
    }

    select.options.length = 0;

    if (data.Data && Array.isArray(data.Data) && data.Data.length > 0) {

        data.Data.forEach((cam, index) => {
            const camName = cam.Name || cam.name || `Camera ${index + 1}`;
            const camId = cam.Id || cam.id || camName;

            const opt = document.createElement('option');
            opt.value = camId;
            opt.text = camName.toUpperCase();
            select.appendChild(opt);
        });
        if (textScreen) textScreen.style.display = 'none';

    } else {
        const opt = document.createElement('option');
        opt.text = "KHÔNG TÌM THẤY CAMERA";
        select.appendChild(opt);
        showToast("DANH SÁCH TRỐNG", true);
    }
}

// ================================================================
// 4. GỬI LỆNH ĐI
// ================================================================
function guiLenh(cmd, element) {
    if (!isConnected) {
        showToast("Vui lòng kết nối hệ thống trước!", true);
        return;
    }

    document.getElementById('loadingSpinner').style.display = 'block';
    hideAllScreens();
    socket.send(JSON.stringify({ Name: cmd, Data: {} }));
}

function startApp() {
    if (!isConnected) return showToast("Hệ thống chưa kết nối!", true);
    const path = document.getElementById('appNameInput').value;
    if (!path) return showToast("Bạn chưa nhập đường dẫn!", true);

    socket.send(JSON.stringify({
        Name: "StartTask",
        Data: { Path: path }
    }));
}

function recordWebcam() {
    if (!isConnected) return showToast("Hệ thống chưa kết nối!", true);

    const select = document.getElementById('webcamSelect');
    const selectedId = select.value;

    if (!selectedId || selectedId.includes(" tải")) {
        return showToast("Vui lòng chọn một Camera hợp lệ!", true);
    }

    const command = {
        Name: "WebcamRecord",
        Data: selectedId
    };

    console.log(">>> Gửi lệnh quay phim:", command);
    socket.send(JSON.stringify(command));

    showToast("ĐANG KHỞI ĐỘNG CAMERA...");
}
function sysAction(type) {
    if (!isConnected) return showToast("Hệ thống chưa kết nối!", true);
    // Vẫn giữ confirm cho các hành động nguy hiểm như Shutdown
    if (confirm("Xác nhận thực hiện lệnh: " + type + "?")) {
        socket.send(JSON.stringify({ Name: type, Data: {} }));
    }
}

// ================================================================
// THÔNG BÁO TOAST TÙY CHỈNH
// ================================================================
function showToast(message, isError = false) {
    let box = document.getElementById("toast-box");
    if (!box) {
        box = document.createElement("div");
        box.id = "toast-box";
        document.body.appendChild(box);
    }

    const toast = document.createElement("div");
    toast.classList.add("toast-msg");

    if (isError) {
        toast.style.borderColor = "#ff0000";
        toast.style.boxShadow = "0 4px 15px rgba(255, 0, 0, 0.3)";
    }

    const icon = isError ? "⚠️" : "🚀";
    toast.innerHTML = `<span>${icon}</span> <span>${message.toUpperCase()}</span>`;

    box.appendChild(toast);

    setTimeout(() => {
        toast.style.animation = "fadeOut 0.5s forwards";
        setTimeout(() => toast.remove(), 500);
    }, 2000);
}

// ================================================================
// UTILS & MENU
// ================================================================
function hideAllScreens() {
    document.getElementById('imgResult').style.display = 'none';
    document.getElementById('videoResult').style.display = 'none';
    document.getElementById('textResult').style.display = 'none';
}

function toggleKeylogMenu() {
    document.getElementById('keylogDropdown').classList.toggle('show');
    document.getElementById('keylogIcon').classList.toggle('rotate-icon');
}

function controlKeylog(action) {
    const btnStart = document.getElementById('btnStartKeylog');
    const btnStop = document.getElementById('btnStopKeylog');

    if (btnStart) btnStart.blur();
    if (btnStop) btnStop.blur();

    if (action === 'Start') {
        if (isConnected && socket) {
            socket.send(JSON.stringify({ Name: "KeyLogger", Data: {} }));
            btnStart.classList.add('active-glow');
            showToast("KEYLOGGER: BẮT ĐẦU");
        }
    } else {
        if (isConnected && socket) {
            socket.send(JSON.stringify({ Name: "StopKeyLogger", Data: {} }));
            btnStart.classList.remove('active-glow');
            showToast("KEYLOGGER: DỪNG", true);
        }
    }
}

function toggleWebcamMenu() {
    const content = document.getElementById('webcamDropdown');
    const icon = document.getElementById('webcamIcon');

    // Đảo ngược class 'show' để thả menu xuống
    const isShowing = content.classList.toggle('show');
    icon.classList.toggle('rotate-icon');

    // Chỉ khi nào mở menu ra mới đi lấy danh sách
    if (isShowing) {
        showToast("Đang quét thiết bị...");
        // Gửi lệnh lấy danh sách webcam
        if (isConnected) {
            socket.send(JSON.stringify({ Name: "WebcamList", Data: {} }));
        } else {
            showToast("Chưa kết nối Server!", true);
        }
    }
}

function clearTablet() {
    const textScreen = document.getElementById('textResult');
    if (textScreen) {
        textScreen.innerText = ""; // Làm trống nội dung
        showToast("ĐÃ DỌN DẸP MÀN HÌNH TABLET", false);
    }
}

function xuatDuLieuTablet() {
    const textScreen = document.getElementById('textResult');
    if (!textScreen) return;

    const content = textScreen.innerText;

    if (!content || content.trim() === "") {
        showToast("MÀN HÌNH TRỐNG, KHÔNG CÓ DỮ LIỆU!", true);
        return;
    }

    try {
        const blob = new Blob([content], { type: 'text/plain' });
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');

        const now = new Date();
        const fileName = `Log_${now.getHours()}${now.getMinutes()}_${now.getDate()}-${now.getMonth() + 1}.txt`;

        a.href = url;
        a.download = fileName;
        document.body.appendChild(a);
        a.click();

        window.URL.revokeObjectURL(url);
        document.body.removeChild(a);

        showToast("ĐÃ TẢI FILE: " + fileName);
    } catch (err) {
        console.error("Lỗi xuất file:", err);
        showToast("LỖI KHI XUẤT FILE!", true);
    }
}

function forceStopKeylogger() {
    const btnStart = document.getElementById('btnStartKeylog');
    const textScreen = document.getElementById('textResult');

    if (isConnected && socket) {
        socket.send(JSON.stringify({ Name: "StopKeyLogger", Data: {} }));
        if (btnStart) btnStart.classList.remove('active-glow');

        // Xóa luôn nội dung cũ trên Tablet để tránh bị rối
        if (textScreen) textScreen.innerText = "";
    }
}

function renderTaskList(data) {
    const displayArea = document.getElementById('displayArea');
    const textScreen = document.getElementById('textResult');

    // Ẩn màn hình text để hiện giao diện đồ họa
    if (textScreen) textScreen.style.display = 'none';

    // Xóa sạch nội dung cũ trong vùng hiển thị
    displayArea.innerHTML = '';

    const listContainer = document.createElement('div');
    listContainer.className = 'task-list-container';

    // Lấy mảng Task từ thuộc tính Data
    const tasks = data.Data;

    if (Array.isArray(tasks)) {
        tasks.forEach(task => {
            const card = document.createElement('div');
            card.className = 'task-card';

            card.innerHTML = `
                <div class="task-header">
                    <i class="fas fa-microchip"></i> <span>${task.Name}</span>
                </div>
                <div class="task-pid">PID: ${task.Id}</div>
                <button class="btn-kill-task" onclick="killProcess(${task.Id}, '${task.Name}')">
                    STOP
                </button>
            `;
            listContainer.appendChild(card);
        });

        displayArea.appendChild(listContainer);
        displayArea.scrollTop = 0; // Cuộn lên đầu
        showToast(`TÌM THẤY ${tasks.length} TIẾN TRÌNH`);
    }
}

// Hàm gửi lệnh tiêu diệt Process
function killProcess(pid, name) {
    if (confirm(`XÁC NHẬN TIÊU DIỆT: ${name.toUpperCase()}?`)) {
        if (isConnected && socket) {
            socket.send(JSON.stringify({
                Name: "StopTask",
                Data: { Id: pid }
            }));

            // Chuyển nút sang trạng thái chờ
            const btn = document.getElementById(`btn-stop-${pid}`);
            if (btn) {
                btn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> EXECUTING...';
                btn.style.borderColor = "#fff";
                btn.style.color = "#fff";
                btn.disabled = true;
            }
        }
    }
}
function removeTaskCard(pid) {
    const cardToRemove = document.getElementById(`card-${pid}`);
    if (cardToRemove) {
        // Hiệu ứng "Bị tiêu diệt" (Kiểu Among Us)
        cardToRemove.style.pointerEvents = "none"; // Khóa mọi tương tác
        cardToRemove.style.transition = "all 0.6s cubic-bezier(0.4, 0, 0.2, 1)";
        cardToRemove.style.background = "rgba(255, 0, 0, 0.4)"; // Đổi sang nền đỏ mờ
        cardToRemove.style.boxShadow = "0 0 20px #ff4444";

        setTimeout(() => {
            cardToRemove.style.transform = "scale(0.8) translateY(-20px)";
            cardToRemove.style.opacity = "0";

            setTimeout(() => {
                cardToRemove.remove();

                // Kiểm tra nếu không còn task nào thì có thể hiện thông báo trống
                const grid = document.getElementById('gridResult');
                if (grid && grid.children.length === 0) {
                    grid.innerHTML = '<div style="color:#555; text-align:center; padding:20px;">DANH SÁCH TRỐNG</div>';
                }
            }, 500);
        }, 300);
    }
}
function renderAppList(data) {
    const displayArea = document.getElementById('displayArea');
    const textScreen = document.getElementById('textResult');

    if (textScreen) textScreen.style.display = 'none';
    displayArea.innerHTML = '';

    const listContainer = document.createElement('div');
    listContainer.className = 'task-list-container';

    const apps = data.Data;

    if (Array.isArray(apps)) {
        apps.forEach(app => {
            // Xử lý tên: Nếu là đường dẫn dài, chỉ lấy tên file cuối cùng
            let displayName = app.Name;
            if (displayName.includes('\\')) {
                displayName = displayName.split('\\').pop();
            }

            const card = document.createElement('div');
            card.className = 'task-card';
            card.style.borderColor = '#ffce54'; // Màu vàng đặc trưng cho App

            card.innerHTML = `
                <div class="task-header" style="color: #ffce54;">
                    <i class="fas fa-window-restore"></i> <span>${displayName}</span>
                </div>
                <div class="task-pid">PID: ${app.Id}</div>
                <button class="btn-kill-task" 
                        style="border-color: #ff4444;" 
                        onclick="killProcess(${app.Id}, '${displayName.replace(/'/g, "\\'")}')">
                    STOP
                </button>
            `;
            listContainer.appendChild(card);
        });

        displayArea.appendChild(listContainer);
        displayArea.scrollTop = 0;
        showToast(`CÓ ${apps.length} ỨNG DỤNG ĐANG CHẠY`);
    }
}

function resetDisplay() {
    const ids = ['textResult', 'gridResult', 'imgResult', 'videoResult', 'loadingSpinner'];
    ids.forEach(id => {
        const el = document.getElementById(id);
        if (el) {
            el.style.display = 'none';
            // Dừng video và xóa nguồn nếu đang ẩn
            if (id === 'videoResult') {
                el.pause();
                el.src = "";
                el.load(); // Buộc trình duyệt giải phóng tài nguyên video
            }
        }
    });

    const grid = document.getElementById('gridResult');
    if (grid) grid.innerHTML = '';
}
function renderGraphicList(data, type) {
    resetDisplay();
    const grid = document.getElementById('gridResult');
    grid.style.display = 'flex';

    const items = data.Data;
    if (!Array.isArray(items)) return;

    items.forEach(item => {
        let displayName = item.Name;
        if (type === 'APP' && displayName.includes('\\')) {
            displayName = displayName.split('\\').pop();
        }

        const color = (type === 'APP') ? '#ffce54' : '#51ffeb';
        const card = document.createElement('div');
        card.className = 'task-card';
        card.id = `card-${item.Id}`;
        card.style.borderColor = color;

        card.innerHTML = `
            <div class="task-header" style="color: ${color}">
                <i class="fas ${type === 'APP' ? 'fa-window-restore' : 'fa-robot'}"></i> 
                <span>${displayName}</span>
            </div>
            <div class="task-pid">PID: ${item.Id}</div>
            <button class="btn-stop-task" onclick="killProcess(${item.Id}, '${displayName.replace(/'/g, "\\'")}')">
                STOP
            </div>
        `;
        grid.appendChild(card);
    });
}

function renderInstalledAppList(data) {
    resetDisplay();
    const grid = document.getElementById('gridResult');
    grid.style.display = 'flex';

    const apps = data.Data;
    if (!Array.isArray(apps)) return;

    // Màu tím đặc trưng cho App đã cài đặt
    const purpleColor = "#a29bfe";

    apps.forEach(app => {
        const card = document.createElement('div');
        card.className = 'task-card';
        card.style.borderColor = purpleColor;

        card.innerHTML = `
            <div class="task-header" style="color: ${purpleColor}">
                <i class="fas fa-app-store"></i> <span>${app.Name}</span>
            </div>
            <div class="task-pid" style="color: #888; font-size: 0.8rem; overflow: hidden; text-overflow: ellipsis; white-space: nowrap;">
                PATH: ${app.ExecutablePath}
            </div>
            <button class="btn-start w-100" 
                    style="font-family: 'VT323'; font-size: 1.1rem; padding: 5px; border-radius: 8px;"
                    onclick="runInstalledApp('${app.ExecutablePath.replace(/\\/g, "\\\\")}')">
                <i class="fas fa-play"></i> CHẠY ỨNG DỤNG
            </button>
        `;
        grid.appendChild(card);
    });

    document.getElementById('displayArea').scrollTop = 0;
    showToast(`TÌM THẤY ${apps.length} ỨNG DỤNG`);
}

function runInstalledApp(path) {
    if (confirm(`BẠN MUỐN CHẠY ỨNG DỤNG NÀY?\n${path}`)) {
        socket.send(JSON.stringify({
            Name: "StartTask",
            Data: { Path: path }
        }));
        showToast("ĐANG KHỞI CHẠY...");
    }
}
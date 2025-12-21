// ================================================================
// 1. CẤU HÌNH & TRẠNG THÁI
// ================================================================
const WS_URL = "ws://192.168.1.124:5000/control";
let socket = null;
let isConnected = false;
let lastImageUrl = null;
// Quy ước MessageType từ phía C# của bạn
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
// ================================================================
// QUẢN LÝ KẾT NỐI (BẢN FULL CHỐNG LIỆT NÚT)
// ================================================================
function handleConnection() {
    const btn = document.getElementById('connectionStatus');
    const spinner = document.getElementById('loadingSpinner');

    // 1. Kiểm tra nếu đang trong quá trình kết nối thì không cho bấm tiếp
    if (socket && socket.readyState === WebSocket.CONNECTING) {
        console.warn("Hệ thống đang kết nối, vui lòng không nhấn liên tục!");
        return;
    }

    if (!isConnected) {
        // 2. Dọn dẹp socket cũ (nếu có) trước khi tạo mới
        if (socket) {
            socket.onopen = socket.onmessage = socket.onclose = socket.onerror = null;
            socket.close();
        }

        // Cập nhật giao diện trạng thái đang kết nối
        btn.className = 'alert-connecting';
        btn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> ĐANG THIẾT LẬP...';

        try {
            socket = new WebSocket(WS_URL);
            socket.binaryType = "arraybuffer"; // Cực kỳ quan trọng để nhận ảnh/video

            // SỰ KIỆN: KHI THÔNG TUYẾN
            socket.onopen = () => {
                isConnected = true;
                btn.className = 'alert-success';
                btn.innerHTML = '<i class="fas fa-check-circle"></i> HỆ THỐNG TRỰC TUYẾN';
                console.log(">> [CONNECTED] Đã kết nối tới Server.");
            };

            // SỰ KIỆN: NHẬN DỮ LIỆU (BẢN ERROR-PROOF)
            socket.onmessage = (event) => {
                // Luôn tắt loading ngay khi có tín hiệu về
                if (spinner) spinner.style.display = 'none';

                if (event.data instanceof ArrayBuffer) {
                    try {
                        const view = new Uint8Array(event.data);
                        const type = view[0];
                        const payload = event.data.slice(1);

                        // Gọi hàm xử lý hiển thị đã viết trước đó
                        handleIncomingData(type, payload, view);
                    } catch (err) {
                        console.error("Lỗi xử lý dữ liệu nhị phân:", err);
                    }
                }
            };

            // SỰ KIỆN: KHI MẤT KẾT NỐI
            socket.onclose = () => {
                cleanupSocketState();
            };

            // SỰ KIỆN: LỖI KẾT NỐI
            socket.onerror = (err) => {
                console.error("WebSocket Error:", err);
                cleanupSocketState();
                alert("Không thể kết nối tới Server! Hãy chắc chắn Server đã bật.");
            };

        } catch (error) {
            console.error("Khởi tạo Socket thất bại:", error);
            cleanupSocketState();
        }
    } else {
        // Nếu đang kết nối mà bấm thì sẽ ngắt kết nối
        if (socket) socket.close();
    }
}

/**
 * Hàm dọn dẹp trạng thái khi ngắt kết nối hoặc lỗi
 */
function cleanupSocketState() {
    isConnected = false;
    socket = null;
    const btn = document.getElementById('connectionStatus');
    const spinner = document.getElementById('loadingSpinner');

    if (spinner) spinner.style.display = 'none';

    btn.className = 'alert-danger';
    btn.innerHTML = '<i class="fas fa-plug"></i> ĐÃ NGẮT KẾT NỐI';

    // Sau 2 giây tự động quay về trạng thái mời gọi kết nối
    setTimeout(() => {
        if (!isConnected) {
            btn.className = '';
            btn.innerHTML = '<i class="fas fa-plug"></i> KẾT NỐI HỆ THỐNG';
        }
    }, 2000);
}

// ================================================================
// 3. XỬ LÝ DỮ LIỆU NHẬN VỀ (HIỂN THỊ LÊN TABLET)
// ================================================================
function handleIncomingData(type, payload, fullView) {
    //hideAllScreens();
    const textScreen = document.getElementById('textResult');

    switch (type) {
        case MessageType.Image:
            const img = document.getElementById('imgResult');
            const blob = new Blob([payload], { type: 'image/jpeg' });
            const newUrl = URL.createObjectURL(blob);

            // 1. Gán ảnh mới
            img.src = newUrl;
            img.style.display = 'block';

            // 2. Chỉ xóa URL cũ sau khi đã có URL mới (tránh nhấp nháy)
            if (lastImageUrl) {
                URL.revokeObjectURL(lastImageUrl);
            }

            // 3. Lưu lại URL vừa tạo để xóa ở lần chụp sau
            lastImageUrl = newUrl;
            break;

        case MessageType.Text: // 1
            textScreen.innerText = new TextDecoder().decode(payload);
            textScreen.style.display = 'block';
            break;

        case MessageType.Video: // 2
            const video = document.getElementById('videoResult');
            video.src = URL.createObjectURL(new Blob([payload], { type: 'video/mp4' }));
            video.style.display = 'block';
            video.play();
            break;

        case MessageType.Json: // 3
            const jsonStr = new TextDecoder().decode(payload);
            const data = JSON.parse(jsonStr);
            processJsonResponse(data);
            break;

        case MessageType.Status: // 4
            const isSuccess = fullView[1] === 1;
            const msg = new TextDecoder().decode(fullView.slice(2));
            alert((isSuccess ? "THÀNH CÔNG: " : "THẤT BẠI: ") + msg);
            break;
    }
}

// Xử lý riêng cho JSON (Danh sách App/Webcam)
function processJsonResponse(data) {
    const textScreen = document.getElementById('textResult');

    // Nếu là danh sách Webcam
    if (data.Webcams) {
        const select = document.getElementById('webcamSelect');
        const container = document.getElementById('webcamControls');
        select.innerHTML = '';
        data.Webcams.forEach(cam => {
            let opt = document.createElement('option');
            opt.value = cam; opt.text = cam;
            select.appendChild(opt);
        });
        container.style.display = 'block';
        textScreen.innerText = "Đã tìm thấy " + data.Webcams.length + " Camera.";
    }
    // Nếu là danh sách Task/App (Render thô vào thẻ pre)
    else {
        textScreen.innerText = JSON.stringify(data, null, 2);
    }
    textScreen.style.display = 'block';
}

// ================================================================
// 4. GỬI LỆNH ĐI (KHỚP VỚI CÁC NÚT BẤM)
// ================================================================
function guiLenh(cmd, element) {
    if (!isConnected) return alert("Phải kết nối trước!");

    document.getElementById('loadingSpinner').style.display = 'block';
    hideAllScreens();

    // Gửi theo class RemoteCommand { Name, Data }
    // Lưu ý: Gửi Data là Object trống để Agent không bị lỗi parse
    socket.send(JSON.stringify({ Name: cmd, Data: {} }));
}

function startApp() {
    const path = document.getElementById('appNameInput').value;
    if (!path) return alert("Nhập đường dẫn!");
    socket.send(JSON.stringify({
        Name: "StartTask",
        Data: { Path: path } // Gửi Object để tránh lỗi "requires an element of type Object"
    }));
}

function recordWebcam() {
    const camName = document.getElementById('webcamSelect').value;
    socket.send(JSON.stringify({
        Name: "WebcamRecord",
        Data: { DeviceName: camName }
    }));
}

function sysAction(type) {
    if (!isConnected) return;
    if (confirm("Xác nhận thực hiện lệnh: " + type + "?")) {
        socket.send(JSON.stringify({ Name: type, Data: {} }));
    }
}

// ================================================================
// UTILS & HIỆU ỨNG
// ================================================================
function hideAllScreens() {
    document.getElementById('imgResult').style.display = 'none';
    document.getElementById('videoResult').style.display = 'none';
    document.getElementById('textResult').style.display = 'none';
}

// Hiệu ứng Ripple cho các nút bấm
document.addEventListener('click', function (e) {
    const target = e.target.closest('.btn-among-us, .btn');
    if (target) {
        let ripple = document.createElement('span');
        ripple.classList.add('ripple');
        target.appendChild(ripple);
        ripple.style.left = e.offsetX + 'px';
        ripple.style.top = e.offsetY + 'px';
        setTimeout(() => ripple.remove(), 600);
    }
});
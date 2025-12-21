let isConnected = false;

// Hiệu ứng Ripple (sóng) khi click
document.querySelectorAll('.btn, .btn-among-us').forEach(button => {
    button.addEventListener('click', function (e) {
        let ripple = document.createElement('span');
        ripple.classList.add('ripple');
        this.appendChild(ripple);
        let x = e.offsetX; let y = e.offsetY;
        ripple.style.left = x + 'px'; ripple.style.top = y + 'px';
        setTimeout(() => ripple.remove(), 600);
    });
});

// Toggle Kết nối
function handleConnection() {
    const btn = document.getElementById('connectionStatus');

    if (!isConnected) {
        btn.className = 'alert-connecting';
        btn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> ĐANG THIẾT LẬP...';

        setTimeout(() => {
            isConnected = true;
            btn.className = 'alert-success';
            btn.innerHTML = '<i class="fas fa-check-circle"></i> HỆ THỐNG TRỰC TUYẾN';
        }, 1200);
    } else {
        isConnected = false;
        btn.className = 'alert-danger';
        btn.innerHTML = '<i class="fas fa-plug"></i> ĐÃ NGẮT KẾT NỐI';

        setTimeout(() => {
            if (!isConnected) {
                btn.className = '';
                btn.id = 'connectionStatus';
                btn.innerHTML = '<i class="fas fa-plug"></i> KẾT NỐI HỆ THỐNG';
            }
        }, 2000);
    }
}

function guiLenh(cmd, element) {
    if (!isConnected) {
        alert("CẢNH BÁO: Phải kết nối hệ thống trước!");
        return;
    }

    // Logic hiển thị loading và kết quả
    const display = {
        img: document.getElementById('imgResult'),
        text: document.getElementById('textResult'),
        loading: document.getElementById('loadingSpinner')
    };

    display.img.style.display = 'none';
    display.text.style.display = 'none';
    display.loading.style.display = 'block';

    setTimeout(() => {
        display.loading.style.display = 'none';
        if (cmd === 'Screenshot') {
            display.img.src = "https://picsum.photos/800/450";
            display.img.style.display = 'block';
        } else {
            display.text.innerText = `[SUCCESS] Lệnh ${cmd} đã được gửi tới Terminal.`;
            display.text.style.display = 'block';
        }
    }, 800);
}

function sysAction(type) {
    if (!isConnected) return;
    console.log(`Executing system ${type}...`);
    alert(`Đang thực hiện lệnh: ${type}`);
}
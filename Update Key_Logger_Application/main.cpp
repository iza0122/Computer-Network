#include <io.h>    // Sửa lỗi gạch đỏ _setmode và _fileno
#include <fcntl.h> // Sửa lỗi gạch đỏ _O_U16TEXT
#include <iostream>
#include <conio.h>
#include "Unpdate.h" // Gọi API từ Module Unpdate

// Hàm xử lý thông điệp nhận được từ Hook thông qua cửa sổ ẩn
LRESULT CALLBACK WndProc(HWND hwnd, UINT msg, WPARAM wParam, LPARAM lParam) {
    if (msg == WM_USER + 100) {
        // 1. Nhận chuỗi văn bản đã qua xử lý (Backspace, Unicode) từ Module Unpdate
        std::wstring result = GetProcessedString(wParam);

        // 2. Lưu nội dung chuỗi vào file log tại đường dẫn Ex6.1
        SaveToFile(result);

        // 3. In kết quả trực tiếp ra màn hình Console để theo dõi
        std::wcout << L"\rDữ liệu hiện tại: " << result << L"   " << std::flush;
    }
    return DefWindowProc(hwnd, msg, wParam, lParam);
}

int main() {
    // Thiết lập Console hỗ trợ hiển thị ký tự Unicode tiếng Việt
    _setmode(_fileno(stdout), _O_U16TEXT);

    std::wcout << L"--- HỆ THỐNG GIÁM SÁT BÀN PHÍM ---" << std::endl;
    std::wcout << L"Trạng thái: Đang chạy (Quyền Administrator)" << std::endl;
    std::wcout << L"Nhấn phím '1' tại cửa sổ này để THOÁT." << std::endl;

    // Khởi tạo cửa sổ ẩn làm "trạm thu nhận" tin nhắn từ DLL
    WNDCLASSW wc = { 0 };
    wc.lpfnWndProc = WndProc;
    wc.lpszClassName = L"Update4Receiver";
    RegisterClassW(&wc);
    HWND hWnd = CreateWindowExW(0, L"Update4Receiver", NULL, 0, 0, 0, 0, 0, NULL, NULL, NULL, NULL);

    // GỌI QUA UNPDATE: Nạp Dll1.dll và thiết lập Hook hệ thống
    if (!StartMonitoring(hWnd)) {
        std::wcout << L"Lỗi: Không thể khởi động! Kiểm tra lại file Dll1.dll." << std::endl;
        return 1;
    }

    // Vòng lặp tin nhắn tích hợp kiểm tra phím thoát số 1
    MSG msg;
    while (true) {
        // Kiểm tra phím bấm tại Console để thoát chương trình
        if (_kbhit()) {
            if (_getch() == '1') {
                std::wcout << L"\nĐang dừng hệ thống và giải phóng tài nguyên..." << std::endl;
                break;
            }
        }

        // Nhận và điều phối tin nhắn từ Hook gửi về
        if (PeekMessage(&msg, NULL, 0, 0, PM_REMOVE)) {
            TranslateMessage(&msg);
            DispatchMessage(&msg);
        }

        // Nghỉ 1ms để giảm tải cho CPU
        Sleep(1);
    }

    // GỌI QUA UNPDATE: Giải phóng Hook khi thoát
    StopMonitoring();

    return 0;
}
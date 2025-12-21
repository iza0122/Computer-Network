#pragma once
#include <windows.h>
#include <string>

bool StartMonitoring(HWND hWndReceiver);
std::wstring GetProcessedString(WPARAM wParam);
void SaveToFile(const std::wstring& content);
void StopMonitoring();
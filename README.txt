AutoFillApp – Demo (.NET 8, WinForms + UI Automation)
=====================================================
Yêu cầu: Windows 10/11, đã cài .NET SDK 8 (nếu bạn muốn build). Nếu chưa có, bạn vẫn có thể chạy file .exe sau khi publish self-contained.

Hướng dẫn nhanh:
1) Mở PowerShell tại thư mục này.
2) Chạy: dotnet build
3) Chạy bản debug: bin\Debug\net8.0-windows\AutoFillApp.exe

Đóng gói 1 file .exe (self-contained, không cần cài .NET runtime):
- Chạy file build_publish.bat
  -> File xuất ra: bin\Release\net8.0-windows\win-x64\publish\AutoFillApp.exe

Cách dùng:
- Mở app, nhấn 'Làm mới danh sách', chọn cửa sổ ứng dụng đích.
- Nhấn 'Quét ô nhập' -> bảng bên trái liệt kê các ô (Edit/ComboBox).
- Ở bảng Key/Value bên phải, điền các key và giá trị. Key nên khớp với 'Key gợi ý' hoặc một phần Name/AutomationId.
- Nhấn 'Điền dữ liệu' để auto-fill.
- Có thể 'Lưu profile' và 'Tải profile' (JSON).

Ghi chú quyền hạn:
- Nếu ứng dụng đích chạy 'Run as Administrator' thì bạn cũng nên chạy AutoFillApp.exe bằng quyền Administrator.
- Một số control tùy biến có thể không support UI Automation tiêu chuẩn, khi đó fallback gửi phím có thể hữu ích nhưng không phải lúc nào cũng được.

Bảo mật:
- Không lưu mật khẩu nhạy cảm ở dạng rõ trong profile. Nếu cần, hãy mã hóa hoặc tránh lưu.

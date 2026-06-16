using AppQuanLyBanQuanAo_Nhom02.Data;
using AppQuanLyBanQuanAo_Nhom02.Views.Windows; // Để mở MainWindow
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Text;
using System.Windows;

namespace AppQuanLyBanQuanAo_Nhom02.ViewModels
{
    public partial class LoginViewModel : ObservableObject
    {
        // 1. Biến lưu Tài khoản từ giao diện gửi xuống
        [ObservableProperty]
        private string username = "";

        // 2. Biến lưu Mật khẩu từ giao diện gửi xuống
        [ObservableProperty]
        private string password = "";

        // 3. Biến lưu Thông báo lỗi (nếu đăng nhập sai)
        [ObservableProperty]
        private string errorMessage = "";

        // 4. Lệnh Đăng nhập (Nhận vào tham số là chính Cửa sổ Đăng nhập hiện tại để đóng nó)
        [RelayCommand]
        private void Login(object parameter) 
        {
            ErrorMessage = "";

            // Lấy mật khẩu trực tiếp từ PasswordBox
            var passBox = parameter as System.Windows.Controls.PasswordBox;
            string realPassword = passBox?.Password ?? "";

            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(realPassword))
            {
                ErrorMessage = "Vui lòng nhập đầy đủ tài khoản và mật khẩu!";
                return;
            }

            var dbService = new DatabaseService();
            // Dùng .Trim() gọt sạch khoảng trắng thừa
            var user = dbService.CheckLogin(Username.Trim(), realPassword);

            if (user != null)
            {
                // KIỂM TRA TRẠNG THÁI TÀI KHOẢN (VÔ HIỆU HÓA)
                if (user.TrangThai == 0) // Nếu cột TrangThai trong DB đang là 0 (Đã nghỉ việc)
                {
                    ErrorMessage = "Tài khoản của bạn đã bị vô hiệu hóa.";
                    return; // Dừng lại ngay, tuyệt đối không cho vào form chính
                }

                // Nếu tài khoản hợp lệ và đang hoạt động (TrangThai == true)
                AppSession.CurrentUser = user;

                MainWindow mainWindow = new MainWindow();
                mainWindow.Show();

                // Đóng form hiện tại
                var currentWindow = Window.GetWindow(passBox);
                currentWindow?.Close();
            }
            else
            {
                // Trường hợp gõ sai Username hoặc Password
                ErrorMessage = "Tài khoản hoặc mật khẩu không chính xác!";
            }
        }

        [RelayCommand]
        private void GoToRegister(Window currentWindow)
        {
            // Khởi tạo cửa sổ nhập mã
            Views.Windows.VerifyCodeWindow verifyWin = new Views.Windows.VerifyCodeWindow();
            verifyWin.Show(); // Dùng Show() thay vì ShowDialog()

            // ĐÓNG FORM ĐĂNG NHẬP HIỆN TẠI LẠI
            currentWindow?.Close();
        }
    }
}

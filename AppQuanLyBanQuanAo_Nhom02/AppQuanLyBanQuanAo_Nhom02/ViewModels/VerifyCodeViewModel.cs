using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;
using Microsoft.Data.Sqlite;
using Dapper;
using AppQuanLyBanQuanAo_Nhom02.Data;
using AppQuanLyBanQuanAo_Nhom02.Views.Windows;
using System.Text.RegularExpressions; // Thêm thư viện này để dùng Regex
using System;
namespace AppQuanLyBanQuanAo_Nhom02.ViewModels
{
    public partial class VerifyCodeViewModel : ObservableObject
    {
        [ObservableProperty]
        private string codeInput = "";

        [ObservableProperty]
        private string errorMessage = "";

        [RelayCommand]
        private void Verify(Window currentWindow)
        {
            // Cắt bỏ khoảng trắng dư thừa ở đầu và cuối nếu có
            string code = CodeInput?.Trim() ?? "";

            // 1. Kiểm tra rỗng
            if (string.IsNullOrWhiteSpace(code))
            {
                ErrorMessage = "Vui lòng nhập mã!";
                return;
            }

            // 2. KIỂM TRA ĐỊNH DẠNG: Bắt buộc phải là 6 chữ số (Không chữ cái, không ký tự đặc biệt, không thừa thiếu)
            if (!Regex.IsMatch(code, @"^\d{6}$"))
            {
                ErrorMessage = "Mã xác thực phải được nhập bằng 6 chữ số.";
                return;
            }

            // 3. KIỂM TRA TRONG DATABASE
            try
            {
                using var conn = new SqliteConnection(DatabaseConfig.ConnectionString);

                // Đảm bảo bảng đã tồn tại để chống crash
                conn.Execute("CREATE TABLE IF NOT EXISTS MaXacNhan (MaCode TEXT, NgayTao DATETIME)");

                string sql = "SELECT COUNT(1) FROM MaXacNhan WHERE MaCode = @Code";
                int count = conn.ExecuteScalar<int>(sql, new { Code = code });

                if (count > 0)
                {
                    // MÃ ĐÚNG: Khởi tạo form Đăng ký lớn
                    RegisterWindow regWin = new RegisterWindow();

                    // Bắn ngầm mã vừa nhập vào biến MaXacNhanInput của form Đăng Ký
                    if (regWin.DataContext is RegisterViewModel regVM)
                    {
                        regVM.MaXacNhanInput = code;
                    }

                    regWin.Show(); // Hiện form đăng ký
                    currentWindow?.Close(); // Đóng popup nhập mã
                }
                else
                {
                    ErrorMessage = "Mã không hợp lệ hoặc đã được sử dụng!";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = "Lỗi hệ thống: " + ex.Message;
            }
        }

        [RelayCommand]
        private void Cancel(Window currentWindow)
        {
            // TẠO LẠI FORM ĐĂNG NHẬP TRƯỚC KHI ĐÓNG POPUP
            LoginWindow loginWin = new LoginWindow();
            loginWin.Show();

            // Đóng cửa sổ nhập mã hiện tại
            currentWindow?.Close();
        }
    }
}
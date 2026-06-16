using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;
using AppQuanLyBanQuanAo_Nhom02.Data;
using AppQuanLyBanQuanAo_Nhom02.Views.Windows;
using System.Text.RegularExpressions;
using System;
using Microsoft.Data.Sqlite; // Bắt buộc phải có để xóa mã
using Dapper;                // Bắt buộc phải có để xóa mã

namespace AppQuanLyBanQuanAo_Nhom02.ViewModels
{
    public partial class RegisterViewModel : ObservableObject
    {
        // 1. Dữ liệu người dùng nhập
        [ObservableProperty] private string hoTen = "";
        [ObservableProperty] private string sdt = "";
        [ObservableProperty] private string username = "";
        [ObservableProperty] private string password = "";
        [ObservableProperty] private string confirmPassword = "";

        // === BIẾN QUAN TRỌNG ĐỂ HỨNG MÃ TỪ POPUP CHUYỂN QUA (Giải quyết lỗi CS1061) ===
        [ObservableProperty] private string maXacNhanInput = "";

        // 2. Biến lưu câu thông báo lỗi cho TỪNG Ô
        [ObservableProperty] private string hoTenError = "";
        [ObservableProperty] private string sdtError = "";
        [ObservableProperty] private string usernameError = "";
        [ObservableProperty] private string passwordError = "";
        [ObservableProperty] private string confirmPasswordError = "";

        // 3. Biến lưu màu viền cho TỪNG Ô
        [ObservableProperty] private string hoTenBorder = "#ABADB3";
        [ObservableProperty] private string sdtBorder = "#ABADB3";
        [ObservableProperty] private string usernameBorder = "#ABADB3";
        [ObservableProperty] private string passwordBorder = "#ABADB3";
        [ObservableProperty] private string confirmPasswordBorder = "#ABADB3";

        [RelayCommand]
        private void Register(Window currentWindow)
        {
            // === BƯỚC NÂNG CẤP: GỌT SẠCH KHOẢNG TRẮNG THỪA NGAY TỪ ĐẦU ===
            // Nếu người dùng gõ " admin " -> Sẽ tự động biến thành "admin"
            HoTen = HoTen?.Trim();
            Sdt = Sdt?.Trim();
            Username = Username?.Trim();
            Password = Password?.Trim();
            ConfirmPassword = ConfirmPassword?.Trim();

            var dbService = new DatabaseService();
            ResetErrors();
            bool hasError = false;

            // --- KIỂM TRA HỌ TÊN ---
            if (string.IsNullOrWhiteSpace(HoTen))
            {
                HoTenError = "Vui lòng nhập họ tên!";
                HoTenBorder = "Red";
                hasError = true;
            }
            else
            {
                string nameLower = HoTen.ToLower();
                if (nameLower.Contains("admin"))
                {
                    HoTenError = "Họ tên không được chứa từ 'admin'!";
                    HoTenBorder = "Red";
                    hasError = true;
                }
                else if (Regex.IsMatch(HoTen, @"\d"))
                {
                    HoTenError = "Họ tên không được chứa chữ số.";
                    HoTenBorder = "Red";
                    hasError = true;
                }
                else
                {
                    var words = HoTen.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                    foreach (var word in words)
                    {
                        if (!char.IsUpper(word[0]))
                        {
                            HoTenError = "Viết hoa chữ cái đầu tiên ở mỗi chữ (Ví dụ: Nguyễn Văn A).";
                            HoTenBorder = "Red";
                            hasError = true; break;
                        }
                    }
                }
            }

            // --- KIỂM TRA SỐ ĐIỆN THOẠI ---
            if (string.IsNullOrWhiteSpace(Sdt) || !Regex.IsMatch(Sdt, @"^0\d{9}$"))
            {
                SdtError = "Phải gồm 10 chữ số và bắt đầu bằng số 0.";
                SdtBorder = "Red";
                hasError = true;
            }
            else if (dbService.CheckPhoneExist(Sdt))
            {
                SdtError = "Số điện thoại này đã được đăng ký bởi tài khoản khác!";
                SdtBorder = "Red";
                hasError = true;
            }

            // --- KIỂM TRA TÊN ĐĂNG NHẬP ---
            if (string.IsNullOrWhiteSpace(Username))
            {
                UsernameError = "Vui lòng nhập tên đăng nhập!"; UsernameBorder = "Red"; hasError = true;
            }
            else if (dbService.CheckUsernameExist(Username))
            {
                UsernameError = "Tên tài khoản này đã có người sử dụng!"; UsernameBorder = "Red"; hasError = true;
            }

            // --- KIỂM TRA MẬT KHẨU ---
            if (string.IsNullOrWhiteSpace(Password))
            {
                PasswordError = "Vui lòng đặt mật khẩu cho tài khoản!";
                PasswordBorder = "Red";
                hasError = true;
            }
            else if (Password.Length < 8 ||
                     !Regex.IsMatch(Password, @"[a-z]") || !Regex.IsMatch(Password, @"[A-Z]") ||
                     !Regex.IsMatch(Password, @"\d") || !Regex.IsMatch(Password, @"[!@#$%^&*]"))
            {
                PasswordError = "Tối thiểu 8 ký tự, gồm chữ hoa, chữ thường, số và ký tự đặc biệt.";
                PasswordBorder = "Red";
                hasError = true;
            }

            // --- KIỂM TRA NHẬP LẠI MẬT KHẨU ---
            if (string.IsNullOrWhiteSpace(ConfirmPassword))
            {
                ConfirmPasswordError = "Vui lòng nhập lại mật khẩu cho tài khoản!";
                ConfirmPasswordBorder = "Red";
                hasError = true;
            }
            else if (Password != ConfirmPassword) // So sánh mượt mà nhờ đã Trim() cả 2 bên
            {
                ConfirmPasswordError = "Mật khẩu xác nhận không khớp!";
                ConfirmPasswordBorder = "Red";
                hasError = true;
            }

            // CHỐT HẠ: Nếu có BẤT KỲ lỗi nào thì dừng
            if (hasError) return;

            // NẾU VƯỢT QUA HẾT THÌ LƯU DỮ LIỆU
            bool isSuccess = dbService.RegisterEmployee(HoTen, Sdt, Username, Password);
            if (isSuccess)
            {
                // === BƯỚC BẢO MẬT: TIÊU HỦY MÃ ĐĂNG KÝ ===
                if (!string.IsNullOrWhiteSpace(MaXacNhanInput))
                {
                    using var conn = new SqliteConnection(DatabaseConfig.ConnectionString);
                    conn.Execute("DELETE FROM MaXacNhan WHERE MaCode = @Code", new { Code = MaXacNhanInput });
                }

                MessageBox.Show("Đăng ký thành công! Hệ thống sẽ chuyển về trang đăng nhập.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                GoToLogin(currentWindow);
            }
            else
            {
                MessageBox.Show("Lỗi hệ thống, không thể lưu dữ liệu.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ResetErrors()
        {
            HoTenError = SdtError = UsernameError = PasswordError = ConfirmPasswordError = "";
            HoTenBorder = SdtBorder = UsernameBorder = PasswordBorder = ConfirmPasswordBorder = "#ABADB3";
        }

        [RelayCommand]
        private void GoToLogin(Window currentWindow)
        {
            LoginWindow loginWindow = new LoginWindow();
            loginWindow.Show();
            currentWindow?.Close();
        }
    }
}
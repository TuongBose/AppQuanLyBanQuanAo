using System;
using System.Collections.Generic;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;
using System.Text.RegularExpressions;
using AppQuanLyBanQuanAo_Nhom02.Data;

namespace AppQuanLyBanQuanAo_Nhom02.ViewModels
{
    public partial class ChangePasswordViewModel : ObservableObject
    {
        // 1. Dữ liệu nhập
        [ObservableProperty] private string oldPassword = "";
        [ObservableProperty] private string newPassword = "";
        [ObservableProperty] private string confirmPassword = "";

        // 2. Thông báo lỗi
        [ObservableProperty] private string oldPasswordError = "";
        [ObservableProperty] private string newPasswordError = "";
        [ObservableProperty] private string confirmPasswordError = "";

        // 3. Viền đỏ
        [ObservableProperty] private string oldPasswordBorder = "#ABADB3";
        [ObservableProperty] private string newPasswordBorder = "#ABADB3";
        [ObservableProperty] private string confirmPasswordBorder = "#ABADB3";

        [RelayCommand]
        private void SavePassword(Window currentWindow)
        {
            ResetErrors();
            bool hasError = false;

            // --- KIỂM TRA MẬT KHẨU CŨ ---
            if (string.IsNullOrWhiteSpace(OldPassword))
            {
                OldPasswordError = "Vui lòng nhập mật khẩu hiện tại."; OldPasswordBorder = "Red"; hasError = true;
            }
            // So sánh với thẻ nhớ Session
            else if (AppSession.CurrentUser != null && OldPassword != AppSession.CurrentUser.PasswordHash)
            {
                OldPasswordError = "Mật khẩu hiện tại không chính xác!"; OldPasswordBorder = "Red"; hasError = true;
            }

            // --- KIỂM TRA MẬT KHẨU MỚI ---
            if (string.IsNullOrWhiteSpace(NewPassword))
            {
                NewPasswordError = "Vui lòng nhập mật khẩu mới."; NewPasswordBorder = "Red"; hasError = true;
            }
            else if (NewPassword.Length < 8 ||
                     !Regex.IsMatch(NewPassword, @"[a-z]") || !Regex.IsMatch(NewPassword, @"[A-Z]") ||
                     !Regex.IsMatch(NewPassword, @"\d") || !Regex.IsMatch(NewPassword, @"[!@#$%^&*.?_\-]"))
            {
                NewPasswordError = "Tối thiểu 8 ký tự, gồm chữ hoa, thường, số và ký tự đặc biệt.";
                NewPasswordBorder = "Red";
                hasError = true;
            }
            else if (NewPassword == OldPassword)
            {
                NewPasswordError = "Mật khẩu mới phải khác mật khẩu hiện tại!"; NewPasswordBorder = "Red"; hasError = true;
            }

            // --- KIỂM TRA XÁC NHẬN ---
            if (string.IsNullOrWhiteSpace(ConfirmPassword))
            {
                ConfirmPasswordError = "Vui lòng xác nhận mật khẩu mới."; ConfirmPasswordBorder = "Red"; hasError = true;
            }
            else if (NewPassword != ConfirmPassword)
            {
                ConfirmPasswordError = "Mật khẩu xác nhận không khớp!"; ConfirmPasswordBorder = "Red"; hasError = true;
            }

            if (hasError) return;

            // NẾU HỢP LỆ -> LƯU DATABASE
            var dbService = new DatabaseService();
            if (AppSession.CurrentUser != null)
            {
                bool isSuccess = dbService.UpdatePassword(AppSession.CurrentUser.MaNV, NewPassword);
                if (isSuccess)
                {
                    // Cập nhật lại thẻ nhớ Session để không bị văng lỗi nếu đổi pass liên tục 2 lần
                    AppSession.CurrentUser.PasswordHash = NewPassword;

                    MessageBox.Show("Đổi mật khẩu thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    currentWindow?.Close();
                }
                else
                {
                    MessageBox.Show("Lỗi hệ thống, không thể cập nhật mật khẩu.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        [RelayCommand]
        private void Cancel(Window currentWindow)
        {
            currentWindow?.Close();
        }

        private void ResetErrors()
        {
            OldPasswordError = NewPasswordError = ConfirmPasswordError = "";
            OldPasswordBorder = NewPasswordBorder = ConfirmPasswordBorder = "#ABADB3";
        }
    }
}

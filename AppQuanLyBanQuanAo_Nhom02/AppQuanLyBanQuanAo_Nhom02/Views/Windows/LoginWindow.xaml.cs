using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace AppQuanLyBanQuanAo_Nhom02.Views.Windows
{
    /// <summary>
    /// Interaction logic for LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
        }

        // HÀM XỬ LÝ NHẢY Ô KHI BẤM ENTER
        private void FocusNext_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                // Ra lệnh cho con trỏ chuột nhảy sang phần tử tiếp theo (giống như bấm Tab)
                TraversalRequest request = new TraversalRequest(FocusNavigationDirection.Next);
                ((UIElement)sender).MoveFocus(request);

                // Báo cho hệ thống biết "Tôi đã xử lý phím Enter rồi, đừng tự động bấm nút Đăng nhập nữa"
                e.Handled = true;
            }
        }

        // Biến cờ hiệu để tránh việc 2 ô copy qua lại vô tận
        private bool _isSyncing = false;

        // Xử lý sự kiện khi bấm nút "Con mắt"
        private void btnShowPassword_Click(object sender, RoutedEventArgs e)
        {
            if (btnShowPassword.IsChecked == true)
            {
                // Bật chế độ HIỆN mật khẩu
                txtVisiblePassword.Visibility = Visibility.Visible;
                txtPassword.Visibility = Visibility.Collapsed;

                // Chuyển con trỏ chuột sang ô TextBox
                txtVisiblePassword.Focus();
                txtVisiblePassword.CaretIndex = txtVisiblePassword.Text.Length;
            }
            else
            {
                // Bật chế độ ẨN mật khẩu
                txtPassword.Visibility = Visibility.Visible;
                txtVisiblePassword.Visibility = Visibility.Collapsed;

                // Chuyển con trỏ chuột sang ô PasswordBox
                txtPassword.Focus();
            }
        }

        // Khi người dùng gõ vào ô ẨN, chép dữ liệu sang ô HIỆN
        private void txtPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (!_isSyncing)
            {
                _isSyncing = true;
                txtVisiblePassword.Text = txtPassword.Password;
                _isSyncing = false;
            }
        }

        // Khi người dùng gõ vào ô HIỆN, chép dữ liệu về lại ô ẨN
        private void txtVisiblePassword_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (!_isSyncing)
            {
                _isSyncing = true;
                txtPassword.Password = txtVisiblePassword.Text;
                _isSyncing = false;
            }
        }
    }
}

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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AppQuanLyBanQuanAo_Nhom02.Views.UserControls
{
    /// <summary>
    /// Interaction logic for HoaDonView.xaml
    /// </summary>
    public partial class HoaDonView : UserControl
    {
        public HoaDonView()
        {
            InitializeComponent();

            // Lắng nghe lệnh từ ViewModel để tự động trỏ chuột vào ô SĐT
            this.Loaded += (s, e) =>
            {
                if (this.DataContext is ViewModels.HoaDonViewModel vm)
                {
                    vm.FocusKhachHangAction = () =>
                    {
                        txtSearchKH.Focus();
                    };
                }
            };
        }

        // Hàm 1: Chặn người dùng gõ chữ cái vào ô Số lượng (Chỉ cho phép gõ số)
        private void NumberValidationTextBox(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        // Hàm 2: Tự động chạy lệnh cập nhật lại Tổng Tiền khi ô nhập liệu mất tiêu điểm (chuột click ra ngoài)
        private void txtSoLuong_LostFocus(object sender, System.Windows.RoutedEventArgs e)
        {
            if (this.DataContext is AppQuanLyBanQuanAo_Nhom02.ViewModels.HoaDonViewModel vm &&
                sender is System.Windows.Controls.TextBox txt &&
                txt.DataContext is AppQuanLyBanQuanAo_Nhom02.Models.ChiTietHoaDon item)
            {
                vm.CapNhatSoLuongNhapCommand.Execute(item);
            }
        }
    }
}

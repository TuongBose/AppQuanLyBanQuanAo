using AppQuanLyBanQuanAo_Nhom02.ViewModels;
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
    public partial class TrangChuView : UserControl
    {
        public TrangChuView()
        {
            InitializeComponent();
        }

        // HÀM MỚI BỔ SUNG: Bắt sự kiện mỗi khi Form Trang chủ xuất hiện trên màn hình
        private async void UserControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // Kiểm tra: Nếu form đang được hiển thị (NewValue == true)
            if ((bool)e.NewValue == true)
            {
                // Lấy ViewModel hiện tại ra và ép nó gọi lệnh tải lại dữ liệu từ CSDL
                if (this.DataContext is TrangChuViewModel vm)
                {
                    await vm.ReloadDataAsync();
                }
            }
        }
    }
}
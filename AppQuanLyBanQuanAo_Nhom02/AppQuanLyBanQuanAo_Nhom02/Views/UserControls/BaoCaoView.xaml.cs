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
    /// <summary>
    /// Interaction logic for BaoCaoView.xaml
    /// </summary>
    public partial class BaoCaoView : UserControl
    {
        public BaoCaoView()
        {
            InitializeComponent();

            // THÊM SỰ KIỆN LOADED: 
            // Mỗi khi mở tab này lên, tự động gọi lệnh Tải Dữ Liệu ngầm
            this.Loaded += (s, e) =>
            {
                if (this.DataContext is BaoCaoViewModel vm)
                {
                    // Chạy lệnh tải lại dữ liệu mà không làm mất khoảng thời gian đang lọc
                    vm.TaiDuLieuCommand.Execute(null);
                }
            };
        }
    }
}

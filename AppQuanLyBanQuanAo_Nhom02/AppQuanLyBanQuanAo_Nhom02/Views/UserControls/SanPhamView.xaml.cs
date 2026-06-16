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
using System.Text.RegularExpressions;

namespace AppQuanLyBanQuanAo_Nhom02.Views.UserControls
{
    /// <summary>
    /// Interaction logic for SanPhamView.xaml
    /// </summary>
    public partial class SanPhamView : UserControl
    {
        public SanPhamView()
        {
            InitializeComponent();
        }

        // === HÀM LỌC 1: CHỈ CHO PHÉP NHẬP SỐ VÀ HIỆN THÔNG BÁO ===
        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");

            // Nếu phát hiện ký tự vừa gõ là chữ cái hoặc ký tự đặc biệt
            if (regex.IsMatch(e.Text))
            {
                e.Handled = true; // Chặn không cho ký tự đó in vào ô nhập liệu
            }
        }

        // === HÀM LỌC 2: CHẶN DẤU CÁCH (SPACE) ===
        private void SpaceValidationTextBox(object sender, KeyEventArgs e)
        {
            // Bắt sự kiện nếu người dùng gõ phím Space
            if (e.Key == Key.Space)
            {
                e.Handled = true; // Hủy lệnh gõ
                MessageBox.Show("Mục này chỉ được phép nhập số!\nVui lòng không nhập khoảng trắng (Space).",
                                "Cảnh báo nhập liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // === HÀM LỌC 3: CHẶN DÁN (PASTE) DỮ LIỆU BẨN ===
        private void TextBox_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            // Kiểm tra xem dữ liệu chuẩn bị dán vào có phải là dạng chuỗi chữ không
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string text = (string)e.DataObject.GetData(typeof(string));
                Regex regex = new Regex("[^0-9]+");

                // Nếu đoạn chữ định dán chứa chữ cái hoặc ký tự đặc biệt
                if (regex.IsMatch(text))
                {
                    e.CancelCommand(); // Hủy lệnh dán ngay lập tức

                    // SỬ DỤNG DISPATCHER ĐỂ HIỆN MESSAGEBOX TRÁNH CRASH APP
                    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        MessageBox.Show("Bạn không được dán dữ liệu có chứa chữ cái hoặc ký tự đặc biệt!",
                                        "Cảnh báo dán dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }));
                }
            }
            else
            {
                e.CancelCommand(); // Không cho dán hình ảnh hay các định dạng khác vào ô số
            }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }
}

using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AppQuanLyBanQuanAo_Nhom02.Views.UserControls
{
    public partial class KhachHangView : UserControl
    {
        public KhachHangView()
        {
            InitializeComponent();

            DataObject.AddPastingHandler(txtSDT, TextBoxPasting_SDT);
            DataObject.AddPastingHandler(txtTenKH, TextBoxPasting_TenKH);
        }

        // ==========================================
        // 1. XỬ LÝ Ô NHẬP TÊN KHÁCH HÀNG
        // ==========================================
        private void txtTenKH_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!e.Text.All(c => char.IsLetter(c) || char.IsWhiteSpace(c)))
            {
                e.Handled = true;
            }
        }

        private void txtTenKH_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                string cleanText = new string(textBox.Text.Where(c => char.IsLetter(c) || char.IsWhiteSpace(c)).ToArray());

                if (textBox.Text != cleanText)
                {
                    int caret = textBox.CaretIndex;
                    textBox.TextChanged -= txtTenKH_TextChanged;
                    textBox.Text = cleanText;
                    textBox.CaretIndex = Math.Max(0, caret - 1);
                    textBox.TextChanged += txtTenKH_TextChanged;
                }
            }
        }

        private void txtTenKH_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && !string.IsNullOrWhiteSpace(textBox.Text))
            {
                var culture = new System.Globalization.CultureInfo("vi-VN");
                textBox.Text = culture.TextInfo.ToTitleCase(textBox.Text.ToLower());
            }
        }

        private void TextBoxPasting_TenKH(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string text = (string)e.DataObject.GetData(typeof(string));
                if (!text.All(c => char.IsLetter(c) || char.IsWhiteSpace(c)))
                    e.CancelCommand();
            }
            else e.CancelCommand();
        }

        // ==========================================
        // 2. XỬ LÝ Ô NHẬP SỐ ĐIỆN THOẠI & ĐIỂM TÍCH LŨY
        // ==========================================
        private void txtSDT_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!e.Text.All(char.IsDigit))
            {
                e.Handled = true;
            }
        }

        private void txtSDT_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox && !string.IsNullOrEmpty(textBox.Text))
            {
                string cleanText = new string(textBox.Text.Where(char.IsDigit).ToArray());

                if (textBox.Text != cleanText)
                {
                    int caret = textBox.CaretIndex;
                    textBox.TextChanged -= txtSDT_TextChanged;
                    textBox.Text = cleanText;
                    textBox.CaretIndex = Math.Max(0, caret - 1);
                    textBox.TextChanged += txtSDT_TextChanged;
                }
            }
        }

        private void TextBoxPasting_SDT(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string text = (string)e.DataObject.GetData(typeof(string));
                if (!text.All(char.IsDigit))
                    e.CancelCommand();
            }
            else e.CancelCommand();
        }

        // ==========================================
        // 3. XỬ LÝ NHẢY Ô BẰNG PHÍM ENTER VÀ CHẶN DẤU CÁCH
        // ==========================================
        private void txtTenKH_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true; // Chặn tiếng "Bíp" của Windows
                txtSDT.Focus();   // Nhảy sang ô SDT
                txtSDT.CaretIndex = txtSDT.Text.Length; // Ép con trỏ về cuối chuỗi
            }
        }

        private void txtSDT_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                e.Handled = true; // Chặn phím cách
            }
            else if (e.Key == Key.Enter)
            {
                e.Handled = true;
                txtDiem.Focus();  // Nhảy sang ô Điểm
                txtDiem.CaretIndex = txtDiem.Text.Length; // Ép con trỏ về cuối chuỗi
            }
        }

        private void txtDiem_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                e.Handled = true; // Chặn phím cách
            }
            else if (e.Key == Key.Enter)
            {
                e.Handled = true;

                // Kích hoạt thẳng lệnh Thêm Khách Hàng (Tương đương với việc lấy chuột click vào nút)
                if (btnThem.Command != null && btnThem.Command.CanExecute(null))
                {
                    btnThem.Command.Execute(null);
                }
            }
        }
    }
}
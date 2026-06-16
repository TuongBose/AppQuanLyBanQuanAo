using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
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
    /// Interaction logic for NhapHangView.xaml
    /// </summary>
    public partial class NhapHangView : UserControl
    {
        public NhapHangView()
        {
            InitializeComponent();
        }

        // 1. Chặn nhập chữ cái và ký tự đặc biệt (Chỉ cho phép 0-9)
        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        // 2. Chặn ấn phím Dấu Cách (Space)
        private void SpaceValidationTextBox(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                e.Handled = true;
            }
        }

        // 3. Chặn Paste (Dán) đoạn chữ có chứa ký tự không phải số
        private void TextBox_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string text = (string)e.DataObject.GetData(typeof(string));
                if (!Regex.IsMatch(text, "^[0-9]+$"))
                {
                    e.CancelCommand(); // Hủy lệnh dán
                }
            }
            else
            {
                e.CancelCommand();
            }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }
}

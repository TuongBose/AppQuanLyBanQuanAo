using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace AppQuanLyBanQuanAo_Nhom02.Untils.Converters
{
    public class RoleToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Trường hợp 1: Nếu trong ViewModel bạn dùng biến bool (VD: public bool IsAdmin)
            // true = Admin (Hiện nút), false = User (Ẩn nút)
            if (value is bool isAdmin)
            {
                return isAdmin ? Visibility.Visible : Visibility.Collapsed;
            }

            // Trường hợp 2: Nếu trong ViewModel bạn dùng biến số nguyên (VD: public int Role)
            // Giả sử 1 = Admin (Hiện nút), 0 = User (Ẩn nút)
            if (value is int role)
            {
                return role == 1 ? Visibility.Visible : Visibility.Collapsed;
            }

            // Mặc định nếu không đúng định dạng thì sẽ ẩn đi cho an toàn
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // ConvertBack thường không cần dùng trong trường hợp ẩn/hiện nút một chiều
            throw new NotImplementedException();
        }
    }
}
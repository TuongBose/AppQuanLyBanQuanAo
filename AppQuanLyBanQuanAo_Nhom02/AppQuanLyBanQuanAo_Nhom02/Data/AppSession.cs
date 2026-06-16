using System;
using System.Collections.Generic;
using System.Text;
using AppQuanLyBanQuanAo_Nhom02.Models;

namespace AppQuanLyBanQuanAo_Nhom02.Data
{
    public static class AppSession
    {
        // Biến này sẽ lưu toàn bộ thông tin của người đang đăng nhập
        public static NhanVien? CurrentUser { get; set; }
    }
}

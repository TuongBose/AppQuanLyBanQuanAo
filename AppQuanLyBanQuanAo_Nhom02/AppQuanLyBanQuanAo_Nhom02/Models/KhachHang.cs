using System;
using System.Collections.Generic;
using System.Text;

namespace AppQuanLyBanQuanAo_Nhom02.Models
{
    public class KhachHang
    {
        public int STT { get; set; }
        public int MaKH { get; set; }
        // Thuộc tính hiển thị: Tự động biến 1 thành "KH001"
        public string MaKH_Display => $"KH{MaKH:D3}";

        public string TenKH { get; set; }
        public string SDT { get; set; }
        public int DiemTichLuy { get; set; }
    }
}

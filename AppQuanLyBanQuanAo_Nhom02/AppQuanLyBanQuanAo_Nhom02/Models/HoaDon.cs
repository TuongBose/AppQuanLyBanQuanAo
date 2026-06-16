using System;
using System.Collections.Generic;
using System.Text;

namespace AppQuanLyBanQuanAo_Nhom02.Models
{
    public class HoaDon
    {
        public int STT { get; set; }
        public int MaHD { get; set; }
        // Thuộc tính hiển thị: Tự động biến 1 thành "HD001"
        public string MaHD_Display => $"HD{MaHD:D3}";

        public int MaNV { get; set; }
        public string MaNV_Display => $"NV{MaNV:D3}";

        public int MaKH { get; set; }
        public string MaKH_Display => $"KH{MaKH:D3}";

        public string NgayLap { get; set; }
        public int TongTien { get; set; }
    }
}

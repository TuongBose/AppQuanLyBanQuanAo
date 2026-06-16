using System;
using System.Collections.Generic;
using System.Text;

namespace AppQuanLyBanQuanAo_Nhom02.Models
{
    public class ChiTietPhieuNhap
    {
        public int STT { get; set; }
        public string TenSP { get; set; }
        public string MauSac { get; set; } 
        public string Size { get; set; }  
        public int MaPN { get; set; }
        public string MaPN_Display => $"PN{MaPN:D3}";

        public int MaSP { get; set; }
        public string MaSP_Display => $"SP{MaSP:D3}";

        public int SoLuongNhap { get; set; }
        public int GiaNhap { get; set; }
        public int ThanhTien => SoLuongNhap * GiaNhap;
    }
}

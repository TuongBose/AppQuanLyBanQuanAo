using System;
using System.Collections.Generic;
using System.Text;

namespace AppQuanLyBanQuanAo_Nhom02.Models
{
    public class ChiTietHoaDon
    {
        public int MaHD { get; set; }
        public string MaHD_Display => $"HD{MaHD:D3}";

        public int MaSP { get; set; }
        public string MaSP_Display => $"SP{MaSP:D3}";

        public int SoLuong { get; set; }
        public int DonGia { get; set; }


        // --- THÊM 2 THUỘC TÍNH NÀY ĐỂ HIỂN THỊ LÊN GIAO DIỆN ---
        // (Chúng sẽ không được lưu xuống Database mà chỉ dùng để hiển thị hóa đơn)
        public string TenSP { get; set; } = "";
        public int ThanhTien { get; set; }
    }
}

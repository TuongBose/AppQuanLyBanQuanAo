using System;
using System.Collections.Generic;
using System.Text;

namespace AppQuanLyBanQuanAo_Nhom02.Models
{
    public class LoaiSanPham
    {
        public int MaLoai { get; set; }
        public string TenLoai { get; set; }

        // BIẾN NÀY CHỈ DÙNG ĐỂ HIỂN THỊ, KHÔNG LƯU XUỐNG DB
        public int STT { get; set; }

        // Biến này chỉ dùng để hiển thị lên bảng, không lưu xuống DB
        public int SoLuongSanPham { get; set; }

        // BỔ SUNG ĐOẠN NÀY ĐỂ HIỂN THỊ MÃ ĐẸP MẮT
        public string MaHienThi
        {
            get { return "LSP" + MaLoai.ToString("D2"); }
        }
    }
}

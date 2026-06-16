using System;
using System.Collections.Generic;
using System.Text;

namespace AppQuanLyBanQuanAo_Nhom02.Models
{
    public class SanPham
    {
        public int MaSP { get; set; }
        public string TenSP { get; set; }
        public int? GiaBan { get; set; }
        public int? SoLuongTon { get; set; }
        public string Size { get; set; }
        public string MauSac { get; set; }
        public int MaLoai { get; set; }
        public string HinhAnh { get; set; }

        // --- 3 BIẾN NÀY CHỈ DÙNG ĐỂ HIỂN THỊ LÊN GIAO DIỆN (UI) ---
        public int STT { get; set; }

        // Thuộc tính tự động định dạng hiển thị thông minh
        public string TenHienThi
        {
            get
            {
                // Kiểm tra xem có dữ liệu Màu và Size hay không
                bool coMau = !string.IsNullOrWhiteSpace(MauSac);
                bool coSize = !string.IsNullOrWhiteSpace(Size);

                // Trường hợp 1: Có cả Màu và Size
                if (coMau && coSize)
                    return $"{TenSP} ({MauSac} - Size {Size})";

                // Trường hợp 2: Chỉ có Màu, không có Size
                else if (coMau)
                    return $"{TenSP} ({MauSac})";

                // Trường hợp 3: Chỉ có Size, không có Màu
                else if (coSize)
                    return $"{TenSP} (Size {Size})";

                // Trường hợp 4: Không có cả Màu và Size
                else
                    return TenSP;
            }
        }

        public string MaHienThi
        {
            get { return "SP" + MaSP.ToString("D3"); } // Hiển thị SP001, SP002...
        }

        public string TenLoai { get; set; } // Hứng dữ liệu Tên loại từ câu lệnh JOIN
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace AppQuanLyBanQuanAo_Nhom02.Models
{
    public class NhanVien
    {
        public int MaNV { get; set; }
        // Thuộc tính hiển thị: Tự động biến 1 thành "NV001"
        public string MaNV_Display => $"NV{MaNV:D3}";

        public string TenNV { get; set; }
        public string SDT { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public int Role { get; set; } // 1: Admin, 0: Nhân viên
        public int TrangThai { get; set; } // 1: Đang làm việc, 0: Đã nghỉ

        public string MaHienThi
        {
            get
            {
                // Nếu là Admin (Role = 1) thì format thành QL + 2 chữ số (QL01, QL02...)
                if (Role == 1) return "QL" + MaNV.ToString("D2");

                // Nếu là Nhân viên (Role = 0) thì format thành NV + 3 chữ số (NV001, NV002...)
                return "NV" + MaNV.ToString("D3");
            }
        }

        public string QuyenHienThi
        {
            get
            {
                // Biến số 0, 1 thành chữ cho dễ đọc
                return Role == 1 ? "Quản lý" : "Nhân viên";
            }
        }
    }
}

using AppQuanLyBanQuanAo_Nhom02.Data;
using AppQuanLyBanQuanAo_Nhom02.Models;
using AppQuanLyBanQuanAo_Nhom02.Untils;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Dapper;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace AppQuanLyBanQuanAo_Nhom02.ViewModels
{
    public partial class HoaDonViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<SanPham> danhSachSanPham = new();

        [ObservableProperty]
        private ObservableCollection<ChiTietHoaDon> gioHang = new();

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ThanhToanCommand))]
        private int tongTien;

        [ObservableProperty]
        private string searchKeyword = "";

        public Action? FocusKhachHangAction { get; set; } // Hành động đưa con trỏ chuột

        partial void OnSearchKeywordChanged(string value)
        {
            LoadKhoHang(value);
        }

        [ObservableProperty]
        private SanPham? sanPhamDuocChon;

        partial void OnSanPhamDuocChonChanged(SanPham? value)
        {
            if (value != null)
            {
                ThemVaoGioHang(value);
            }
        }

        // ==========================================
        // CÁC BIẾN MỚI CHO TÍNH NĂNG KHÁCH HÀNG
        // ==========================================
        [ObservableProperty]
        private string searchKhachHangKeyword = "";

        [ObservableProperty]
        private KhachHang? khachHangHienTai; // Lưu trữ khách hàng tìm được để đưa vào hóa đơn

        public HoaDonViewModel()
        {
            LoadKhoHang("");

            // ĐĂNG KÝ LẮNG NGHE: Nếu có ai đó báo kho hàng thay đổi, tự động tải lại danh sách!
            WeakReferenceMessenger.Default.Register<KhoHangChangedMessage>(this, (recipient, message) =>
            {
                LoadKhoHang(SearchKeyword);
            });
        }

        private void LoadKhoHang(string keyword)
        {
            using var conn = new SqliteConnection(DatabaseConfig.ConnectionString);
            string sql = "SELECT * FROM SanPham";
            var sps = conn.Query<SanPham>(sql).ToList();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                string keyLower = keyword.Trim().ToLower();
                sps = sps.Where(sp =>
                    (sp.TenSP != null && sp.TenSP.ToLower().Contains(keyLower)) ||
                    sp.MaSP.ToString().Contains(keyLower)
                ).ToList();
            }

            for (int i = 0; i < sps.Count; i++) sps[i].STT = i + 1;

            DanhSachSanPham = new ObservableCollection<SanPham>(sps);
        }

        private void ThemVaoGioHang(SanPham sp)
        {
            int tonKho = sp.SoLuongTon ?? 0;
            if (tonKho <= 0)
            {
                MessageBox.Show("Sản phẩm này đã hết hàng!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var itemTrongGio = GioHang.FirstOrDefault(x => x.MaSP == sp.MaSP);
            if (itemTrongGio != null)
            {
                MessageBox.Show("Sản phẩm đã có trong giỏ.\nVui lòng dùng nút [+] hoặc [-] hoặc nhập liệu bên bảng phải để chỉnh số lượng!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            else
            {
                string tenHienThi = sp.TenSP ?? "";
                if (!string.IsNullOrWhiteSpace(sp.MauSac)) tenHienThi += $" - Màu: {sp.MauSac.Trim()}";
                if (!string.IsNullOrWhiteSpace(sp.Size)) tenHienThi += $" - Size: {sp.Size.Trim()}";

                GioHang.Add(new ChiTietHoaDon
                {
                    MaSP = sp.MaSP,
                    TenSP = tenHienThi,
                    SoLuong = 1,
                    DonGia = sp.GiaBan ?? 0,
                    ThanhTien = sp.GiaBan ?? 0
                });
            }
            LamMoiGioHang();
        }

        [RelayCommand]
        private void TangSoLuong(ChiTietHoaDon item)
        {
            if (item == null) return;
            using var conn = new SqliteConnection(DatabaseConfig.ConnectionString);
            int tonKhoHienTai = conn.ExecuteScalar<int>("SELECT SoLuongTon FROM SanPham WHERE MaSP = @MaSP", new { MaSP = item.MaSP });

            if (item.SoLuong >= tonKhoHienTai)
            {
                MessageBox.Show("Đã đạt giới hạn số lượng tồn kho!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            item.SoLuong += 1;
            item.ThanhTien = item.SoLuong * item.DonGia;
            LamMoiGioHang();
        }

        [RelayCommand]
        private void CapNhatSoLuongNhap(ChiTietHoaDon item)
        {
            if (item == null) return;

            using var conn = new SqliteConnection(DatabaseConfig.ConnectionString);
            int tonKhoHienTai = conn.ExecuteScalar<int>("SELECT SoLuongTon FROM SanPham WHERE MaSP = @MaSP", new { MaSP = item.MaSP });

            // 1. Nếu nhân viên gõ số lượng lớn hơn trong kho -> Tự động ép về mức tối đa của kho
            if (item.SoLuong > tonKhoHienTai)
            {
                MessageBox.Show($"Kho chỉ còn {tonKhoHienTai} sản phẩm này!", "Vượt quá tồn kho", MessageBoxButton.OK, MessageBoxImage.Warning);
                item.SoLuong = tonKhoHienTai;
            }
            // 2. Không cho phép gõ số âm hoặc số 0. Bét nhất là 1 (nếu muốn xóa thì bấm icon thùng rác)
            else if (item.SoLuong <= 0)
            {
                item.SoLuong = 1;
            }

            // 3. Tính toán lại tiền và làm mới UI
            item.ThanhTien = item.SoLuong * item.DonGia;
            LamMoiGioHang();
        }

        [RelayCommand]
        private void GiamSoLuong(ChiTietHoaDon item)
        {
            if (item == null) return;
            if (item.SoLuong > 1)
            {
                item.SoLuong -= 1;
                item.ThanhTien = item.SoLuong * item.DonGia;
            }
            else GioHang.Remove(item);
            LamMoiGioHang();
        }

        [RelayCommand]
        private void XoaMon(ChiTietHoaDon item)
        {
            if (item != null) { GioHang.Remove(item); LamMoiGioHang(); }
        }

        private void LamMoiGioHang()
        {
            var temp = GioHang.ToList();
            GioHang.Clear();
            foreach (var i in temp) GioHang.Add(i);
            TongTien = GioHang.Sum(x => x.ThanhTien);
        }

        // ==========================================
        // LỆNH TÌM KIẾM KHÁCH HÀNG (MỚI)
        // ==========================================
        [RelayCommand]
        private void TimKhachHang()
        {
            if (string.IsNullOrWhiteSpace(SearchKhachHangKeyword))
            {
                MessageBox.Show("Vui lòng nhập số điện thoại để tìm khách hàng!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using var conn = new SqliteConnection(DatabaseConfig.ConnectionString);
            // Tìm khách hàng theo Số điện thoại
            var kh = conn.QueryFirstOrDefault<KhachHang>("SELECT * FROM KhachHang WHERE SDT = @SDT", new { SDT = SearchKhachHangKeyword.Trim() });

            if (kh != null)
            {
                KhachHangHienTai = kh; // Tìm thấy -> Gán vào để UI hiển thị
            }
            else
            {
                KhachHangHienTai = null;
                MessageBox.Show("Không tìm thấy khách hàng nào với số điện thoại này!\nVui lòng sang tab Quản lý khách hàng để thêm mới.", "Khách hàng không tồn tại", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // ==========================================
        // LỆNH LÀM MỚI FORM (HỦY ĐƠN ĐANG TẠO)
        // ==========================================
        [RelayCommand]
        private void LamMoi()
        {
            // Hỏi xác nhận trước khi xóa sạch giỏ hàng
            if (GioHang.Count > 0)
            {
                var result = MessageBox.Show("Bạn có chắc chắn muốn hủy đơn hàng hiện tại và làm mới form không?", "Xác nhận làm mới", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result != MessageBoxResult.Yes) return;
            }

            // Dọn dẹp toàn bộ dữ liệu
            GioHang.Clear();
            TongTien = 0;
            KhachHangHienTai = null;
            SearchKhachHangKeyword = "";
            SearchKeyword = ""; // Xóa cả ô tìm kiếm sản phẩm
            LoadKhoHang("");    // Tải lại toàn bộ danh sách sản phẩm
        }

        // ==========================================
        // LỆNH THANH TOÁN (NÂNG CẤP TÍCH ĐIỂM)
        // ==========================================
        private bool CanThanhToan() => GioHang.Count > 0;

        [RelayCommand(CanExecute = nameof(CanThanhToan))]
        private void ThanhToan()
        {
            int? maKhachHangThuTe = KhachHangHienTai?.MaKH;
            string tenKhachHienThi = KhachHangHienTai?.TenKH ?? "Khách vãng lai";
            string sdtHienThi = KhachHangHienTai?.SDT ?? "Không có";
            bool laKhachVangLai = false; // Biến đánh dấu khách không tích điểm

            if (KhachHangHienTai == null)
            {
                var result = MessageBox.Show("Khách hàng có muốn tích điểm không?", "Xác nhận tích điểm", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    FocusKhachHangAction?.Invoke(); // Giao diện XAML sẽ tự bắt sự kiện sáng viền
                    return;
                }
                else if (result == MessageBoxResult.No)
                {
                    laKhachVangLai = true; // Xác nhận thanh toán kiểu vãng lai
                }
                else
                {
                    return; // Nhấn Cancel (dấu X) thì hủy thao tác
                }
            }

            // === BẮT ĐẦU GIAO DỊCH LƯU DATABASE ===
            using var conn = new SqliteConnection(DatabaseConfig.ConnectionString);
            conn.Open();
            using var transaction = conn.BeginTransaction();
            try
            {
                // ==========================================================
                // 1. TỰ ĐỘNG XỬ LÝ KHÁCH VÃNG LAI (Chống lỗi FOREIGN KEY)
                // ==========================================================
                if (laKhachVangLai)
                {
                    // Tìm trong DB xem đã có "Khách vãng lai" chưa
                    var maKVL = conn.QueryFirstOrDefault<int?>("SELECT MaKH FROM KhachHang WHERE TenKH = 'Khách vãng lai'", null, transaction);

                    // Nếu chưa có, tự động tạo mới luôn một người tên "Khách vãng lai"
                    if (maKVL == null || maKVL == 0)
                    {
                        maKVL = conn.ExecuteScalar<int>("INSERT INTO KhachHang (TenKH, SDT, DiemTichLuy) VALUES ('Khách vãng lai', '0000000000', 0); SELECT last_insert_rowid();", null, transaction);
                    }
                    maKhachHangThuTe = maKVL; // Gán ID thực tế an toàn 100%
                }

                // ==========================================================
                // 2. TỰ ĐỘNG XỬ LÝ NHÂN VIÊN LẬP ĐƠN (Chống lỗi lúc test code)
                // ==========================================================
                int maNhanVien = AppSession.CurrentUser?.MaNV ?? 0;
                var nvTonTai = conn.QueryFirstOrDefault<int?>("SELECT MaNV FROM NhanVien WHERE MaNV = @MaNV", new { MaNV = maNhanVien }, transaction);
                if (nvTonTai == null || nvTonTai == 0)
                {
                    // Nếu chưa đăng nhập, tự động lấy đại người đầu tiên trong danh sách nhân viên để test
                    maNhanVien = conn.QueryFirstOrDefault<int>("SELECT MaNV FROM NhanVien LIMIT 1", null, transaction);
                }
                string tenNhanVien = AppSession.CurrentUser?.TenNV ?? "Nhân viên Test";

                // ==========================================================
                // 3. LƯU HÓA ĐƠN VÀ TRỪ TỒN KHO
                // ==========================================================
                string insertHDSql = "INSERT INTO HoaDon (MaKH, MaNV, NgayLap, TongTien) VALUES (@MaKH, @MaNV, datetime('now', 'localtime'), @TongTien); SELECT last_insert_rowid();";
                long idHD = conn.ExecuteScalar<long>(insertHDSql, new { MaKH = maKhachHangThuTe, MaNV = maNhanVien, TongTien = TongTien }, transaction);

                string insertCT = "INSERT INTO ChiTietHoaDon (MaHD, MaSP, SoLuong, DonGia) VALUES (@MaHD, @MaSP, @SoLuong, @DonGia)";
                string updateKho = "UPDATE SanPham SET SoLuongTon = SoLuongTon - @SoLuong WHERE MaSP = @MaSP";

                foreach (var item in GioHang)
                {
                    conn.Execute(insertCT, new { MaHD = idHD, MaSP = item.MaSP, SoLuong = item.SoLuong, DonGia = item.DonGia }, transaction);
                    conn.Execute(updateKho, new { SoLuong = item.SoLuong, MaSP = item.MaSP }, transaction);
                }

                // ==========================================================
                // 4. TÍCH ĐIỂM (Chỉ cộng nếu CÓ khách hàng thật)
                // ==========================================================
                if (!laKhachVangLai && maKhachHangThuTe != null)
                {
                    string capNhatDiem = "UPDATE KhachHang SET DiemTichLuy = IFNULL(DiemTichLuy, 0) + 1 WHERE MaKH = @MaKH";
                    conn.Execute(capNhatDiem, new { MaKH = maKhachHangThuTe }, transaction);
                }

                transaction.Commit();

                // ==========================================================
                // 5. IN BILL CHUYÊN NGHIỆP RA MÀN HÌNH
                // ==========================================================
                System.Text.StringBuilder bill = new System.Text.StringBuilder();
                bill.AppendLine("========================================");
                bill.AppendLine("                                       SHOP HUIT02         ");
                bill.AppendLine("Đ/c: 140 Lê Trọng Tấn, P.Tây Thạnh, Q.Tân Phú, TP.HCM   ");
                bill.AppendLine("========================================");
                bill.AppendLine($"Mã Hóa Đơn: HD{idHD:D4}");
                bill.AppendLine($"Ngày lập:   {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
                bill.AppendLine($"Nhân viên:  {tenNhanVien}");
                bill.AppendLine($"Khách hàng: {tenKhachHienThi}");
                if (sdtHienThi != "Không có") bill.AppendLine($"SĐT Khách:  {sdtHienThi}");
                bill.AppendLine("----------------------------------------");

                foreach (var item in GioHang)
                {
                    // Đã bỏ hàm cắt chuỗi (Substring) để tên sản phẩm, màu, size hiện đầy đủ 100%
                    bill.AppendLine($"- {item.TenSP}");

                    // Dàn lại dòng tính tiền cho rõ ràng: Số lượng | Đơn giá | Thành tiền
                    bill.AppendLine($"    SL: {item.SoLuong}  |  Giá: {item.DonGia:N0}đ  |  Tổng: {item.ThanhTien:N0}đ");
                }

                bill.AppendLine("----------------------------------------");
                bill.AppendLine($"TỔNG CỘNG:               {TongTien:N0} VNĐ");
                if (!laKhachVangLai && maKhachHangThuTe != null) bill.AppendLine($"(Khách hàng đã được tích thêm 1 điểm)");
                bill.AppendLine("========================================");
                bill.AppendLine("                       CẢM ƠN QUÝ KHÁCH & HẸN GẶP LẠI!    ");

                MessageBox.Show(bill.ToString(), "HÓA ĐƠN BÁN HÀNG", MessageBoxButton.OK, MessageBoxImage.None);

                // Khôi phục form về trạng thái trống
                GioHang.Clear();
                TongTien = 0;
                KhachHangHienTai = null;
                SearchKhachHangKeyword = "";
                LoadKhoHang("");
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                MessageBox.Show("Lỗi trong quá trình thanh toán: " + ex.Message, "Lỗi Database", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
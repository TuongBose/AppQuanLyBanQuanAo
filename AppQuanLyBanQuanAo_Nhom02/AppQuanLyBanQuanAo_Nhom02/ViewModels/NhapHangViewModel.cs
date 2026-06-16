using CommunityToolkit.Mvvm.Messaging;
using AppQuanLyBanQuanAo_Nhom02.Data;
using AppQuanLyBanQuanAo_Nhom02.Models;
using AppQuanLyBanQuanAo_Nhom02.Untils;
using Dapper;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace AppQuanLyBanQuanAo_Nhom02.ViewModels
{
    public class NhapHangViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<PhieuNhap> _danhSachPhieuNhap;
        private ObservableCollection<SanPham> _danhSachSanPham;
        private ObservableCollection<ChiTietPhieuNhap> _chiTietPhieuNhap;
        private PhieuNhap _phieuNhapDangChon;
        private ChiTietPhieuNhap _chiTietDangChon;
        private PhieuNhap _phieuNhapMoi;
        private ChiTietPhieuNhap _chiTietMoi;
        private string _timKiem = "";
        private bool _isLoading = false;
        private string _thongBao = "";
        private int _tongTien = 0;
        private bool _coThayDoiChiTiet = false;

        private ICollectionView _phieuNhapView;
        public ICollectionView PhieuNhapView
        {
            get => _phieuNhapView;
            set { _phieuNhapView = value; OnPropertyChanged(); }
        }

        public ChiTietPhieuNhap ChiTietDangChon
        {
            get => _chiTietDangChon;
            set
            {
                _chiTietDangChon = value;
                OnPropertyChanged();

                // KHI NGƯỜI DÙNG CLICK CHỌN 1 DÒNG TRONG BẢNG:
                if (value != null)
                {
                    // 1. Đẩy dữ liệu ngược lên các ô nhập liệu
                    ChiTietMoi = new ChiTietPhieuNhap
                    {
                        MaSP = value.MaSP,
                        SoLuongNhap = value.SoLuongNhap,
                        GiaNhap = value.GiaNhap
                    };

                    // 2. Xóa từ khóa tìm kiếm để ComboBox hiển thị đúng sản phẩm đó
                    TimKiemSanPham = "";
                }

                // 3. Kích hoạt đổi UI của nút bấm
                OnPropertyChanged(nameof(TextNutThemChiTiet));
                OnPropertyChanged(nameof(MauNutThemChiTiet));
                OnPropertyChanged(nameof(IconNutThemChiTiet));
                OnPropertyChanged(nameof(IsEditChiTietMode));
            }
        }

        // CÁC BIẾN KIỂM SOÁT GIAO DIỆN NÚT BẤM
        public string TextNutThemChiTiet => ChiTietDangChon != null ? "Cập nhật lại sản phẩm này" : "Thêm vào danh sách nhập";
        public string MauNutThemChiTiet => ChiTietDangChon != null ? "#F39C12" : "#3498DB"; // Màu Cam (Sửa) hoặc Xanh dương (Thêm)
        public string IconNutThemChiTiet => ChiTietDangChon != null ? "📝" : "➕";

        public ObservableCollection<PhieuNhap> DanhSachPhieuNhap
        {
            get { return _danhSachPhieuNhap; }
            set { _danhSachPhieuNhap = value; OnPropertyChanged(); }
        }

        public ObservableCollection<SanPham> DanhSachSanPham
        {
            get { return _danhSachSanPham; }
            set { _danhSachSanPham = value; OnPropertyChanged(); }
        }

        public ObservableCollection<ChiTietPhieuNhap> ChiTietPhieuNhap
        {
            get { return _chiTietPhieuNhap; }
            set { _chiTietPhieuNhap = value; OnPropertyChanged(); }
        }

        public PhieuNhap PhieuNhapDangChon
        {
            get { return _phieuNhapDangChon; }
            set
            {
                _phieuNhapDangChon = value; OnPropertyChanged();
                if (value != null)
                {
                    PhieuNhapMoi = new PhieuNhap
                    {
                        MaPN = value.MaPN,
                        NhaCungCap = value.NhaCungCap,
                        NgayNhap = value.NgayNhap,
                        TongTien = value.TongTien
                    };

                    TongTien = value.TongTien;
                    LoadChiTietPhieuNhap(value.MaPN);

                    // --- ĐẶT LẠI CỜ KHI CLICK XEM MỘT PHIẾU CŨ TRONG DANH SÁCH ---
                    _coThayDoiChiTiet = false;
                }
            }
        }

        public PhieuNhap PhieuNhapMoi
        {
            get { return _phieuNhapMoi; }
            set { _phieuNhapMoi = value; OnPropertyChanged(); }
        }

        public ChiTietPhieuNhap ChiTietMoi
        {
            get { return _chiTietMoi; }
            set { _chiTietMoi = value; OnPropertyChanged(); }
        }

        public string TimKiem
        {
            get { return _timKiem; }
            set
            {
                _timKiem = value;
                OnPropertyChanged();
                PhieuNhapView?.Refresh();
            }
        }

        public bool IsLoading
        {
            get { return _isLoading; }
            set { _isLoading = value; OnPropertyChanged(); }
        }

        public string ThongBao
        {
            get { return _thongBao; }
            set
            {
                _thongBao = value;
                OnPropertyChanged();

                // Nếu chuỗi rỗng (như lúc vừa Làm Mới xong), không làm gì cả
                if (string.IsNullOrWhiteSpace(value)) return;

                // Tự động phân loại icon MessageBox dựa vào nội dung chữ
                if (value.StartsWith("Lỗi") || value.Contains("LỖ VỐN"))
                {
                    MessageBox.Show(value, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else if (value.Contains("Vui lòng") || value.Contains("chưa có thao tác") || value.Contains("không tồn tại"))
                {
                    MessageBox.Show(value, "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    MessageBox.Show(value, "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        public int TongTien
        {
            get { return _tongTien; }
            set { _tongTien = value; OnPropertyChanged(); }
        }

        public ICommand TaiDuLieuCommand { get; }
        public ICommand TaoPhieuNhapCommand { get; }
        public ICommand ThemChiTietCommand { get; }
        public ICommand XoaChiTietCommand { get; }
        public ICommand LuuPhieuNhapCommand { get; }
        public ICommand CapNhatPhieuNhapCommand { get; }
        public ICommand TimKiemCommand { get; }
        public ICommand LamMoiCommand { get; }
        public ICommand HuySuaChiTietCommand { get; }

        private string _timKiemSanPham = "";
        public string TimKiemSanPham
        {
            get => _timKiemSanPham;
            set
            {
                _timKiemSanPham = value;
                OnPropertyChanged();
                SanPhamView?.Refresh(); // Kích hoạt lệnh lọc ngay khi gõ phím
            }
        }

        private ICollectionView _sanPhamView;
        public ICollectionView SanPhamView
        {
            get => _sanPhamView;
            set { _sanPhamView = value; OnPropertyChanged(); }
        }

        public NhapHangViewModel()
        {
            DanhSachPhieuNhap = new ObservableCollection<PhieuNhap>();
            DanhSachSanPham = new ObservableCollection<SanPham>();
            ChiTietPhieuNhap = new ObservableCollection<ChiTietPhieuNhap>();

            PhieuNhapMoi = new PhieuNhap
            {
                NgayNhap = DateTime.Now.ToString("dd/MM/yyyy")
            };

            ChiTietMoi = new ChiTietPhieuNhap();

            TaiDuLieuCommand = new RelayCommand(_ => TaiDuLieu());
            TaoPhieuNhapCommand = new RelayCommand(_ => TaoPhieuNhap());
            ThemChiTietCommand = new RelayCommand(_ => ThemChiTiet(), _ => CanThemChiTiet());
            XoaChiTietCommand = new RelayCommand(_ => XoaChiTiet(), _ => CanXoaChiTiet());
            LuuPhieuNhapCommand = new RelayCommand(_ => LuuPhieuNhap(), _ => CanLuuPhieuNhap());
            CapNhatPhieuNhapCommand = new RelayCommand(_ => CapNhatPhieuNhap(), _ => CanCapNhat());
            TimKiemCommand = new RelayCommand(_ => TimKiemPhieuNhap());
            LamMoiCommand = new RelayCommand(_ => LamMoi());
            HuySuaChiTietCommand = new RelayCommand(_ => HuySuaChiTiet());

            TaiDuLieu();
        }

        private void TaiDuLieu()
        {
            try
            {
                IsLoading = true;

                DanhSachPhieuNhap.Clear();
                DanhSachSanPham.Clear();

                using var conn = new SqliteConnection(DatabaseConfig.ConnectionString);

                var phieuNhaps = conn.Query<PhieuNhap>("SELECT * FROM PhieuNhap").ToList();
                var sanPhams = conn.Query<SanPham>("SELECT * FROM SanPham").ToList();

                // ĐÁNH SỐ THỨ TỰ (STT) TỰ ĐỘNG
                for (int i = 0; i < phieuNhaps.Count; i++)
                {
                    phieuNhaps[i].STT = i + 1;
                }

                foreach (var pn in phieuNhaps) DanhSachPhieuNhap.Add(pn);
                foreach (var sp in sanPhams) DanhSachSanPham.Add(sp);

                SanPhamView = CollectionViewSource.GetDefaultView(DanhSachSanPham);
                SanPhamView.Filter = LocSanPham;

                PhieuNhapView = CollectionViewSource.GetDefaultView(DanhSachPhieuNhap);
                PhieuNhapView.Filter = LocPhieuNhap;

                // Xóa bỏ dòng thông báo "Tải dữ liệu thành công!" gây rối mắt, chỉ để lại chuỗi rỗng
                ThongBao = "";
            }
            catch (Exception ex)
            {
                ThongBao = $"Lỗi: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void LoadChiTietPhieuNhap(int maPN)
        {
            ChiTietPhieuNhap.Clear();
            using var conn = new SqliteConnection(DatabaseConfig.ConnectionString);

            // 1. Lấy dữ liệu thô từ database
            var data = conn.Query<ChiTietPhieuNhap>("SELECT * FROM ChiTietPhieuNhap WHERE MaPN = @MaPN", new { MaPN = maPN }).ToList();

            int stt = 1;
            foreach (var ct in data)
            {
                ct.STT = stt++; // Đánh số thứ tự 1, 2, 3...

                // 2. Dò tìm thông tin sản phẩm trong danh sách đã tải sẵn để lấy Tên, Màu, Size
                var sp = DanhSachSanPham.FirstOrDefault(x => x.MaSP == ct.MaSP);
                if (sp != null)
                {
                    ct.TenSP = sp.TenSP;   // Lấy tên gốc (không bao gồm màu/size)
                    ct.MauSac = sp.MauSac; // Lấy màu sắc
                    ct.Size = sp.Size;     // Lấy size
                }

                ChiTietPhieuNhap.Add(ct);
            }
        }

        private bool LocPhieuNhap(object obj)
        {
            if (obj is PhieuNhap pn)
            {
                if (string.IsNullOrWhiteSpace(TimKiem)) return true;
                return pn.NhaCungCap.Contains(TimKiem, StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }

        private bool LocSanPham(object obj)
        {
            if (obj is SanPham sp)
            {
                // Nếu chưa gõ gì thì hiện tất cả
                if (string.IsNullOrWhiteSpace(TimKiemSanPham)) return true;

                // Tìm kiếm linh hoạt theo Tên, Màu hoặc Size (Không phân biệt hoa/thường)
                return sp.TenHienThi.IndexOf(TimKiemSanPham, StringComparison.OrdinalIgnoreCase) >= 0;
            }
            return false;
        }

        private void TaoPhieuNhap()
        {
            try
            {
                ChiTietPhieuNhap.Clear();
                PhieuNhapMoi = new PhieuNhap { NgayNhap = DateTime.Now.ToString("dd/MM/yyyy") };
                TongTien = 0;
                ThongBao = "Tạo phiếu nhập mới thành công!";
            }
            catch (Exception ex)
            {
                ThongBao = $"Lỗi: {ex.Message}";
            }
        }

        private void ThemChiTiet()
        {
            try
            {
                if (ChiTietMoi.MaSP <= 0 || ChiTietMoi.SoLuongNhap <= 0 || ChiTietMoi.GiaNhap <= 0)
                {
                    ThongBao = "Vui lòng nhập đầy đủ thông tin chi tiết!";
                    return;
                }

                var sanPham = DanhSachSanPham.FirstOrDefault(x => x.MaSP == ChiTietMoi.MaSP);
                if (sanPham == null)
                {
                    ThongBao = "Sản phẩm không tồn tại!";
                    return;
                }

                if (ChiTietMoi.GiaNhap > sanPham.GiaBan)
                {
                    ThongBao = $"Giá nhập ({ChiTietMoi.GiaNhap}) > Giá bán ({sanPham.GiaBan}) → LỖ VỐN!";
                    return;
                }

                // =========================================================
                // CHẾ ĐỘ 1: ĐANG CẬP NHẬT (Đã click chọn 1 dòng trên bảng)
                // =========================================================
                if (ChiTietDangChon != null)
                {
                    // Kiểm tra xem có gì thay đổi không
                    if (ChiTietMoi.MaSP == ChiTietDangChon.MaSP &&
                        ChiTietMoi.SoLuongNhap == ChiTietDangChon.SoLuongNhap &&
                        ChiTietMoi.GiaNhap == ChiTietDangChon.GiaNhap)
                    {
                        ThongBao = "Bạn chưa có thao tác cập nhật mới!";
                        return; // Dừng lại, không chạy code update bên dưới nữa
                    }

                    // Lấy vị trí của dòng đang được chọn
                    int index = ChiTietPhieuNhap.IndexOf(ChiTietDangChon);

                    // Trừ số tiền của dòng cũ khỏi Tổng Tiền
                    TongTien -= (ChiTietDangChon.SoLuongNhap * ChiTietDangChon.GiaNhap);

                    // Ghi đè toàn bộ dữ liệu mới (Sản phẩm mới, Số lượng mới, Giá mới) vào đúng vị trí đó
                    var updated = new ChiTietPhieuNhap
                    {
                        MaSP = ChiTietMoi.MaSP,
                        TenSP = sanPham.TenSP,
                        MauSac = sanPham.MauSac,
                        Size = sanPham.Size,
                        SoLuongNhap = ChiTietMoi.SoLuongNhap,
                        GiaNhap = ChiTietMoi.GiaNhap
                    };
                    ChiTietPhieuNhap[index] = updated;

                    // Cộng lại số tiền mới vào Tổng Tiền
                    TongTien += (updated.SoLuongNhap * updated.GiaNhap);
                }
                // =========================================================
                // CHẾ ĐỘ 2: THÊM MỚI HOÀN TOÀN
                // =========================================================
                else
                {
                    var existing = ChiTietPhieuNhap.FirstOrDefault(x => x.MaSP == ChiTietMoi.MaSP);
                    if (existing != null)
                    {
                        // Nếu đã có trong danh sách -> Cộng dồn số lượng
                        TongTien -= (existing.SoLuongNhap * existing.GiaNhap);
                        int index = ChiTietPhieuNhap.IndexOf(existing);

                        var updated = new ChiTietPhieuNhap
                        {
                            MaSP = existing.MaSP,
                            TenSP = existing.TenSP,
                            MauSac = existing.MauSac,
                            Size = existing.Size,
                            SoLuongNhap = existing.SoLuongNhap + ChiTietMoi.SoLuongNhap, // Cộng dồn
                            GiaNhap = ChiTietMoi.GiaNhap // Lấy giá mới nhất
                        };
                        ChiTietPhieuNhap[index] = updated;
                        TongTien += (updated.SoLuongNhap * updated.GiaNhap);
                    }
                    else
                    {
                        // Nếu chưa có -> Thêm một dòng mới toanh
                        ChiTietPhieuNhap.Add(new ChiTietPhieuNhap
                        {
                            MaSP = ChiTietMoi.MaSP,
                            TenSP = sanPham.TenSP,
                            MauSac = sanPham.MauSac,
                            Size = sanPham.Size,
                            SoLuongNhap = ChiTietMoi.SoLuongNhap,
                            GiaNhap = ChiTietMoi.GiaNhap
                        });
                        TongTien += (ChiTietMoi.SoLuongNhap * ChiTietMoi.GiaNhap);
                    }
                }

                // --- ĐÁNH LẠI STT SAU KHI THAY ĐỔI ---
                int stt = 1;
                foreach (var item in ChiTietPhieuNhap) item.STT = stt++;

                _coThayDoiChiTiet = true;
                PhieuNhapMoi.TongTien = TongTien;

                // Reset lại form nhập liệu sau khi thao tác xong
                ChiTietMoi = new ChiTietPhieuNhap();
                TimKiemSanPham = "";
                ChiTietDangChon = null; // Trả nút bấm về màu Xanh mặc định

                ThongBao = "Cập nhật danh sách nhập thành công!";
            }
            catch (Exception ex)
            {
                ThongBao = $"Lỗi: {ex.Message}";
            }
        }

        private void XoaChiTiet()
        {
            try
            {
                if (ChiTietPhieuNhap.Count == 0 || ChiTietDangChon == null)
                {
                    ThongBao = "Vui lòng chọn sản phẩm để xóa!";
                    return;
                }

                TongTien -= ChiTietDangChon.SoLuongNhap * ChiTietDangChon.GiaNhap;
                PhieuNhapMoi.TongTien = TongTien;
                ChiTietPhieuNhap.Remove(ChiTietDangChon);

                _coThayDoiChiTiet = true;
                // Đánh lại số thứ tự sau khi xóa
                int stt = 1;
                foreach (var item in ChiTietPhieuNhap) item.STT = stt++;
                ThongBao = "Xóa chi tiết thành công!";
            }
            catch (Exception ex)
            {
                ThongBao = $"Lỗi: {ex.Message}";
            }
        }

        private void LuuPhieuNhap()
        {
            try
            {
                if (ChiTietPhieuNhap.Count == 0)
                {
                    ThongBao = "Vui lòng thêm ít nhất một chi tiết!";
                    return;
                }

                if (string.IsNullOrWhiteSpace(PhieuNhapMoi.NhaCungCap))
                {
                    ThongBao = "Vui lòng nhập tên nhà cung cấp!";
                    return;
                }

                if (PhieuNhapDangChon != null && PhieuNhapMoi.MaPN > 0)
                {
                    // Nếu không có item nào bị thêm/xóa VÀ tên nhà cung cấp giữ nguyên
                    if (!_coThayDoiChiTiet && PhieuNhapMoi.NhaCungCap == PhieuNhapDangChon.NhaCungCap)
                    {
                        ThongBao = $"Phiếu nhập 'PN{PhieuNhapMoi.MaPN:D3}' chưa có thao tác cập nhật mới!";
                        return; // Chặn đứng, không cho chạy lệnh INSERT bên dưới
                    }
                }

                using var conn = new SqliteConnection(DatabaseConfig.ConnectionString);
                conn.Open();
                using var transaction = conn.BeginTransaction();

                try
                {
                    // 1. Tạo Phiếu Nhập
                    string insertPNSql = "INSERT INTO PhieuNhap (MaNV, NhaCungCap, NgayNhap, TongTien) VALUES (@MaNV, @NhaCungCap, @NgayNhap, @TongTien); SELECT last_insert_rowid();";
                    long maPN = conn.ExecuteScalar<long>(insertPNSql, new
                    {
                        MaNV = 1, // AppSession.CurrentUser?.MaNV ?? 1,
                        NhaCungCap = PhieuNhapMoi.NhaCungCap,
                        NgayNhap = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        TongTien = TongTien
                    }, transaction);

                    // 2. Lưu Chi Tiết và Cộng Kho
                    string insertCT = "INSERT INTO ChiTietPhieuNhap (MaPN, MaSP, SoLuongNhap, GiaNhap) VALUES (@MaPN, @MaSP, @SoLuongNhap, @GiaNhap)";
                    string updateKho = "UPDATE SanPham SET SoLuongTon = SoLuongTon + @SoLuongNhap WHERE MaSP = @MaSP";

                    foreach (var ct in ChiTietPhieuNhap)
                    {
                        conn.Execute(insertCT, new { MaPN = maPN, MaSP = ct.MaSP, SoLuongNhap = ct.SoLuongNhap, GiaNhap = ct.GiaNhap }, transaction);
                        conn.Execute(updateKho, new { SoLuongNhap = ct.SoLuongNhap, MaSP = ct.MaSP }, transaction);
                    }

                    transaction.Commit();

                    //Bắn tín hiệu thông báo kho hàng đã thay đổi!
                    WeakReferenceMessenger.Default.Send(new KhoHangChangedMessage());

                    ThongBao = "Lưu phiếu nhập thành công!";
                    LamMoi();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    ThongBao = $"Lỗi Database: {ex.Message}";
                }
            }
            catch (Exception ex)
            {
                ThongBao = $"Lỗi: {ex.Message}";
            }
        }

        private void CapNhatPhieuNhap()
        {
            try
            {
                // Kiểm tra nếu không thêm/xóa hàng VÀ tên nhà cung cấp giữ nguyên y hệt
                if (!_coThayDoiChiTiet && PhieuNhapMoi.NhaCungCap == PhieuNhapDangChon.NhaCungCap)
                {
                    ThongBao = $"Phiếu nhập 'PN{PhieuNhapMoi.MaPN:D3}' chưa có thao tác cập nhật mới!";
                    return;
                }

                var result = MessageBox.Show($"Bạn có chắc chắn muốn cập nhật lại Phiếu nhập 'PN{PhieuNhapMoi.MaPN:D3}'?\nSố lượng kho của các sản phẩm liên quan sẽ được tính toán lại.",
                                            "Xác nhận cập nhật", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.No) return;

                using var conn = new SqliteConnection(DatabaseConfig.ConnectionString);
                conn.Open();
                using var transaction = conn.BeginTransaction();

                try
                {
                    // Giữ lại mã phiếu nhập để hiển thị thông báo
                    int maPNCu = PhieuNhapMoi.MaPN;

                    // 1. Lấy lại danh sách chi tiết CŨ đang có trong database để HOÀN TÁC KHO
                    var chiTietCu = conn.Query<ChiTietPhieuNhap>(
                        "SELECT * FROM ChiTietPhieuNhap WHERE MaPN = @MaPN",
                        new { MaPN = maPNCu }, transaction).ToList();

                    foreach (var item in chiTietCu)
                    {
                        conn.Execute("UPDATE SanPham SET SoLuongTon = SoLuongTon - @SoLuongNhap WHERE MaSP = @MaSP",
                                    new { SoLuongNhap = item.SoLuongNhap, MaSP = item.MaSP }, transaction);
                    }

                    // 2. Cập nhật thông tin chung của Phiếu Nhập
                    conn.Execute("UPDATE PhieuNhap SET NhaCungCap = @NhaCungCap, TongTien = @TongTien WHERE MaPN = @MaPN",
                                new { NhaCungCap = PhieuNhapMoi.NhaCungCap, TongTien = TongTien, MaPN = maPNCu }, transaction);

                    // 3. Xóa sạch chi tiết phiếu CŨ
                    conn.Execute("DELETE FROM ChiTietPhieuNhap WHERE MaPN = @MaPN", new { MaPN = maPNCu }, transaction);

                    // 4. Ghi đè chi tiết phiếu MỚI và CỘNG KHO MỚI
                    string insertCT = "INSERT INTO ChiTietPhieuNhap (MaPN, MaSP, SoLuongNhap, GiaNhap) VALUES (@MaPN, @MaSP, @SoLuongNhap, @GiaNhap)";
                    string updateKho = "UPDATE SanPham SET SoLuongTon = SoLuongTon + @SoLuongNhap WHERE MaSP = @MaSP";

                    foreach (var ct in ChiTietPhieuNhap)
                    {
                        conn.Execute(insertCT, new { MaPN = maPNCu, MaSP = ct.MaSP, SoLuongNhap = ct.SoLuongNhap, GiaNhap = ct.GiaNhap }, transaction);
                        conn.Execute(updateKho, new { SoLuongNhap = ct.SoLuongNhap, MaSP = ct.MaSP }, transaction);
                    }

                    transaction.Commit();

                    //Bắn tín hiệu thông báo kho hàng đã thay đổi!
                    WeakReferenceMessenger.Default.Send(new KhoHangChangedMessage());

                    _coThayDoiChiTiet = false;

                    // Hiển thị hộp thoại báo thành công trước
                    ThongBao = $"Cập nhật Phiếu nhập 'PN{maPNCu:D3}' thành công!";

                    // [THAY ĐỔI TẠI ĐÂY]: Hủy chọn phiếu cũ và gọi hàm LamMoi() để dọn sạch form
                    // Việc gán bằng null sẽ giúp nút "NHẬP KHO" sáng trở lại khi người dùng nhập phiếu mới
                    PhieuNhapDangChon = null;
                    LamMoi();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    ThongBao = $"Lỗi cập nhật: {ex.Message}";
                }
            }
            catch (Exception ex)
            {
                ThongBao = $"Lỗi: {ex.Message}";
            }
        }

        private void TimKiemPhieuNhap()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(TimKiem))
                {
                    TaiDuLieu();
                    return;
                }

                var ketQua = DanhSachPhieuNhap.Where(x =>
                    x.NhaCungCap.Contains(TimKiem, StringComparison.OrdinalIgnoreCase)
                ).ToList();

                DanhSachPhieuNhap.Clear();
                foreach (var pn in ketQua) DanhSachPhieuNhap.Add(pn);

                ThongBao = $"Tìm thấy {ketQua.Count} phiếu nhập";
            }
            catch (Exception ex)
            {
                ThongBao = $"Lỗi: {ex.Message}";
            }
        }

        private void LamMoi()
        {
            TimKiemSanPham = ""; // Xóa trắng ô tìm kiếm sản phẩm
            TimKiem = "";
            ChiTietPhieuNhap.Clear();
            PhieuNhapMoi = new PhieuNhap { NgayNhap = DateTime.Now.ToString("dd/MM/yyyy") };
            ChiTietMoi = new ChiTietPhieuNhap();
            TongTien = 0;
            _coThayDoiChiTiet = false;
            TaiDuLieu();
        }

        // Biến này sẽ trả về True nếu đang chọn 1 dòng, False nếu không chọn gì
        public bool IsEditChiTietMode => ChiTietDangChon != null;

        private void HuySuaChiTiet()
        {
            ChiTietDangChon = null; // Trả nút chính về màu Xanh mặc định
            ChiTietMoi = new ChiTietPhieuNhap(); // Làm sạch ô số lượng, giá
            TimKiemSanPham = ""; // Xóa ô lọc sản phẩm
        }

        private bool CanThemChiTiet() => ChiTietMoi?.MaSP > 0 && ChiTietMoi?.SoLuongNhap > 0;
        private bool CanLuuPhieuNhap() => PhieuNhapDangChon == null && !string.IsNullOrWhiteSpace(PhieuNhapMoi?.NhaCungCap) && ChiTietPhieuNhap.Count > 0; private bool CanXoaChiTiet() => ChiTietDangChon != null;
        private bool CanCapNhat() => PhieuNhapDangChon != null && ChiTietPhieuNhap.Count > 0;

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
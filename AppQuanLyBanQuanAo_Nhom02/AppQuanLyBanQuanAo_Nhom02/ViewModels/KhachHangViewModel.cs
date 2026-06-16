using AppQuanLyBanQuanAo_Nhom02.Models;
using AppQuanLyBanQuanAo_Nhom02.Data;
using AppQuanLyBanQuanAo_Nhom02.Untils;
using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Windows.Data;
using Microsoft.Data.Sqlite;
using Dapper;
using System.Windows;

namespace AppQuanLyBanQuanAo_Nhom02.ViewModels
{
    public class KhachHangViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<KhachHang> _danhSachKhachHang;
        private KhachHang _khachHangDangChon;
        private KhachHang _khachHangMoi;
        private string _timKiem = "";
        private bool _isLoading = false;
        private string _thongBao = "";
        private ICollectionView _view;

        // --- CÁC BIẾN MỚI ĐỂ XỬ LÝ LỖI GIAO DIỆN ---
        private string _loiTenKH = "";
        public string LoiTenKH { get => _loiTenKH; set { _loiTenKH = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasLoiTenKH)); } }
        public bool HasLoiTenKH => !string.IsNullOrEmpty(LoiTenKH);

        private string _loiSDT = "";
        public string LoiSDT { get => _loiSDT; set { _loiSDT = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasLoiSDT)); } }
        public bool HasLoiSDT => !string.IsNullOrEmpty(LoiSDT);
        // -------------------------------------------

        public ObservableCollection<KhachHang> DanhSachKhachHang
        {
            get { return _danhSachKhachHang; }
            set { _danhSachKhachHang = value; OnPropertyChanged(); }
        }

        public KhachHang KhachHangDangChon
        {
            get { return _khachHangDangChon; }
            set
            {
                _khachHangDangChon = value;
                if (value != null)
                {
                    KhachHangMoi = new KhachHang
                    {
                        MaKH = value.MaKH,
                        TenKH = value.TenKH,
                        SDT = value.SDT,
                        DiemTichLuy = value.DiemTichLuy
                    };
                }
                OnPropertyChanged();
            }
        }

        public KhachHang KhachHangMoi
        {
            get { return _khachHangMoi; }
            set { _khachHangMoi = value; OnPropertyChanged(); }
        }

        public string TimKiem
        {
            get { return _timKiem; }
            set { _timKiem = value; OnPropertyChanged(); _view?.Refresh(); }
        }

        public bool IsLoading
        {
            get { return _isLoading; }
            set { _isLoading = value; OnPropertyChanged(); }
        }

        public string ThongBao
        {
            get { return _thongBao; }
            set { _thongBao = value; OnPropertyChanged(); }
        }

        public ICommand TaiDuLieuCommand { get; }
        public ICommand ThemKhachHangCommand { get; }
        public ICommand SuaKhachHangCommand { get; }
        public ICommand XoaKhachHangCommand { get; }
        public ICommand LamMoiCommand { get; }

        public KhachHangViewModel()
        {
            DanhSachKhachHang = new ObservableCollection<KhachHang>();
            KhachHangMoi = new KhachHang();

            TaiDuLieuCommand = new RelayCommand(_ => TaiDuLieu());
            ThemKhachHangCommand = new RelayCommand(_ => ThemKhachHang());
            SuaKhachHangCommand = new RelayCommand(_ => SuaKhachHang());
            XoaKhachHangCommand = new RelayCommand(p => XoaKhachHang(p));
            LamMoiCommand = new RelayCommand(_ => LamMoi());

            TaiDuLieu();
        }

        private void TaiDuLieu()
        {
            try
            {
                IsLoading = true;
                using var conn = new SqliteConnection(DatabaseConfig.ConnectionString);

                // === CHỈNH SỬA Ở ĐÂY: Thêm đuôi WHERE để ẩn Khách vãng lai ===
                var data = conn.Query<KhachHang>("SELECT * FROM KhachHang WHERE TenKH != 'Khách vãng lai'").ToList();

                // ĐÁNH SỐ THỨ TỰ (STT) TỰ ĐỘNG Ở ĐÂY
                for (int i = 0; i < data.Count; i++)
                {
                    data[i].STT = i + 1;
                }

                DanhSachKhachHang.Clear();
                foreach (var kh in data) DanhSachKhachHang.Add(kh);

                _view = CollectionViewSource.GetDefaultView(DanhSachKhachHang);
                _view.Filter = LocDuLieu;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải dữ liệu: {ex.Message}", "Lỗi Hệ Thống", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { IsLoading = false; }
        }

        private bool LocDuLieu(object obj)
        {
            if (obj is KhachHang kh)
            {
                if (string.IsNullOrWhiteSpace(TimKiem)) return true;
                return kh.TenKH.Contains(TimKiem, StringComparison.OrdinalIgnoreCase)
                    || (kh.SDT != null && kh.SDT.Contains(TimKiem));
            }
            return false;
        }

        // HÀM KIỂM TRA LỖI CHUYÊN NGHIỆP (ĐÃ NÂNG CẤP CHỐT CHẶN CUỐI)
        private bool ValidateData()
        {
            LoiTenKH = "";
            LoiSDT = "";
            bool isValid = true;

            string tenKH = KhachHangMoi.TenKH ?? "";

            // ==========================================
            // 1. KIỂM TRA TÊN KHÁCH HÀNG
            // ==========================================

            // ƯU TIÊN 1: Soi xem có lọt bất kỳ chữ số nào vào không (Ví dụ: 23123Nguyễn Văn A)
            if (tenKH.Any(char.IsDigit))
            {
                // Lọc bỏ toàn bộ số, chỉ giữ lại chữ cái và khoảng trắng
                string cleanName = new string(tenKH.Where(c => char.IsLetter(c) || char.IsWhiteSpace(c)).ToArray());

                KhachHangMoi.TenKH = cleanName.Trim(); // Gán lại tên đã sạch số
                OnPropertyChanged(nameof(KhachHangMoi)); // Kích hoạt lệnh ép UI xóa số trên màn hình ngay lập tức

                // Báo lỗi viền đỏ theo đúng yêu cầu
                LoiTenKH = "Tên khách hàng không được nhập bằng chữ số!";
                isValid = false;
            }
            // ƯU TIÊN 2: Kiểm tra nếu để trống
            else if (string.IsNullOrWhiteSpace(tenKH))
            {
                LoiTenKH = "Tên khách hàng không được để trống!";
                isValid = false;
            }
            // ƯU TIÊN 3: Kiểm tra nếu có ký tự đặc biệt (@, #, $,...)
            else if (!tenKH.All(c => char.IsLetter(c) || char.IsWhiteSpace(c)))
            {
                string cleanName = new string(tenKH.Where(c => char.IsLetter(c) || char.IsWhiteSpace(c)).ToArray());
                KhachHangMoi.TenKH = cleanName.Trim();
                OnPropertyChanged(nameof(KhachHangMoi));

                LoiTenKH = "Tên khách hàng không được chứa ký tự đặc biệt!";
                isValid = false;
            }

            // ==========================================
            // 2. KIỂM TRA SỐ ĐIỆN THOẠI
            // ==========================================
            if (string.IsNullOrWhiteSpace(KhachHangMoi.SDT))
            {
                LoiSDT = "Số điện thoại là thông tin bắt buộc!";
                isValid = false;
            }
            else if (KhachHangMoi.SDT.Length < 10)
            {
                LoiSDT = "Số điện thoại phải được nhập đủ 10 chữ số!";
                isValid = false;
            }

            return isValid;
        }

        private void ThemKhachHang()
        {
            try
            {
                if (KhachHangDangChon != null)
                {
                    MessageBox.Show("Khách hàng đã có trong danh sách!\nVui lòng bấm 'Làm mới' trước khi thêm mới.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Nếu nhập sai/thiếu thông tin -> Dừng lại ngay lập tức
                if (!ValidateData()) return;

                using var conn = new SqliteConnection(DatabaseConfig.ConnectionString);

                int count = conn.ExecuteScalar<int>("SELECT COUNT(1) FROM KhachHang WHERE SDT = @SDT", new { SDT = KhachHangMoi.SDT });
                if (count > 0)
                {
                    LoiSDT = "Số điện thoại này đã được đăng ký!";
                    return;
                }

                string sql = "INSERT INTO KhachHang (TenKH, SDT, DiemTichLuy) VALUES (@TenKH, @SDT, @DiemTichLuy)";
                conn.Execute(sql, new
                {
                    TenKH = KhachHangMoi.TenKH,
                    SDT = KhachHangMoi.SDT,
                    DiemTichLuy = KhachHangMoi.DiemTichLuy
                });

                MessageBox.Show("Thêm khách hàng thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                LamMoi();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi Hệ Thống", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SuaKhachHang()
        {
            try
            {
                if (KhachHangDangChon == null)
                {
                    MessageBox.Show("Vui lòng chọn một khách hàng trong danh sách để cập nhật!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                    return;
                }

                string tenCu = KhachHangDangChon.TenKH ?? "";
                string tenMoi = KhachHangMoi.TenKH ?? "";
                string sdtCu = KhachHangDangChon.SDT ?? "";
                string sdtMoi = KhachHangMoi.SDT ?? "";
                int diemCu = KhachHangDangChon.DiemTichLuy;
                int diemMoi = KhachHangMoi.DiemTichLuy;

                if (tenCu == tenMoi && sdtCu == sdtMoi && diemCu == diemMoi)
                {
                    MessageBox.Show($"Chưa có thao tác cập nhật mới cho khách hàng '{tenCu}'", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Nếu nhập sai/thiếu thông tin -> Dừng lại
                if (!ValidateData()) return;

                using var conn = new SqliteConnection(DatabaseConfig.ConnectionString);

                if (sdtMoi != sdtCu)
                {
                    int count = conn.ExecuteScalar<int>(
                        "SELECT COUNT(1) FROM KhachHang WHERE SDT = @SDT AND MaKH != @MaKH",
                        new { SDT = sdtMoi, MaKH = KhachHangDangChon.MaKH });

                    if (count > 0)
                    {
                        LoiSDT = "Số điện thoại này đã được đăng ký cho người khác!";
                        return;
                    }
                }

                string sql = "UPDATE KhachHang SET TenKH = @TenKH, SDT = @SDT, DiemTichLuy = @DiemTichLuy WHERE MaKH = @MaKH";
                conn.Execute(sql, new
                {
                    TenKH = KhachHangMoi.TenKH,
                    SDT = KhachHangMoi.SDT,
                    DiemTichLuy = KhachHangMoi.DiemTichLuy,
                    MaKH = KhachHangDangChon.MaKH
                });

                MessageBox.Show("Cập nhật khách hàng thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                LamMoi();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi Hệ Thống", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void XoaKhachHang(object parameter)
        {
            try
            {
                // Nhận danh sách các dòng đang được bôi xanh từ DataGrid
                var selectedItems = parameter as System.Collections.IList;

                if (selectedItems == null || selectedItems.Count == 0)
                {
                    MessageBox.Show("Vui lòng chọn ít nhất một khách hàng để xóa!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                    return;
                }

                // Chuyển đổi thành danh sách đối tượng KhachHang
                var listToDelete = selectedItems.Cast<KhachHang>().ToList();
                var maKHs = listToDelete.Select(k => k.MaKH).ToList(); // Lấy ra toàn bộ Mã KH

                using var conn = new SqliteConnection(DatabaseConfig.ConnectionString);

                // KIỂM TRA ĐỒNG LOẠT: Có ai trong số này đã mua hàng chưa?
                // Dapper sẽ tự động biến @Ids thành danh sách (1, 2, 3...)
                int countHoaDon = conn.ExecuteScalar<int>("SELECT COUNT(1) FROM HoaDon WHERE MaKH IN @Ids", new { Ids = maKHs });
                if (countHoaDon > 0)
                {
                    MessageBox.Show("Không thể xóa!\nMột hoặc nhiều khách hàng được chọn đã phát sinh hóa đơn mua hàng.", "Lỗi Xóa Dữ Liệu", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Tạo câu thông báo thông minh (Xóa 1 người thì hiện Tên, Xóa nhiều người thì hiện Số lượng)
                string msg = listToDelete.Count == 1
                    ? $"Bạn có chắc chắn muốn xóa khách hàng '{listToDelete[0].TenKH}' không?"
                    : $"Bạn có chắc chắn muốn xóa {listToDelete.Count} khách hàng đã chọn không?";

                var result = MessageBox.Show(msg, "Xác nhận xóa hàng loạt", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    // Lệnh DELETE siêu tốc bằng từ khóa IN
                    conn.Execute("DELETE FROM KhachHang WHERE MaKH IN @Ids", new { Ids = maKHs });
                    LamMoi();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi Hệ Thống", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LamMoi()
        {
            TimKiem = "";
            KhachHangMoi = new KhachHang();
            KhachHangDangChon = null;
            ThongBao = "";
            LoiTenKH = "";
            LoiSDT = "";
            TaiDuLieu();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Media;
using AppQuanLyBanQuanAo_Nhom02.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Data.Sqlite;
using Dapper;
using CommunityToolkit.Mvvm.Messaging;
using AppQuanLyBanQuanAo_Nhom02.Messages;

namespace AppQuanLyBanQuanAo_Nhom02.ViewModels
{
    public partial class TrangChuViewModel : ObservableObject
    {
        // Phân quyền hiển thị giao diện người dùng
        public Visibility AdminVisibility => (AppSession.CurrentUser?.Role == 1) ? Visibility.Visible : Visibility.Collapsed;
        public Visibility EmployeeVisibility => (AppSession.CurrentUser?.Role == 0) ? Visibility.Visible : Visibility.Collapsed;

        public string EmployeeName => AppSession.CurrentUser?.TenNV ?? "Nhân viên";

        // ==========================================
        // DỮ LIỆU & MÀU SẮC CHO ADMIN (Role = 1)
        // ==========================================
        [ObservableProperty] private string tongDoanhThu = "0 VNĐ";
        [ObservableProperty] private string soHoaDonHomNay = "0 Đơn";
        [ObservableProperty] private string tongKhachHang = "0 Người";
        [ObservableProperty] private string tongNhanVien = "0 Người";

        [ObservableProperty] private SolidColorBrush doanhThuColor;
        [ObservableProperty] private SolidColorBrush hoaDonColor;
        [ObservableProperty] private SolidColorBrush khachHangColor;
        [ObservableProperty] private SolidColorBrush nhanVienColor;

        public ObservableCollection<SanPhamTonKho> SanPhamSapHetHang { get; set; } = new ObservableCollection<SanPhamTonKho>();

        // ==========================================
        // DỮ LIỆU CHO NHÂN VIÊN (Role = 0)
        // ==========================================
        [ObservableProperty] private string currentTime = "";
        private DispatcherTimer timer;
        public ObservableCollection<HoaDonGanDay> HoaDonCuaToi { get; set; } = new ObservableCollection<HoaDonGanDay>();

        public TrangChuViewModel()
        {
            // Cài đặt bảng màu theo cấu trúc Outline - Tinted mẫu
            SolidColorBrush FromHex(string hex) => new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            DoanhThuColor = FromHex("#27AE60"); // Xanh lá
            HoaDonColor = FromHex("#2980B9");   // Xanh dương
            KhachHangColor = FromHex("#F39C12"); // Vàng cam
            NhanVienColor = FromHex("#8E44AD");  // Tím

            // Khởi chạy đồng hồ thời gian thực
            StartClock();

        }

        public async Task ReloadDataAsync()
        {
            if (AppSession.CurrentUser?.Role == 1)
            {
                await LoadAdminDataAsync();
            }
            else
            {
                await LoadEmployeeDataAsync();
            }
        }

        /// <summary>
        /// Tải dữ liệu tổng hợp động dành cho tài khoản Quản trị viên
        /// </summary>
        private async Task LoadAdminDataAsync()
        {
            try
            {
                using var conn = new SqliteConnection(DatabaseConfig.ConnectionString);
                await conn.OpenAsync();

                // Lấy ngày hiện tại dưới định dạng chuỗi chuẩn ISO để so khớp CSDL
                var today = DateTime.Today.ToString("yyyy-MM-dd");

                // 1. Truy vấn tính tổng doanh thu trong ngày hôm nay
                var doanhThu = await conn.ExecuteScalarAsync<long?>("SELECT SUM(TongTien) FROM HoaDon WHERE date(NgayLap) = date(@today)", new { today });
                TongDoanhThu = (doanhThu ?? 0).ToString("N0") + " VNĐ";

                // 2. Truy vấn đếm số lượng hóa đơn
                var soHD = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM HoaDon WHERE date(NgayLap) = date(@today)", new { today });
                SoHoaDonHomNay = $"{soHD} Đơn";

                // 3. Đếm tổng số lượng khách hàng
                var soKH = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM KhachHang");
                TongKhachHang = $"{soKH:N0} Người";

                // 4. Đếm tổng số lượng nhân viên
                var soNV = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM NhanVien");
                TongNhanVien = $"{soNV:N0} Người";

                // 5. Truy vấn danh sách cảnh báo tồn kho
                string querySP = "SELECT MaSP, TenSP, MauSac, Size, GiaBan, SoLuongTon AS SoLuong FROM SanPham WHERE SoLuongTon <= 5 ORDER BY SoLuongTon ASC";
                var spList = await conn.QueryAsync<SanPhamTonKho>(querySP);

                // Đồng bộ danh sách lên giao diện an toàn
                Application.Current.Dispatcher.Invoke(() =>
                {
                    SanPhamSapHetHang.Clear();
                    int stt = 1;
                    foreach (var sp in spList)
                    {
                        sp.STT = stt++;

                        // ==================================================
                        // ĐÃ THÊM: TỰ ĐỘNG FORMAT MÃ SẢN PHẨM (1 -> SP001)
                        // ==================================================
                        if (int.TryParse(sp.MaSP, out int parsedId))
                        {
                            sp.MaSP = $"SP{parsedId:D3}"; // D3 nghĩa là luôn độn số 0 cho đủ 3 chữ số
                        }

                        SanPhamSapHetHang.Add(sp);
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi kết nối cơ sở dữ liệu Admin: {ex.Message}", "Lỗi Hệ Thống", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Tải danh sách lịch sử tác vụ dành riêng cho Nhân viên đang đăng nhập ca
        /// </summary>
        private async Task LoadEmployeeDataAsync()
        {
            try
            {
                using var conn = new SqliteConnection(DatabaseConfig.ConnectionString);
                await conn.OpenAsync();

                var today = DateTime.Today.ToString("yyyy-MM-dd");
                var currentUserId = AppSession.CurrentUser?.MaNV; // Định danh chính xác khóa chính của nhân viên đăng nhập

                // ĐÃ SỬA: Đổi '%H:%M' thành '%H:%M:%S' để hiển thị đầy đủ Giờ:Phút:Giây
                string queryHD = @"SELECT MaHD, strftime('%H:%M:%S', NgayLap) as ThoiGian, TongTien 
                                  FROM HoaDon 
                                  WHERE MaNV = @currentUserId AND date(NgayLap) = date(@today) 
                                  ORDER BY NgayLap DESC 
                                  LIMIT 5";

                var hdList = await conn.QueryAsync<dynamic>(queryHD, new { currentUserId, today });

                Application.Current.Dispatcher.Invoke(() =>
                {
                    HoaDonCuaToi.Clear();
                    foreach (var hd in hdList)
                    {
                        long tongTienRaw = hd.TongTien != null ? Convert.ToInt64(hd.TongTien) : 0;
                        HoaDonCuaToi.Add(new HoaDonGanDay
                        {
                            MaHD = $"HD{Convert.ToInt32(hd.MaHD):D3}",
                            // Cập nhật lại giá trị mặc định nếu rỗng cũng theo chuẩn 3 thành phần
                            ThoiGian = hd.ThoiGian?.ToString() ?? "--:--:--",
                            TongTien = tongTienRaw.ToString("N0") + " VNĐ"
                        });
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi tải dữ liệu tác vụ Nhân viên: {ex.Message}");
            }
        }

        private void StartClock()
        {
            CurrentTime = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
            timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            timer.Tick += (s, e) => CurrentTime = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
            timer.Start();
        }

        [RelayCommand]
        private void LapHoaDonMoi()
        {
            // Bắn tín hiệu yêu cầu MainViewModel đổi sang màn hình Lập Hóa Đơn
            // (Lưu ý: Thay 'LapHoaDonViewModel' bằng tên class ViewModel thực tế của bạn)
            WeakReferenceMessenger.Default.Send(new NavigationMessage(typeof(HoaDonViewModel)));
        }

        [RelayCommand]
        private void TraCuuKhachHang()
        {
            // Bắn tín hiệu yêu cầu MainViewModel đổi sang màn hình Quản lý Khách Hàng
            // (Lưu ý: Thay 'KhachHangViewModel' bằng tên class ViewModel thực tế của bạn)
            WeakReferenceMessenger.Default.Send(new NavigationMessage(typeof(KhachHangViewModel)));
        }
    }

    public class SanPhamTonKho
    {
        public int STT { get; set; }
        public string MaSP { get; set; }
        public string TenSP { get; set; }
        public string MauSac { get; set; }
        public string Size { get; set; }
        public int GiaBan { get; set; }
        public int SoLuong { get; set; }
    }

    public class HoaDonGanDay
    {
        public string MaHD { get; set; }
        public string ThoiGian { get; set; }
        public string TongTien { get; set; }
    }
}
using AppQuanLyBanQuanAo_Nhom02.Data;
using AppQuanLyBanQuanAo_Nhom02.Models;
using AppQuanLyBanQuanAo_Nhom02.Untils;
using Dapper;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using Microsoft.Data.Sqlite;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using ClosedXML.Excel; // Đã thêm thư viện ClosedXML

namespace AppQuanLyBanQuanAo_Nhom02.ViewModels
{
    public class BaoCaoViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<HoaDon> _danhSachHoaDon;
        private DateTime _tuNgay;
        private DateTime _denNgay;
        private int _tongDoanhThu;
        private bool _isLoading = false;
        private List<HoaDon> _allHoaDon;

        public string[] Labels { get; set; }
        public Axis[] XAxes { get; set; }

        public ISeries[] ColumnSeries { get; set; }
        public ISeries[] PieSeries { get; set; }

        private string _kieuThongKe = "Ngay";
        public string KieuThongKe
        {
            get => _kieuThongKe;
            set { _kieuThongKe = value; OnPropertyChanged(); }
        }

        public ICommand ThongKeCommand { get; }
        public ObservableCollection<HoaDon> DanhSachHoaDon
        {
            get { return _danhSachHoaDon; }
            set { _danhSachHoaDon = value; OnPropertyChanged(); }
        }

        private int _tongHoaDon;
        public int TongHoaDon
        {
            get => _tongHoaDon;
            set { _tongHoaDon = value; OnPropertyChanged(); }
        }

        private string _khachHangTop;
        public string KhachHangTop
        {
            get => _khachHangTop;
            set { _khachHangTop = value; OnPropertyChanged(); }
        }

        private string _tieuDeThongKe;
        public string TieuDeThongKe
        {
            get => _tieuDeThongKe;
            set { _tieuDeThongKe = value; OnPropertyChanged(); }
        }

        public DateTime TuNgay
        {
            get { return _tuNgay; }
            set { _tuNgay = value; OnPropertyChanged(); }
        }

        public DateTime DenNgay
        {
            get { return _denNgay; }
            set { _denNgay = value; OnPropertyChanged(); }
        }

        public int TongDoanhThu
        {
            get { return _tongDoanhThu; }
            set { _tongDoanhThu = value; OnPropertyChanged(); }
        }

        public bool IsLoading
        {
            get { return _isLoading; }
            set { _isLoading = value; OnPropertyChanged(); }
        }

        public ICommand TaiDuLieuCommand { get; }
        public ICommand LocTheoNgayCommand { get; }
        public ICommand LamMoiCommand { get; }
        public ICommand HomNayCommand { get; }
        public ICommand TuanNayCommand { get; }
        public ICommand ThangNayCommand { get; }
        public ICommand ThongKeTheoNgayCommand { get; }
        public ICommand ExportExcelCommand { get; }
        public ICommand XemChiTietCommand { get; }

        public BaoCaoViewModel()
        {
            DanhSachHoaDon = new ObservableCollection<HoaDon>();
            TuNgay = DateTime.Now.AddDays(-7);
            DenNgay = DateTime.Now;

            TaiDuLieuCommand = new RelayCommand(_ => TaiDuLieu());
            LocTheoNgayCommand = new RelayCommand(_ => LocTheoNgay());
            LamMoiCommand = new RelayCommand(_ => LamMoi());
            ThongKeCommand = new RelayCommand(_ => ThongKeTheoKhoang());
            HomNayCommand = new RelayCommand(_ => ThongKeHomNay());
            TuanNayCommand = new RelayCommand(_ => ThongKeTuanNay());
            ThangNayCommand = new RelayCommand(_ => ThongKeThangNay());
            ThongKeTheoNgayCommand = new RelayCommand(_ => ThongKeTheoKhoang());
            ExportExcelCommand = new RelayCommand(_ => ExportExcel());
            XemChiTietCommand = new RelayCommand(p => XemChiTiet(p as HoaDon));

            TaiDuLieu();
        }

        private void XemChiTiet(HoaDon hd)
        {
            if (hd == null) return;

            try
            {
                using var conn = new SqliteConnection(DatabaseConfig.ConnectionString);
                conn.Open();

                // 1. Lấy tên khách hàng
                string tenKhachHang = "Khách vãng lai";
                if (hd.MaKH != 0)
                {
                    var kh = conn.QueryFirstOrDefault<string>("SELECT TenKH FROM KhachHang WHERE MaKH = @MaKH", new { hd.MaKH });
                    if (!string.IsNullOrEmpty(kh)) tenKhachHang = kh;
                }

                // 2. LẤY ĐÚNG TÊN NHÂN VIÊN ĐÃ LẬP HÓA ĐƠN TRONG QUÁ KHỨ
                string tenNhanVien = "Không xác định";
                string queryNhanVien = @"
                    SELECT nv.TenNV 
                    FROM HoaDon hd 
                    JOIN NhanVien nv ON hd.MaNV = nv.MaNV 
                    WHERE hd.MaHD = @MaHD";

                var nv = conn.QueryFirstOrDefault<string>(queryNhanVien, new { hd.MaHD });
                if (!string.IsNullOrEmpty(nv)) tenNhanVien = nv;

                // 3. Lấy chi tiết sản phẩm
                string queryChiTiet = @"
                    SELECT sp.TenSP, ct.SoLuong, ct.DonGia, (ct.SoLuong * ct.DonGia) as ThanhTien 
                    FROM ChiTietHoaDon ct 
                    JOIN SanPham sp ON ct.MaSP = sp.MaSP 
                    WHERE ct.MaHD = @MaHD";

                var chiTietList = conn.Query(queryChiTiet, new { hd.MaHD }).ToList();

                // 4. Xử lý logic hiển thị
                string ngayLapFormat = hd.NgayLap; // Lấy trực tiếp chuỗi đã format sẵn
                string receipt = $"\n                   MÃ HÓA ĐƠN: HD{hd.MaHD:D3}\n" +
                                 "------------------------------------------------\n" +
                                 $"Ngày lập:   {ngayLapFormat}\n" +
                                 $"Nhân viên:  {tenNhanVien}\n" +
                                 $"Khách hàng: {tenKhachHang}\n" +
                                 "------------------------------------------------\n";

                if (chiTietList.Count > 0)
                {
                    foreach (var item in chiTietList)
                    {
                        receipt += $"- {item.TenSP}\n" +
                                   $"  SL: {item.SoLuong} | Giá: {item.DonGia:N0}đ | Tổng: {item.ThanhTien:N0}đ\n";
                    }
                }
                else
                {
                    receipt += "(Đang cập nhật chi tiết sản phẩm...)\n";
                }

                receipt += "------------------------------------------------\n" +
                           $"TỔNG CỘNG:             {hd.TongTien:N0} VNĐ\n";

                MessageBox.Show(receipt, "HÓA ĐƠN BÁN HÀNG", MessageBoxButton.OK, MessageBoxImage.None);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải chi tiết hóa đơn: {ex.Message}\n(Lưu ý: Kiểm tra lại tên bảng NhanVien và cột TenNV xem có khớp với CSDL không)", "Lỗi truy vấn", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ThongKe()
        {
            try
            {
                IsLoading = true;

                if (DanhSachHoaDon.Count == 0)
                {
                    ColumnSeries = null;
                    PieSeries = null;
                    OnPropertyChanged(nameof(ColumnSeries));
                    OnPropertyChanged(nameof(PieSeries));
                    return;
                }

                var data = DanhSachHoaDon.Select(hd => new
                {
                    Ngay = DateTime.ParseExact(hd.NgayLap, "dd-MM-yyyy HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture),
                    hd.TongTien
                }).ToList();

                var grouped = KieuThongKe switch
                {
                    "Ngay" => data.GroupBy(x => x.Ngay.Date).OrderBy(g => g.Key).Select(g => new { Label = g.Key.ToString("dd/MM"), Value = g.Sum(x => x.TongTien) }),
                    "Tuan" => data.GroupBy(x => System.Globalization.CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(x.Ngay, System.Globalization.CalendarWeekRule.FirstDay, DayOfWeek.Monday)).OrderBy(g => g.Key).Select(g => new { Label = "Tuần " + g.Key, Value = g.Sum(x => x.TongTien) }),
                    "Thang" => data.GroupBy(x => new { x.Ngay.Year, x.Ngay.Month }).OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month).Select(g => new { Label = $"{g.Key.Month}/{g.Key.Year}", Value = g.Sum(x => x.TongTien) }),
                    "Nam" => data.GroupBy(x => x.Ngay.Year).OrderBy(g => g.Key).Select(g => new { Label = g.Key.ToString(), Value = g.Sum(x => x.TongTien) }),
                    _ => data.GroupBy(x => x.Ngay.Date).Select(g => new { Label = g.Key.ToString("dd/MM"), Value = g.Sum(x => x.TongTien) })
                };

                var labels = grouped.Select(x => x.Label).ToArray();
                var values = grouped.Select(x => x.Value).ToArray();

                string FormatTien(double val)
                {
                    if (val >= 1000000)
                        return (val / 1000000D).ToString("0.##") + " M";
                    if (val >= 1000)
                        return (val / 1000D).ToString("0.##") + " K";
                    return val.ToString("N0");
                }

                ColumnSeries = new ISeries[]
                {
                    new ColumnSeries<int>
                    {
                        Values = values,
                        YToolTipLabelFormatter = point => FormatTien(Convert.ToDouble(point.Model))
                    }
                };

                XAxes = new Axis[] { new Axis { Labels = labels, LabelsRotation = 45 } };

                PieSeries = values.Select((v, i) => new PieSeries<int>
                {
                    Values = new[] { v },
                    Name = labels[i],
                    ToolTipLabelFormatter = point => FormatTien(Convert.ToDouble(point.Model))
                }).ToArray();

                OnPropertyChanged(nameof(ColumnSeries));
                OnPropertyChanged(nameof(PieSeries));
                OnPropertyChanged(nameof(XAxes));
            }
            catch (Exception ex) { MessageBox.Show($"Lỗi vẽ biểu đồ: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error); }
            finally { IsLoading = false; }
        }

        private void XuLyThongKe(DateTime from, DateTime to, string title)
        {
            if (from > to)
            {
                MessageBox.Show("Khoảng thời gian không hợp lệ (Từ ngày phải nhỏ hơn Đến ngày)!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var data = _allHoaDon
                .Select(hd => new
                {
                    hd.MaHD,
                    Ngay = DateTime.Parse(hd.NgayLap),
                    hd.TongTien,
                    hd.MaKH
                })
                .Where(x => x.Ngay >= from && x.Ngay <= to)
                .OrderByDescending(x => x.Ngay)
                .ToList();

            DanhSachHoaDon.Clear();

            if (data.Count == 0)
            {
                TongHoaDon = 0;
                TongDoanhThu = 0;
                KhachHangTop = "Không có";
                TieuDeThongKe = title;
                ThongKe();

                MessageBox.Show("Không có doanh thu nào được ghi nhận trong khoảng thời gian này.", "Dữ liệu trống", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            int stt = 1;
            foreach (var item in data)
            {
                DanhSachHoaDon.Add(new HoaDon
                {
                    STT = stt++,
                    MaHD = item.MaHD,
                    NgayLap = item.Ngay.ToString("dd-MM-yyyy HH:mm:ss"),
                    TongTien = item.TongTien,
                    MaKH = item.MaKH
                });
            }

            TongHoaDon = data.Count;
            TongDoanhThu = data.Sum(x => x.TongTien);

            var top = data.GroupBy(x => x.MaKH).OrderByDescending(g => g.Count()).FirstOrDefault();
            KhachHangTop = top != null && top.Key != 0 ? $"KH{top.Key:D3} ({top.Count()} hóa đơn)" : "Không có";

            TieuDeThongKe = title;
            ThongKe();
        }

        private void ThongKeHomNay()
        {
            var today = DateTime.Today;
            TuNgay = today; DenNgay = today;
            XuLyThongKe(today, today.AddDays(1).AddSeconds(-1), "TỔNG DOANH THU HÔM NAY");
        }

        private void ThongKeTuanNay()
        {
            var start = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek + 1);
            var end = start.AddDays(6);
            TuNgay = start; DenNgay = end;
            XuLyThongKe(start, end.AddDays(1).AddSeconds(-1), "TỔNG DOANH THU TUẦN NÀY");
        }

        private void ThongKeThangNay()
        {
            var start = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var end = start.AddMonths(1).AddDays(-1);
            TuNgay = start; DenNgay = end;
            XuLyThongKe(start, end.AddDays(1).AddSeconds(-1), "TỔNG DOANH THU THÁNG NÀY");
        }

        private void ThongKeTheoKhoang()
        {
            XuLyThongKe(TuNgay, DenNgay.AddDays(1).AddSeconds(-1), $"TỔNG DOANH THU TỪ {TuNgay:dd/MM/yyyy} ĐẾN {DenNgay:dd/MM/yyyy}");
        }

        // =========================================================
        // ĐÃ CẬP NHẬT: HÀM XUẤT EXCEL SỬ DỤNG CLOSEDXML
        // =========================================================
        private void ExportExcel()
        {
            try
            {
                if (DanhSachHoaDon == null || DanhSachHoaDon.Count == 0)
                {
                    MessageBox.Show("Không có dữ liệu để xuất Excel!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                Microsoft.Win32.SaveFileDialog dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Excel file (*.xlsx)|*.xlsx",
                    FileName = $"BaoCaoDoanhThu_{DateTime.Now:ddMMyyyy}.xlsx",
                    DefaultExt = ".xlsx",
                };

                if (dialog.ShowDialog() != true) return;

                // Khởi tạo file Excel mới bằng ClosedXML (Hoàn toàn miễn phí, không lo bản quyền)
                using (var workbook = new XLWorkbook())
                {
                    var ws = workbook.Worksheets.Add("BaoCaoDoanhThu");

                    // 1. Tạo Header
                    ws.Cell(1, 1).Value = "STT";
                    ws.Cell(1, 2).Value = "Mã Hóa Đơn";
                    ws.Cell(1, 3).Value = "Mã KH";
                    ws.Cell(1, 4).Value = "Ngày Lập";
                    ws.Cell(1, 5).Value = "Tổng Tiền (VNĐ)";

                    // 2. Style cho Header (In đậm, căn giữa, nền xám nhạt)
                    var headerRange = ws.Range(1, 1, 1, 5);
                    headerRange.Style.Font.Bold = true;
                    headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

                    // 3. Đổ dữ liệu
                    int row = 2;
                    foreach (var hd in DanhSachHoaDon)
                    {
                        ws.Cell(row, 1).Value = hd.STT;
                        ws.Cell(row, 2).Value = $"HD{hd.MaHD:D3}";
                        ws.Cell(row, 3).Value = $"KH{hd.MaKH:D3}";
                        ws.Cell(row, 4).Value = hd.NgayLap;
                        ws.Cell(row, 5).Value = hd.TongTien;
                        row++;
                    }

                    // 4. Format cột Tổng tiền hiển thị dấu phẩy hàng nghìn
                    ws.Column(5).Style.NumberFormat.Format = "#,##0";

                    // Tự động căn chỉnh độ rộng các cột
                    ws.Columns().AdjustToContents();

                    // 5. Lưu File
                    workbook.SaveAs(dialog.FileName);
                }

                MessageBox.Show("Xuất file Excel thành công!", "Hoàn tất", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi xuất Excel: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TaiDuLieu()
        {
            try
            {
                IsLoading = true;
                using var conn = new SqliteConnection(DatabaseConfig.ConnectionString);
                _allHoaDon = conn.Query<HoaDon>("SELECT * FROM HoaDon").ToList();

                ThongKeTheoKhoang();
            }
            catch (Exception ex) { MessageBox.Show($"Lỗi kết nối cơ sở dữ liệu: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error); }
            finally { IsLoading = false; }
        }

        private void LocTheoNgay() { ThongKeTheoKhoang(); }

        private void LamMoi()
        {
            TuNgay = DateTime.Now.AddDays(-7);
            DenNgay = DateTime.Now;
            TaiDuLieu();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
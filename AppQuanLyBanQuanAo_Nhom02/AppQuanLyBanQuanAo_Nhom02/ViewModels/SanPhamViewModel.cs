using AppQuanLyBanQuanAo_Nhom02.Data;
using AppQuanLyBanQuanAo_Nhom02.Models;
using AppQuanLyBanQuanAo_Nhom02.Untils;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace AppQuanLyBanQuanAo_Nhom02.ViewModels
{
    public partial class SanPhamViewModel : ObservableObject
    {
        // THUỘC TÍNH PHÂN QUYỀN (Binding sang giao diện)
        [ObservableProperty]
        private bool isAdmin;

        [ObservableProperty]
        private string searchKeyword = "";

        // Tự động tìm kiếm khi gõ phím (Live Search)
        partial void OnSearchKeywordChanged(string value)
        {
            LoadSanPham();
        }

        [ObservableProperty]
        private ObservableCollection<SanPham> danhSachSanPham = new();

        [ObservableProperty]
        private ObservableCollection<LoaiSanPham> danhSachLoai = new();

        [ObservableProperty]
        private SanPham currentSanPham = new SanPham();

        public SanPhamViewModel()
        {
            // =======================================================
            // LẤY QUYỀN HẠN TỪ TÀI KHOẢN ĐĂNG NHẬP 
            // =======================================================
            if (AppSession.CurrentUser != null)
            {
                // Giả sử Role = 1 là Quản lý, Role = 0 là Nhân viên
                IsAdmin = (AppSession.CurrentUser.Role == 1);
            }
            else
            {
                // Nếu chưa có ai đăng nhập (lúc đang thiết kế), mặc định ẩn hoặc hiện tùy bạn.
                // Ở đây mình set thử = false để bạn thấy các nút bị ẩn đi.
                IsAdmin = false;
            }

            LoadLoaiSanPham();
            LoadSanPham();

            // ĐĂNG KÝ LẮNG NGHE
            WeakReferenceMessenger.Default.Register<KhoHangChangedMessage>(this, (recipient, message) =>
            {
                LoadSanPham(); // Gọi đúng tên hàm tải lại dữ liệu DataGrid của form Sản Phẩm
            });
        }

        // Kích hoạt chống Crash Database
        private void EnableWalMode(SqliteConnection conn)
        {
            conn.Execute("PRAGMA journal_mode = WAL;");
            conn.Execute("PRAGMA synchronous = NORMAL;");
            conn.Execute("PRAGMA busy_timeout = 5000;");
        }

        // Tự động viết hoa chữ cái đầu
        private string ChuanHoaTen(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";
            text = text.Trim();
            return char.ToUpper(text[0]) + text.Substring(1);
        }

        private void LoadLoaiSanPham()
        {
            using var conn = new SqliteConnection(DatabaseConfig.ConnectionString);
            var loais = conn.Query<LoaiSanPham>("SELECT * FROM LoaiSanPham").ToList();
            DanhSachLoai = new ObservableCollection<LoaiSanPham>(loais);
        }

        private void LoadSanPham()
        {
            try
            {
                using (var conn = new SqliteConnection(DatabaseConfig.ConnectionString))
                {
                    conn.Open();
                    EnableWalMode(conn);

                    // 1. KÉO TOÀN BỘ DỮ LIỆU TỪ DATABASE LÊN TRƯỚC
                    string sql = @"
                        SELECT sp.*, lsp.TenLoai 
                        FROM SanPham sp 
                        INNER JOIN LoaiSanPham lsp ON sp.MaLoai = lsp.MaLoai";

                    var sps = conn.Query<SanPham>(sql).ToList();

                    // 2. DÙNG C# ĐỂ LỌC TÌM KIẾM (Khắc phục lỗi phân biệt hoa/thường tiếng Việt của SQLite)
                    if (!string.IsNullOrWhiteSpace(SearchKeyword))
                    {
                        // Ép từ khóa người dùng nhập về chữ thường (ví dụ: "ÁO" -> "áo")
                        string keyword = SearchKeyword.Trim().ToLower();

                        // Lọc trong danh sách: Nếu Tên SP hoặc Tên Loại chứa từ khóa thì giữ lại
                        sps = sps.Where(sp =>
                            (sp.TenSP != null && sp.TenSP.ToLower().Contains(keyword)) ||
                            (sp.TenLoai != null && sp.TenLoai.ToLower().Contains(keyword))
                        ).ToList();
                    }

                    // 3. Đánh Số thứ tự (STT) cho danh sách đã được lọc
                    for (int i = 0; i < sps.Count; i++)
                    {
                        sps[i].STT = i + 1;
                    }

                    DanhSachSanPham = new ObservableCollection<SanPham>(sps);
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Lỗi tải danh sách sản phẩm: " + ex.Message, "Lỗi Database", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void Add()
        {
            if (string.IsNullOrWhiteSpace(CurrentSanPham?.TenSP) || CurrentSanPham?.MaLoai == 0)
            {
                MessageBox.Show("Vui lòng nhập Tên sản phẩm và chọn Loại sản phẩm!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var conn = new SqliteConnection(DatabaseConfig.ConnectionString))
                {
                    conn.Open();
                    EnableWalMode(conn);

                    // === CHUẨN HÓA DỮ LIỆU ===
                    CurrentSanPham.TenSP = ChuanHoaTen(CurrentSanPham.TenSP);
                    CurrentSanPham.MauSac = ChuanHoaTen(CurrentSanPham.MauSac); // <-- Tự động viết hoa chữ đầu ô Màu sắc

                    // XỬ LÝ AN TOÀN: Nếu để trống thì tự động gán là 0 trước khi lưu
                    CurrentSanPham.GiaBan ??= 0;
                    CurrentSanPham.SoLuongTon ??= 0;

                    // KIỂM TRA TRÙNG LẶP (Giữ nguyên logic 4 điều kiện khắt khe)
                    string checkSql = @"SELECT COUNT(1) FROM SanPham 
                                        WHERE TenSP = @TenSP 
                                        AND MaLoai = @MaLoai 
                                        AND IFNULL(Size, '') = IFNULL(@Size, '') 
                                        AND IFNULL(MauSac, '') = IFNULL(@MauSac, '')";

                    int count = conn.ExecuteScalar<int>(checkSql, CurrentSanPham);

                    if (count > 0)
                    {
                        MessageBox.Show("Sản phẩm này đã có trong danh sách.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    string sql = @"INSERT INTO SanPham (TenSP, GiaBan, SoLuongTon, Size, MauSac, MaLoai, HinhAnh) 
                                   VALUES (@TenSP, @GiaBan, @SoLuongTon, @Size, @MauSac, @MaLoai, @HinhAnh)";
                    conn.Execute(sql, CurrentSanPham);
                }

                MessageBox.Show("Thêm sản phẩm thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                Refresh();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Lỗi hệ thống khi thêm: " + ex.Message, "Lỗi Database", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void Update()
        {
            if (CurrentSanPham == null || CurrentSanPham.MaSP == 0)
            {
                MessageBox.Show("Vui lòng chọn một sản phẩm trong danh sách để cập nhật!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(CurrentSanPham.TenSP))
            {
                MessageBox.Show("Vui lòng nhập Tên sản phẩm!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var conn = new SqliteConnection(DatabaseConfig.ConnectionString))
                {
                    conn.Open();
                    EnableWalMode(conn);

                    // === CHUẨN HÓA DỮ LIỆU ===
                    // Cập nhật trực tiếp lên CurrentSanPham để dữ liệu đồng bộ khi gọi checkSql và Update
                    CurrentSanPham.TenSP = ChuanHoaTen(CurrentSanPham.TenSP);
                    CurrentSanPham.MauSac = ChuanHoaTen(CurrentSanPham.MauSac); // <-- Tự động viết hoa chữ đầu ô Màu sắc

                    string tenMoi = CurrentSanPham.TenSP;
                    int? giaMoi = CurrentSanPham.GiaBan ?? 0;
                    int? tonMoi = CurrentSanPham.SoLuongTon ?? 0;

                    // 1. LẤY DỮ LIỆU GỐC TỪ DATABASE ĐỂ SO SÁNH
                    var spGoc = conn.QueryFirstOrDefault<SanPham>("SELECT * FROM SanPham WHERE MaSP = @MaSP", new { MaSP = CurrentSanPham.MaSP });

                    if (spGoc != null &&
                        spGoc.TenSP == tenMoi &&
                        (spGoc.GiaBan ?? 0) == giaMoi &&
                        (spGoc.SoLuongTon ?? 0) == tonMoi &&
                        IFNullMatch(spGoc.Size, CurrentSanPham.Size) &&
                        IFNullMatch(spGoc.MauSac, CurrentSanPham.MauSac) &&
                        spGoc.MaLoai == CurrentSanPham.MaLoai &&
                        IFNullMatch(spGoc.HinhAnh, CurrentSanPham.HinhAnh)) // <-- BỔ SUNG THÊM DÒNG NÀY (Kiểm tra xem ảnh có bị thay đổi không)
                    {
                        // THÔNG BÁO ĐÚNG NHƯ BẠN YÊU CẦU
                        MessageBox.Show("Sản phẩm chưa có thao tác cập nhật mới.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    // 2. KIỂM TRA TRÙNG LẶP VỚI CÁC SẢN PHẨM KHÁC (NẾU CÓ THAY ĐỔI)
                    string checkSql = @"SELECT COUNT(1) FROM SanPham 
                                        WHERE TenSP = @TenSP 
                                        AND MaLoai = @MaLoai 
                                        AND IFNULL(Size, '') = IFNULL(@Size, '') 
                                        AND IFNULL(MauSac, '') = IFNULL(@MauSac, '')
                                        AND MaSP != @MaSP";

                    int count = conn.ExecuteScalar<int>(checkSql, new
                    {
                        TenSP = tenMoi,
                        MaLoai = CurrentSanPham.MaLoai,
                        Size = CurrentSanPham.Size,
                        MauSac = CurrentSanPham.MauSac,
                        MaSP = CurrentSanPham.MaSP
                    });

                    if (count > 0)
                    {
                        MessageBox.Show("Sản phẩm đã có trong danh sách sản phẩm.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);

                        // Giữ nguyên dữ liệu cũ bằng cách tải lại từ DB
                        int idHienTai = CurrentSanPham.MaSP;
                        LoadSanPham();
                        CurrentSanPham = DanhSachSanPham.FirstOrDefault(sp => sp.MaSP == idHienTai) ?? new SanPham();
                        return;
                    }

                    // 3. THỰC HIỆN CẬP NHẬT
                    string sql = @"UPDATE SanPham SET TenSP=@TenSP, GiaBan=@GiaBan, SoLuongTon=@SoLuongTon, 
                                   Size=@Size, MauSac=@MauSac, MaLoai=@MaLoai, HinhAnh=@HinhAnh WHERE MaSP=@MaSP";

                    conn.Execute(sql, new
                    {
                        TenSP = tenMoi,
                        GiaBan = giaMoi,
                        SoLuongTon = tonMoi,
                        Size = CurrentSanPham.Size,
                        MauSac = CurrentSanPham.MauSac,
                        MaLoai = CurrentSanPham.MaLoai,
                        HinhAnh = CurrentSanPham.HinhAnh,
                        MaSP = CurrentSanPham.MaSP
                    });
                }

                MessageBox.Show("Cập nhật thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                Refresh();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Lỗi hệ thống khi cập nhật: " + ex.Message, "Lỗi Database", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Hàm phụ trợ để so sánh chuỗi có thể null
        private bool IFNullMatch(string a, string b)
        {
            return (a ?? "").Trim() == (b ?? "").Trim();
        }

        [RelayCommand]
        private void Delete(System.Collections.IList selectedItems) // Nhận danh sách các dòng được bôi xanh
        {
            // Kiểm tra xem người dùng đã chọn gì chưa
            if (selectedItems == null || selectedItems.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn ít nhất một sản phẩm để xóa!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Chuyển đổi danh sách bôi xanh thành danh sách Sản Phẩm (List<SanPham>)
            var listToDelete = selectedItems.Cast<SanPham>().ToList();

            // Hiển thị câu hỏi xác nhận thông minh (Xóa 1 cái hay xóa nhiều cái)
            if (listToDelete.Count == 1)
            {
                var sp = listToDelete[0];
                var result = MessageBox.Show($"Bạn có chắc chắn muốn xóa sản phẩm '{sp.TenSP}' không?", "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result != MessageBoxResult.Yes) return;
            }
            else
            {
                var result = MessageBox.Show($"Bạn đang chọn {listToDelete.Count} sản phẩm.\nBạn có chắc chắn muốn xóa toàn bộ chúng không?", "Xác nhận xóa hàng loạt", MessageBoxButton.YesNo, MessageBoxImage.Information);
                if (result != MessageBoxResult.Yes) return;
            }

            try
            {
                using (var conn = new SqliteConnection(DatabaseConfig.ConnectionString))
                {
                    conn.Open();
                    EnableWalMode(conn);

                    // Sử dụng Transaction (Giao dịch) để xóa an toàn. 
                    // Nếu đang xóa giữa chừng mà cúp điện thì nó sẽ hoàn tác (Rollback) lại dữ liệu.
                    using (var transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            string sql = "DELETE FROM SanPham WHERE MaSP = @MaSP";

                            // Thư viện Dapper cực kỳ thông minh, truyền nguyên 1 List vào nó sẽ tự động chạy lệnh Delete cho từng cái
                            conn.Execute(sql, listToDelete, transaction);

                            transaction.Commit(); // Xác nhận xóa thành công
                        }
                        catch
                        {
                            transaction.Rollback(); // Có lỗi thì khôi phục lại
                            throw;
                        }
                    }
                }

                MessageBox.Show($"Đã xóa thành công {listToDelete.Count} sản phẩm!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                Refresh(); // Tải lại bảng
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("FOREIGN KEY"))
                {
                    MessageBox.Show("Không thể xóa sản phẩm này vì đã phát sinh lịch sử Nhập/Xuất kho!\nNếu không bán nữa, hãy chuyển trạng thái sang 'Ngừng kinh doanh'.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    MessageBox.Show($"Lỗi hệ thống khi xóa: {ex.Message}", "Lỗi Database", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        [RelayCommand]
        private void ChonAnh()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "Chọn hình ảnh sản phẩm";
            // Chỉ cho phép chọn các định dạng ảnh phổ biến
            openFileDialog.Filter = "Image files (*.png;*.jpeg;*.jpg)|*.png;*.jpeg;*.jpg|All files (*.*)|*.*";

            if (openFileDialog.ShowDialog() == true)
            {
                // Gán đường dẫn ảnh vừa chọn vào thuộc tính HinhAnh của sản phẩm hiện tại
                CurrentSanPham.HinhAnh = openFileDialog.FileName;

                // Mẹo cực kỳ quan trọng: Ép Giao diện (UI) phải load lại ngay lập tức
                // Vì HinhAnh là thuộc tính con nằm sâu bên trong CurrentSanPham
                OnPropertyChanged(nameof(CurrentSanPham));
            }
        }

        [RelayCommand]
        private void Refresh()
        {
            // --- THÊM DÒNG NÀY ĐỂ CẬP NHẬT COMBOBOX LOẠI SẢN PHẨM ---
            // Gọi lệnh này để kéo lại toàn bộ data mới nhất từ bảng LoaiSanPham lên
            LoadLoaiSanPham();

            if (!string.IsNullOrEmpty(SearchKeyword))
            {
                SearchKeyword = ""; // Khi gán = rỗng, OnSearchKeywordChanged sẽ tự động chạy và gọi LoadSanPham()
            }
            else
            {
                LoadSanPham(); // Đồng thời Load lại bảng DataGrid để cập nhật tên Loại mới (nhờ câu lệnh JOIN)
            }

            CurrentSanPham = new SanPham();
        }

        // --- CÁC BIẾN VÀ LỆNH CHO TÍNH NĂNG PHÓNG TO ẢNH ---

        [ObservableProperty]
        private bool isImagePopupOpen = false;

        [ObservableProperty]
        private string popupImageUrl = "";

        [RelayCommand]
        private void XemAnhTo(string urlAnh)
        {
            if (!string.IsNullOrWhiteSpace(urlAnh))
            {
                PopupImageUrl = urlAnh;
                IsImagePopupOpen = true; // Hiện lớp phủ
            }
        }

        [RelayCommand]
        private void DongAnhTo()
        {
            IsImagePopupOpen = false; // Ẩn lớp phủ
            PopupImageUrl = "";
        }
    }
}
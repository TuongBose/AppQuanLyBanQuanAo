using AppQuanLyBanQuanAo_Nhom02.Models;
using AppQuanLyBanQuanAo_Nhom02.Data;
using AppQuanLyBanQuanAo_Nhom02.Untils; // Đã thêm để lấy thông tin đăng nhập
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Data.Sqlite;
using Dapper;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace AppQuanLyBanQuanAo_Nhom02.ViewModels
{
    public partial class LoaiSanPhamViewModel : ObservableObject
    {
        // THUỘC TÍNH PHÂN QUYỀN
        [ObservableProperty]
        private bool isAdmin;

        [ObservableProperty]
        private ObservableCollection<LoaiSanPham> danhSachLoai = new();

        [ObservableProperty]
        private LoaiSanPham currentLoai = new();

        [ObservableProperty]
        private string searchKeyword = "";

        partial void OnSearchKeywordChanged(string value)
        {
            LoadDanhSach();
        }

        public LoaiSanPhamViewModel()
        {
            // LẤY QUYỀN HẠN TỪ TÀI KHOẢN ĐĂNG NHẬP 
            if (AppSession.CurrentUser != null)
            {
                IsAdmin = (AppSession.CurrentUser.Role == 1);
            }
            else
            {
                // Mặc định để false để test giao diện User bị mờ
                IsAdmin = false;
            }

            LoadDanhSach();
        }

        private void EnableWalMode(SqliteConnection conn)
        {
            conn.Execute("PRAGMA journal_mode = WAL;");
            conn.Execute("PRAGMA synchronous = NORMAL;");
            conn.Execute("PRAGMA busy_timeout = 5000;");
        }

        // TỰ ĐỘNG VIẾT HOA CHỮ CÁI ĐẦU TIÊN
        private string ChuanHoaTen(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";

            text = text.Trim();
            return char.ToUpper(text[0]) + text.Substring(1);
        }

        private void LoadDanhSach()
        {
            try
            {
                using (var conn = new SqliteConnection(DatabaseConfig.ConnectionString))
                {
                    conn.Open();
                    EnableWalMode(conn);

                    string sql = @"
                        SELECT lsp.*, 
                               (SELECT COUNT(*) FROM SanPham sp WHERE sp.MaLoai = lsp.MaLoai) as SoLuongSanPham
                        FROM LoaiSanPham lsp";

                    if (!string.IsNullOrWhiteSpace(SearchKeyword))
                    {
                        sql += " WHERE lsp.TenLoai LIKE @Keyword";
                    }

                    var loais = conn.Query<LoaiSanPham>(sql, new { Keyword = $"%{SearchKeyword.Trim()}%" }).ToList();

                    for (int i = 0; i < loais.Count; i++)
                    {
                        loais[i].STT = i + 1;
                    }

                    DanhSachLoai = new ObservableCollection<LoaiSanPham>(loais);
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Lỗi tải danh sách: " + ex.Message, "Lỗi Database", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void Add()
        {
            if (string.IsNullOrWhiteSpace(CurrentLoai?.TenLoai))
            {
                MessageBox.Show("Vui lòng nhập tên loại sản phẩm!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var conn = new SqliteConnection(DatabaseConfig.ConnectionString))
                {
                    conn.Open();
                    EnableWalMode(conn);

                    string tenLoaiMoi = ChuanHoaTen(CurrentLoai.TenLoai);

                    int count = conn.ExecuteScalar<int>("SELECT COUNT(1) FROM LoaiSanPham WHERE TenLoai = @TenLoai", new { TenLoai = tenLoaiMoi });
                    if (count > 0)
                    {
                        MessageBox.Show("Tên loại sản phẩm này đã tồn tại!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    string sql = "INSERT INTO LoaiSanPham (TenLoai) VALUES (@TenLoai)";
                    conn.Execute(sql, new { TenLoai = tenLoaiMoi });
                }

                MessageBox.Show("Thêm loại sản phẩm thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
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
            if (CurrentLoai == null || CurrentLoai.MaLoai == 0)
            {
                MessageBox.Show("Vui lòng chọn một loại sản phẩm từ danh sách để cập nhật!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(CurrentLoai.TenLoai))
            {
                MessageBox.Show("Vui lòng nhập tên loại sản phẩm!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var conn = new SqliteConnection(DatabaseConfig.ConnectionString))
                {
                    conn.Open();
                    EnableWalMode(conn);

                    string tenLoaiMoi = ChuanHoaTen(CurrentLoai.TenLoai);

                    string tenGocTrongDB = conn.ExecuteScalar<string>("SELECT TenLoai FROM LoaiSanPham WHERE MaLoai = @MaLoai", new { MaLoai = CurrentLoai.MaLoai });

                    if (tenGocTrongDB != null && tenGocTrongDB.Trim() == tenLoaiMoi)
                    {
                        MessageBox.Show("Chưa có chỉnh sửa mới cho loại sản phẩm.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    string checkSql = "SELECT COUNT(1) FROM LoaiSanPham WHERE TenLoai = @TenLoai AND MaLoai != @MaLoai";
                    int count = conn.ExecuteScalar<int>(checkSql, new { TenLoai = tenLoaiMoi, MaLoai = CurrentLoai.MaLoai });

                    if (count > 0)
                    {
                        MessageBox.Show("Tên loại sản phẩm này đã tồn tại ở một mục khác!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    string sql = "UPDATE LoaiSanPham SET TenLoai = @TenLoai WHERE MaLoai = @MaLoai";
                    conn.Execute(sql, new { TenLoai = tenLoaiMoi, MaLoai = CurrentLoai.MaLoai });
                }

                MessageBox.Show("Cập nhật thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                Refresh();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Lỗi hệ thống khi cập nhật: " + ex.Message, "Lỗi Database", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void Delete(System.Collections.IList selectedItems)
        {
            if (selectedItems == null || selectedItems.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn ít nhất một loại sản phẩm để xóa!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var listToDelete = selectedItems.Cast<LoaiSanPham>().ToList();

            if (listToDelete.Count == 1)
            {
                var loai = listToDelete[0];
                var result = MessageBox.Show($"Bạn có chắc chắn muốn xóa loại sản phẩm '{loai.TenLoai}' không?", "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result != MessageBoxResult.Yes) return;
            }
            else
            {
                var result = MessageBox.Show($"Bạn đang chọn {listToDelete.Count} loại sản phẩm.\nBạn có chắc chắn muốn xóa toàn bộ chúng không?", "Xác nhận xóa hàng loạt", MessageBoxButton.YesNo, MessageBoxImage.Information);
                if (result != MessageBoxResult.Yes) return;
            }

            try
            {
                using (var conn = new SqliteConnection(DatabaseConfig.ConnectionString))
                {
                    conn.Open();
                    conn.Execute("PRAGMA journal_mode = WAL;");

                    using (var transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            string sql = "DELETE FROM LoaiSanPham WHERE MaLoai = @MaLoai";
                            conn.Execute(sql, listToDelete, transaction);
                            transaction.Commit();
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }

                MessageBox.Show($"Đã xóa thành công {listToDelete.Count} loại sản phẩm!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                Refresh();
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("FOREIGN KEY"))
                {
                    string thongBao = "Không thể xóa loại sản phẩm này vì vẫn đang có mặt hàng thuộc loại này trong hệ thống!\n\n";
                    MessageBox.Show(thongBao, "Từ chối xóa", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    MessageBox.Show($"Lỗi hệ thống khi xóa:\n{ex.Message}", "Lỗi Database", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        [RelayCommand]
        private void Refresh()
        {
            if (!string.IsNullOrEmpty(SearchKeyword))
            {
                SearchKeyword = "";
            }
            else
            {
                LoadDanhSach();
            }

            CurrentLoai = new LoaiSanPham();
        }
    }
}
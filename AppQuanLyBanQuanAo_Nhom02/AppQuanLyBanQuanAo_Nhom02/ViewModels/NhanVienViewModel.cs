using AppQuanLyBanQuanAo_Nhom02.Models;
using AppQuanLyBanQuanAo_Nhom02.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Data.Sqlite;
using Dapper;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace AppQuanLyBanQuanAo_Nhom02.ViewModels
{
    public partial class NhanVienViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<NhanVien> danhSachNhanVien = new();

        [ObservableProperty]
        private NhanVien currentNhanVien = new();

        [ObservableProperty]
        private string searchKeyword = "";

        // Hàm này TỰ ĐỘNG CHẠY mỗi khi ô tìm kiếm có sự thay đổi (bạn gõ thêm chữ hoặc xóa bớt)
        partial void OnSearchKeywordChanged(string value)
        {
            LoadDanhSach(); // Gọi lại hàm load dữ liệu để lọc
        }

        [ObservableProperty]
        private string registrationCode = "------"; // Mã đăng ký hiển thị trên UI

        public NhanVienViewModel()
        {
            LoadDanhSach();
        }

        private void LoadDanhSach()
        {
            using var conn = new SqliteConnection(DatabaseConfig.ConnectionString);

            string sql = "SELECT * FROM NhanVien";

            // Nếu có gõ từ khóa, thì thêm điều kiện tìm theo Tên hoặc Số điện thoại
            if (!string.IsNullOrWhiteSpace(SearchKeyword))
            {
                sql += " WHERE TenNV LIKE @Keyword OR SDT LIKE @Keyword";
            }

            // Keyword bọc trong % để tìm kiếm gần đúng (Ví dụ gõ "Vinh" sẽ ra "Phan Văn Vinh")
            var nvs = conn.Query<NhanVien>(sql, new { Keyword = $"%{SearchKeyword.Trim()}%" }).ToList();
            DanhSachNhanVien = new ObservableCollection<NhanVien>(nvs);
        }

        [RelayCommand]
        private void Update()
        {
            if (CurrentNhanVien == null || CurrentNhanVien.MaNV == 0)
            {
                MessageBox.Show("Vui lòng chọn nhân viên từ danh sách để cập nhật!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 1. Kiểm tra rỗng và định dạng SĐT (Gắn thêm Regex cho xịn)
            if (string.IsNullOrWhiteSpace(CurrentNhanVien.SDT) || !System.Text.RegularExpressions.Regex.IsMatch(CurrentNhanVien.SDT, @"^0\d{9}$"))
            {
                MessageBox.Show("Số điện thoại không hợp lệ!\nPhải gồm 10 chữ số và bắt đầu bằng số 0.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using var conn = new SqliteConnection(DatabaseConfig.ConnectionString);

            // 2. BƯỚC QUAN TRỌNG: Kiểm tra trùng SĐT nhưng LOẠI TRỪ chính nhân viên đang được sửa
            string checkSql = "SELECT COUNT(1) FROM NhanVien WHERE SDT = @SDT AND MaNV != @MaNV";
            int count = conn.ExecuteScalar<int>(checkSql, new { SDT = CurrentNhanVien.SDT, MaNV = CurrentNhanVien.MaNV });

            if (count > 0)
            {
                MessageBox.Show("Số điện thoại này đã thuộc về một nhân viên khác!\nVui lòng nhập số khác.", "Lỗi trùng lặp", MessageBoxButton.OK, MessageBoxImage.Error);
                return; // Dừng lại ngay, không cho lưu
            }

            // 3. Nếu mọi thứ an toàn, tiến hành lưu vào DB
            string sql = "UPDATE NhanVien SET TenNV = @TenNV, SDT = @SDT, Role = @Role, TrangThai = @TrangThai WHERE MaNV = @MaNV";
            conn.Execute(sql, CurrentNhanVien);

            MessageBox.Show("Cập nhật thông tin nhân viên thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            LoadDanhSach();
        }

        [RelayCommand]
        private void Delete(System.Collections.IList selectedItems)
        {
            // 1. Kiểm tra xem có nhân viên nào được chọn không
            if (selectedItems == null || selectedItems.Count == 0)
            {
                MessageBox.Show("Vui lòng bôi xanh ít nhất một nhân viên trong bảng để xóa!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 2. Chuyển đổi sang List nhân viên
            var listToDelete = selectedItems.Cast<NhanVien>().ToList();

            // 3. Hỏi xác nhận thông minh
            string mess = listToDelete.Count == 1
                ? $"Bạn có chắc muốn xóa nhân viên '{listToDelete[0].TenNV}'?"
                : $"Bạn có chắc muốn xóa {listToDelete.Count} nhân viên đang chọn?";

            if (MessageBox.Show(mess, "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            try
            {
                using (var conn = new SqliteConnection(DatabaseConfig.ConnectionString))
                {
                    conn.Open();
                    using (var transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            // Lệnh xóa nhân viên theo mã
                            string sql = "DELETE FROM NhanVien WHERE MaNV = @MaNV";

                            // Dapper tự động lặp qua danh sách và xóa từng người
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

                MessageBox.Show($"Đã xóa thành công {listToDelete.Count} nhân viên!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                Refresh(); // Tải lại danh sách
            }
            catch (Exception ex)
            {
                // Bắt lỗi nếu nhân viên này đã từng lập hóa đơn (Khóa ngoại)
                if (ex.Message.Contains("FOREIGN KEY"))
                {
                    string thongBao = "Không thể xóa nhân viên này vì họ đã có lịch sử lập hóa đơn bán hàng cho khách!\n\n";

                    MessageBox.Show(thongBao, "Từ chối xóa", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    MessageBox.Show($"Lỗi hệ thống khi xóa nhân viên: {ex.Message}", "Lỗi Database", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        [RelayCommand]
        private void Refresh() // Hàm làm mới dữ liệu
        {
            // Xóa trắng form nhập liệu bằng cách gán nó thành một nhân viên hoàn toàn mới
            CurrentNhanVien = new NhanVien();
            
            // Xóa nội dung thanh tìm kiếm (nếu bạn có tính năng tìm kiếm)
            SearchKeyword = string.Empty; 
            
            // Kéo lại dữ liệu sạch từ Database lên lưới
            LoadDanhSach(); // Lưu ý: Hàm tải dữ liệu của bạn có thể tên là LoadNhanVien() hoặc GetData() nhé
        }

        // ==========================================
        // CÁC QUYỀN ĐẶC BIỆT CỦA ADMIN
        // ==========================================

        [RelayCommand]
        private void CreateRegCode()
        {
            // Sinh mã ngẫu nhiên 6 chữ số
            Random rnd = new Random();
            RegistrationCode = rnd.Next(100000, 999999).ToString();

            try
            {
                using var conn = new SqliteConnection(DatabaseConfig.ConnectionString);

                // === ĐOẠN SỬA LỖI: ĐẬP BỎ BẢNG CŨ VÀ XÂY LẠI ===
                conn.Execute("DROP TABLE IF EXISTS MaXacNhan"); // Quét sạch bảng cũ bị sai tên cột
                conn.Execute("CREATE TABLE MaXacNhan (MaCode TEXT, NgayTao DATETIME)"); // Xây lại bảng chuẩn
                // ===============================================

                // Thêm mã mới vào
                conn.Execute("INSERT INTO MaXacNhan (MaCode, NgayTao) VALUES (@Code, datetime('now'))", new { Code = RegistrationCode });

                MessageBox.Show($"Đã tạo mã đăng ký: {RegistrationCode}\nHãy đưa mã này cho nhân viên để họ tự điền vào form đăng ký.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi tạo mã: " + ex.Message, "Lỗi Database", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void ResetPassword(System.Collections.IList selectedItems) // Tên hàm khớp với tên Command của bạn (bỏ chữ Command đi)
        {
            // Kiểm tra xem người dùng đã chọn gì chưa
            if (selectedItems == null || selectedItems.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn ít nhất một nhân viên để reset mật khẩu!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Chuyển đổi danh sách bôi xanh thành danh sách Nhân Viên
            var listToReset = selectedItems.Cast<NhanVien>().ToList();

            // Hiển thị câu hỏi xác nhận thông minh
            if (listToReset.Count == 1)
            {
                var nv = listToReset[0];
                var result = MessageBox.Show($"Bạn có chắc chắn muốn đưa mật khẩu của nhân viên '{nv.TenNV}' về mặc định (123456) không?", "Xác nhận Reset", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result != MessageBoxResult.Yes) return;
            }
            else
            {
                var result = MessageBox.Show($"Bạn đang chọn {listToReset.Count} nhân viên.\nBạn có chắc chắn muốn reset mật khẩu toàn bộ những tài khoản này về 123456 không?", "Xác nhận Reset hàng loạt", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result != MessageBoxResult.Yes) return;
            }

            try
            {
                using (var conn = new SqliteConnection(DatabaseConfig.ConnectionString))
                {
                    conn.Open();

                    // Sử dụng Transaction để bảo đảm an toàn dữ liệu
                    using (var transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            // Lệnh cập nhật mật khẩu về 123456 cho mã nhân viên tương ứng
                            string sql = "UPDATE NhanVien SET PasswordHash = '123456' WHERE MaNV = @MaNV";

                            // Dapper tự động lặp và chạy lệnh UPDATE cho từng nhân viên trong danh sách
                            conn.Execute(sql, listToReset, transaction);

                            transaction.Commit();
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }

                MessageBox.Show($"Đã reset mật khẩu thành công cho {listToReset.Count} nhân viên!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);

                // Cập nhật lại giao diện (Bỏ chọn các dòng đã bôi xanh)
                Refresh(); // Hoặc LoadDanhSach() tùy vào tên hàm của bạn
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Lỗi hệ thống khi reset mật khẩu: " + ex.Message, "Lỗi Database", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
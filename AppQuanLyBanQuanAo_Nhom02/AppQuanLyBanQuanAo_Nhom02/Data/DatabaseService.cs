using AppQuanLyBanQuanAo_Nhom02.Models;
using Dapper;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Text;

namespace AppQuanLyBanQuanAo_Nhom02.Data
{
    public class DatabaseService
    {
        /// <summary>
        /// Hàm kiểm tra thông tin đăng nhập của nhân viên
        /// </summary>
        /// <param name="username">Tài khoản</param>
        /// <param name="password">Mật khẩu</param>
        /// <returns>Trả về object NhanVien nếu đúng, trả về null nếu sai thông tin</returns>
        public NhanVien CheckLogin(string username, string password)
        {
            // Khởi tạo đường ống kết nối đến file SQLite dựa vào địa chỉ trong DatabaseConfig
            using (var connection = new SqliteConnection(DatabaseConfig.ConnectionString))
            {
                // Câu lệnh SQL: Tìm nhân viên khớp tài khoản, mật khẩu và đang làm việc (TrangThai = 1)
                // Lưu ý: Dùng @Username và @Password để chống Hacker tấn công SQL Injection
                string sql = @"SELECT * FROM NhanVien 
                               WHERE Username = @Username 
                               AND PasswordHash = @Password 
                               ";

                // Dapper sẽ thực thi lệnh SQL và tự động nhét dữ liệu vào class NhanVien
                var user = connection.QueryFirstOrDefault<NhanVien>(sql, new
                {
                    Username = username,
                    Password = password
                });

                return user; // Nếu không tìm thấy, nó sẽ tự trả về null
            }
        }

        /// <summary>
        /// Kiểm tra xem Username đã tồn tại trong Database chưa
        /// </summary>
        public bool CheckUsernameExist(string username)
        {
            using (var connection = new SqliteConnection(DatabaseConfig.ConnectionString))
            {
                // Đếm xem có bao nhiêu dòng có Username trùng khớp
                string sql = "SELECT COUNT(1) FROM NhanVien WHERE Username = @Username";
                int count = connection.ExecuteScalar<int>(sql, new { Username = username });

                return count > 0; // Trả về true nếu đã tồn tại
            }
        }

        /// <summary>
        /// Thêm một nhân viên mới vào Database (Mặc định Role = 0, TrangThai = 1)
        /// </summary>
        public bool RegisterEmployee(string tenNV, string sdt, string username, string password)
        {
            using (var connection = new SqliteConnection(DatabaseConfig.ConnectionString))
            {
                string sql = @"INSERT INTO NhanVien (TenNV, SDT, Username, PasswordHash, Role, TrangThai) 
                               VALUES (@TenNV, @SDT, @Username, @Password, 0, 1)";

                // Execute trả về số dòng bị ảnh hưởng. Nếu > 0 nghĩa là thêm thành công.
                int rowsAffected = connection.Execute(sql, new
                {
                    TenNV = tenNV,
                    SDT = sdt,
                    Username = username,
                    Password = password
                });

                return rowsAffected > 0;
            }
        }

        /// <summary>
        /// Kiểm tra xem Số điện thoại đã tồn tại trong Database chưa
        /// </summary>
        public bool CheckPhoneExist(string sdt)
        {
            using (var connection = new SqliteConnection(DatabaseConfig.ConnectionString))
            {
                // Truy vấn đếm số lượng dòng có SDT trùng khớp
                string sql = "SELECT COUNT(1) FROM NhanVien WHERE SDT = @SDT";
                int count = connection.ExecuteScalar<int>(sql, new { SDT = sdt });

                return count > 0; // Trả về true nếu đã tồn tại
            }
        }

        /// <summary>
        /// Cập nhật mật khẩu mới cho Nhân viên
        /// </summary>
        public bool UpdatePassword(int maNV, string newPassword)
        {
            using (var connection = new Microsoft.Data.Sqlite.SqliteConnection(DatabaseConfig.ConnectionString))
            {
                string sql = "UPDATE NhanVien SET PasswordHash = @Password WHERE MaNV = @MaNV";

                // Execute trả về số dòng bị ảnh hưởng
                int rowsAffected = Dapper.SqlMapper.Execute(connection, sql, new { Password = newPassword, MaNV = maNV });

                return rowsAffected > 0;
            }
        }
    }
}

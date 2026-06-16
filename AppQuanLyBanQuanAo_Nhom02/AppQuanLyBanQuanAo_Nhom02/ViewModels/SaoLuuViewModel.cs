using AppQuanLyBanQuanAo_Nhom02.Data;
using AppQuanLyBanQuanAo_Nhom02.Untils;
using Microsoft.Data.Sqlite;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace AppQuanLyBanQuanAo_Nhom02.ViewModels
{
    public class SaoLuuViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<BackupItem> _danhSachBackup;
        private string _duongDanBackup;
        private bool _isLoading = false;
        private int _tienDoSaoLuu = 0;
        private string _thoiGianCuoiSaoLuuFormatted = "00/00/0000 00:00:00";
        private long _dungLuongBackup = 0;
        private BackupItem _backupDangChon;

        public BackupItem BackupDangChon
        {
            get => _backupDangChon;
            set { _backupDangChon = value; OnPropertyChanged(); }
        }

        public ObservableCollection<BackupItem> DanhSachBackup
        {
            get { return _danhSachBackup; }
            set { _danhSachBackup = value; OnPropertyChanged(); }
        }

        public string DuongDanBackup
        {
            get { return _duongDanBackup; }
            set { _duongDanBackup = value; OnPropertyChanged(); }
        }

        public bool IsLoading
        {
            get { return _isLoading; }
            set { _isLoading = value; OnPropertyChanged(); }
        }

        public int TienDoSaoLuu
        {
            get { return _tienDoSaoLuu; }
            set { _tienDoSaoLuu = value; OnPropertyChanged(); }
        }

        public string ThoiGianCuoiSaoLuuFormatted
        {
            get { return _thoiGianCuoiSaoLuuFormatted; }
            set { _thoiGianCuoiSaoLuuFormatted = value; OnPropertyChanged(); }
        }

        public long DungLuongBackup
        {
            get { return _dungLuongBackup; }
            set
            {
                _dungLuongBackup = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DungLuongFormatted));
            }
        }

        public ICommand SaoLuuNgayCommand { get; }
        public ICommand PhucHoiCommand { get; }
        public ICommand ChonThuMucCommand { get; }
        public ICommand XoaBackupCommand { get; }
        public ICommand LamMoiCommand { get; }

        public SaoLuuViewModel()
        {
            DanhSachBackup = new ObservableCollection<BackupItem>();

            // THAY ĐỔI ĐƯỜNG DẪN MẶC ĐỊNH TẠI ĐÂY
            // Sử dụng @ phía trước chuỗi để C# hiểu đúng các dấu gạch chéo (\)
            DuongDanBackup = @"D:\CN.NET\BackupTest";

            SaoLuuNgayCommand = new RelayCommand(_ => SaoLuuNgay());
            PhucHoiCommand = new RelayCommand(p => PhucHoi(), p => p is System.Collections.IList list && list.Count == 1);
            ChonThuMucCommand = new RelayCommand(_ => ChonThuMuc());
            XoaBackupCommand = new RelayCommand(p => XoaBackup(p), p => p is System.Collections.IList list && list.Count > 0);
            LamMoiCommand = new RelayCommand(_ => TaiDuLieu());

            // Nếu thư mục này chưa tồn tại trên ổ D, hệ thống sẽ tự động tạo mới
            if (!Directory.Exists(DuongDanBackup))
                Directory.CreateDirectory(DuongDanBackup);

            TaiDuLieu();
        }

        private void TaiDuLieu()
        {
            try
            {
                IsLoading = true;
                DanhSachBackup.Clear();

                if (!Directory.Exists(DuongDanBackup))
                {
                    ThoiGianCuoiSaoLuuFormatted = "00/00/0000 00:00:00";
                    return;
                }

                var extensions = new[] { ".bak", ".db" };
                var files = Directory.GetFiles(DuongDanBackup)
                                     .Where(f => extensions.Contains(Path.GetExtension(f)))
                                     .OrderByDescending(f => new FileInfo(f).LastWriteTime)
                                     .ToArray();

                DungLuongBackup = 0;
                int stt = 1;

                foreach (var file in files)
                {
                    var fileInfo = new FileInfo(file);
                    DanhSachBackup.Add(new BackupItem
                    {
                        STT = stt++,
                        FileName = fileInfo.Name,
                        CreatedDate = fileInfo.LastWriteTime
                    });
                    DungLuongBackup += fileInfo.Length;
                }

                if (DanhSachBackup.Count > 0)
                {
                    // Lấy thời gian của bản sao lưu mới nhất định dạng dd/MM/yyyy HH:mm:ss
                    ThoiGianCuoiSaoLuuFormatted = DanhSachBackup[0].CreatedDate.ToString("dd/MM/yyyy HH:mm:ss");
                }
                else
                {
                    // Trả về mặc định nếu không có file nào
                    ThoiGianCuoiSaoLuuFormatted = "00/00/0000 00:00:00";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải dữ liệu: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { IsLoading = false; }
        }

        private void SaoLuuNgay()
        {
            try
            {
                IsLoading = true;
                TienDoSaoLuu = 0;

                // 1. Cập nhật tên file tự sinh theo chuẩn: ngày-tháng-năm_giờ-phút-giây
                string backupFileName = $"Backup_{DateTime.Now:dd-MM-yyyy_HH-mm-ss}.db";
                string backupPath = Path.Combine(DuongDanBackup, backupFileName);

                // 2. Thêm tham số "Pooling=False" cho file đích để nhả file ngay lập tức sau khi backup
                using (var source = new SqliteConnection(DatabaseConfig.ConnectionString))
                using (var destination = new SqliteConnection($"Data Source={backupPath};Pooling=False;"))
                {
                    source.Open();
                    destination.Open();
                    source.BackupDatabase(destination);
                }

                // 3. Ép dọn dẹp các kết nối ngầm còn sót lại để mở khóa file hoàn toàn
                SqliteConnection.ClearAllPools();
                GC.Collect();
                GC.WaitForPendingFinalizers();

                TienDoSaoLuu = 100;
                MessageBox.Show($"Đã tạo bản sao lưu thành công!\n\nTên file: {backupFileName}\nLưu tại: {DuongDanBackup}", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);

                TaiDuLieu();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi sao lưu: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { IsLoading = false; }
        }

        private void PhucHoi()
        {
            try
            {
                if (DanhSachBackup.Count == 0) { MessageBox.Show("Không có file backup để phục hồi!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
                if (BackupDangChon == null) { MessageBox.Show("Vui lòng chọn file để phục hồi!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning); return; }

                var result = MessageBox.Show(
                    $"CẢNH BÁO NGUY HIỂM:\n\nQuá trình này sẽ xóa sạch dữ liệu hiện tại và thay thế bằng dữ liệu từ bản sao lưu '{BackupDangChon.FileName}'.\n\nPhần mềm sẽ tự động khởi động lại sau khi hoàn tất. Bạn có chắc chắn muốn tiếp tục?",
                    "Xác nhận phục hồi",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.No) return;

                IsLoading = true;
                TienDoSaoLuu = 0;

                string backupFile = BackupDangChon.FileName;
                string backupPath = Path.Combine(DuongDanBackup, backupFile);
                string dbPath = "data/HUIT02DataBase.db";

                if (!File.Exists(backupPath)) { MessageBox.Show("File backup không tồn tại!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error); return; }

                SqliteConnection.ClearAllPools();
                GC.Collect();
                GC.WaitForPendingFinalizers();

                string walPath = dbPath + "-wal";
                string shmPath = dbPath + "-shm";
                if (File.Exists(walPath)) File.Delete(walPath);
                if (File.Exists(shmPath)) File.Delete(shmPath);

                File.Copy(backupPath, dbPath, true);
                TienDoSaoLuu = 100;

                MessageBox.Show("Phục hồi dữ liệu thành công! Phần mềm sẽ khởi động lại ngay bây giờ.", "Hoàn tất", MessageBoxButton.OK, MessageBoxImage.Information);

                var currentExecutablePath = Process.GetCurrentProcess().MainModule.FileName;
                Process.Start(currentExecutablePath);
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi phục hồi: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { IsLoading = false; }
        }

        private void ChonThuMuc()
        {
            var dialog = new OpenFileDialog { CheckFileExists = false, FileName = "Chọn thư mục" };
            if (dialog.ShowDialog() == true)
            {
                DuongDanBackup = Path.GetDirectoryName(dialog.FileName);
                TaiDuLieu();
            }
        }

        private void XoaBackup(object parameter)
        {
            try
            {
                // Ép kiểu tham số truyền sang thành danh sách các dòng được chọn
                if (parameter is System.Collections.IList selectedItems)
                {
                    int soLuongFileXoa = selectedItems.Count;
                    if (soLuongFileXoa == 0) return;

                    // 1. Hỏi xác nhận một lần duy nhất cho tất cả các file được chọn
                    var result = MessageBox.Show($"Bạn có chắc chắn muốn xóa {soLuongFileXoa} bản sao lưu đã chọn không? Hành động này không thể hoàn tác.", "Xác nhận xóa hàng loạt", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result == MessageBoxResult.No) return;

                    // 2. Chuyển danh sách chọn sang mảng tĩnh để tránh lỗi xung đột luồng khi xóa phần tử trong DataGrid
                    var danhSachXoa = selectedItems.Cast<BackupItem>().ToList();

                    // 3. Vòng lặp xóa vật lý từng file
                    foreach (var item in danhSachXoa)
                    {
                        string backupPath = Path.Combine(DuongDanBackup, item.FileName);
                        if (File.Exists(backupPath))
                        {
                            File.Delete(backupPath);
                        }
                    }

                    MessageBox.Show($"Đã xóa thành công {soLuongFileXoa} file bản sao lưu!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);

                    BackupDangChon = null; // Reset ô chọn
                    TaiDuLieu(); // Tính toán lại dung lượng và cập nhật STT mới
                }
            }
            catch (Exception ex) { MessageBox.Show($"Lỗi xóa hàng loạt: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        public string DungLuongFormatted
        {
            get
            {
                double size = DungLuongBackup;
                if (size < 1024) return $"{size} Bytes";
                else if (size < 1024 * 1024) return $"{size / 1024:F2} KB";
                else if (size < 1024 * 1024 * 1024) return $"{size / (1024 * 1024):F2} MB";
                else return $"{size / (1024 * 1024 * 1024):F2} GB";
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class BackupItem
    {
        public int STT { get; set; }
        public string FileName { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
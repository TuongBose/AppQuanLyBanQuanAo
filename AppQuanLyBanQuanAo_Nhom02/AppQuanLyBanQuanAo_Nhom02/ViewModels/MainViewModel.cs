using AppQuanLyBanQuanAo_Nhom02.Data;
using AppQuanLyBanQuanAo_Nhom02.Messages;
using AppQuanLyBanQuanAo_Nhom02.Untils;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace AppQuanLyBanQuanAo_Nhom02.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        // 1. Lấy tên người dùng để hiển thị lời chào (Góc trên cùng)
        public string WelcomeMessage
        {
            get
            {
                if (AppSession.CurrentUser != null)
                {
                    string chucDanh = AppSession.CurrentUser.Role == 1 ? "Quản lý" : "Nhân viên";
                    return $"{chucDanh} {AppSession.CurrentUser.TenNV}";
                }
                return "Chưa đăng nhập";
            }
        }

        // 2. CÔNG TẮC PHÂN QUYỀN: Ẩn/Hiện các nút chức năng dành riêng cho Admin
        public Visibility AdminOnlyVisibility
        {
            get
            {
                if (AppSession.CurrentUser != null && AppSession.CurrentUser.Role == 1)
                {
                    return Visibility.Visible;
                }
                return Visibility.Collapsed;
            }
        }

        public Visibility AdminSectionVisibility
        {
            get
            {
                return AdminOnlyVisibility; 
            }
        }

        // 3. Vùng hiển thị nội dung bên phải
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CurrentViewModelName))] // Yêu cầu cập nhật tên Form mỗi khi đổi View
        private object currentView;

        // Thuộc tính này sẽ lấy tên Class (ví dụ: "HoaDonViewModel") đưa ra cho XAML đọc
        public string CurrentViewModelName => CurrentView?.GetType().Name;

        // ==========================================
        // KHO LƯU TRỮ TRẠNG THÁI CÁC FORM (CACHING)
        // ==========================================
        // Các biến này sẽ giữ lại form cũ, không cho phép bị xóa đi khi chuyển tab
        private TrangChuViewModel _trangChuVM;
        private HoaDonViewModel _hoaDonVM;
        private SanPhamViewModel _sanPhamVM;
        private LoaiSanPhamViewModel _loaiSPVM;
        private KhachHangViewModel _khachHangVM;
        private NhapHangViewModel _nhapHangVM;
        private BaoCaoViewModel _baoCaoVM;
        private SaoLuuViewModel _saoLuuVM;
        private NhanVienViewModel _nhanVienVM;

        public MainViewModel()
        {
            // Ra lệnh cho hệ thống: Vừa mở lên thì khởi tạo Trang chủ 1 lần duy nhất và cất vào kho
            _trangChuVM = new TrangChuViewModel();
            CurrentView = _trangChuVM;

            // ==============================================
            // ĐĂNG KÝ NHẬN TÍN HIỆU CHUYỂN TRANG
            // ==============================================
            WeakReferenceMessenger.Default.Register<NavigationMessage>(this, (recipient, message) =>
            {
                // Dựa vào bức thư gửi đến để đổi View tương ứng
                if (message.TargetViewModelType == typeof(HoaDonViewModel))
                {
                    CurrentView = new HoaDonViewModel();
                }
                else if (message.TargetViewModelType == typeof(KhachHangViewModel))
                {
                    CurrentView = new KhachHangViewModel();
                }
            });
        }

        [RelayCommand]
        private void SwitchView(string parameter)
        {
            // LOGIC MỚI: Kiểm tra xem kho đã có form này chưa? 
            // Nếu chưa có (null) -> Tạo mới. 
            // Nếu có rồi -> Lấy cái cũ ra xài lại (Giữ nguyên toàn bộ dữ liệu đang gõ dở).
            switch (parameter)
            {
                case "TrangChu": 
                    if (_trangChuVM == null) _trangChuVM = new TrangChuViewModel(); 
                    CurrentView = _trangChuVM; 
                    break;
                case "HoaDon": 
                    if (_hoaDonVM == null) _hoaDonVM = new HoaDonViewModel(); 
                    CurrentView = _hoaDonVM; 
                    break;
                case "SanPham": 
                    if (_sanPhamVM == null) _sanPhamVM = new SanPhamViewModel(); 
                    CurrentView = _sanPhamVM; 
                    break;
                case "LoaiSP": 
                    if (_loaiSPVM == null) _loaiSPVM = new LoaiSanPhamViewModel(); 
                    CurrentView = _loaiSPVM; 
                    break;
                case "KhachHang": 
                    if (_khachHangVM == null) _khachHangVM = new KhachHangViewModel(); 
                    CurrentView = _khachHangVM; 
                    break;
                case "NhapHang": 
                    if (_nhapHangVM == null) _nhapHangVM = new NhapHangViewModel(); 
                    CurrentView = _nhapHangVM; 
                    break;
                case "BaoCao": 
                    if (_baoCaoVM == null) _baoCaoVM = new BaoCaoViewModel(); 
                    CurrentView = _baoCaoVM; 
                    break;
                case "SaoLuu": 
                    if (_saoLuuVM == null) _saoLuuVM = new SaoLuuViewModel(); 
                    CurrentView = _saoLuuVM; 
                    break;
                case "NhanVien": 
                    if (_nhanVienVM == null) _nhanVienVM = new NhanVienViewModel(); 
                    CurrentView = _nhanVienVM; 
                    break;
            }
        }

        [RelayCommand]
        private void ChangePassword()
        {
            Views.Windows.ChangePasswordWindow changePassWin = new Views.Windows.ChangePasswordWindow();
            changePassWin.ShowDialog();
        }

        [RelayCommand]
        private void Logout(Window currentWindow)
        {
            var result = MessageBox.Show("Bạn có chắc chắn muốn đăng xuất không?",
                                         "Xác nhận đăng xuất",
                                         MessageBoxButton.YesNo,
                                         MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // Xóa cả kho dữ liệu tạm khi đăng xuất để đảm bảo an toàn
                _trangChuVM = null; _hoaDonVM = null; _sanPhamVM = null; _loaiSPVM = null;
                _khachHangVM = null; _nhapHangVM = null; _baoCaoVM = null; _saoLuuVM = null; _nhanVienVM = null;

                AppSession.CurrentUser = null;

                Views.Windows.LoginWindow loginWindow = new Views.Windows.LoginWindow();
                loginWindow.Show();

                Application.Current.MainWindow = loginWindow;
                currentWindow?.Close();
            }
        }
    }
}
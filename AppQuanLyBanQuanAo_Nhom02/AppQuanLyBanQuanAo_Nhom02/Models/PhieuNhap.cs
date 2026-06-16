using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AppQuanLyBanQuanAo_Nhom02.Models
{
    public class PhieuNhap : INotifyPropertyChanged
    {
        public int STT { get; set; }
        public int MaPN { get; set; }
        public string MaPN_Display => $"PN{MaPN:D3}";

        public int MaNV { get; set; }
        public string MaNV_Display => $"NV{MaNV:D3}";

        private string _nhaCungCap;
        public string NhaCungCap
        {
            get => _nhaCungCap;
            set
            {
                if (_nhaCungCap != value)
                {
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        // Tự động viết hoa chữ cái đầu mỗi từ
                        var textInfo = System.Globalization.CultureInfo.CurrentCulture.TextInfo;
                        _nhaCungCap = textInfo.ToTitleCase(value.ToLower());
                    }
                    else
                    {
                        _nhaCungCap = value;
                    }
                    OnPropertyChanged();
                }
            }
        }

        public string NgayNhap { get; set; }
        public int TongTien { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
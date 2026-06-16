using System;
using System.Collections.Generic;
using System.Text;

namespace AppQuanLyBanQuanAo_Nhom02.Untils
{
    public static class GlobalState
    {
        public static int CurrentUserId { get; set; }
        public static string CurrentUserName { get; set; }
        public static int Role { get; set; } // 1: Admin, 0: User (nhân viên)

    }
}

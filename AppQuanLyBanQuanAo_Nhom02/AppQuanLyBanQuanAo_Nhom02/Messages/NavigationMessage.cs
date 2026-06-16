using System;
using System.Collections.Generic;
using System.Text;

namespace AppQuanLyBanQuanAo_Nhom02.Messages
{
    public class NavigationMessage
    {
        public Type TargetViewModelType { get; }

        public NavigationMessage(Type targetViewModelType)
        {
            TargetViewModelType = targetViewModelType;
        }
    }
}

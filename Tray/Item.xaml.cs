using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Windows.Interop;

namespace Tray
{
    /// <summary>
    /// Interaction logic for Item.xaml
    /// </summary>
    public partial class Item : UserControl
    {
        public TrayIconData buttonData;
        public string ToolTip;
        public MouseButtonEventHandler MouseLeftButtonDown;
        public MouseButtonEventHandler MouseLeftButtonUp;
        public MouseButtonEventHandler MouseRightButtonDown;
        public MouseButtonEventHandler MouseDoubleClick;
        
        public Item()
        {
            InitializeComponent();
        }
    }
}

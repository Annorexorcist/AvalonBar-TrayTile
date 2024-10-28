using ManagedShell.WindowsTray;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using TileLib;
using static ManagedShell.Interop.NativeMethods;

namespace Tray
{
    public partial class Tray : UserControl
    {
        [DllImport("User32.dll")]
        public static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool PostMessage(IntPtr hWnd, uint Msg, int wParam, int lParam);

        [DllImport("user32.dll")]
        private static extern IntPtr SetForegroundWindow(IntPtr hWnd);

        private DispatcherTimer refreshTimer;
        private DispatcherTimer clickTimer;
        private const int DoubleClickDelay = 500; // Milliseconds
        private SystemTray systemTray;

        public Tray()
        {
            InitializeComponent();
            Load();
            clickTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(DoubleClickDelay) };
            clickTimer.Tick += ClickTimer_Tick;
        }

        public void Load()
        {
            systemTray = new SystemTray();
            systemTray.SetSystrayCallback(OnTrayIconAdded);

            Refresh();
            refreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5)
            };
            refreshTimer.Tick += RefreshTimer_Tick;
            refreshTimer.Start();
        }

        public void Unload()
        {
            refreshTimer.Stop();
        }

        private void RefreshTimer_Tick(object sender, EventArgs e)
        {
            Refresh();
        }

        private void Refresh()
        {
            Root.Children.Clear();
            systemTray.Run();
        }

        private bool OnTrayIconAdded(uint msg, SafeNotifyIconData nid)
        {
            if (nid.uFlags.HasFlag(NIF.ICON) && nid.hIcon != IntPtr.Zero)
            {
                AddIconToUI(nid);
                return true; // Indicate success
            }
            return false; // Indicate failure
        }

        private void AddIconToUI(SafeNotifyIconData nid)
        {
            Item item = new Item();

            // Set buttonData to the corresponding trayIcon data
            item.buttonData = new TrayIconData
            {
                traydata = new TRAYDATA
                {
                    hWnd = nid.hWnd,
                    uID = nid.uID,
                    uCallbackMessage = nid.uCallbackMessage,
                    hIcon = nid.hIcon,
                    // other properties...
                },
                Text = nid.szTip // Set this if you want to use it for tooltips
            };

            try
            {
                // Check if the icon handle is valid
                if (nid.hIcon == IntPtr.Zero)
                {
                    throw new InvalidOperationException("Icon handle is invalid.");
                }

                // Create BitmapSource from the icon handle
                item.Icon.Source = Imaging.CreateBitmapSourceFromHIcon(nid.hIcon, Int32Rect.Empty, null);

                // Set tool tip text
                item.ToolTip = !string.IsNullOrEmpty(nid.szTip) ? nid.szTip : "No Title";

                // Attach event handlers
                item.MouseLeftButtonDown += new MouseButtonEventHandler(this.item_MouseLeftButtonDown);
                item.MouseRightButtonDown += new MouseButtonEventHandler(this.item_MouseRightButtonDown);
                item.MouseRightButtonUp += new MouseButtonEventHandler(this.item_MouseRightButtonUp);

                Root.Children.Add(item);
            }
            catch (Exception ex)
            {
                // Log the error message including the icon title for debugging
                MessageBox.Show($"Error adding icon: {nid.szTip ?? "Unknown"} - {ex.Message}");
            }
        }

        private void item_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var item = sender as Item;
            if (item?.buttonData?.traydata.hWnd != IntPtr.Zero)
            {
                if (clickTimer.IsEnabled)
                {
                    // Double-click detected
                    clickTimer.Stop();
                    HandleDoubleClick(item);
                    e.Handled = true;
                }
                else
                {
                    // Start timer for single click
                    clickTimer.Start();
                    HandleSingleClick(item);
                    e.Handled = true;
                }
            }
        }

        private void ClickTimer_Tick(object sender, EventArgs e)
        {
            clickTimer.Stop(); // Stop the timer when the time elapses
        }

        private void HandleSingleClick(Item item)
        {
            Tray.PostMessage(item.buttonData.traydata.hWnd, item.buttonData.traydata.uCallbackMessage, (int)item.buttonData.traydata.uID, 0x0201);
        }

        private void HandleDoubleClick(Item item)
        {
            Tray.PostMessage(item.buttonData.traydata.hWnd, item.buttonData.traydata.uCallbackMessage,
                                 (int)item.buttonData.traydata.uID, 0x0203);
            Tray.SetForegroundWindow(item.buttonData.traydata.hWnd);
        }

        private void item_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var item = sender as Item;
            if (item?.buttonData?.traydata.hWnd != IntPtr.Zero)
            {
                Tray.PostMessage(item.buttonData.traydata.hWnd, item.buttonData.traydata.uCallbackMessage,
                                 (int)item.buttonData.traydata.uID, 0x0204);
                e.Handled = true;
            }
        }

        private void item_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            var item = sender as Item;
            if (item?.buttonData?.traydata.hWnd != IntPtr.Zero)
            {
                Tray.PostMessage(item.buttonData.traydata.hWnd, item.buttonData.traydata.uCallbackMessage, (int)item.buttonData.traydata.uID, 0x0205);
                Tray.PostMessage(item.buttonData.traydata.hWnd, item.buttonData.traydata.uCallbackMessage, (int)item.buttonData.traydata.uID, 0x007B);
                e.Handled = true;
            }
        }
    }

    public class Tile : BaseTile
    {
        private Tray Content;

        public override UserControl Load()
        {
            Content = new Tray();
            this.Content.Load();
            return Content;
        }

        public override void ShowFlyout()
        {
            base.ShowFlyout();
        }

        public override void ShowOptions()
        {
            base.ShowOptions();
        }

        public override void ChangeSide(int side)
        {
            base.ChangeSide(side);
        }

        public override void Unload()
        {
            base.Unload();
            Content.Unload();
        }
    }
}

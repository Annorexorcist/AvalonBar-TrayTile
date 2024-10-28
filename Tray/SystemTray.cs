using System;
using System.Runtime.InteropServices;
using ManagedShell.WindowsTray;
using Microsoft.Win32;
using ManagedShell.Interop;
using ManagedShell.Common.Helpers;
using ManagedShell.Common.Logging;
using static ManagedShell.Interop.NativeMethods;

namespace Tray
{
    public class SystemTray
    {
        private SystrayDelegate trayDelegate;

        public SystemTray()
        {
        }

        internal void SetSystrayCallback(SystrayDelegate theDelegate)
        {
            trayDelegate = theDelegate;
        }

        internal void Run()
        {
            if (!EnvironmentHelper.IsAppRunningAsShell && trayDelegate != null)
            {

                 GetTrayItems();
            }
        }

        private void GetTrayItems()
        {
            IntPtr toolbarHwnd = FindExplorerTrayToolbarHwnd();

            if (toolbarHwnd == IntPtr.Zero)
            {
                return;
            }

            int count = GetNumTrayIcons(toolbarHwnd);

            if (count < 1)
            {
                return;
            }

            GetWindowThreadProcessId(toolbarHwnd, out var processId);
            IntPtr hProcess = OpenProcess(ProcessAccessFlags.All, false, (int)processId);
            IntPtr hBuffer = VirtualAllocEx(hProcess, IntPtr.Zero, (uint)Marshal.SizeOf(new TBBUTTON()), AllocationType.Commit, MemoryProtection.ReadWrite);

            for (int i = 0; i < count; i++)
            {
                TrayItem trayItem = GetTrayItem(i, hBuffer, hProcess, toolbarHwnd);

                if (trayItem.hWnd == IntPtr.Zero || !IsWindow(trayItem.hWnd))
                {
                    ShellLogger.Debug($"SystemTray: Ignored notify icon {trayItem.szIconText} due to invalid handle");
                    continue;
                }

                SafeNotifyIconData nid = GetTrayItemIconData(trayItem);

                if (trayDelegate != null)
                {
                    if (!trayDelegate((uint)NIM.NIM_ADD, nid))
                    {
                        ShellLogger.Debug("SystemTray: Ignored notify icon message");
                    }
                }
                else
                {
                    ShellLogger.Debug("SystemTray: trayDelegate is null");
                }
            }

            VirtualFreeEx(hProcess, hBuffer, 0, AllocationType.Release);
            CloseHandle((int)hProcess);
        }

        private IntPtr FindExplorerTrayToolbarHwnd()
        {
            IntPtr hwnd = FindWindow("Shell_TrayWnd", null);

            if (hwnd != IntPtr.Zero)
            {
                hwnd = FindWindowEx(hwnd, IntPtr.Zero, "TrayNotifyWnd", null);

                if (hwnd != IntPtr.Zero)
                {
                    hwnd = FindWindowEx(hwnd, IntPtr.Zero, "SysPager", null);

                    if (hwnd != IntPtr.Zero)
                    {
                        hwnd = FindWindowEx(hwnd, IntPtr.Zero, "ToolbarWindow32", null);
                        return hwnd;
                    }
                }
            }

            return IntPtr.Zero;
        }

        private int GetNumTrayIcons(IntPtr toolbarHwnd)
        {
            return (int)SendMessage(toolbarHwnd, (int)TB.BUTTONCOUNT, IntPtr.Zero, IntPtr.Zero);
        }

        private TrayItem GetTrayItem(int index, IntPtr hBuffer, IntPtr hProcess, IntPtr toolbarHwnd)
        {
            TBBUTTON tbButton = new TBBUTTON();
            TrayItem trayItem = new TrayItem();
            IntPtr hTBButton = Marshal.AllocHGlobal(Marshal.SizeOf(tbButton));
            IntPtr hTrayItem = Marshal.AllocHGlobal(Marshal.SizeOf(trayItem));

            IntPtr msgSuccess = SendMessage(toolbarHwnd, (int)TB.GETBUTTON, (IntPtr)index, hBuffer);
            if (ReadProcessMemory(hProcess, hBuffer, hTBButton, Marshal.SizeOf(tbButton), out _))
            {
                tbButton = (TBBUTTON)Marshal.PtrToStructure(hTBButton, typeof(TBBUTTON));

                if (tbButton.dwData != UIntPtr.Zero)
                {
                    if (ReadProcessMemory(hProcess, tbButton.dwData, hTrayItem, Marshal.SizeOf(trayItem), out _))
                    {
                        trayItem = (TrayItem)Marshal.PtrToStructure(hTrayItem, typeof(TrayItem));

                        trayItem.dwState = (uint)((tbButton.fsState & TBSTATE_HIDDEN) != 0 ? 1 : 0);
                        ShellLogger.Debug($"SystemTray: Got tray item: {trayItem.szIconText}");
                    }
                }
            }

            Marshal.FreeHGlobal(hTBButton);
            Marshal.FreeHGlobal(hTrayItem);

            return trayItem;
        }
        private SafeNotifyIconData GetTrayItemIconData(TrayItem trayItem)
        {
            SafeNotifyIconData nid = new SafeNotifyIconData
            {
                hWnd = trayItem.hWnd,
                uID = trayItem.uID,
                uCallbackMessage = trayItem.uCallbackMessage,
                szTip = trayItem.szIconText,
                hIcon = trayItem.hIcon,
                uVersion = trayItem.uVersion,
                guidItem = trayItem.guidItem,
                dwState = (int)trayItem.dwState,
                uFlags = NIF.GUID | NIF.MESSAGE | NIF.TIP | NIF.STATE
            };

            if (nid.hIcon != IntPtr.Zero)
            {
                nid.uFlags |= NIF.ICON;
            }
            else
            {
                ShellLogger.Warning($"SystemTray: Unable to use {trayItem.szIconText} icon handle for NOTIFYICONDATA struct");
            }

            return nid;
        }

        private const byte TBSTATE_HIDDEN = 8;

        private enum TB : uint
        {
            GETBUTTON = WM.USER + 23,
            BUTTONCOUNT = WM.USER + 24
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct TrayItem
        {
            public IntPtr hWnd;
            public uint uID;
            public uint uCallbackMessage;
            public uint dwState;
            public uint uVersion;
            public IntPtr hIcon;
            public IntPtr uIconDemoteTimerID;
            public uint dwUserPref;
            public uint dwLastSoundTime;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szExeName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szIconText;
            public uint uNumSeconds;
            public Guid guidItem;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct TBBUTTON
        {
            public int iBitmap;
            public int idCommand;
            [StructLayout(LayoutKind.Explicit)]
            private struct TBBUTTON_U
            {
                [FieldOffset(0)] public byte fsState;
                [FieldOffset(1)] public byte fsStyle;
                [FieldOffset(0)] private IntPtr bReserved;
            }
            private TBBUTTON_U union;
            public byte fsState { get { return union.fsState; } set { union.fsState = value; } }
            public byte fsStyle { get { return union.fsStyle; } set { union.fsStyle = value; } }
            public UIntPtr dwData;
            public IntPtr iString;
        }
    }
}

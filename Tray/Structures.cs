using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Drawing;

namespace Tray
{
    [StructLayout(LayoutKind.Sequential)]
    public struct TBBUTTON
    {
        public int iBitmap;
        public int idCommand;
        public byte fsState;
        public byte fsStyle;
        public byte bReserved;
        public byte dwData;
        public IntPtr iString;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct TRAYDATA
    {
        public IntPtr hIcon;
        public IntPtr hWnd;
        public uint uCallbackMessage;
        public uint uID;
        // Add other fields as necessary
    }

    public class TrayIconData
    {
        public TBBUTTON button;
        public TRAYDATA traydata;
        public string Text { get; set; }
    }
}

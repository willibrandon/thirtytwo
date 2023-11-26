using Windows;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.WindowsAndMessaging;

namespace DevCaps;


internal class Program
{
    [STAThread]
    private static void Main()
    {
        using DeviceCapsWindow devCapsWindow = new();
        Application.Run(devCapsWindow);

        using CloverWindow cloverWindow = new();
        Application.Run(cloverWindow);
    }

    /// <summary>
    ///  Sample from Programming Windows, 5th Edition.
    ///  Original (c) Charles Petzold, 1998
    ///  Figure 5-27, Pages 205-208.
    /// </summary>
    private class CloverWindow : Window
    {
        private const double TWOPI = 2 * Math.PI;

        private static HRGN s_hRgnClip;
        private static int s_cxClient, s_cyClient;
        private static double s_fAngle, s_fRadius;
        private static HCURSOR s_hCursor;
        private static readonly HRGN[] s_hRgnTemp = new HRGN[6];

        public CloverWindow() : base(DefaultBounds, text: "Draw a Clover", style: WindowStyles.OverlappedWindow)
        {
        }

        protected override LRESULT WindowProcedure(HWND window, MessageType message, WPARAM wParam, LPARAM lParam)
        {
            switch (message)
            {
                case MessageType.Paint:
                    PaintMessage(window);
                    return (LRESULT)0;
                case MessageType.Size:
                    Size(window, lParam);
                    return (LRESULT)0;
            }

            return base.WindowProcedure(window, message, wParam, lParam);
        }

        private static double Hypontenuse(double x, double y)
        {
            return Math.Sqrt(x * x + y * y);
        }

        private static void PaintMessage(HWND window)
        {
            using DeviceContext dc = window.BeginPaint();

            dc.SetViewPortOrgEx(s_cxClient / 2, s_cyClient / 2);
            dc.SelectClipRgn(s_hRgnClip);

            s_fRadius = Hypontenuse(s_cxClient / 2.0, s_cyClient / 2.0);

            for (s_fAngle = 0.0; s_fAngle < TWOPI; s_fAngle += TWOPI / 360)
            {
                dc.MoveToEx(0, 0);
                dc.LineTo((int)(s_fRadius * Math.Cos(s_fAngle) + 0.5),
                    (int)(-s_fRadius * Math.Sin(s_fAngle) + 0.5));
            }
        }

        private static void Size(HWND window, LPARAM lParam)
        {
            using DeviceContext dc = window.GetDeviceContext();

            s_cxClient = (int)lParam.Value & 0xFFFF;
            s_cyClient = (int)lParam.Value >> 16;

            s_hCursor = dc.SetCursor(dc.LoadCursor(default, 32514));
            dc.ShowCursor(true);

            if (s_hRgnClip != default)
            {
                dc.DeleteObject(s_hRgnClip);
            }

            s_hRgnTemp[0] = dc.CreateEllipticRgn(0, s_cyClient / 3,
                s_cxClient / 2, 2 * s_cyClient / 3);
            s_hRgnTemp[1] = dc.CreateEllipticRgn(s_cxClient / 2, s_cyClient / 3,
                s_cxClient, 2 * s_cyClient / 3);
            s_hRgnTemp[2] = dc.CreateEllipticRgn(s_cxClient / 3, 0,
                2 * s_cxClient / 3, s_cyClient / 2);
            s_hRgnTemp[3] = dc.CreateEllipticRgn(s_cxClient / 3, s_cyClient / 2,
                2 * s_cxClient / 3, s_cyClient);
            s_hRgnTemp[4] = dc.CreateRectRgn(0, 0, 1, 1);
            s_hRgnTemp[5] = dc.CreateRectRgn(0, 0, 1, 1);
            s_hRgnClip = dc.CreateRectRgn(0, 0, 1, 1);

            dc.CombineRgn(s_hRgnTemp[4], s_hRgnTemp[0], s_hRgnTemp[1], RGN_COMBINE_MODE.RGN_OR);
            dc.CombineRgn(s_hRgnTemp[5], s_hRgnTemp[2], s_hRgnTemp[3], RGN_COMBINE_MODE.RGN_OR);
            dc.CombineRgn(s_hRgnClip, s_hRgnTemp[4], s_hRgnTemp[5], RGN_COMBINE_MODE.RGN_XOR);

            for (int i = 0; i < 6; i++)
            {
                dc.DeleteObject(s_hRgnTemp[i]);
            }

            dc.SetCursor(s_hCursor);
            dc.ShowCursor(false);
        }
    }

    /// <summary>
    ///  Sample from Programming Windows, 5th Edition.
    ///  Original (c) Charles Petzold, 1998
    ///  Figure 5-1, Pages 129-132.
    /// </summary>
    private class DeviceCapsWindow : Window
    {
        public struct DEVCAPS
        {
            public int iIndex;
            public string szLabel;
            public string szDesc;
        }

        private readonly int _cxChar;
        private readonly int _cxCaps;
        private readonly int _cyChar;
        private readonly DEVCAPS[] _devcaps =
        [
            new DEVCAPS() { iIndex = (int)GET_DEVICE_CAPS_INDEX.HORZSIZE, szLabel = "HORSIZE", szDesc = "Width in millimeters:" },
            new DEVCAPS() { iIndex = (int)GET_DEVICE_CAPS_INDEX.VERTSIZE, szLabel = "VERTSIZE", szDesc = "Height in millimeters:" },
            new DEVCAPS() { iIndex = (int)GET_DEVICE_CAPS_INDEX.HORZRES, szLabel = "HORZRES", szDesc = "Width in pixels:" },
            new DEVCAPS() { iIndex = (int)GET_DEVICE_CAPS_INDEX.VERTRES, szLabel = "VERTRES", szDesc = "Height in raster lines:" },
            new DEVCAPS() { iIndex = (int)GET_DEVICE_CAPS_INDEX.BITSPIXEL, szLabel = "BITSPIXEL", szDesc = "Color bits per pixel:" },
            new DEVCAPS() { iIndex = (int)GET_DEVICE_CAPS_INDEX.PLANES, szLabel = "PLANES", szDesc = "Number of color planes:" },
            new DEVCAPS() { iIndex = (int)GET_DEVICE_CAPS_INDEX.NUMBRUSHES, szLabel = "NUMBRUSHES", szDesc = "Number of device brushes:" },
            new DEVCAPS() { iIndex = (int)GET_DEVICE_CAPS_INDEX.NUMPENS, szLabel = "NUMPENS", szDesc = "Number of device pens:" },
            new DEVCAPS() { iIndex = (int)GET_DEVICE_CAPS_INDEX.NUMMARKERS, szLabel = "NUMMARKERS", szDesc = "Number of device markers:" },
            new DEVCAPS() { iIndex = (int)GET_DEVICE_CAPS_INDEX.NUMFONTS, szLabel = "NUMFONTS", szDesc = "Number of device fonts:" },
            new DEVCAPS() { iIndex = (int)GET_DEVICE_CAPS_INDEX.NUMCOLORS, szLabel = "NUMCOLORS", szDesc = "Number of device colors:" },
            new DEVCAPS() { iIndex = (int)GET_DEVICE_CAPS_INDEX.PDEVICESIZE, szLabel = "PDEVICESIZE", szDesc = "Size of device structure:" },
            new DEVCAPS() { iIndex = (int)GET_DEVICE_CAPS_INDEX.ASPECTX, szLabel = "ASPECTX", szDesc = "Relative width of pixel:" },
            new DEVCAPS() { iIndex = (int)GET_DEVICE_CAPS_INDEX.ASPECTY, szLabel = "ASPECTY", szDesc = "Relative height of pixel:" },
            new DEVCAPS() { iIndex = (int)GET_DEVICE_CAPS_INDEX.LOGPIXELSX, szLabel = "LOGPIXELSX", szDesc = "Horizontal dots per inch:" },
            new DEVCAPS() { iIndex = (int)GET_DEVICE_CAPS_INDEX.LOGPIXELSY, szLabel = "LOGPIXELSY", szDesc = "Vertical dots per inch:" },
            new DEVCAPS() { iIndex = (int)GET_DEVICE_CAPS_INDEX.SIZEPALETTE, szLabel = "SIZEPALETTE", szDesc = "Number of palette entries:" },
            new DEVCAPS() { iIndex = (int)GET_DEVICE_CAPS_INDEX.NUMRESERVED, szLabel = "NUMRESERVED", szDesc = "Reserved palette entries:" },
            new DEVCAPS() { iIndex = (int)GET_DEVICE_CAPS_INDEX.COLORRES, szLabel = "COLORRES", szDesc = "Actual color resolution:" },
        ];

        public DeviceCapsWindow() : base(DefaultBounds, text: "Device Capabilities", style: WindowStyles.OverlappedWindow)
        {
            using DeviceContext dc = this.GetDeviceContext();
            dc.GetTextMetrics(out TEXTMETRICW tm);
            _cxChar = tm.tmAveCharWidth;
            _cxCaps = (((tm.tmPitchAndFamily & TMPF_FLAGS.TMPF_FIXED_PITCH) != 0) ? 3 : 2) * _cxChar / 2;
            _cyChar = tm.tmHeight + tm.tmExternalLeading;
        }

        protected override LRESULT WindowProcedure(HWND window, MessageType message, WPARAM wParam, LPARAM lParam)
        {
            switch (message)
            {
                case MessageType.Paint:
                    PaintMessage(window, _devcaps, _cxChar, _cxCaps, _cyChar);
                    return (LRESULT)0;
            }

            return base.WindowProcedure(window, message, wParam, lParam);
        }

        private static void PaintMessage(HWND window, DEVCAPS[] devcaps, int cxChar, int cxCaps, int cyChar)
        {
            using DeviceContext dc = window.BeginPaint();

            for (int i = 0; i < devcaps.Length; i++)
            {
                dc.TextOut(0, cyChar * i, devcaps[i].szLabel);
                dc.TextOut(14 * cxCaps, cyChar * i, devcaps[i].szDesc);

                dc.SetTextAlign(TEXT_ALIGN_OPTIONS.TA_RIGHT | TEXT_ALIGN_OPTIONS.TA_TOP);
                dc.TextOut(14 * cxCaps + 35 * cxChar, cyChar * i, dc.GetDeviceCaps((GET_DEVICE_CAPS_INDEX)devcaps[i].iIndex).ToString());

                dc.SetTextAlign(TEXT_ALIGN_OPTIONS.TA_LEFT | TEXT_ALIGN_OPTIONS.TA_TOP);
            }
        }
    }
}
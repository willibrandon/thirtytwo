using Windows;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
namespace Windows101;

/// <summary>
///  Sample from Programming Windows, 5th Edition.
///  Original (c) Charles Petzold, 1998
///  Figure 5-1, Pages 129-132.
/// </summary>
internal class Program
{
    public struct DEVCAPS
    {
        public int iIndex;
        public string szLabel;
        public string szDesc;
    }

    [STAThread]
    private static void Main()
    {
        using DeviceCapsWindow devCapsWindow = new();
        Application.Run(devCapsWindow);
    }

    private class DeviceCapsWindow : Window
    {
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
using System.Drawing;
using System.Windows.Forms;

namespace GXLightBrowser
{
    internal sealed class BorderlessTabControl : TabControl
    {
        public BorderlessTabControl()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
        }

        public override Rectangle DisplayRectangle
        {
            get { return ClientRectangle; }
        }
    }
}

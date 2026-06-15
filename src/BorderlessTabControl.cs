using System.Drawing;
using System.Windows.Forms;

namespace GXLightBrowser
{
    internal sealed class BorderlessTabControl : TabControl
    {
        public override Rectangle DisplayRectangle
        {
            get { return ClientRectangle; }
        }
    }
}

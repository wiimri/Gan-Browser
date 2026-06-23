using System.Drawing;
using System.Windows.Forms;

namespace GXLightBrowser
{
    internal sealed class BorderlessTabControl : TabControl
    {
        public BorderlessTabControl()
        {
            SetStyle(
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.Opaque |
                ControlStyles.UserPaint |
                ControlStyles.ResizeRedraw, true);
            UpdateStyles();
            BackColor = Theme.Window;
        }

        public override Rectangle DisplayRectangle
        {
            get { return ClientRectangle; }
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            using (Brush b = new SolidBrush(Theme.Window))
            {
                e.Graphics.FillRectangle(b, ClientRectangle);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (SelectedTab != null)
            {
                using (Brush b = new SolidBrush(Theme.Window))
                {
                    e.Graphics.FillRectangle(b, SelectedTab.Bounds);
                }
            }
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_ERASEBKGND = 0x0014;
            if (m.Msg == WM_ERASEBKGND)
            {
                using (Graphics g = Graphics.FromHdc(m.WParam))
                using (Brush b = new SolidBrush(Theme.Window))
                {
                    g.FillRectangle(b, ClientRectangle);
                }
                m.Result = (System.IntPtr)1;
                return;
            }
            base.WndProc(ref m);
        }
    }
}

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace GXLightBrowser
{
    internal sealed class ChromeButton : Button
    {
        private bool _hover;

        public ChromeButton()
        {
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            BackColor = Theme.Button;
            ForeColor = Theme.Text;
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            Cursor = Cursors.Hand;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
        }

        public bool IsSelected { get; set; }
        public bool IsMultiSelected { get; set; }

        private Color? _accent;
        public Color Accent
        {
            get { return _accent ?? Theme.Accent; }
            set { _accent = value; }
        }
        public bool ShowCloseGlyph { get; set; }
        public bool ShowIslandStripe { get; set; }
        public Color IslandColor { get; set; }
        private Image _iconImage;
        public Image IconImage
        {
            get { return _iconImage; }
            set
            {
                Image replacement = null;
                if (value != null)
                {
                    try
                    {
                        replacement = new Bitmap(value);
                    }
                    catch (ArgumentException)
                    {
                    }
                }

                Image previous = _iconImage;
                _iconImage = replacement;
                if (previous != null)
                {
                    previous.Dispose();
                }
            }
        }
        public bool IconOnly { get; set; }
        public bool ShowIconPlaceholder { get; set; }
        public Color IconPlaceholderColor { get; set; }
        public bool IsIslandToggle { get; set; }
        public int IslandMemberCount { get; set; }

        public bool IsCloseHit(Point point)
        {
            if (!ShowCloseGlyph)
            {
                return false;
            }

            return CloseGlyphBounds().Contains(point);
        }

        protected override void OnMouseEnter(System.EventArgs e)
        {
            _hover = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(System.EventArgs e)
        {
            _hover = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            pevent.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);
            int radius = 8;

            Color fill = IsSelected || IsMultiSelected ? Theme.Selected : (_hover ? Theme.Hover : Theme.Button);
            Color borderColor = IsMultiSelected ? Theme.Warning : (IsSelected ? Accent : Theme.Border);
            using (GraphicsPath path = RoundedRect(rect, radius))
            using (SolidBrush brush = new SolidBrush(fill))
            using (Pen border = new Pen(borderColor, IsMultiSelected ? 2f : 1f))
            {
                pevent.Graphics.FillPath(brush, path);
                pevent.Graphics.DrawPath(border, path);
            }

            if (IsIslandToggle)
            {
                int bars = Math.Max(2, Math.Min(5, IslandMemberCount));
                int totalWidth = bars * 4 + (bars - 1) * 2;
                int left = rect.Left + (rect.Width - totalWidth) / 2;
                for (int i = 0; i < bars; i++)
                {
                    Color barColor = i % 2 == 0 ? IslandColor : ControlPaint.Light(IslandColor, 0.25f);
                    using (SolidBrush islandBrush = new SolidBrush(barColor))
                    {
                        pevent.Graphics.FillRectangle(islandBrush, left + i * 6, rect.Top + 5, 4, rect.Height - 10);
                    }
                }
                return;
            }

            if (ShowIslandStripe)
            {
                using (SolidBrush islandBrush = new SolidBrush(IslandColor))
                {
                    pevent.Graphics.FillRectangle(islandBrush, rect.Left + 5, rect.Top + 5, 5, rect.Height - 10);
                }
            }

            if (IsMultiSelected)
            {
                using (SolidBrush selectedBrush = new SolidBrush(Theme.Warning))
                {
                    pevent.Graphics.FillEllipse(selectedBrush, rect.Right - 11, rect.Top + 3, 7, 7);
                }
            }

            int contentLeft = rect.Left + (ShowIslandStripe ? 16 : 8);
            if (_iconImage != null || ShowIconPlaceholder)
            {
                int iconSize = Math.Min(16, rect.Height - 8);
                int iconLeft = IconOnly ? rect.Left + (rect.Width - iconSize) / 2 : contentLeft;
                int iconTop = rect.Top + (rect.Height - iconSize) / 2;
                if (_iconImage != null)
                {
                    pevent.Graphics.DrawImage(_iconImage, new Rectangle(iconLeft, iconTop, iconSize, iconSize));
                }
                else
                {
                    using (SolidBrush placeholder = new SolidBrush(IconPlaceholderColor))
                    {
                        pevent.Graphics.FillEllipse(placeholder, iconLeft, iconTop, iconSize, iconSize);
                    }
                    using (SolidBrush center = new SolidBrush(Color.FromArgb(230, 255, 255, 255)))
                    {
                        pevent.Graphics.FillEllipse(center, iconLeft + 5, iconTop + 5, 6, 6);
                    }
                }
                contentLeft += iconSize + 6;
            }

            Rectangle textRect = ShowCloseGlyph
                ? new Rectangle(contentLeft, rect.Top, Math.Max(0, rect.Right - contentLeft - 27), rect.Height)
                : new Rectangle(contentLeft, rect.Top, Math.Max(0, rect.Right - contentLeft - 7), rect.Height);

            TextFormatFlags flags = (ShowCloseGlyph ? TextFormatFlags.Left : TextFormatFlags.HorizontalCenter) |
                TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis |
                TextFormatFlags.NoPadding;
            if (!IconOnly)
            {
                TextRenderer.DrawText(pevent.Graphics, Text, Font, textRect, Theme.Text, flags);
            }

            if (ShowCloseGlyph)
            {
                Rectangle close = CloseGlyphBounds();
                using (Pen pen = new Pen(_hover ? Theme.Accent : Theme.Muted, 1.7f))
                {
                    pevent.Graphics.DrawLine(pen, close.Left + 5, close.Top + 5, close.Right - 5, close.Bottom - 5);
                    pevent.Graphics.DrawLine(pen, close.Right - 5, close.Top + 5, close.Left + 5, close.Bottom - 5);
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _iconImage != null)
            {
                _iconImage.Dispose();
                _iconImage = null;
            }
            base.Dispose(disposing);
        }

        private Rectangle CloseGlyphBounds()
        {
            return new Rectangle(Width - 25, (Height - 20) / 2, 20, 20);
        }

        private static GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            int diameter = radius * 2;
            GraphicsPath path = new GraphicsPath();
            path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}

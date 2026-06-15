using System;
using System.Drawing;
using GXLightBrowser;

internal static class LayoutProbe
{
    private static int Main()
    {
        using (BorderlessTabControl tabs = new BorderlessTabControl())
        {
            tabs.Size = new Size(800, 600);
            if (tabs.DisplayRectangle != tabs.ClientRectangle)
            {
                Console.Error.WriteLine("Expected tab content to fill the entire client area without a native border.");
                return 1;
            }
        }

        using (Bitmap sourceIcon = new Bitmap(16, 16))
        using (ChromeButton button = new ChromeButton())
        {
            button.Size = new Size(120, 24);
            button.IconImage = sourceIcon;
            sourceIcon.Dispose();

            using (Bitmap rendered = new Bitmap(button.Width, button.Height))
            {
                button.DrawToBitmap(rendered, button.ClientRectangle);
            }
        }

        Console.WriteLine("Layout probe passed.");
        return 0;
    }
}

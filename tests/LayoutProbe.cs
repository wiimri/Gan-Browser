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

        Console.WriteLine("Layout probe passed.");
        return 0;
    }
}

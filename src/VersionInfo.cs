namespace GXLightBrowser
{
    internal static class VersionInfo
    {
        public const string CurrentVersion = "2.6";
        public const string ReleaseName = "Gan Browser 2.6";

        public static string[] Highlights()
        {
            return new string[]
            {
                "Las nuevas pestanas abren directamente la pagina de inicio sin mostrar about:blank.",
                "El cierre difiere la destruccion del WebView hasta mostrar la pestana vecina.",
                "Se agrega una prueba Playwright para detectar transiciones sin contenido."
            };
        }
    }
}

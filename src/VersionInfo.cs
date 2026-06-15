namespace GXLightBrowser
{
    internal static class VersionInfo
    {
        public const string CurrentVersion = "2.5";
        public const string ReleaseName = "Gan Browser 2.5";

        public static string[] Highlights()
        {
            return new string[]
            {
                "Abrir y cerrar pestanas conserva el contenido visible durante la transicion.",
                "Triple clic en la barra de direcciones selecciona todo el texto.",
                "Doble clic en una descarga abre el archivo disponible."
            };
        }
    }
}

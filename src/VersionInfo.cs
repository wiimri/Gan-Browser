namespace GXLightBrowser
{
    internal static class VersionInfo
    {
        public const string CurrentVersion = "2.8";
        public const string ReleaseName = "Gan Browser 2.8";

        public static string[] Highlights()
        {
            return new string[]
            {
                "Los favicons de las pestanas se dibujan desde copias seguras independientes.",
                "Los controles retirados de la barra de pestanas se liberan correctamente.",
                "Se evita la excepcion de dibujo que podia aparecer al cerrar una pestana."
            };
        }
    }
}

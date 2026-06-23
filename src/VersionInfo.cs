namespace GXLightBrowser
{
    internal static class VersionInfo
    {
        public const string CurrentVersion = "2.13";
        public const string ReleaseName = "Gan Browser 2.13";

        public static string[] Highlights()
        {
            return new string[]
            {
                "El actualizador descarga versiones nuevas silenciosamente en segundo plano.",
                "El menu muestra progreso, permite cancelar y ofrece reiniciar cuando el instalador queda verificado.",
                "El manifiesto incluye SHA-256 inline con prioridad sobre el hash externo."
            };
        }
    }
}

namespace GXLightBrowser
{
    internal static class VersionInfo
    {
        public const string CurrentVersion = "2.11";
        public const string ReleaseName = "Gan Browser 2.11";

        public static string[] Highlights()
        {
            return new string[]
            {
                "La importacion de contrasenas acepta CSV exportados desde OperaGX y navegadores Chromium.",
                "El importador detecta encabezados, BOM, columnas variantes y campos multilinea.",
                "Las credenciales importadas quedan cifradas con Windows DPAPI y listas para el rellenado."
            };
        }
    }
}

namespace GXLightBrowser
{
    internal static class VersionInfo
    {
        public const string CurrentVersion = "2.12";
        public const string ReleaseName = "Gan Browser 2.12";

        public static string[] Highlights()
        {
            return new string[]
            {
                "La boveda de contrasenas queda unificada dentro de Configuracion > Privacidad y seguridad.",
                "La importacion acepta CSV y TXT/TSV de OperaGX, Chromium y exportaciones compatibles.",
                "El autocompletado reconoce subdominios seguros como cursos.desafiolatam.com sin exponer contrasenas."
            };
        }
    }
}

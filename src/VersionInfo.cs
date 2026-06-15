namespace GXLightBrowser
{
    internal static class VersionInfo
    {
        public const string CurrentVersion = "2.10";
        public const string ReleaseName = "Gan Browser 2.10";

        public static string[] Highlights()
        {
            return new string[]
            {
                "El usuario se rellena automaticamente al detectar credenciales compatibles.",
                "Cada formulario muestra un selector de cuentas junto al campo de contrasena.",
                "La contrasena solo se completa despues de seleccionar la cuenta y aprobar Windows Hello/PIN."
            };
        }
    }
}

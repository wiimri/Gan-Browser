namespace GXLightBrowser
{
    internal static class VersionInfo
    {
        public const string CurrentVersion = "2.9";
        public const string ReleaseName = "Gan Browser 2.9";

        public static string[] Highlights()
        {
            return new string[]
            {
                "La boveda permite ver y rellenar credenciales despues de aprobar Windows Hello/PIN.",
                "El autocompletado exige coincidencia exacta del dominio y nunca envia el formulario.",
                "Las contrasenas permanecen cifradas con DPAPI en disco y mientras estan cargadas."
            };
        }
    }
}

namespace GXLightBrowser
{
    internal static class VersionInfo
    {
        public const string CurrentVersion = "2.7";
        public const string ReleaseName = "Gan Browser 2.7";

        public static string[] Highlights()
        {
            return new string[]
            {
                "La barra superior reserva espacio vertical completo para las pestanas.",
                "El texto, los iconos y el indicador de suspension ya no quedan recortados.",
                "Se agrega una comprobacion automatica de las dimensiones de la fila de pestanas."
            };
        }
    }
}

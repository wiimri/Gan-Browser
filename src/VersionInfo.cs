namespace GXLightBrowser
{
    internal static class VersionInfo
    {
        public const string CurrentVersion = "2.14";
        public const string ReleaseName = "Gan Browser 2.14";

        public static string[] Highlights()
        {
            return new string[]
            {
                "Reducción de parpadeo (flickering) en la interfaz principal.",
                "Optimización del dibujado del fondo en las pestañas sin bordes.",
                "Estilo de dibujado personalizado mejorado con soporte para doble búfer de control."
            };
        }
    }
}

namespace GXLightBrowser
{
    internal static class VersionInfo
    {
        public const string CurrentVersion = "2.15";
        public const string ReleaseName = "Gan Browser 2.15";

        public static string[] Highlights()
        {
            return new string[]
            {
                "YouTube Shields mejorado: inyección JavaScript más robusta contra anuncios.",
                "Capa adicional de bloqueo CSS + MutationObserver para elementos dinámicos.",
                "Skip automático de anuncios que logren pasar el filtro inicial.",
                "Más claves de anuncio bloqueadas en respuestas JSON (deepPrune mejorado).",
                "Corregido el cache busting en JSON.parse override para evitar condiciones de carrera.",
                "Nuevas reglas de red en AdBlocker para endpoints de YouTube."
            };
        }
    }
}

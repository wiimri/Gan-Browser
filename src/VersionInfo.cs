namespace GXLightBrowser
{
    internal static class VersionInfo
    {
        public const string CurrentVersion = "2.16";
        public const string ReleaseName = "Gan Browser 2.16";

        public static string[] Highlights()
        {
            return new string[]
            {
                "YouTube Shields reescrito: intercepta ytInitialPlayerResponse/ytInitialData via Object.defineProperty.",
                "deleteAdProps con 45 patrones de clave y delete real (no empty array).",
                "Polling cada 2s + MutationObserver con atributos para ads dinámicos.",
                "Anti-adblock neutralizado (onAbnormalityDetected) y snackbars bloqueados.",
                "Auto-update con fallback SHA-256 y descarga directa desde GitHub.",
                "Nuevas reglas de red en AdBlocker para endpoints de YouTube."
            };
        }
    }
}

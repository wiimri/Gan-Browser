# Seguridad de Gan Browser

Gan Browser debe tratar cualquier dato de navegacion como sensible. El proyecto sigue estas reglas:

- No interceptar passwords con JavaScript ni guardarlas sin confirmacion.
- Permitir que WebView2 muestre su aviso nativo y guardar solo cuando el usuario acepta.
- Conservar passwords nativas en el perfil persistente protegido por Windows.
- Cifrar la boveda importada con Windows DPAPI para el usuario actual.
- Mantener las contrasenas de la boveda protegidas con DPAPI tambien mientras estan cargadas.
- Exigir Windows Hello/PIN antes de visualizar, rellenar o exportar credenciales.
- Rellenar solamente cuando el host coincide exactamente y nunca enviar el formulario.
- Mantener telemetria propia desactivada: Gan Browser no envia historial, passwords ni uso.
- Mostrar advertencia antes de exportar passwords a CSV, porque ese archivo queda en texto visible.
- Descargar actualizaciones solamente desde el manifiesto y Releases oficiales del repositorio.
- No instalar actualizaciones silenciosamente ni intentar evitar SmartScreen.

## Prioridades recomendadas

1. Firmar digitalmente el ejecutable y el instalador con certificado de firma de codigo y timestamp.
2. Firmar `update.json`; el instalador ya se verifica mediante SHA-256 antes de ejecutarlo.
3. Sustituir gradualmente el bloqueador prototipo por `brave/adblock-rust`.
4. Crear permisos por sitio para camara, microfono, ubicacion, notificaciones y descargas.
5. Agregar modo privado con un perfil WebView2 temporal que se elimine al cerrar.
6. Anadir borrado selectivo de cookies, cache, historial y datos por sitio.
7. Guardar sesiones y preferencias mediante escritura atomica con copia de respaldo.
8. Ejecutar CI, analisis estatico y pruebas de instalacion en Windows limpio.
9. Mantener WebView2 Evergreen actualizado y mostrar claramente su version.
10. Publicar una politica de seguridad y un canal privado para reportar vulnerabilidades.

## Actualizaciones

`Menu > Buscar actualizaciones` consulta `update.json`. Si hay una version mayor, Gan Browser descarga el
instalador permanente de GitHub Releases, verifica su SHA-256 y solicita permiso para abrirlo. El
instalador actualiza los binarios y conserva el perfil ubicado en `%LOCALAPPDATA%\GXLightBrowser`.

Hasta implementar firma y verificacion criptografica completa, el usuario debe confirmar manualmente la
instalacion y comprobar que la descarga proviene de `github.com/wiimri/Gan-Browser`.

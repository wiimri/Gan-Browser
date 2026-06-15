# Gan Browser Audit

Fecha: 2026-06-15

Version actual: 2.1

## Revision de mantenimiento 2026-06-15

- Version `2.7`: corregido el espacio vertical de la barra superior para evitar pestanas e indicadores recortados.
- Version `2.6`: eliminado `about:blank` del flujo normal y diferida la destruccion visual al cerrar.
- Version `2.5`: optimizadas transiciones de pestanas y agregadas acciones de triple/doble clic.
- Version `2.4`: eliminado el borde blanco nativo del contenido y corregida la altura de bookmarks.
- Version `2.3`: el actualizador tolera conexiones cerradas, reintenta y elimina descargas parciales.
- Version `2.2`: corregida la barra de marcadores recortada y agregada opcion persistente para mostrarla u ocultarla.
- El publicador de releases ahora reintenta cargas de artefactos interrumpidas.
- Build del navegador verificado correctamente.
- Pruebas de UI, firewall y adblocker verificadas correctamente.
- Repositorio local confirmado contra `wiimri/Gan-Browser`, rama `main`.
- Version `2.1` confirmada en codigo, instalador, `update.json`, README y CHANGELOG.
- Se detecto y corrigio `package-lock.json` desfasado en `2.0.0`.
- Se endurecio la publicacion para exigir instaladores y archivos SHA-256.
- Se agrego una comprobacion automatica del conjunto de release.
- Riesgo principal pendiente: ejecutable, instalador y manifiesto remoto aun no tienen firma digital.

## Estado Actual

Gan Browser ya es una base viable para un navegador Windows liviano porque usa WebView2 en vez de Electron y comparte un solo `CoreWebView2Environment` entre pestañas.

Medicion local despues de iniciar la app:

- Proceso host `GXLightBrowser`: ~77 MB working set.
- Arbol completo host + WebView2: ~402 MB working set.
- Perfil local `%LOCALAPPDATA%\GXLightBrowser`: ~233 MB.
- Pruebas Playwright: pasan en desktop, compacto y mock de YouTube Shields.

## Cambios Aplicados en Esta Auditoria

- El bloqueador ahora separa reglas de dominio en `HashSet`, evitando escanear toda la lista de dominios en cada request.
- Las pestanas inactivas pasan a `CoreWebView2MemoryUsageTargetLevel.Low`; la activa vuelve a `Normal`.
- Se agrego `PrivacyFirewall`: bloqueo local de trackers conocidos, endpoints tipo beacon/telemetry/pixel y limpieza de parametros de seguimiento.
- Se agrego menu principal con secciones internas para historial, descargas, extensiones, passwords/autofill, memoria, shields y settings.
- Se agrego monitor visible y Gan Pulse configurable para RAM, hard limit, hot tabs killer, CPU policy y network policy.
- Se agrego sistema de versionado y pestana de novedades de una sola aparicion por version.
- Se agrego suspension real de pestanas inactivas mediante descarte del WebView.
- Se agrego barra de favoritos con import/export HTML compatible con navegadores.
- Se agrego menu contextual de pestanas con seleccion multiple e islas coloreadas.
- Se agrego boveda local DPAPI para import/export CSV de passwords.
- Se agrego manifiesto remoto `update.json` en GitHub para mostrar novedades sin recompilar solo el texto del aviso.
- Se corrigio la navegacion de botones en paginas internas para abrir GitHub/Releases mediante el host WebView2.
- Se agrego gestor de bookmarks con carpetas, seleccion multiple y eliminacion masiva.
- Se corrigio la recepcion de comandos internos de bookmarks cuando WebView2 reporta origen gxlight.
- Se mantuvo el build limpio y las pruebas UI automatizadas.

## Hallazgos Criticos

### 1. Bloqueador Todavia No Es Brave-Level

El bloqueador actual soporta un subconjunto ABP:

- dominios `||domain.com^`
- substrings simples
- allow rules basicas
- excepciones manuales para YouTube

Falta:

- parsing completo de opciones ABP (`$script`, `$domain`, `$third-party`, `$important`, etc.)
- cosmetic filtering real
- scriptlets tipo uBlock/Brave
- resource replacements
- cache compilado de filtros

Recomendacion: integrar `adblock-rust` como motor nativo o WASM/Node sidecar. Es la diferencia entre “bloqueador artesanal” y “bloqueador serio”.

### 2. Cada Pestana Es Un WebView2

Esto es normal en un navegador con tabs, pero consume memoria. WebView2 usa arquitectura multiproceso, por lo que cada control puede levantar procesos de renderer/GPU/utility.

Ya se mitigo con:

- ambiente compartido
- `MemoryUsageTargetLevel.Low` en tabs inactivas

Siguiente paso:

- suspender tabs tras N minutos sin uso
- no suspender audio/video reproduciendose
- descargar tabs antiguas y restaurarlas por URL/titulo
- limite configurable de pestanas vivas

### 3. `BrowserForm.cs` Esta Haciendo Demasiado

Actualmente mezcla:

- layout visual
- navegacion
- ciclo de vida de tabs
- extensiones
- bloqueo de requests
- scripts de YouTube
- home HTML

Recomendacion de refactor:

- `TabManager.cs`
- `BrowserChrome.cs`
- `NavigationController.cs`
- `YouTubeShield.cs`
- `AdBlocker.cs`
- `ExtensionService.cs`
- `SettingsStore.cs`

Esto baja riesgo de bugs al seguir agregando funciones.

### 4. Extensiones: Soporte Practico Pero No Store-Nativo

WebView2 permite extensiones desempaquetadas. No equivale a instalar directo desde Chrome Web Store con un click.

Siguiente paso:

- pagina `gxlight://extensions`
- activar/desactivar/remover extension
- abrir carpeta de perfil/extensiones
- detector de extensiones instaladas en Chrome/Edge/Opera
- importador con lista visual, no solo `FolderBrowserDialog`

### 5. UI Aun Debe Acercarse a Navegador Diario

Falta para uso diario:

- reordenar pestanas
- duplicar pestana
- restaurar pestana cerrada
- historial
- descargas
- configuracion
- modo compacto real para pantallas chicas
- indicador de audio/mute por pestana
- dialogo de permisos por sitio

## Roadmap Priorizado

### P0 - Estabilidad Basica

- Manejar `ProcessFailed` de WebView2 y mostrar recuperacion.
- Persistir sesion: pestanas abiertas, URL activa y ventana.
- Cerrar correctamente eventos antes de disponer WebViews.
- Agregar logs rotativos en `%LOCALAPPDATA%\GXLightBrowser\logs`.

### P1 - Bajos Recursos

- Suspender pestanas inactivas despues de 5 minutos.
- No suspender pestanas con audio/video.
- Boton “Liberar memoria ahora”.
- Modo “Bajo consumo”: maximo de tabs activas + descarga de tabs antiguas.
- Medidor interno de memoria por proceso WebView2.

### P2 - Bloqueo Serio

- Integrar `adblock-rust`.
- Compilar filtros al iniciar y cachear.
- Cosmetic filtering/scriptlets por sitio.
- Dashboard Shields por sitio: requests bloqueadas, reglas, excepciones.
- Excepciones por dominio.
- Convertir el Privacy Firewall en una lista configurable por usuario.
- Separar contadores de anuncios, trackers y parametros limpiados.

### P3 - Navegador Completo

- Favoritos y barra de favoritos.
- Historial local SQLite.
- Descargas con progreso.
- Atajos: `Ctrl+L`, `Ctrl+Tab`, `Ctrl+Shift+T`, `Alt+Left`, `Alt+Right`.
- Omnibox mejorada con sugerencias locales.
- Configuracion visual.

### P4 - Distribucion

- Instalador.
- Auto-update binario basado en releases de GitHub.
- Firma de ejecutable.
- Builds x64/arm64.

## Verificacion Actual

Comandos:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Build.ps1
npm.cmd run test:ui
```

Resultado esperado:

- build exitoso
- desktop sin overflow
- compacto sin overflow
- cierre de pestanas en preview
- middle-click en accesos en preview
- YouTube Shields mock sin fallas

## Decision Tecnica Recomendada

Mantener WebView2 para ligereza de distribucion, pero cambiar el bloqueador a `adblock-rust` y convertir el shell en modulos separados antes de agregar mas funciones grandes.

El objetivo realista:

- host nativo pequeno
- WebView2 evergreen
- tabs con suspension agresiva
- bloqueo nativo serio
- UI simple y densa, no pesada
- extensiones desempaquetadas con gestor visual

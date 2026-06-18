# Gan Browser

Version actual: `2.11`

Gan Browser es un navegador liviano y enfocado en privacidad para Windows. Usa WebView2 para aprovechar el runtime del sistema y reducir el consumo frente a alternativas basadas en Electron.

Usa Microsoft Edge WebView2 en vez de Electron. Eso permite que la aplicacion sea mas pequena y aproveche el runtime WebView2 que ya viene instalado o disponible en muchos equipos Windows.

## Descargar Gan Browser

[![Descargar Gan Browser para Windows](https://img.shields.io/badge/Descargar-Gan_Browser_para_Windows-4CE0BE?style=for-the-badge&logo=windows11&logoColor=11151B)](https://github.com/wiimri/Gan-Browser/releases/latest/download/GanBrowser-Setup-x64.exe)

- [Descargar el instalador de la ultima version](https://github.com/wiimri/Gan-Browser/releases/latest/download/GanBrowser-Setup-x64.exe)
- [Ver todas las versiones publicadas](https://github.com/wiimri/Gan-Browser/releases)
- [Comprobar el SHA-256 del instalador](https://github.com/wiimri/Gan-Browser/releases/latest/download/GanBrowser-Setup-x64.sha256.txt)

> Para instalar el navegador, descarga `GanBrowser-Setup-x64.exe` desde **Releases**. La opcion **Code > Download ZIP** descarga solamente el codigo fuente.

## Caracteristicas actuales

- Navegacion con multiples pestanas.
- Perfil persistente aislado en `%LOCALAPPDATA%\GXLightBrowser`.
- Extensiones WebView2 habilitadas.
- Importacion de extensiones Chrome/Edge desempaquetadas desde una carpeta.
- Bloqueo nativo de requests antes de que lleguen a la pagina.
- Archivo local de filtros tipo ABP en `%LOCALAPPDATA%\GXLightBrowser\filters.txt`.
- Prevencion estricta de seguimiento de WebView2.
- Interfaz compacta con marco oscuro, toolbar adaptable, pestanas, accesos directos laterales y tooltips.
- Accesos a Chrome Web Store y Opera Addons.
- Cierre de pestanas con `x`, click del scroll o `Ctrl+W`.
- Accesos directos con click del scroll para abrir en una nueva pestana interna.
- YouTube Guard para bloquear instrucciones publicitarias y quitar contenedores comunes de anuncios.
- Modo de baja memoria para pestanas inactivas.
- Busqueda rapida de reglas de dominio en el bloqueador nativo.
- Privacy Firewall integrado para trackers conocidos, endpoints tipo telemetria y parametros de seguimiento.
- Menu principal con accesos a pestanas, historial, descargas, extensiones, passwords, memoria, Gan Guard, settings y herramientas de desarrollador.
- Pagina de historial de sesion.
- Pagina de descargas de sesion.
- Pregunta nativa antes de guardar passwords y autofill de WebView2 habilitado.
- Tab islands colapsables y persistentes para agrupar pestanas.
- Seleccion multiple de pestanas con `Ctrl+clic` o `Shift+clic`.
- Seleccion multiple visible mediante borde y marcador de color.
- Islas con barra propia siempre visible; la barra colapsa, despliega y recibe pestanas arrastradas.
- Arrastrar una pestana sobre otra crea una isla nueva de dos pestanas.
- Menu contextual de tab islands: crear, colapsar, desplegar, disolver, duplicar, recargar, copiar URLs y cerrar seleccionadas.
- Suspension real de pestanas inactivas para liberar WebView2 y memoria.
- Monitor visible de memoria y Gan Pulse configurable.
- Pestana de novedades que aparece solo una vez por version instalada.
- Bitacora acumulativa dentro de Update notes, cargada desde `CHANGELOG.md` en GitHub.
- Barra de favoritos con apertura en nueva pestana mediante click del scroll y opcion para mostrarla u ocultarla.
- Importacion/exportacion de bookmarks en HTML compatible con navegadores.
- Gestor de bookmarks con carpetas, busqueda, seleccion multiple y eliminacion masiva.
- Importacion/exportacion de passwords CSV mediante una boveda local protegida con Windows DPAPI.
- Guardado nativo de passwords solo despues de aceptar el aviso de WebView2; Windows protege las credenciales del perfil.
- Restauracion de sesion optimizada: una pestana activa y las demas suspendidas para reducir RAM.
- Favicons por pagina, pestanas compactas cuadradas y tamanos pequeno, mediano, grande o automatico.
- Reduccion automatica progresiva: al llenar la barra desaparecen cierre y texto, pero permanece el favicon.
- Marcador visual de dominio cuando una pagina no entrega favicon.
- Playlist local para guardar, volver a abrir y eliminar paginas multimedia.
- Icono propio integrado en la ventana y el ejecutable.

## Versionado

Gan Browser usa versiones simples pensadas para el proyecto personal:

- linea estable actual: `2.11`
- cada mejora o correccion del navegador sube la version menor: `2.1`, `2.2`, `2.3` ...

Cuando una nueva version se ejecuta por primera vez, el navegador abre `gxlight://updated` con un resumen corto. La app guarda la ultima version vista en `%LOCALAPPDATA%\GXLightBrowser\settings.ini`, por lo que esa pestana aparece solo una vez por version.

Desde `1.2`, el aviso de novedades se lee desde `update.json` en GitHub:

```text
https://raw.githubusercontent.com/wiimri/Gan-Browser/main/update.json
```

Eso permite cambiar el texto de novedades, links y version publicada desde GitHub. Si el navegador no puede conectar a GitHub, usa las notas locales compiladas como respaldo.

El historico completo de cambios esta en `CHANGELOG.md`:

```text
https://github.com/wiimri/Gan-Browser/blob/main/CHANGELOG.md
```

`Menu > Buscar actualizaciones` compara la version instalada con `update.json`. Si hay una version mayor,
descarga y verifica el instalador permanente en segundo plano. Cuando queda listo, Gan Browser permite
reiniciar para aplicarlo; el instalador actualiza los binarios, vuelve a abrir el navegador y conserva
perfil, passwords, favoritos y sesion.

Por seguridad, Gan Browser todavia no instala silenciosamente: la descarga y ejecucion deben ser confirmadas.
La siguiente mejora critica es firmar el instalador y verificar criptograficamente su manifiesto y SHA-256.

Los cambios solo de documentacion pueden ir en GitHub sin subir la version de la aplicacion, porque no cambian el binario ni la experiencia dentro del navegador.

## Compilacion

Ejecutar desde PowerShell:

```powershell
cd C:\Users\arias\Documents\DEV\GXLightBrowser
.\scripts\Build.ps1
.\scripts\Run.ps1
```

El script de build descarga el paquete oficial `Microsoft.Web.WebView2` desde NuGet, compila con el compilador de .NET Framework incluido en Windows y genera `bin\GXLightBrowser.exe`.

## Instalador

Desde la version `1.9`, Gan Browser cuenta con un instalador para Windows 10/11 x64. Desde `1.13`, instala en
la carpeta nativa de programas de Windows, normalmente `C:\Program Files\GXLightBrowser`, y solicita
permisos de administrador. El instalador copia el navegador completo y comprueba los requisitos que
sistemas modificados como Atlas OS pueden eliminar:

- Microsoft Edge WebView2 Evergreen Runtime.
- Microsoft .NET Framework 4.8.
- `Microsoft.Web.WebView2.Core.dll`.
- `Microsoft.Web.WebView2.WinForms.dll`.
- `WebView2Loader.dll`.

Cada instalacion o actualizacion recrea el acceso directo comun del escritorio y del menu Inicio. Los
accesos directos personales obsoletos se eliminan solamente despues de completar correctamente la
instalacion, evitando que una actualizacion deje un icono apuntando a una ruta inexistente.

Para construirlo:

```powershell
.\scripts\Build-Installer.ps1
```

El resultado queda en `dist\`. Consultar [docs/INSTALACION.md](docs/INSTALACION.md) para detalles de
compatibilidad y reparacion en Atlas OS.

Para publicar el instalador como asset de GitHub Release usando la credencial segura de Git:

```powershell
.\scripts\Publish-Release.ps1 -Version 2.11
```

El script valida que navegador, instalador, manifiesto, paquetes y bitacora usen la misma version. Tambien
publica los tres instaladores y sus tres archivos SHA-256; el actualizador rechaza una release sin el hash
permanente `GanBrowser-Setup-x64.sha256.txt`.

Antes de publicar se puede ejecutar solamente la comprobacion:

```powershell
.\scripts\Verify-Release.ps1 -Version 2.11 -RequireAssets
```

El instalador y su comprobacion SHA-256 permanentes pueden descargarse desde:

```text
https://github.com/wiimri/Gan-Browser/releases/latest/download/GanBrowser-Setup-x64.exe
https://github.com/wiimri/Gan-Browser/releases/latest/download/GanBrowser-Setup-x64.sha256.txt
```

## Compatibilidad del cambio de nombre

Gan Browser `2.11` conserva temporalmente varios identificadores internos de GX Light Browser para que la actualizacion no pierda datos:

- perfil y configuracion en `%LOCALAPPDATA%\GXLightBrowser`
- ejecutable interno `GXLightBrowser.exe`
- protocolo interno `gxlight://`
- AppId del instalador
- instalador permanente legado `GXLightBrowser-Setup-x64.exe` para actualizar clientes `1.22`

Estos nombres son detalles tecnicos heredados y no forman parte de la marca visible.

## Extensiones

Abrir `Menu > Extensions` y usar una de estas opciones:

- `Importar extension desempaquetada...`
- `Ver extensiones instaladas`
- `Abrir Chrome Web Store`
- `Abrir Opera Addons`

Al importar, seleccionar:

- una carpeta de extension desempaquetada que contenga `manifest.json`, o
- una carpeta de version de una extension instalada en Chrome/Edge.

El importador copia la extension al perfil de Gan Browser y elimina carpetas tipo `_metadata`, porque WebView2 rechaza rutas de extension con carpetas que comienzan con `_`.

Importante: WebView2 permite cargar extensiones locales, pero no equivale a instalar directo desde Chrome Web Store con un click. Algunas extensiones que dependen de la UI propia de Chrome, de instalacion desde tienda o de APIs no soportadas pueden requerir adaptacion.

Los botones de tiendas son accesos de navegacion. La instalacion directa desde Chrome Web Store depende de la integracion tienda/navegador; por ahora el camino confiable es descargar o ubicar la carpeta de la extension e importarla.

## Favoritos

La barra de favoritos queda visible bajo la barra de navegacion.

- `Ctrl+D` guarda la pagina actual en favoritos.
- Click izquierdo abre el favorito en la pestana actual.
- Click del scroll abre el favorito en una nueva pestana interna.
- Click derecho permite abrir, copiar la direccion o eliminar el favorito.
- `Menu > Bookmarks` permite importar/exportar bookmarks HTML compatibles con navegadores.

## Passwords

El aviso nativo para guardar passwords y el autofill de WebView2 estan habilitados para el perfil de la app en `%LOCALAPPDATA%\GXLightBrowser\Profile`.

`Menu > Passwords and autofill > Preguntar antes de guardar passwords` permite comprobar o cambiar esta
preferencia. Gan Browser nunca captura passwords mediante JavaScript ni las guarda al escribirlas: WebView2
solo las conserva despues de que el usuario acepta su popup nativo.

Gan Browser tambien incluye una boveda local para importar, visualizar y rellenar passwords:

- formatos CSV compatibles: OperaGX/Chromium `name,url,username,password` y variantes con `origin_url`, `username_value` o `password_value`
- las entradas importadas se guardan cifradas para el usuario actual de Windows con DPAPI
- las contrasenas permanecen protegidas con DPAPI mientras estan cargadas en memoria
- visualizar, rellenar o exportar exige aprobar Windows Hello/PIN
- el rellenado solo se habilita cuando el dominio coincide exactamente y nunca envia el formulario
- al exportar se genera un CSV normal en texto visible, asi que hay que tratarlo con cuidado

Al abrir un formulario compatible, Gan Browser rellena automaticamente el usuario de la primera cuenta y muestra todas las cuentas disponibles junto al campo de contrasena. Al seleccionar una cuenta cambia el usuario si corresponde y solicita Windows Hello/PIN antes de completar la contrasena. `Ctrl+Shift+L` permanece como respaldo.

La hoja de ruta y recomendaciones estan documentadas en [docs/SEGURIDAD.md](docs/SEGURIDAD.md).

## Bloqueo de anuncios

La version actual incluye un bloqueador nativo que intercepta requests de WebView2 mediante `WebResourceRequested`. Soporta el subconjunto mas importante de reglas ABP/EasyList:

- `||domain.com^`
- reglas simples por substring de URL
- reglas basicas de permiso con `@@`
- comentarios y reglas cosmeticas se ignoran de forma segura

Para refrescar listas grandes de filtros:

```powershell
.\scripts\Update-Filters.ps1
```

## Camino hacia un bloqueador nivel Brave

El bloqueador de produccion de Brave se basa en el motor open source `brave/adblock-rust`. Este prototipo esta preparado para que la clase `AdBlocker` pueda reemplazarse mas adelante por una integracion nativa con `adblock-rust`.

Opciones posibles:

- toolchain de Rust mas un wrapper C ABI/UniFFI para C#, o
- binding Node `adblock-rs` como sidecar local.

El bloqueador actual ya intercepta requests de forma nativa y no depende de APIs Manifest V3 para su bloqueo incorporado.

## Privacy Firewall

Gan Browser incluye un Privacy Firewall local a nivel navegador. No instala drivers, no modifica Windows Firewall y no actua como VPN; funciona dentro del pipeline de requests del navegador.

Actualmente:

- bloquea dominios conocidos de trackers, analytics, pixels, RUM, ad-tech y attribution
- bloquea requests de terceros que parecen telemetria, beacons, pixels o endpoints de tracking
- limpia parametros de seguimiento como `utm_*`, `fbclid`, `gclid`, `msclkid`, `mc_cid` y similares durante la navegacion
- mantiene excepciones de compatibilidad para recursos necesarios de reproduccion de YouTube y Crunchyroll

Esto no oculta tu IP ni reemplaza un bloqueador nativo completo como `adblock-rust`. Su objetivo es reducir superficies de seguimiento dentro del navegador.

## Verificacion UI

El chrome responsive del navegador esta reflejado en `docs\ui-preview.html` para revisarlo con Playwright:

```powershell
npm.cmd run test:ui
npm.cmd run test:firewall
```

Esto guarda screenshots en `screenshots\` y revisa que no haya overflow horizontal en escritorio ni modo compacto. Tambien verifica cierre de pestanas, click del scroll en accesos/favoritos y el script de YouTube Shields contra una pagina mock con anuncios.

La prueba de firewall revisa bloqueo de trackers y limpieza de parametros de seguimiento.

## Controles para bajos recursos

- La barra superior mantiene navegacion, direccion, uso de memoria, limitador de memoria y menu.
- El resto de funciones vive dentro de `Menu`.
- `Pulse 0.8G` abre Gan Pulse con RAM limiter, hard limit, hot tabs killer, CPU policy y network policy.
- Las pestanas inactivas pasan a bajo consumo.
- `Menu > Suspend inactive tabs now` descarta WebViews inactivos y mantiene la pestana visible.
- `Menu > Guardar pestanas al cerrar` conserva la sesion. Al reiniciar, solo la pestana activa crea un
  WebView; las demas se muestran suspendidas hasta seleccionarlas.

## Auditoria

Ver `AUDIT.md` para la revision actual de arquitectura, roadmap de bajos recursos, notas de memoria y prioridades siguientes.

## Fuentes revisadas

- APIs de extensiones de Microsoft WebView2: `CoreWebView2EnvironmentOptions.AreBrowserExtensionsEnabled`, `CoreWebView2Profile.AddBrowserExtensionAsync` y `GetBrowserExtensionsAsync`.
- Motor de bloqueo de Brave: `https://github.com/brave/adblock-rust`.

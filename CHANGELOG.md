# Historial de versiones

Este archivo es el historial estable de Gan Browser, anteriormente llamado GX Light Browser.

## Version actual

- Version publicada: `2.11`
- Fecha: `2026-06-18`
- Codigo fuente: <https://github.com/wiimri/Gan-Browser>
- Tags: <https://github.com/wiimri/Gan-Browser/tags>

## Como funciona el aviso de novedades

Desde la version `1.2`, el navegador lee el manifiesto remoto:

```text
https://raw.githubusercontent.com/wiimri/Gan-Browser/main/update.json
```

Ese archivo indica cual es la version publicada, el nombre de la release, los enlaces y las novedades que debe mostrar `gxlight://updated`.

Si GitHub no responde, Gan Browser usa las notas locales compiladas como respaldo.

## v2.11 - Importacion OperaGX/Chromium a boveda segura

Fecha: `2026-06-18`

Cambios:

- El importador de contrasenas acepta CSV exportados desde OperaGX y navegadores Chromium.
- Se detectan encabezados con BOM y columnas variantes como `origin_url`, `username_value` y `password_value`.
- Se soportan campos entre comillas, comas dentro de contrasenas y valores multilinea.
- Las filas incompletas se omiten con conteo visible y los duplicados no se vuelven a guardar.
- Las credenciales importadas quedan cifradas con Windows DPAPI y disponibles para el asistente de rellenado.
- Se agrego una prueba dedicada para CSV de Opera/Chromium.

## v2.10 - Asistente contextual de credenciales

Fecha: `2026-06-15`

Cambios:

- El usuario o correo de la primera cuenta se rellena automaticamente al detectar credenciales para el dominio.
- Los formularios muestran las cuentas disponibles junto al campo de contrasena.
- Al seleccionar una cuenta se completa su usuario y se solicita Windows Hello/PIN antes de rellenar la contrasena.
- El asistente detecta formularios agregados dinamicamente sin exponer contrasenas en el DOM.
- Las paginas en segundo plano no pueden solicitar el desbloqueo de la boveda.
- `Ctrl+Shift+L` permanece disponible como accion de respaldo.

## v2.9 - Boveda util con Windows Hello

Fecha: `2026-06-15`

Cambios:

- La pagina de contrasenas permite abrir una entrada y visualizarla despues de aprobar Windows Hello/PIN.
- Se agrega `Rellenar sitio actual desde la boveda` al menu de contrasenas.
- La barra de estado avisa cuando hay credenciales compatibles y `Ctrl+Shift+L` inicia el rellenado.
- El rellenado exige coincidencia exacta del host, requiere Windows Hello/PIN y nunca envia el formulario.
- Exportar toda la boveda tambien requiere verificar la identidad.
- Las contrasenas permanecen protegidas con DPAPI incluso mientras las entradas estan cargadas en memoria.
- Se agrego una prueba que rechaza coincidencias de dominio inseguras y cualquier intento de enviar formularios.

## v2.8 - Cierre seguro de pestanas con favicon

Fecha: `2026-06-15`

Cambios:

- Se corrigio la excepcion `System.ArgumentException: Parameter is not valid` detectada al cerrar una pestana.
- Cada boton de pestana conserva una copia propia del favicon mientras necesita dibujarlo.
- La barra desecha explicitamente los botones retirados durante cada reconstruccion.
- Se agrego una prueba que libera el favicon original y verifica que el boton pueda repintarse sin errores.

## v2.7 - Espacio vertical completo en pestanas

Fecha: `2026-06-15`

Cambios:

- La fila superior aumenta su altura para respetar los margenes verticales de las pestanas.
- Las pestanas, el boton de nueva pestana y los indicadores de isla usan una altura uniforme.
- El texto, los iconos y el indicador `[S]` de suspension ya no quedan recortados en la parte inferior.
- Se agrego una comprobacion automatica que impide publicar dimensiones incompatibles.

## v2.6 - Eliminacion profunda de parpadeos

Fecha: `2026-06-15`

Cambios:

- Se analizo cuadro por cuadro el video `2026-06-15 00-20-04.mp4`.
- Las nuevas pestanas normales abren directamente `gxlight://home`; ya no muestran `about:blank`.
- Al cerrar una pestana, su WebView se destruye en el siguiente ciclo de UI despues de mostrar la vecina.
- La pestana vecina recibe foco y se lleva al frente antes de retirar la pestana cerrada.
- Cada WebView nuevo precarga una superficie interna y espera su primer render antes de mostrarse.
- Se agrego una prueba Playwright que repite aperturas y cierres y rechaza transiciones sin contenido.

## v2.5 - Transiciones fluidas y acciones rapidas

Fecha: `2026-06-15`

Cambios:

- Las nuevas pestanas se seleccionan solamente despues de inicializar WebView2.
- Al cerrar una pestana activa se selecciona primero la siguiente, evitando mostrar el fondo vacio.
- El contenedor de pestanas usa doble buffer para reducir repintados intermedios.
- Triple clic en la barra de direcciones selecciona todo el texto.
- Doble clic sobre una descarga intenta abrir el archivo disponible.
- Se analizo la grabacion `2026-06-14 23-36-55.mp4` para identificar las transiciones sin contenido.

## v2.4 - Navegacion sin borde y bookmarks completos

Fecha: `2026-06-15`

Cambios:

- Se reemplazo el contenedor nativo de paginas por una variante sin marco visual.
- Se elimina el borde blanco que rodeaba el contenido web y reducia el area de navegacion.
- La barra de marcadores reserva la altura real necesaria para mostrar botones completos.
- Las paginas y WebView ocupan todo el espacio disponible sin margenes internos.
- Se agrego una prueba de layout para impedir que reaparezca el borde nativo del contenido.

## v2.3 - Actualizaciones robustas desde GitHub

Fecha: `2026-06-15`

Cambios:

- El actualizador reintenta hasta tres veces cuando GitHub interrumpe una descarga.
- Los instaladores parciales o vacios se eliminan antes de cada nuevo intento.
- Se registran los intentos fallidos para facilitar el diagnostico.
- Se verifico que el instalador permanente remoto y su SHA-256 responden correctamente y coinciden.

## v2.2 - Barra de marcadores corregida

Fecha: `2026-06-15`

Cambios:

- Se agrego `Mostrar barra de marcadores` al submenu Marcadores, con indicador del estado actual.
- La opcion muestra u oculta la barra inmediatamente y conserva la preferencia entre sesiones.
- La fila de marcadores ahora usa un alto fijo correcto cuando esta visible.
- Al ocultar la barra se elimina completamente la franja residual bajo la barra de direcciones.
- El publicador de releases reintenta automaticamente las cargas interrumpidas por GitHub.

## Bitacora de mantenimiento

### 2026-06-15 - Auditoria de repositorio y proceso de release

- Se verifico la compilacion del navegador y las pruebas automatizadas de UI, firewall y adblocker.
- Se agrego `scripts/Verify-Release.ps1` para validar la sincronizacion de version entre codigo, instalador, manifiesto, paquetes y bitacora.
- `Publish-Release.ps1` ahora publica por defecto los tres instaladores y sus archivos SHA-256 requeridos.
- Se sincronizo `package-lock.json` con la version `2.1.0`.
- Se corrigio la recomendacion del instalador principal.
- Estos cambios son de mantenimiento y distribucion; no cambian el binario publicado ni suben la version de la aplicacion.

## v2.1 - Compartir y Exportar Pestañas

Fecha: `2026-06-13`

Cambios:

- **Compartir y guardar pestañas**: Nueva opción añadida al menú principal para administrar las pestañas abiertas colectivamente.
- **Copiar todas las URLs**: Permite copiar de un solo clic al portapapeles todas las direcciones (URLs) de las pestañas que estén abiertas en la sesión activa.
- **Exportar pestañas**: Exporta el listado completo de URLs de las pestañas abiertas a un archivo de texto plano (`.txt`), una por línea.
- **Importar pestañas**: Abre y procesa un archivo `.txt` cargando de manera automática todas las URLs contenidas en él en pestañas individuales del navegador.
- **Versión 2.1**: Bump de versión global del navegador, instalador y manifiesto remoto.

## v2.0 - Gan Browser

Fecha: `2026-06-11`

Cambios:

- GX Light Browser adopta el nuevo nombre publico `Gan Browser`.
- `GX Control` pasa a llamarse `Gan Pulse`.
- La proteccion integrada se presenta como `Gan Guard`.
- Se incorpora un icono propio para Gan Browser.
- El instalador, manifiesto, documentacion y repositorio adoptan la nueva marca.
- La actualizacion conserva el perfil, passwords, favoritos, sesiones, AppId, ruta instalada y protocolo interno existentes.
- Se publica un instalador permanente legado para que los clientes `1.22` puedan actualizar a `2.0`.
- El actualizador descarga y verifica primero; solicita reiniciar solamente cuando el instalador ya esta preparado.
- Las actualizaciones sin comprobacion SHA-256 valida se rechazan.

## v1.22 - Búsqueda automática de actualizaciones al iniciar

Fecha: `2026-06-11`

Cambios:

- **Búsqueda automática de actualizaciones**: Opción configurable en la sección de configuración ("Sistema y WebRTC") para buscar actualizaciones en segundo plano de forma silenciosa al arrancar el navegador. No genera diálogos intrusivos si la aplicación está actualizada.
- **Notificación interactiva**: Si se encuentra una actualización disponible al iniciar, se muestra un mensaje informativo preguntando al usuario si desea proceder con la descarga e instalación.
- **Preparación de versión 1.22**: Bump global de versión en todos los archivos de metadatos de compilación, instalador (Inno Setup) y manifiestos de actualización en línea.

## v1.21 - Modo Claro/Oscuro Inmersivo y Corrección de Atajos WebView2

Fecha: `2026-06-11`

Cambios:

- **Modo Claro / Oscuro / Automático**: Soporte completo para cambiar el tema de la interfaz del contenedor nativo y de las páginas internas en tiempo real.
- **Fondos Inmersivos**: La barra lateral, la barra de navegación y las páginas de configuración e inicio (`gxlight://home`) adoptan un matiz del color de acento del tema seleccionado, replicando la estética premium de Opera GX.
- **Foco de Atajos Solucionado**: Implementación definitiva mediante la intercepción del evento `AcceleratorKeyPressed` en `CoreWebView2Controller` que asegura que los atajos `Ctrl+T` (nueva pestaña blank en blanco) y `Ctrl+W` (cerrar pestaña) funcionen siempre, incluso si el WebView2 tiene el foco.
- **Buscador Predeterminado Persistente**: Se corrigió el flujo de cambio y guardado de motor de búsqueda predeterminado (Google, DuckDuckGo, Bing, Yahoo) en `settings.ini`.

## v1.20 - Temas, Rediseño de Configuración y UI Compacta

Fecha: `2026-06-11`

Cambios:

- Se ha rediseñado por completo la pagina de configuración (`gxlight://settings`) con una interfaz de barra lateral interactiva inspirada en Opera GX.
- Se implementó un sistema de 12 temas personalizables (Classic, Ultraviolet, Sub Zero, Frutti Di Mare, etc.) con cambios de color de acento en tiempo real en los controles de C# y variables CSS.
- Se ha compactado la interfaz de usuario para optimizar el espacio visual de la pagina web (barra lateral de 46px, fila de pestañas de 22px, navegación de 30px, estado de 22px).
- La barra de marcadores se oculta por completo cuando está desactivada en la configuración, maximizando el espacio de visualización vertical.
- Se implementaron diálogos interactivos para cambiar la carpeta de descargas por defecto y para preguntar la ubicación de guardado antes de descargar cada archivo.
- Los menús contextuales principales y de pestañas ahora usan un renderizado limpio, sin bordes y con estilo profesional oscuro.

## v1.19 - Actualizador preparado y atajos WebView2

Fecha: `2026-06-11`

Cambios:

- Las actualizaciones se descargan y validan por SHA-256 en segundo plano sin cerrar el navegador.
- Cuando el instalador esta listo, GX Light muestra un aviso y permite seguir navegando hasta reiniciar.
- El menu muestra `Reiniciar para aplicar` mientras exista una actualizacion preparada.
- Al aplicar la actualizacion, el instalador trabaja en modo silencioso y vuelve a abrir GX Light.
- La sesion se guarda antes del reinicio para restaurar las pestanas abiertas.
- La pagina Update notes puede iniciar la preparacion de una actualizacion dentro del navegador.
- Un filtro nativo de mensajes captura los comandos antes del control WebView2 para que `Ctrl+T`, `Ctrl+W`, `Ctrl+L`, `Ctrl+J`, `Ctrl+H`, `Ctrl+D`, `Ctrl+F`, `Ctrl+R`, `Ctrl+N`, `Alt+T`, `Alt+P` y `F12` funcionen con la pagina enfocada.

## v1.18 - Bloqueo del reproductor, boton visible y favicons

Fecha: `2026-06-11`

Cambios:

- YouTube Shields limpia `adPlacements`, `playerAds`, `adSlots` y estructuras equivalentes antes de que el reproductor pueda iniciar anuncios.
- La limpieza cubre datos iniciales, respuestas JSON y llamadas `fetch` del endpoint del reproductor.
- Se conservan los datos normales del video y ya no se acelera, silencia ni adelanta contenido.
- Se agrego el boton visible `Block Ads On/Off` junto a GX Control en la barra superior.
- Los favicons ahora prueban el icono de WebView2, el enlace declarado por la pagina y `/favicon.ico`.
- Los favicons descargados se guardan en un cache local por dominio para pestañas activas y suspendidas.

## v1.17 - Comentarios, bloqueo real, fullscreen y colapso individual

Fecha: `2026-06-11`

Cambios:

- YouTube Shields deja de alterar `currentTime`, velocidad y mute del video para intentar saltar anuncios.
- El detector solo pulsa botones de omitir visibles mientras el reproductor confirma que hay un anuncio.
- El ciclo de Shields ya no pulsa controles ocultos repetidamente, evitando que el editor de comentarios pierda foco.
- Las reglas publicitarias se evalúan antes de las excepciones de compatibilidad multimedia.
- La compatibilidad de YouTube deja de permitir indiscriminadamente todas las solicitudes XHR y Fetch.
- Se bloquean endpoints publicitarios conocidos tanto de `youtube.com` como de `youtubei.googleapis.com`.
- WebView2 conecta `ContainsFullScreenElementChanged` para ocultar la interfaz y ocupar la pantalla real.
- Cada pestana conserva un estado compacto individual.
- El menu contextual separa colapsar esta pestana, colapsar seleccionadas y modo compacto global.
- El estado compacto individual se conserva al restaurar la sesion.

## v1.16 - Motor interno, privacidad y actualizacion reparada

Fecha: `2026-06-11`

Cambios:

- Los modelos de historial, descargas, favoritos y passwords se separaron de `BrowserForm.cs`.
- Las paginas internas se trasladaron a `InternalPages.cs`, reduciendo el nucleo del navegador en mas de 600 lineas.
- Se corrigieron las rutas `gxlight://home` y `gxlight://updated` para evitar la pagina `Section not found`.
- Update notes descarga y muestra la bitacora acumulativa de `CHANGELOG.md`, conservando todas las versiones anteriores.
- El menu contextual permite suspender manualmente todas las pestanas seleccionadas.
- El limitador de RAM evita suspensiones en cascada mientras Windows libera procesos WebView2.
- El monitor de memoria cuenta solamente los procesos pertenecientes al entorno WebView2 de GX Light.
- El fallback de favicons tolera APIs no implementadas y continua con la descarga de `/favicon.ico`.
- WebView2 activa el opt-out de telemetria y argumentos adicionales contra reportes en segundo plano.
- El analizador de filtros conserva las rutas de excepciones ABP y evita permitir dominios completos por error.
- Se agregaron reglas integradas para endpoints publicitarios y de telemetria de YouTube.
- YouTube Shields oculta anuncios antes de mostrarlos, acelera su deteccion y restaura audio y velocidad al volver al contenido.
- El navegador deja de mantener un mutex que impedia al instalador completar una actualizacion iniciada desde GX Light.
- El actualizador solicita a Inno Setup cerrar aplicaciones de forma controlada antes de reemplazar binarios.
- El acceso directo comun del escritorio y del menu Inicio se crea siempre, sin depender de una opcion desmarcable.
- Los accesos directos personales obsoletos se eliminan solamente despues de completar correctamente la instalacion.
- La migracion mantiene intacto el perfil ubicado en `%LOCALAPPDATA%\GXLightBrowser`.

## v1.15 - Pestanas adaptables con favicon permanente

Fecha: `2026-06-10`

Cambios:

- El ancho automatico deja de imponer el minimo fijo de 118 px y puede reducirse hasta 38 px.
- Cuando falta espacio desaparece primero el cierre, despues el titulo y permanece el favicon centrado.
- Los tamanos manuales tambien se reducen temporalmente si son demasiadas pestanas para la barra.
- Las paginas sin favicon muestran un marcador coloreado basado en su dominio.
- Las pestanas suspendidas descargan el favicon del sitio sin crear un WebView adicional.
- Las islas compactas muestran varias barras verticales segun la cantidad de pestanas agrupadas.
- El calculo descuenta barras de isla, pestanas colapsadas y el boton de nueva pestana.

## v1.14 - Seleccion visible, arrastre e islas funcionales

Fecha: `2026-06-10`

Cambios:

- Las pestanas multiseleccionadas muestran un borde rojo y un marcador visible.
- `Ctrl+clic` alterna la seleccion individual y `Shift+clic` selecciona un rango.
- Crear una isla manualmente exige al menos dos pestanas seleccionadas, evitando islas individuales accidentales.
- Cada isla mantiene una barra vertical independiente cuando esta desplegada o colapsada.
- Pulsar la barra alterna entre colapsar y desplegar la isla.
- Se pueden arrastrar pestanas hacia la barra o hacia una pestana que ya pertenece a una isla.
- Arrastrar una pestana sobre otra sin isla crea una isla nueva.
- El menu contextual permite agregar seleccionadas a una isla, cambiar el tamano y activar modo compacto.
- Las sesiones antiguas con `[Suspended]` se muestran usando el indicador corto `[S]`.
- Los favicons usan la URL informada por WebView2 o `/favicon.ico` cuando el metodo directo no esta implementado.

## v1.13 - Instalacion en Program Files y actualizador verificado

Fecha: `2026-06-10`

Cambios:

- El instalador usa `{autopf}\GXLightBrowser`, que corresponde a la carpeta nativa Program Files.
- La instalacion solicita permisos de administrador y deja de reutilizar la antigua ruta en LocalAppData.
- La migracion elimina solamente los binarios y accesos directos antiguos; el perfil del usuario permanece intacto.
- GX Light detecta automaticamente una version remota mayor al iniciar.
- `Menu > Buscar actualizaciones` descarga el instalador en vez de abrir solamente un enlace.
- La descarga se valida contra el SHA-256 publicado antes de abrir el instalador.
- El proceso guarda la sesion, abre el instalador y cierra GX Light para permitir reemplazar los binarios.
- El build genera hashes permanentes y versionados para GitHub Releases.

Pruebas:

- Compilacion del ejecutable completada.
- Pruebas Playwright y Privacy Firewall ejecutadas.
- Instalador construido y comprobado sobre la ruta de programas de Windows.

## v1.12 - Islas colapsables, favicons y actualizaciones visibles

Fecha: `2026-06-10`

Cambios:

- El ajuste de passwords ahora se llama `Preguntar antes de guardar passwords` y explica que solo se guarda despues de aceptar el popup nativo.
- Las credenciales nativas quedan bajo la proteccion del perfil de Windows y la boveda importada conserva DPAPI.
- Las pestanas suspendidas usan el indicador corto `[S]`.
- Los favicons se vuelven a consultar al completar cada navegacion.
- Se agregaron tamanos de pestana automatico, pequeno, mediano y grande.
- `Ctrl+clic` selecciona pestanas individuales y `Shift+clic` selecciona rangos.
- Las islas nuevas se colapsan en una barra vertical y pueden desplegarse, colapsarse o disolverse.
- El estado colapsado de las islas se conserva entre sesiones.
- `Menu > Buscar actualizaciones` consulta GitHub y ofrece descargar el instalador permanente.
- Se agrego `docs/SEGURIDAD.md` con reglas actuales y prioridades de seguridad.

Pruebas:

- Compilacion de .NET Framework completada.
- Pruebas Playwright de UI y aislamiento de YouTube ejecutadas.
- Prueba del Privacy Firewall ejecutada.

## v1.11 - Passwords persistentes y restauracion ligera de sesion

Fecha: `2026-06-09`

Cambios:

- El guardado de passwords se activa tanto en `CoreWebView2.Settings` como en `CoreWebView2.Profile`.
- Nuevo interruptor `Guardar passwords automaticamente` dentro del menu de passwords.
- El cierre de GX Light guarda configuración y sesión, detiene tareas periódicas y dispone limpiamente los WebViews.
- `session.dat` usa formato v2 con URL y título codificados, evitando fallos por caracteres como `|`.
- Nuevo interruptor `Guardar pestanas al cerrar`.
- Al restaurar una sesión, solamente la pestaña seleccionada crea un WebView; todas las demás quedan suspendidas hasta abrirlas.

Pruebas:

- El log confirmó `Password autosave=True` sobre el perfil persistente de GX Light.
- Una prueba controlada reinició tres pestañas y conservó URLs/títulos que contenían separadores.
- La sesión y configuración originales fueron respaldadas y restauradas después de la prueba.

## v1.10 - Modo de compatibilidad Crunchyroll

Fecha: `2026-06-09`

Cambios:

- Crunchyroll activa automaticamente un modo de compatibilidad que pausa el bloqueo de recursos para ese sitio.
- El modo de compatibilidad sigue bloqueando ventanas emergentes automáticas.
- GX Light usa el host de la navegación iniciada para evaluar los primeros recursos, incluso cuando WebView2 todavía reporta `about:blank`.
- Los recursos bloqueados y popups bloqueados se cuentan por separado.
- La última URL realmente bloqueada queda visible en la barra de estado y registrada en el log.
- El instalador también se publica como `GXLightBrowser-Setup-x64.exe`, habilitando un enlace permanente a la última versión.

Pruebas:

- Dos pruebas controladas de Crunchyroll permanecieron abiertas durante 25 segundos sin registrar el `HTTP 403` anterior.
- La sesión original fue respaldada y restaurada automáticamente después de cada prueba.

## v1.9 - Instalador con requisitos para Windows y Atlas OS

Fecha: `2026-06-09`

Cambios:

- Nuevo instalador x64 construido con Inno Setup.
- El instalador copia el ejecutable y las tres bibliotecas requeridas por WebView2.
- Se detecta e instala Microsoft Edge WebView2 Evergreen Runtime cuando falta.
- Se detecta e instala Microsoft .NET Framework 4.8 cuando falta.
- GX Light comprueba WebView2 antes de crear pestañas y muestra instrucciones de reparación si no esta disponible.
- Nueva guia `docs/INSTALACION.md` para Windows y Atlas OS.

Notas:

- El instalador usa los bootstrapper oficiales de Microsoft.
- Atlas OS puede impedir la instalación si fueron deshabilitados servicios esenciales de Microsoft.
- La distribución actual requiere Windows 10/11 x64.

## v1.8 - Aislamiento de YouTube Shields y diagnostico de Crunchyroll

Fecha: `2026-06-09`

Cambios:

- YouTube Shields termina inmediatamente fuera de `youtube.com` y `youtu.be`.
- El `MutationObserver` espera a que exista `document.documentElement`, corrigiendo el error mostrado en DevTools.
- Se agrego una prueba Playwright que abre una pagina simulada de Crunchyroll y comprueba que YouTube Shields no se instale ni genere errores.
- La barra de estado identifica cuando Crunchyroll responde `HTTP 403` y aclara que no fue bloqueado por Shields.
- El diagnostico del rechazo queda registrado en el log local.

Notas:

- La prueba con la sesion real confirmo que Crunchyroll esta respondiendo `HTTP 403` desde el servidor.
- GX Light no intentara evadir restricciones de acceso o DRM del servicio.

## v1.7 - Atajos, favicons, Playlist y compatibilidad multimedia

Fecha: `2026-06-09`

Cambios:

- `Ctrl+W` se procesa a nivel del formulario aunque una pagina WebView2 tenga el foco.
- El ejecutable y la ventana incorporan un icono propio de GX Light.
- WebView2 entrega los favicons de cada pagina a la barra de pestanas.
- `Menu > Tab appearance` permite mostrar u ocultar favicons y activar pestanas compactas cuadradas.
- YouTube Shields ya no elimina el contenedor principal de anuncios ni fuerza saltos de tiempo agresivos; intenta mantener y recuperar la reproduccion.
- Las solicitudes multimedia necesarias de YouTube y Crunchyroll tienen excepciones de compatibilidad limitadas al sitio activo.
- Se agrego una Playlist local para guardar, abrir y eliminar paginas multimedia.

Notas:

- La Playlist guarda enlaces; no descarga ni evita contenido protegido por DRM.
- Crunchyroll puede seguir rechazando sesiones por reglas propias del servicio o limitaciones DRM de WebView2.

## v1.6 - Correccion de eliminacion de bookmarks y carpetas importadas

Fecha: `2026-06-08`

Cambios:

- Se corrigio `Eliminar todos` en el gestor de bookmarks.
- Los comandos internos de bookmarks ya no dependen solo de que WebView2 reporte el origen como `data:text/html`.
- Si la pestana interna aparece como `gxlight://home`, los comandos siguen siendo aceptados mientras la pestana activa sea interna.
- La importacion de bookmarks HTML conserva carpetas anidadas como rutas `Padre / Hija`.
- La exportacion HTML agrupa favoritos por carpeta.
- README, CHANGELOG y `update.json` quedan sincronizados con la version actual.

Notas:

- Esta version corrige el caso donde aparecia el dialogo de confirmacion, pero al aceptar no se eliminaban los favoritos.

## v1.5 - Gestion avanzada de favoritos

Fecha: `2026-06-08`

Cambios:

- Se agrego seleccion multiple de bookmarks con checkboxes en el gestor de favoritos.
- Nuevo boton "Seleccionar todos / Deseleccionar todos" en la barra de herramientas.
- Nuevo boton "Eliminar seleccionados" que elimina multiples favoritos con una sola confirmacion.
- Nuevo boton "Eliminar todos" para borrar todos los favoritos de una vez con confirmacion.
- La tecla Suprimir (Delete) elimina los favoritos seleccionados directamente.
- Ctrl+A selecciona o deselecciona todos los favoritos visibles.
- Eliminacion individual ya no requiere confirmacion extra (un solo clic).
- Se agrego checkbox maestro en la cabecera de la tabla.
- Las filas seleccionadas se resaltan con un fondo distinto.
- Se agrego indicador de cuantos favoritos estan seleccionados.
- Se agrego un tip visible sobre las teclas Suprimir y Ctrl+A.

Notas:

- La version 1.4 ya habia incluido importacion con jerarquia de carpetas y remocion de Opera Addons.
- Esta version completa la experiencia de gestion masiva de favoritos.

## v1.4 - Bookmarks con carpetas y mejoras de UI

Fecha: `2026-06-08`

Cambios:

- Se mejoro la importacion de bookmarks HTML conservando la jerarquia original de carpetas.
- La barra de favoritos ahora muestra carpetas como botones desplegables con dropdown.
- Se agrego menu contextual en carpetas de la barra de favoritos.
- Se removio el boton de Opera Addons ya que no era funcional.
- El boton de menu principal ahora usa el icono hamburguesa estandar.

Notas:

- La importacion ahora usa un parser basado en stack para respetar la estructura DL/DT/DD del HTML de bookmarks.

## v1.3 - Correccion de links desde novedades

Fecha: `2026-06-08`

Cambios:

- Se corrigio la apertura de `Ver release` y `Abrir GitHub` desde la pestana de novedades.
- Las paginas internas ahora pueden pedir al host que navegue explicitamente a una URL externa.
- El canal `gxlight:navigate` esta limitado a documentos internos generados por GX Light.
- El navegador sigue leyendo `update.json` desde GitHub al iniciar.
- Este cambio prepara mejor la experiencia para futuros releases descargables.

Notas:

- Esta version corrige el caso donde la barra de direccion cambiaba a GitHub, pero el contenido seguia mostrando la pagina interna `data:text/html`.
- El boton `Ver release` apunta a este historial para evitar caer en una pagina de Releases vacia.

## v1.2 - Manifiesto remoto de actualizaciones

Fecha: `2026-06-08`

Cambios:

- Se agrego `update.json` en GitHub como fuente remota de novedades.
- La pestana `gxlight://updated` puede mostrar novedades editadas desde GitHub.
- Si GitHub no responde, el navegador usa notas locales de respaldo.
- El aviso sigue apareciendo solo una vez por version publicada.
- Se preparo el camino para un actualizador binario futuro.

Limitacion:

- Esta version no reemplaza automaticamente el `.exe`; solo agrega el canal remoto de informacion.

## v1.1 - Favoritos, tab islands y passwords import/export

Fecha: `2026-06-08`

Cambios:

- Se agrego barra de favoritos.
- `Ctrl+D` guarda la pagina actual como favorito.
- Click del scroll en favoritos abre una nueva pestana interna.
- Se agrego importacion/exportacion de bookmarks en HTML compatible con navegadores.
- Se agrego menu contextual de pestanas para seleccionar multiples pestanas.
- Se agrego creacion de tab islands coloreadas desde pestanas seleccionadas.
- Se agrego importacion/exportacion de passwords CSV mediante boveda local protegida con Windows DPAPI.
- Se actualizaron pruebas Playwright para cubrir la barra de favoritos.

Limitacion:

- WebView2 no expone API publica para inyectar passwords importadas directamente al gestor nativo; la boveda local funciona como companera de import/export.

## v1.0 - Base versionada del navegador

Fecha: `2026-06-08`

Cambios:

- Base Windows ligera con WebView2.
- Pestanas, cierre con `x`, click del scroll y atajos basicos.
- Menu principal con historial, descargas, extensiones, passwords, memoria, shields y settings.
- GX Control configurable con RAM limiter, hard limit, hot tabs killer, CPU policy y network policy.
- Suspension real de pestanas para liberar WebView2 y memoria.
- Privacy Firewall local y bloqueo nativo de trackers/anuncios.
- Pestana de novedades que aparece solo una vez por version instalada.
- Pruebas Playwright para UI responsive, tabs y YouTube Shields.

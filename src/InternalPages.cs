using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Microsoft.Web.WebView2.Core;

namespace GXLightBrowser
{
    internal static class InternalPages
    {
        public const string HomeUrl = "gxlight://home";
        public const string UpdatedUrl = "gxlight://updated";
        private const string ChromeStoreUrl = "https://chromewebstore.google.com/category/extensions?pli=1";

        public static string HtmlShell(string title, string body)
        {
            return "<!doctype html><html><head><meta charset='utf-8'><meta name='viewport' content='width=device-width,initial-scale=1'>" +
                "<style>body{margin:0;background:#0d0f14;color:#eef7fa;font-family:Segoe UI,Arial,sans-serif}" +
                "main{padding:28px;max-width:1100px}h1{color:#72f5ff;margin:0 0 18px;font-size:30px}p{color:#c3ced8}" +
                "table{border-collapse:collapse;width:100%;background:#171a22}th,td{border-bottom:1px solid #2e3440;padding:10px;text-align:left;vertical-align:top}" +
                "th{color:#72f5ff;font-size:13px}a{color:#72f5ff}.pill{display:inline-block;background:#252833;border:1px solid #484d5c;padding:6px 9px;margin:3px}</style></head>" +
                "<body><main><h1>" + EscapeHtml(title) + "</h1>" + body + "</main></body></html>";
        }

        public static string EscapeHtml(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
        }

        public static string Trim(string value, int max)
        {
            return value.Length <= max ? value : value.Substring(0, max - 1) + "...";
        }

        public static string SafeJsString(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            return value.Replace("\\", "\\\\").Replace("'", "\\'").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
        }

        public static string HomeHtml(AppSettings settings)
        {
            if (settings == null)
            {
                settings = new AppSettings();
            }

            string searchAction = "https://duckduckgo.com/";
            string searchParamName = "q";
            string searchEngine = settings.DefaultSearchEngine ?? "DuckDuckGo";
            switch (searchEngine.ToLowerInvariant())
            {
                case "google":
                    searchAction = "https://www.google.com/search";
                    searchParamName = "q";
                    break;
                case "bing":
                    searchAction = "https://www.bing.com/search";
                    searchParamName = "q";
                    break;
                case "yahoo":
                    searchAction = "https://search.yahoo.com/search";
                    searchParamName = "p";
                    break;
                default:
                    searchAction = "https://duckduckgo.com/";
                    searchParamName = "q";
                    break;
            }

            string accentHex = Theme.AccentHex;
            System.Drawing.Color acc = Theme.Accent;
            string accentRgb = string.Format("{0},{1},{2}", acc.R, acc.G, acc.B);
            string bgHex = Theme.WindowHex;
            string panelHex = Theme.PanelHex;
            string borderHex = Theme.BorderHex;
            string textHex = Theme.TextHex;
            string mutedHex = Theme.MutedHex;

            bool isLight = string.Equals(settings.ThemeMode, "light", StringComparison.OrdinalIgnoreCase) ||
                           (string.Equals(settings.ThemeMode, "auto", StringComparison.OrdinalIgnoreCase) && Theme.Window.R > 128);
            string inputBgHex = isLight ? "#ffffff" : "#1a1e26";
            string buttonTextHex = (Theme.Accent.R * 0.299f + Theme.Accent.G * 0.587f + Theme.Accent.B * 0.114f > 150f) ? "#0e1117" : "#ffffff";
            
            string wrapBg;
            if (isLight)
            {
                wrapBg = "linear-gradient(135deg, rgba(" + Theme.Accent.R + "," + Theme.Accent.G + "," + Theme.Accent.B + ",0.08), " + bgHex + " 60%, rgba(" + Theme.Accent.R + "," + Theme.Accent.G + "," + Theme.Accent.B + ",0.05))";
            }
            else
            {
                wrapBg = "linear-gradient(135deg, rgba(" + Theme.Accent.R + "," + Theme.Accent.G + "," + Theme.Accent.B + ",0.15), " + bgHex + " 55%, rgba(" + Theme.Accent.R + "," + Theme.Accent.G + "," + Theme.Accent.B + ",0.1))";
            }

            return "<!doctype html><html><head><meta charset='utf-8'><meta name='viewport' content='width=device-width,initial-scale=1'>" +
                "<style>" +
                ":root {" +
                "  --bg: " + bgHex + ";" +
                "  --panel: " + panelHex + ";" +
                "  --text: " + textHex + ";" +
                "  --muted: " + mutedHex + ";" +
                "  --accent: " + accentHex + ";" +
                "  --accent-rgb: " + accentRgb + ";" +
                "  --border: " + borderHex + ";" +
                "  --input-bg: " + inputBgHex + ";" +
                "}" +
                "body{margin:0;background:var(--bg);color:var(--text);font-family:Segoe UI,Arial,sans-serif}" +
                ".wrap{min-height:100vh;display:grid;place-items:center;padding:28px;background:" + wrapBg + "}" +
                ".box{width:min(860px,92vw)}h1{font-size:44px;margin:0 0 10px;color:var(--accent);letter-spacing:0}" +
                "p{color:var(--muted);margin:0 0 24px}.search{display:flex;gap:10px}.search input{flex:1;background:var(--input-bg);border:1px solid var(--border);color:var(--text);padding:15px 16px;font-size:16px;outline:0}" +
                ".search input:focus{border-color:var(--accent);box-shadow:0 0 8px rgba(var(--accent-rgb),0.25)}" +
                ".search button,.link{background:var(--accent);color:" + buttonTextHex + ";border:0;padding:0 18px;font-weight:700;text-decoration:none;display:inline-flex;align-items:center;justify-content:center;min-height:44px}" +
                ".grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(210px,1fr));gap:12px;margin-top:18px}.card{border:1px solid var(--border);background:var(--panel);padding:16px}.card b{display:block;margin-bottom:6px}" +
                "@media(max-width:620px){h1{font-size:34px}.search{flex-direction:column}.search button{padding:13px}}</style></head>" +
                "<body><main class='wrap'><section class='box'><h1>Gan Browser</h1><p>Navegación ligera con bloqueo nativo, extensiones locales y accesos rápidos.</p>" +
                "<form class='search' action='" + searchAction + "' method='get'><input name='" + searchParamName + "' autofocus placeholder='Buscar o escribir una URL en " + searchEngine + "'><button>Buscar</button></form>" +
                "<div class='grid'><article class='card'><b>Chrome Web Store</b><a class='link' href='" + ChromeStoreUrl + "'>Abrir tienda</a></article>" +
                "<article class='card'><b>Gan Guard</b><span>Bloqueador y protección de privacidad integrados en el navegador.</span></article></div>" +
                "</section></main></body></html>";
        }

        public static string UpdateNoticeHtml(UpdateManifest manifest)
        {
            StringBuilder items = new StringBuilder();
            UpdateManifest activeManifest = manifest ?? UpdateManifest.LocalFallback();
            string[] highlights = activeManifest.Highlights ?? VersionInfo.Highlights();
            for (int i = 0; i < highlights.Length; i++)
            {
                items.Append("<li>").Append(EscapeHtml(highlights[i])).Append("</li>");
            }

            string links = string.Empty;
            if (!string.IsNullOrWhiteSpace(activeManifest.DownloadUrl))
            {
                links += "<a class='link' data-prepare-update href='#'>Buscar y preparar actualizacion</a>";
            }
            if (!string.IsNullOrWhiteSpace(activeManifest.SourceUrl))
            {
                links += "<a class='link secondary' data-open-url='" + EscapeHtml(activeManifest.SourceUrl) + "' href='" + EscapeHtml(activeManifest.SourceUrl) + "'>Abrir GitHub</a>";
            }
            string history = ChangelogHtml(activeManifest.ChangelogMarkdown);

            return "<!doctype html><html><head><meta charset='utf-8'><meta name='viewport' content='width=device-width,initial-scale=1'>" +
                "<style>body{margin:0;background:#0d0f14;color:#eef7fa;font-family:Segoe UI,Arial,sans-serif}" +
                "main{min-height:100vh;padding:28px;background:#0d0f14}.content{width:min(920px,94vw);margin:0 auto}" +
                "section{border:1px solid #2e3440;background:#171a22;padding:28px;margin-bottom:18px}" +
                "h1{margin:0 0 8px;color:#72f5ff;font-size:34px;letter-spacing:0}h2{color:#72f5ff;margin:30px 0 10px}h3{color:#eef7fa;margin:22px 0 8px}p{color:#c3ced8;line-height:1.5}" +
                "ul{margin:18px 0 0;padding-left:22px}li{margin:10px 0;color:#eef7fa}.tag{display:inline-block;color:#061116;background:#72f5ff;padding:5px 9px;font-weight:700;margin-bottom:14px}" +
                ".links{display:flex;gap:10px;flex-wrap:wrap;margin-top:22px}.link{background:#72f5ff;color:#061116;text-decoration:none;font-weight:700;padding:10px 13px}.secondary{background:#252833;color:#eef7fa;border:1px solid #484d5c}" +
                ".history{border-left:3px solid #72f5ff}.history ul{margin-top:8px}.history code{background:#252833;padding:2px 5px}</style></head>" +
                "<body><main><div class='content'><section><span class='tag'>Version " + EscapeHtml(activeManifest.Version) + "</span>" +
                "<h1>" + EscapeHtml(activeManifest.ReleaseName) + "</h1>" +
                "<p>Esta pestana aparece automaticamente una vez por version publicada. Puedes volver a abrir la bitacora completa desde el menu.</p>" +
                "<p>Cliente instalado: <b>" + EscapeHtml(VersionInfo.CurrentVersion) + "</b>" +
                (string.IsNullOrWhiteSpace(activeManifest.PublishedAt) ? string.Empty : " - Publicado: <b>" + EscapeHtml(activeManifest.PublishedAt) + "</b>") + "</p>" +
                "<ul>" + items.ToString() + "</ul><div class='links'>" + links + "</div></section>" +
                "<section class='history'><h1>Bitacora de versiones</h1>" + history + "</section></div></main>" +
                "<script>document.querySelectorAll('[data-open-url]').forEach(function(link){link.addEventListener('click',function(event){event.preventDefault();window.chrome.webview.postMessage('gxlight:navigate:'+link.getAttribute('data-open-url'));});});document.querySelectorAll('[data-prepare-update]').forEach(function(link){link.addEventListener('click',function(event){event.preventDefault();window.chrome.webview.postMessage('gxlight:update:prepare');});});</script>" +
                "</body></html>";
        }

        private static string ChangelogHtml(string markdown)
        {
            if (string.IsNullOrWhiteSpace(markdown))
            {
                return "<p>No se pudo descargar la bitacora completa. Usa Abrir GitHub para consultarla.</p>";
            }

            StringBuilder html = new StringBuilder();
            bool listOpen = false;
            string[] lines = markdown.Replace("\r\n", "\n").Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.StartsWith("```", StringComparison.Ordinal))
                {
                    continue;
                }
                if (line.StartsWith("- ", StringComparison.Ordinal))
                {
                    if (!listOpen)
                    {
                        html.Append("<ul>");
                        listOpen = true;
                    }
                    html.Append("<li>").Append(EscapeHtml(line.Substring(2))).Append("</li>");
                    continue;
                }
                if (listOpen)
                {
                    html.Append("</ul>");
                    listOpen = false;
                }
                if (line.StartsWith("## ", StringComparison.Ordinal))
                {
                    html.Append("<h2>").Append(EscapeHtml(line.Substring(3))).Append("</h2>");
                }
                else if (line.StartsWith("### ", StringComparison.Ordinal))
                {
                    html.Append("<h3>").Append(EscapeHtml(line.Substring(4))).Append("</h3>");
                }
                else if (line.Length > 0 && !line.StartsWith("# ", StringComparison.Ordinal))
                {
                    html.Append("<p>").Append(EscapeHtml(line)).Append("</p>");
                }
            }
            if (listOpen)
            {
                html.Append("</ul>");
            }
            return html.ToString();
        }

        public static string HistoryHtml(List<HistoryEntry> history)
        {
            if (history == null || history.Count == 0)
            {
                return "<p>No history in this session yet.</p>";
            }

            StringBuilder body = new StringBuilder();
            body.Append("<table><tr><th>Time</th><th>Title</th><th>URL</th></tr>");
            int count = Math.Min(100, history.Count);
            for (int i = 0; i < count; i++)
            {
                HistoryEntry entry = history[i];
                body.Append("<tr><td>" + entry.VisitedUtc.ToLocalTime().ToString("HH:mm") + "</td><td>" +
                    EscapeHtml(entry.Title) + "</td><td><a href='" + EscapeHtml(entry.Url) + "'>" +
                    EscapeHtml(entry.Url) + "</a></td></tr>");
            }
            body.Append("</table>");
            return body.ToString();
        }

        public static string DownloadsHtml(List<DownloadEntry> downloads)
        {
            if (downloads == null || downloads.Count == 0)
            {
                return "<p>No downloads in this session yet.</p>";
            }

            StringBuilder body = new StringBuilder();
            body.Append("<p>Doble clic sobre una descarga para abrir el archivo disponible.</p>");
            body.Append("<table><tr><th>Time</th><th>File</th><th>Status</th><th>Path</th></tr>");
            for (int i = 0; i < downloads.Count; i++)
            {
                DownloadEntry entry = downloads[i];
                string encodedPath = Convert.ToBase64String(Encoding.UTF8.GetBytes(entry.Path ?? string.Empty));
                body.Append("<tr style='cursor:pointer' ondblclick=\"chrome.webview.postMessage('gxlight:download:open:" + encodedPath + "')\"><td>" +
                    entry.StartedUtc.ToLocalTime().ToString("HH:mm") + "</td><td>" +
                    EscapeHtml(entry.FileName) + "</td><td>" + EscapeHtml(entry.State) + "</td><td>" +
                    EscapeHtml(entry.Path) + "</td></tr>");
            }
            body.Append("</table>");
            return body.ToString();
        }

        public static string BookmarksJson(List<BookmarkEntry> bookmarks)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("[");
            for (int i = 0; i < bookmarks.Count; i++)
            {
                BookmarkEntry entry = bookmarks[i];
                if (i > 0) sb.Append(",");
                sb.Append("{")
                  .Append("\"title\":\"").Append(SafeJsString(entry.Title)).Append("\",")
                  .Append("\"url\":\"").Append(SafeJsString(entry.Url)).Append("\",")
                  .Append("\"folder\":\"").Append(SafeJsString(entry.Folder)).Append("\",")
                  .Append("\"created\":").Append((long)(entry.CreatedUtc - new DateTime(1970, 1, 1)).TotalMilliseconds)
                  .Append("}");
            }
            sb.Append("]");
            return sb.ToString();
        }

        public static string BookmarksHtml(List<BookmarkEntry> bookmarks)
        {
            string json = BookmarksJson(bookmarks);
            StringBuilder h = new StringBuilder();
            h.Append("<!doctype html><html><head><meta charset='utf-8'>");
            h.Append("<meta name='viewport' content='width=device-width,initial-scale=1'>");
            h.Append("<title>Bookmarks Manager</title>");
            h.Append("<style>");
            h.Append("*{box-sizing:border-box}");
            h.Append("body{margin:0;background:#0d0f14;color:#eef7fa;font-family:'Segoe UI',Arial,sans-serif;display:flex;min-height:100vh}");
            h.Append("aside{width:240px;background:#13171f;border-right:1px solid #222936;padding:20px;display:flex;flex-direction:column;gap:15px}");
            h.Append("aside h2{color:#72f5ff;font-size:16px;text-transform:uppercase;letter-spacing:1px;margin:0 0 10px;display:flex;align-items:center;gap:8px}");
            h.Append(".folder-list{list-style:none;padding:0;margin:0;display:flex;flex-direction:column;gap:5px}");
            h.Append(".folder-item{padding:8px 12px;cursor:pointer;border-radius:4px;transition:background .2s,color .2s;display:flex;align-items:center;gap:8px;color:#aeb8c4;font-size:14px;overflow:hidden;text-overflow:ellipsis;white-space:nowrap}");
            h.Append(".folder-item:hover,.folder-item.active{background:#1c2331;color:#72f5ff}");
            h.Append(".btn-new-folder{background:transparent;border:1px dashed #484d5c;color:#aeb8c4;padding:10px;border-radius:4px;cursor:pointer;text-align:center;font-size:13px;transition:border-color .2s,color .2s;margin-top:10px}");
            h.Append(".btn-new-folder:hover{border-color:#72f5ff;color:#72f5ff}");
            h.Append("main{flex:1;padding:30px;display:flex;flex-direction:column;gap:16px;outline:none}");
            h.Append(".header-bar{display:flex;justify-content:space-between;align-items:center;gap:16px;flex-wrap:wrap}");
            h.Append("h1{color:#72f5ff;font-size:28px;margin:0}");
            h.Append(".toolbar{display:flex;align-items:center;gap:10px;flex-wrap:wrap}");
            h.Append(".search-box{background:#171a22;border:1px solid #2e3440;border-radius:4px;padding:8px 14px;width:280px;display:flex;align-items:center}");
            h.Append(".search-box input{background:transparent;border:none;color:#fff;font-size:14px;outline:none;width:100%}");
            h.Append(".btn-toolbar{background:#1c2331;border:1px solid #2e3440;color:#aeb8c4;padding:7px 14px;border-radius:4px;cursor:pointer;font-size:13px;transition:all .2s;white-space:nowrap}");
            h.Append(".btn-toolbar:hover{border-color:#72f5ff;color:#72f5ff}");
            h.Append(".btn-toolbar.danger{border-color:rgba(255,107,107,.2);color:#ff6b6b}");
            h.Append(".btn-toolbar.danger:hover{background:rgba(255,107,107,.13);border-color:#ff6b6b}");
            h.Append(".btn-toolbar:disabled{opacity:.35;cursor:not-allowed}");
            h.Append(".selection-info{color:#72f5ff;font-size:13px;font-weight:600;min-width:80px}");
            h.Append(".table-container{background:#13171f;border:1px solid #222936;border-radius:6px;overflow:auto;flex:1}");
            h.Append("table{border-collapse:collapse;width:100%;text-align:left}");
            h.Append("th,td{padding:10px 14px;border-bottom:1px solid #222936;font-size:13px}");
            h.Append("th{background:#1c2331;color:#72f5ff;font-weight:600;text-transform:uppercase;font-size:11px;letter-spacing:.5px;position:sticky;top:0;z-index:1}");
            h.Append("tr:last-child td{border-bottom:none}");
            h.Append("tr:hover td{background:#171c26}");
            h.Append("tr.selected td{background:#1a2a35}");
            h.Append("a{color:#72f5ff;text-decoration:none;word-break:break-all}");
            h.Append("a:hover{text-decoration:underline}");
            h.Append(".actions{display:flex;align-items:center;gap:8px}");
            h.Append(".btn-delete{background:transparent;border:none;color:#ff6b6b;cursor:pointer;padding:4px 8px;border-radius:4px;transition:background .2s;font-size:13px}");
            h.Append(".btn-delete:hover{background:rgba(255,107,107,.15)}");
            h.Append("select{background:#171a22;border:1px solid #2e3440;color:#eef7fa;padding:4px 8px;border-radius:4px;outline:none;cursor:pointer;font-size:12px}");
            h.Append("select:hover{border-color:#72f5ff}");
            h.Append("input[type=checkbox]{accent-color:#72f5ff;width:16px;height:16px;cursor:pointer}");
            h.Append(".hint{color:#555e6e;font-size:12px;margin-top:4px}");
            h.Append("</style></head><body>");

            // Sidebar
            h.Append("<aside>");
            h.Append("<h2>\ud83d\udcc1 Carpetas</h2>");
            h.Append("<ul class='folder-list' id='folderList'></ul>");
            h.Append("<button class='btn-new-folder' onclick='createNewFolder()'>+ Nueva Carpeta</button>");
            h.Append("</aside>");

            // Main area
            h.Append("<main id='mainArea' tabindex='0'>");
            h.Append("<div class='header-bar'><h1 id='pageTitle'>Todos los favoritos</h1></div>");
            h.Append("<div class='toolbar'>");
            h.Append("<div class='search-box'><input type='text' id='searchInput' placeholder='Buscar favoritos...' oninput='renderBookmarks()'></div>");
            h.Append("<button class='btn-toolbar' onclick='toggleSelectAll()' id='btnSelectAll'>Seleccionar todos</button>");
            h.Append("<button class='btn-toolbar danger' onclick='deleteSelected()' id='btnDeleteSelected' disabled>Eliminar seleccionados</button>");
            h.Append("<button class='btn-toolbar danger' onclick='deleteAll()' id='btnDeleteAll'>Eliminar todos</button>");
            h.Append("<span class='selection-info' id='selectionInfo'></span>");
            h.Append("</div>");
            h.Append("<div class='table-container'><table><thead><tr>");
            h.Append("<th style='width:40px'><input type='checkbox' id='headerCheckbox' onchange='toggleSelectAll()'></th>");
            h.Append("<th>Titulo</th><th>URL</th><th>Carpeta</th><th>Acciones</th>");
            h.Append("</tr></thead><tbody id='bookmarksTableBody'></tbody></table></div>");
            h.Append("<div class='hint'>Tip: selecciona varios favoritos con los checkboxes y presiona <b>Suprimir</b> (Delete) para eliminarlos. <b>Ctrl+A</b> selecciona todos.</div>");
            h.Append("</main>");

            // JavaScript
            h.Append("<script>");
            h.Append("var bookmarks=").Append(json).Append(";");
            h.Append("var activeFilter='all';");
            h.Append("var selected=new Set();");
            h.Append("for(var _i=0;_i<bookmarks.length;_i++){bookmarks[_i].originalIndex=_i;}");

            // Helpers
            h.Append("function b64(s){try{return btoa(unescape(encodeURIComponent(s||'')));}catch(e){return '';}}");
            h.Append("function escapeHtml(s){if(!s)return '';var d=document.createElement('div');d.appendChild(document.createTextNode(s));return d.innerHTML;}");

            // getFolders
            h.Append("function getFolders(){var f=[];for(var i=0;i<bookmarks.length;i++){var b=bookmarks[i];if(b.folder&&b.folder.trim()!==''&&f.indexOf(b.folder)===-1)f.push(b.folder);}return f;}");

            // getVisibleIndices
            h.Append("function getVisibleIndices(){var s=document.getElementById('searchInput').value.toLowerCase();var r=[];");
            h.Append("for(var i=0;i<bookmarks.length;i++){var b=bookmarks[i];");
            h.Append("if(activeFilter==='all'){if(!b.url||b.url.trim()==='')continue;}else{if(b.folder!==activeFilter)continue;}");
            h.Append("if(s&&!((b.title&&b.title.toLowerCase().indexOf(s)>=0)||(b.url&&b.url.toLowerCase().indexOf(s)>=0)))continue;");
            h.Append("r.push(b.originalIndex);}return r;}");

            // updateSelectionUI
            h.Append("function updateSelectionUI(){var c=selected.size;");
            h.Append("document.getElementById('selectionInfo').innerText=c>0?c+' seleccionado'+(c>1?'s':''):'';");
            h.Append("document.getElementById('btnDeleteSelected').disabled=c===0;");
            h.Append("var vis=getVisibleIndices();var all=vis.length>0;");
            h.Append("for(var i=0;i<vis.length;i++){if(!selected.has(vis[i])){all=false;break;}}");
            h.Append("document.getElementById('headerCheckbox').checked=all&&vis.length>0;");
            h.Append("document.getElementById('btnSelectAll').innerText=(all&&vis.length>0)?'Deseleccionar todos':'Seleccionar todos';}");

            // toggleCheck
            h.Append("function toggleCheck(idx){if(selected.has(idx))selected.delete(idx);else selected.add(idx);");
            h.Append("var row=document.querySelector('tr[data-idx=\"'+idx+'\"]');if(row)row.className=selected.has(idx)?'selected':'';");
            h.Append("updateSelectionUI();}");

            // toggleSelectAll
            h.Append("function toggleSelectAll(){var vis=getVisibleIndices();var all=true;");
            h.Append("for(var i=0;i<vis.length;i++){if(!selected.has(vis[i])){all=false;break;}}");
            h.Append("for(var i=0;i<vis.length;i++){if(all)selected.delete(vis[i]);else selected.add(vis[i]);}");
            h.Append("renderBookmarks();updateSelectionUI();}");

            // deleteSelected
            h.Append("function deleteSelected(){if(selected.size===0)return;");
            h.Append("if(!confirm('\\u00bfEliminar '+selected.size+' favorito'+(selected.size>1?'s':'')+'?'))return;");
            h.Append("var payload='';selected.forEach(function(idx){var b=bookmarks[idx];");
            h.Append("if(payload.length>0)payload+=';';payload+=b64(b.title)+'|'+b64(b.url)+'|'+b64(b.folder);});");
            h.Append("selected.clear();window.chrome.webview.postMessage('gxlight:bookmarks:delete-batch:'+payload);}");

            // deleteAll
            h.Append("function deleteAll(){if(bookmarks.length===0)return;");
            h.Append("if(!confirm('\\u00bfSeguro que quieres ELIMINAR TODOS los favoritos? Esta accion no se puede deshacer.'))return;");
            h.Append("selected.clear();window.chrome.webview.postMessage('gxlight:bookmarks:delete-all');}");

            // renderFolders
            h.Append("function renderFolders(){var el=document.getElementById('folderList');var folders=getFolders();var out='';");
            h.Append("out+='<li class=\"folder-item '+(activeFilter==='all'?'active':'')+'\" onclick=\"filterFolder(this)\" data-folder=\"all\">\\ud83d\\udcc2 Todos</li>';");
            h.Append("out+='<li class=\"folder-item '+(activeFilter==='Favorites bar'?'active':'')+'\" onclick=\"filterFolder(this)\" data-folder=\"Favorites bar\">\\u2b50 Barra</li>';");
            h.Append("for(var i=0;i<folders.length;i++){var f=folders[i];if(f==='Favorites bar')continue;");
            h.Append("out+='<li class=\"folder-item '+(activeFilter===f?'active':'')+'\" onclick=\"filterFolder(this)\" data-folder=\"'+escapeHtml(f)+'\">\\ud83d\\udcc1 '+escapeHtml(f)+'</li>';}");
            h.Append("el.innerHTML=out;}");

            // filterFolder (uses data-folder attribute, no inline escaping needed)
            h.Append("function filterFolder(el){activeFilter=el.getAttribute('data-folder');");
            h.Append("document.getElementById('pageTitle').innerText=activeFilter==='all'?'Todos los favoritos':activeFilter;");
            h.Append("renderFolders();renderBookmarks();updateSelectionUI();}");

            // renderBookmarks
            h.Append("function renderBookmarks(){var tbody=document.getElementById('bookmarksTableBody');var folders=getFolders();");
            h.Append("if(folders.indexOf('Favorites bar')===-1)folders.push('Favorites bar');");
            h.Append("var vis=getVisibleIndices();var out='';");
            h.Append("if(vis.length===0){out='<tr><td colspan=\"5\" style=\"text-align:center;color:#aeb8c4;padding:30px\">No hay favoritos en esta seccion.</td></tr>';}");
            h.Append("else{for(var vi=0;vi<vis.length;vi++){var idx=vis[vi];var b=bookmarks[idx];");
            h.Append("var noUrl=!b.url||b.url.trim()==='';");
            h.Append("var urlCell=noUrl?'<i style=\"color:#555e6e\">(Carpeta vacia)</i>':'<a href=\"#\" onclick=\"navigate('+idx+');return false\">'+escapeHtml(b.url)+'</a>';");
            h.Append("var opts='';for(var fi=0;fi<folders.length;fi++){var f=folders[fi];opts+='<option value=\"'+escapeHtml(f)+'\" '+(b.folder===f?'selected':'')+'>'+escapeHtml(f)+'</option>';}");
            h.Append("var chk=selected.has(idx);");
            h.Append("out+='<tr data-idx=\"'+idx+'\" class=\"'+(chk?'selected':'')+'\">';");
            h.Append("out+='<td><input type=\"checkbox\" '+(chk?'checked':'')+' onchange=\"toggleCheck('+idx+')\"></td>';");
            h.Append("out+='<td style=\"font-weight:500\">'+escapeHtml(b.title||'Sin titulo')+'</td>';");
            h.Append("out+='<td>'+urlCell+'</td>';");
            h.Append("out+='<td><select onchange=\"moveBookmark('+idx+',this.value)\">'+opts+'</select></td>';");
            h.Append("out+='<td><div class=\"actions\"><button class=\"btn-delete\" onclick=\"deleteSingle('+idx+')\">\\u2715</button></div></td>';");
            h.Append("out+='</tr>';");
            h.Append("}}");
            h.Append("tbody.innerHTML=out;updateSelectionUI();}");

            // navigate
            h.Append("function navigate(idx){window.chrome.webview.postMessage('gxlight:navigate:'+bookmarks[idx].url);}");

            // createNewFolder
            h.Append("function createNewFolder(){var n=prompt('Nombre de la nueva carpeta:');if(n&&n.trim()!=='')");
            h.Append("window.chrome.webview.postMessage('gxlight:bookmarks:create-folder:'+b64(n.trim()));}");

            // deleteSingle (no confirmation for single delete – one click)
            h.Append("function deleteSingle(idx){var b=bookmarks[idx];");
            h.Append("window.chrome.webview.postMessage('gxlight:bookmarks:delete:'+b64(b.title)+'|'+b64(b.url)+'|'+b64(b.folder));}");

            // moveBookmark
            h.Append("function moveBookmark(idx,nf){var b=bookmarks[idx];");
            h.Append("window.chrome.webview.postMessage('gxlight:bookmarks:move:'+b64(b.title)+'|'+b64(b.url)+'|'+b64(b.folder)+'|'+b64(nf));}");

            // Keyboard shortcuts
            h.Append("document.getElementById('mainArea').addEventListener('keydown',function(e){");
            h.Append("if(e.key==='Delete'||e.keyCode===46){e.preventDefault();deleteSelected();}");
            h.Append("if(e.key==='a'&&(e.ctrlKey||e.metaKey)){e.preventDefault();toggleSelectAll();}");
            h.Append("});");

            // Init
            h.Append("renderFolders();renderBookmarks();document.getElementById('mainArea').focus();");
            h.Append("</script></body></html>");
            return h.ToString();
        }

        public static string PasswordsHtml(bool passwordSavingEnabled, List<PasswordVaultEntry> passwordVault)
        {
            StringBuilder body = new StringBuilder();
            body.Append("<p>Preguntar antes de guardar passwords: <b>")
                .Append(passwordSavingEnabled ? "activado" : "desactivado")
                .Append("</b>.</p>");
            body.Append("<p>Gan Browser nunca guarda una credencial al escribirla. WebView2 muestra su aviso nativo despues de iniciar sesion y solo la conserva cuando eliges guardar.</p>");
            body.Append("<p>Las credenciales nativas quedan cifradas por Windows dentro del perfil persistente. La boveda de importacion/exportacion tambien usa Windows DPAPI para el usuario actual.</p>");
            body.Append("<p>Entradas importadas en la boveda: <b>").Append(passwordVault.Count).Append("</b>. Usa Menu &gt; Passwords and autofill para importar o exportar CSV.</p>");

            if (passwordVault.Count == 0)
            {
                return body.ToString();
            }

            body.Append("<table><tr><th>Name</th><th>URL</th><th>Username</th><th>Imported</th></tr>");
            for (int i = 0; i < passwordVault.Count; i++)
            {
                PasswordVaultEntry entry = passwordVault[i];
                body.Append("<tr><td>").Append(EscapeHtml(entry.Name)).Append("</td><td>")
                    .Append(EscapeHtml(entry.Url)).Append("</td><td>")
                    .Append(EscapeHtml(entry.Username)).Append("</td><td>")
                    .Append(entry.ImportedUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm")).Append("</td></tr>");
            }
            body.Append("</table>");
            return body.ToString();
        }

        public static string PlaylistHtml(List<PlaylistEntry> playlist)
        {
            StringBuilder body = new StringBuilder();
            body.Append("<p>Guarda videos y paginas multimedia para volver a abrirlos rapidamente. La Playlist no copia ni evita contenido protegido por DRM.</p>");
            if (playlist.Count == 0)
            {
                body.Append("<p>No hay elementos guardados. Usa Menu &gt; Playlist &gt; Add current page.</p>");
                return body.ToString();
            }

            body.Append("<table><tr><th>Titulo</th><th>Agregado</th><th>Acciones</th></tr>");
            for (int i = 0; i < playlist.Count; i++)
            {
                PlaylistEntry item = playlist[i];
                string encodedUrl = Convert.ToBase64String(Encoding.UTF8.GetBytes(item.Url ?? string.Empty));
                body.Append("<tr><td><b>").Append(EscapeHtml(item.Title)).Append("</b><br><span>")
                    .Append(EscapeHtml(item.Url)).Append("</span></td><td>")
                    .Append(item.AddedUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm")).Append("</td><td>")
                    .Append("<button onclick=\"chrome.webview.postMessage('gxlight:playlist:open:").Append(encodedUrl).Append("')\">Abrir</button> ")
                    .Append("<button onclick=\"chrome.webview.postMessage('gxlight:playlist:delete:").Append(encodedUrl).Append("')\">Eliminar</button>")
                    .Append("</td></tr>");
            }
            body.Append("</table>");
            return body.ToString();
        }

        public static string GetMemoryProcessesHtml(CoreWebView2Environment environment)
        {
            if (environment == null)
            {
                return "<p>No active WebView2 environment.</p>";
            }

            StringBuilder sb = new StringBuilder();
            sb.Append("<h3>Active WebView2 Processes</h3>");
            sb.Append("<table><tr><th>Process ID</th><th>Type</th><th>Memory (Working Set)</th></tr>");

            try
            {
                var infos = environment.GetProcessInfos();
                if (infos != null)
                {
                    long total = 0;
                    foreach (var info in infos)
                    {
                        string typeStr = info.Kind.ToString();
                        string memStr = "Unknown";
                        try
                        {
                            using (Process p = Process.GetProcessById((int)info.ProcessId))
                            {
                                long memMb = p.WorkingSet64 / (1024 * 1024);
                                memStr = memMb + " MB";
                                total += memMb;
                            }
                        }
                        catch
                        {
                            memStr = "Access Denied / Exited";
                        }
                        sb.Append("<tr><td>" + info.ProcessId + "</td><td>" + typeStr + "</td><td>" + memStr + "</td></tr>");
                    }
                    sb.Append("<tr style='font-weight:bold;color:#72f5ff;'><td>Total Environment</td><td>-</td><td>" + total + " MB</td></tr>");
                }
            }
            catch (Exception ex)
            {
                sb.Append("<tr><td colspan='3'>Error retrieving processes: " + EscapeHtml(ex.Message) + "</td></tr>");
            }
            sb.Append("</table>");
            sb.Append("<div style='margin-top:20px;'><button onclick='location.href=\"gxlight://free-memory\"' style='background:#72f5ff;color:#061116;border:0;padding:10px 20px;font-weight:700;cursor:pointer;'>Liberar memoria ahora</button></div>");

            return sb.ToString();
        }

        public static string SettingsHtml(AppSettings settings)
        {
            if (settings == null)
            {
                settings = new AppSettings();
            }

            string accentHex = Theme.AccentHex;
            System.Drawing.Color acc = Theme.Accent;
            string accentRgb = string.Format("{0},{1},{2}", acc.R, acc.G, acc.B);
            string bgHex = Theme.WindowHex;
            string panelHex = Theme.PanelHex;
            string sidebarHex = "#" + Theme.Sidebar.R.ToString("X2") + Theme.Sidebar.G.ToString("X2") + Theme.Sidebar.B.ToString("X2");
            string textHex = Theme.TextHex;
            string mutedHex = Theme.MutedHex;
            string borderHex = Theme.BorderHex;

            bool isLight = string.Equals(settings.ThemeMode, "light", StringComparison.OrdinalIgnoreCase) ||
                           (string.Equals(settings.ThemeMode, "auto", StringComparison.OrdinalIgnoreCase) && Theme.Window.R > 128);
            string inputBgHex = isLight ? "#ffffff" : "#1a1e26";
            string themeCardBgHex = isLight ? "#e5e7eb" : "#13161d";

            StringBuilder html = new StringBuilder();
            html.Append("<!doctype html><html><head><meta charset='utf-8'><title>Settings</title>");
            html.Append("<style>");
            html.Append(":root {");
            html.Append("  --bg-dark: " + bgHex + ";");
            html.Append("  --bg-panel: " + panelHex + ";");
            html.Append("  --bg-sidebar: " + sidebarHex + ";");
            html.Append("  --text: " + textHex + ";");
            html.Append("  --text-muted: " + mutedHex + ";");
            html.Append("  --accent: " + accentHex + ";");
            html.Append("  --accent-rgb: " + accentRgb + ";");
            html.Append("  --border: " + borderHex + ";");
            html.Append("  --input-bg: " + inputBgHex + ";");
            html.Append("  --theme-card-bg: " + themeCardBgHex + ";");
            html.Append("}");
            html.Append("* { box-sizing: border-box; }");
            html.Append("body { margin: 0; background: var(--bg-dark); color: var(--text); font-family: 'Segoe UI', Arial, sans-serif; overflow: hidden; }");
            html.Append(".container { display: flex; height: 100vh; }");
            html.Append(".sidebar { width: 250px; background: var(--bg-sidebar); border-right: 1px solid var(--border); padding: 20px 0; overflow-y: auto; }");
            html.Append(".sidebar-item { padding: 12px 24px; color: var(--text-muted); cursor: pointer; font-size: 13px; font-weight: 600; text-transform: uppercase; letter-spacing: 0.5px; transition: color 0.2s, border-left 0.2s; border-left: 3px solid transparent; }");
            html.Append(".sidebar-item:hover { color: var(--text); }");
            html.Append(".sidebar-item.active { color: var(--accent); border-left: 3px solid var(--accent); background: rgba(var(--accent-rgb), 0.05); }");
            html.Append(".content { flex: 1; padding: 40px; overflow-y: auto; scroll-behavior: smooth; }");
            html.Append("h1 { font-size: 28px; font-weight: 700; margin: 0 0 20px; color: var(--text); letter-spacing: 0.5px; }");
            html.Append("h2 { font-size: 18px; font-weight: 600; margin: 30px 0 15px; color: var(--accent); border-bottom: 1px solid var(--border); padding-bottom: 8px; }");
            html.Append(".card { background: var(--bg-panel); border: 1px solid var(--border); border-radius: 4px; padding: 20px; margin-bottom: 16px; transition: border-color 0.2s; }");
            html.Append(".card:hover { border-color: rgba(var(--accent-rgb), 0.3); }");
            html.Append(".setting-row { display: flex; align-items: center; justify-content: space-between; padding: 12px 0; border-bottom: 1px solid rgba(255,255,255,0.05); }");
            html.Append(".setting-row:last-child { border-bottom: 0; }");
            html.Append(".setting-info { flex: 1; padding-right: 20px; }");
            html.Append(".setting-title { font-size: 14px; font-weight: 600; color: var(--text); }");
            html.Append(".setting-desc { font-size: 12px; color: var(--text-muted); margin-top: 4px; }");
            html.Append(".switch { position: relative; display: inline-block; width: 44px; height: 22px; }");
            html.Append(".switch input { opacity: 0; width: 0; height: 0; }");
            html.Append(".slider { position: absolute; cursor: pointer; top: 0; left: 0; right: 0; bottom: 0; background-color: var(--input-bg); transition: .3s; border-radius: 22px; border: 1px solid var(--border); }");
            html.Append(".slider:before { position: absolute; content: ''; height: 14px; width: 14px; left: 3px; bottom: 3px; background-color: var(--text-muted); transition: .3s; border-radius: 50%; }");
            html.Append("input:checked + .slider { background-color: rgba(var(--accent-rgb), 0.2); border-color: var(--accent); }");
            html.Append("input:checked + .slider:before { transform: translateX(22px); background-color: var(--accent); box-shadow: 0 0 8px var(--accent); }");
            html.Append("select, input[type='text'], input[type='number'] { background: var(--input-bg); color: var(--text); border: 1px solid var(--border); padding: 8px 12px; border-radius: 4px; outline: none; font-size: 13px; font-family: inherit; }");
            html.Append("select:focus, input[type='text']:focus, input[type='number']:focus { border-color: var(--accent); box-shadow: 0 0 6px rgba(var(--accent-rgb), 0.2); }");
            html.Append(".theme-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(130px, 1fr)); gap: 12px; margin-top: 15px; }");
            html.Append(".theme-card { background: var(--theme-card-bg); border: 1px solid var(--border); border-radius: 4px; padding: 12px; text-align: center; cursor: pointer; transition: all 0.2s; position: relative; }");
            html.Append(".theme-card:hover { border-color: var(--accent); transform: translateY(-2px); }");
            html.Append(".theme-card.active { border-color: var(--accent); background: rgba(var(--accent-rgb), 0.08); box-shadow: 0 0 10px rgba(var(--accent-rgb), 0.15); }");
            html.Append(".theme-preview { height: 40px; border-radius: 3px; margin-bottom: 8px; position: relative; display: flex; align-items: center; justify-content: center; }");
            html.Append(".theme-preview span { font-size: 10px; font-weight: bold; padding: 2px 6px; border-radius: 20px; background: rgba(0,0,0,0.4); color: #fff; }");
            html.Append(".theme-name { font-size: 12px; font-weight: 600; color: var(--text); }");
            html.Append(".btn { background: var(--accent); color: #061116; border: 0; padding: 10px 20px; font-weight: 700; border-radius: 4px; cursor: pointer; font-size: 13px; transition: opacity 0.2s; }");
            html.Append(".btn:hover { opacity: 0.9; }");
            html.Append(".btn-secondary { background: var(--input-bg); color: var(--text); border: 1px solid var(--border); margin-left: 8px; }");
            html.Append(".btn-secondary:hover { opacity: 0.8; }");
            html.Append("</style></head>");

            html.Append("<body><div class='container'>");

            // Sidebar
            html.Append("<div class='sidebar'>");
            html.Append("<div class='sidebar-item active' onclick='scrollToSec(\"apariencia\")'>Apariencia</div>");
            html.Append("<div class='sidebar-item' onclick='scrollToSec(\"limitadores\")'>Gan Pulse</div>");
            html.Append("<div class='sidebar-item' onclick='scrollToSec(\"privacidad\")'>Privacidad y Seguridad</div>");
            html.Append("<div class='sidebar-item' onclick='scrollToSec(\"inicio\")'>Al Iniciar</div>");
            html.Append("<div class='sidebar-item' onclick='scrollToSec(\"descargas\")'>Descargas</div>");
            html.Append("<div class='sidebar-item' onclick='scrollToSec(\"sistema\")'>Sistema y WebRTC</div>");
            html.Append("</div>");

            // Content area
            html.Append("<div class='content'>");
            html.Append("<h1>Configuración</h1>");

            // Section 1: Apariencia
            html.Append("<section id='apariencia'>");
            html.Append("<h2>Apariencia</h2>");

            // Interface Theme Selector Card
            html.Append("<div class='card'>");
            html.Append("<div class='setting-row'>");
            html.Append("  <div class='setting-info'>");
            html.Append("    <div class='setting-title'>Tema de Interfaz</div>");
            html.Append("    <div class='setting-desc'>Selecciona si deseas una apariencia Clara (Light), Oscura (Dark), o que se adapte automáticamente al sistema (Auto).</div>");
            html.Append("  </div>");
            html.Append("  <select onchange='changeThemeMode(this.value)'>");
            string[] modes = { "Dark", "Light", "Auto" };
            for (int i = 0; i < modes.Length; i++)
            {
                bool sel = string.Equals(settings.ThemeMode, modes[i], StringComparison.OrdinalIgnoreCase);
                html.Append("    <option value='" + modes[i] + "' " + (sel ? "selected" : "") + ">" + modes[i] + "</option>");
            }
            html.Append("  </select>");
            html.Append("</div>");
            html.Append("</div>");

            // Theme grid
            html.Append("<div class='card'>");
            html.Append("<div class='setting-title'>Temas del Navegador</div>");
            html.Append("<div class='setting-desc'>Elige un tema de acento propio de Gan Browser.</div>");
            html.Append("<div class='theme-grid'>");

            string[] themes = { "Crimson", "Aurora", "Glacier", "Coral", "Orchid", "Neon Rose", "Blush", "Terminal", "Amber", "Mint", "Gold", "Silver" };
            string[] colors = { "#FA114F", "#7A3CFF", "#00BFF3", "#FF6347", "#CC3399", "#FF69B4", "#F7CAC9", "#00FF41", "#FDB813", "#00994C", "#FFD700", "#D3D3D3" };

            for (int i = 0; i < themes.Length; i++)
            {
                bool active = string.Equals(settings.SelectedTheme, themes[i], StringComparison.OrdinalIgnoreCase);
                html.Append("<div class='theme-card" + (active ? " active" : "") + "' onclick='changeTheme(\"" + themes[i] + "\")'>");
                html.Append("  <div class='theme-preview' style='background:" + colors[i] + ";'><span>" + themes[i][0] + "</span></div>");
                html.Append("  <div class='theme-name'>" + themes[i] + "</div>");
                html.Append("</div>");
            }
            html.Append("</div></div>");

            // Checkboxes / Toggles
            html.Append("<div class='card'>");

            // Show Bookmarks Bar
            html.Append("<div class='setting-row'>");
            html.Append("  <div class='setting-info'>");
            html.Append("    <div class='setting-title'>Mostrar barra de marcadores</div>");
            html.Append("    <div class='setting-desc'>Muestra la barra de marcadores rápidos debajo de la barra de direcciones.</div>");
            html.Append("  </div>");
            html.Append("  <label class='switch'>");
            html.Append("    <input type='checkbox' " + (settings.ShowBookmarksBar ? "checked" : "") + " onchange='toggleSetting(\"ShowBookmarksBar\", this.checked)'>");
            html.Append("    <span class='slider'></span>");
            html.Append("  </label>");
            html.Append("</div>");

            // Show Page Icons
            html.Append("<div class='setting-row'>");
            html.Append("  <div class='setting-info'>");
            html.Append("    <div class='setting-title'>Mostrar iconos en las pestañas</div>");
            html.Append("    <div class='setting-desc'>Carga y dibuja los favicons de las páginas web en cada pestaña.</div>");
            html.Append("  </div>");
            html.Append("  <label class='switch'>");
            html.Append("    <input type='checkbox' " + (settings.ShowPageIcons ? "checked" : "") + " onchange='toggleSetting(\"ShowPageIcons\", this.checked)'>");
            html.Append("    <span class='slider'></span>");
            html.Append("  </label>");
            html.Append("</div>");

            // Compact Icon Tabs
            html.Append("<div class='setting-row'>");
            html.Append("  <div class='setting-info'>");
            html.Append("    <div class='setting-title'>Pestañas compactas con iconos</div>");
            html.Append("    <div class='setting-desc'>Reduce las pestañas inactivas a mostrar solo el icono para ahorrar espacio.</div>");
            html.Append("  </div>");
            html.Append("  <label class='switch'>");
            html.Append("    <input type='checkbox' " + (settings.CompactIconTabs ? "checked" : "") + " onchange='toggleSetting(\"CompactIconTabs\", this.checked)'>");
            html.Append("    <span class='slider'></span>");
            html.Append("  </label>");
            html.Append("</div>");

            html.Append("</div>");

            // Search engine card
            html.Append("<div class='card'>");
            html.Append("<div class='setting-row'>");
            html.Append("  <div class='setting-info'>");
            html.Append("    <div class='setting-title'>Buscador predeterminado</div>");
            html.Append("    <div class='setting-desc'>Selecciona el motor de búsqueda que se utilizará al escribir en la barra de direcciones o en la página de inicio.</div>");
            html.Append("  </div>");
            html.Append("  <select onchange='selectSetting(\"DefaultSearchEngine\", this.value)'>");
            string[] engines = { "Google", "DuckDuckGo", "Bing", "Yahoo" };
            for (int i = 0; i < engines.Length; i++)
            {
                bool sel = string.Equals(settings.DefaultSearchEngine, engines[i], StringComparison.OrdinalIgnoreCase);
                html.Append("    <option value='" + engines[i] + "' " + (sel ? "selected" : "") + ">" + engines[i] + "</option>");
            }
            html.Append("  </select>");
            html.Append("</div>");
            html.Append("</div>");

            html.Append("</section>");

            // Section 2: Gan Pulse
            html.Append("<section id='limitadores'>");
            html.Append("<h2>Gan Pulse</h2>");
            html.Append("<div class='card'>");

            // RAM Limiter
            html.Append("<div class='setting-row'>");
            html.Append("  <div class='setting-info'>");
            html.Append("    <div class='setting-title'>Limitador de RAM</div>");
            html.Append("    <div class='setting-desc'>Controla cuánta memoria RAM puede consumir el navegador en segundo plano.</div>");
            html.Append("  </div>");
            html.Append("  <label class='switch'>");
            html.Append("    <input type='checkbox' " + (settings.RamLimiterEnabled ? "checked" : "") + " onchange='toggleSetting(\"RamLimiterEnabled\", this.checked)'>");
            html.Append("    <span class='slider'></span>");
            html.Append("  </label>");
            html.Append("</div>");

            // Hard Limit
            html.Append("<div class='setting-row'>");
            html.Append("  <div class='setting-info'>");
            html.Append("    <div class='setting-title'>Límite estricto de RAM</div>");
            html.Append("    <div class='setting-desc'>Suspende pestañas agresivamente en cuanto se alcanza el límite establecido.</div>");
            html.Append("  </div>");
            html.Append("  <label class='switch'>");
            html.Append("    <input type='checkbox' " + (settings.HardMemoryLimit ? "checked" : "") + " onchange='toggleSetting(\"HardMemoryLimit\", this.checked)'>");
            html.Append("    <span class='slider'></span>");
            html.Append("  </label>");
            html.Append("</div>");

            // Low Resources Mode
            html.Append("<div class='setting-row'>");
            html.Append("  <div class='setting-info'>");
            html.Append("    <div class='setting-title'>Modo de bajos recursos</div>");
            html.Append("    <div class='setting-desc'>Mantiene activas únicamente una cantidad máxima de pestañas para ahorrar recursos.</div>");
            html.Append("  </div>");
            html.Append("  <label class='switch'>");
            html.Append("    <input type='checkbox' " + (settings.LowResourcesModeEnabled ? "checked" : "") + " onchange='toggleSetting(\"LowResourcesModeEnabled\", this.checked)'>");
            html.Append("    <span class='slider'></span>");
            html.Append("  </label>");
            html.Append("</div>");

            // Max Active Tabs
            html.Append("<div class='setting-row'>");
            html.Append("  <div class='setting-info'>");
            html.Append("    <div class='setting-title'>Pestañas activas máximas</div>");
            html.Append("    <div class='setting-desc'>Límite de pestañas abiertas antes de que comience la suspensión automática.</div>");
            html.Append("  </div>");
            html.Append("  <input type='number' min='2' max='50' value='" + settings.MaxActiveTabs + "' onchange='selectSetting(\"MaxActiveTabs\", this.value)'>");
            html.Append("</div>");

            html.Append("</div></section>");

            // Section 3: Privacidad
            html.Append("<section id='privacidad'>");
            html.Append("<h2>Privacidad y Seguridad</h2>");
            html.Append("<div class='card'>");

            // Adblocker
            html.Append("<div class='setting-row'>");
            html.Append("  <div class='setting-info'>");
            html.Append("    <div class='setting-title'>Bloquear anuncios (AdBlock)</div>");
            html.Append("    <div class='setting-desc'>Bloquea anuncios publicitarios no deseados y comerciales en páginas web.</div>");
            html.Append("  </div>");
            html.Append("  <label class='switch'>");
            html.Append("    <input type='checkbox' " + (settings.AdBlockEnabled ? "checked" : "") + " onchange='toggleSetting(\"AdBlockEnabled\", this.checked)'>");
            html.Append("    <span class='slider'></span>");
            html.Append("  </label>");
            html.Append("</div>");

            // Tracker blocker
            html.Append("<div class='setting-row'>");
            html.Append("  <div class='setting-info'>");
            html.Append("    <div class='setting-title'>Bloquear rastreadores (Privacy Firewall)</div>");
            html.Append("    <div class='setting-desc'>Bloquea scripts de telemetría y rastreo de comportamiento de terceros.</div>");
            html.Append("  </div>");
            html.Append("  <label class='switch'>");
            html.Append("    <input type='checkbox' " + (settings.PrivacyFirewallEnabled ? "checked" : "") + " onchange='toggleSetting(\"PrivacyFirewallEnabled\", this.checked)'>");
            html.Append("    <span class='slider'></span>");
            html.Append("  </label>");
            html.Append("</div>");

            // Auto-save passwords
            html.Append("<div class='setting-row'>");
            html.Append("  <div class='setting-info'>");
            html.Append("    <div class='setting-title'>Preguntar para guardar contraseñas</div>");
            html.Append("    <div class='setting-desc'>Permite almacenar credenciales de inicio de sesión de forma segura y local.</div>");
            html.Append("  </div>");
            html.Append("  <label class='switch'>");
            html.Append("    <input type='checkbox' " + (settings.PasswordSavingEnabled ? "checked" : "") + " onchange='toggleSetting(\"PasswordSavingEnabled\", this.checked)'>");
            html.Append("    <span class='slider'></span>");
            html.Append("  </label>");
            html.Append("</div>");

            html.Append("</div></section>");

            // Section 4: Al Iniciar
            html.Append("<section id='inicio'>");
            html.Append("<h2>Al Iniciar</h2>");
            html.Append("<div class='card'>");

            // Restore previous session
            html.Append("<div class='setting-row'>");
            html.Append("  <div class='setting-info'>");
            html.Append("    <div class='setting-title'>Restaurar sesión anterior al abrir</div>");
            html.Append("    <div class='setting-desc'>Vuelve a abrir las pestañas activas de la última sesión al iniciar.</div>");
            html.Append("  </div>");
            html.Append("  <label class='switch'>");
            html.Append("    <input type='checkbox' " + (settings.RestorePreviousSession ? "checked" : "") + " onchange='toggleSetting(\"RestorePreviousSession\", this.checked)'>");
            html.Append("    <span class='slider'></span>");
            html.Append("  </label>");
            html.Append("</div>");

            html.Append("</div></section>");

            // Section 5: Descargas
            html.Append("<section id='descargas'>");
            html.Append("<h2>Descargas</h2>");
            html.Append("<div class='card'>");

            // Path selector
            html.Append("<div class='setting-row'>");
            html.Append("  <div class='setting-info'>");
            html.Append("    <div class='setting-title'>Carpeta de descargas</div>");
            html.Append("    <div class='setting-desc'>Directorio donde se guardarán los archivos descargados: <b id='dlPathText'>" + (string.IsNullOrWhiteSpace(settings.CustomDownloadsFolder) ? "Carpeta Descargas de Windows" : EscapeHtml(settings.CustomDownloadsFolder)) + "</b></div>");
            html.Append("  </div>");
            html.Append("  <button class='btn btn-secondary' onclick='changeDownloadsFolder()'>Cambiar</button>");
            html.Append("</div>");

            // Ask where to save
            html.Append("<div class='setting-row'>");
            html.Append("  <div class='setting-info'>");
            html.Append("    <div class='setting-title'>Preguntar dónde guardar cada descarga</div>");
            html.Append("    <div class='setting-desc'>Muestra el cuadro de diálogo para confirmar la ubicación del archivo antes de descargar.</div>");
            html.Append("  </div>");
            html.Append("  <label class='switch'>");
            html.Append("    <input type='checkbox' " + (settings.AskSavePathBeforeDownload ? "checked" : "") + " onchange='toggleSetting(\"AskSavePathBeforeDownload\", this.checked)'>");
            html.Append("    <span class='slider'></span>");
            html.Append("  </label>");
            html.Append("</div>");

            html.Append("</div></section>");

            // Section 6: Sistema y WebRTC
            html.Append("<section id='sistema'>");
            html.Append("<h2>Sistema y WebRTC</h2>");
            html.Append("<div class='card'>");

            // Auto Check Updates Toggle
            html.Append("<div class='setting-row'>");
            html.Append("  <div class='setting-info'>");
            html.Append("    <div class='setting-title'>Buscar actualizaciones automáticamente al iniciar</div>");
            html.Append("    <div class='setting-desc'>Busca nuevas versiones al arrancar el navegador de forma silenciosa e informa solo si hay una disponible.</div>");
            html.Append("  </div>");
            html.Append("  <label class='switch'>");
            html.Append("    <input type='checkbox' " + (settings.AutoCheckUpdates ? "checked" : "") + " onchange='toggleSetting(\"AutoCheckUpdates\", this.checked)'>");
            html.Append("    <span class='slider'></span>");
            html.Append("  </label>");
            html.Append("</div>");

            // Reset Settings
            html.Append("<div class='setting-row'>");
            html.Append("  <div class='setting-info'>");
            html.Append("    <div class='setting-title'>Restablecer valores predeterminados</div>");
            html.Append("    <div class='setting-desc'>Restaura todas las configuraciones a sus valores iniciales recomendados.</div>");
            html.Append("  </div>");
            html.Append("  <button class='btn btn-secondary' style='border-color:#ff6974;color:#ff6974;' onclick='resetSettings()'>Restablecer</button>");
            html.Append("</div>");

            html.Append("</div></section>");

            html.Append("</div></div>"); // Close content, container

            // JavaScript handlers
            html.Append("<script>");
            html.Append("function scrollToSec(id) {");
            html.Append("  const target = document.getElementById(id);");
            html.Append("  if(target) { target.scrollIntoView({behavior:'smooth'}); }");
            html.Append("  document.querySelectorAll('.sidebar-item').forEach(item => item.classList.remove('active'));");
            html.Append("  event.currentTarget.classList.add('active');");
            html.Append("}");
            html.Append("function toggleSetting(key, checked) {");
            html.Append("  window.chrome.webview.postMessage('gxlight:settings:toggle:' + key + ':' + checked);");
            html.Append("}");
            html.Append("function selectSetting(key, val) {");
            html.Append("  window.chrome.webview.postMessage('gxlight:settings:select:' + key + ':' + val);");
            html.Append("}");
            html.Append("function changeTheme(themeName) {");
            html.Append("  window.chrome.webview.postMessage('gxlight:settings:theme:' + themeName);");
            html.Append("  document.querySelectorAll('.theme-card').forEach(card => {");
            html.Append("    if(card.querySelector('.theme-name').innerText.trim() === themeName) {");
            html.Append("      card.classList.add('active');");
            html.Append("    } else { card.classList.remove('active'); }");
            html.Append("  });");
            html.Append("}");
            html.Append("function changeThemeMode(mode) {");
            html.Append("  window.chrome.webview.postMessage('gxlight:settings:theme-mode:' + mode);");
            html.Append("}");
            html.Append("function changeDownloadsFolder() {");
            html.Append("  window.chrome.webview.postMessage('gxlight:settings:downloads:change');");
            html.Append("}");
            html.Append("function resetSettings() {");
            html.Append("  if (confirm('¿Estás seguro de que deseas restablecer todos los ajustes a los valores iniciales?')) {");
            html.Append("    window.chrome.webview.postMessage('gxlight:settings:reset');");
            html.Append("  }");
            html.Append("}");
            html.Append("window.chrome.webview.addEventListener('message', event => {");
            html.Append("  if (event.data.startsWith('gxlight:settings:downloads:updated:')) {");
            html.Append("    const path = event.data.substring('gxlight:settings:downloads:updated:'.length);");
            html.Append("    document.getElementById('dlPathText').innerText = path || 'Carpeta Descargas de Windows';");
            html.Append("  }");
            html.Append("});");
            html.Append("</script></body></html>");

            return html.ToString();
        }
    }
}

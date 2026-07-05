using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GXLightBrowser
{
    public sealed class BrowserForm : Form, IMessageFilter
    {
        private const string HomeUrl = "gxlight://home";
        private const string UpdatedUrl = "gxlight://updated";
        private const string ChromeStoreUrl = "https://chromewebstore.google.com/category/extensions?pli=1";

        private readonly AdBlocker _adBlocker = new AdBlocker();
        private readonly PrivacyFirewall _privacyFirewall = new PrivacyFirewall();
        private readonly ExtensionImporter _extensionImporter = new ExtensionImporter();
        private readonly BorderlessTabControl _tabs = new BorderlessTabControl();
        private readonly FlowLayoutPanel _tabStrip = new FlowLayoutPanel();
        private readonly FlowLayoutPanel _bookmarksBar = new FlowLayoutPanel();
        private readonly TextBox _address = new TextBox();
        private readonly Label _status = new Label();
        private readonly ToolTip _tips = new ToolTip();

        private readonly ChromeButton _back = new ChromeButton();
        private readonly ChromeButton _forward = new ChromeButton();
        private readonly ChromeButton _reload = new ChromeButton();
        private readonly ChromeButton _newTab = new ChromeButton();
        private readonly ChromeButton _shield = new ChromeButton();
        private readonly ChromeButton _extensions = new ChromeButton();
        private readonly ChromeButton _chromeStore = new ChromeButton();
        private readonly ChromeButton _tabStripNewTab = new ChromeButton();
        private readonly ChromeButton _menuButton = new ChromeButton();
        private readonly ChromeButton _memoryLimitButton = new ChromeButton();
        private readonly Label _memoryLabel = new Label();
        private readonly System.Windows.Forms.Timer _memoryTimer = new System.Windows.Forms.Timer();
        private readonly List<HistoryEntry> _history = new List<HistoryEntry>();
        private readonly List<DownloadEntry> _downloads = new List<DownloadEntry>();
        private readonly List<BookmarkEntry> _bookmarks = new List<BookmarkEntry>();
        private readonly List<PasswordVaultEntry> _passwordVault = new List<PasswordVaultEntry>();
        private readonly List<PlaylistEntry> _playlist = new List<PlaylistEntry>();
        private readonly Dictionary<int, Color> _islandColors = new Dictionary<int, Color>();
        private readonly HashSet<int> _collapsedIslands = new HashSet<int>();
        private readonly GxControlSettings _gxControl = new GxControlSettings();
        private AppSettings _appSettings = new AppSettings();
        private UpdateManifest _updateManifest = UpdateManifest.LocalFallback();

        private CoreWebView2Environment _environment;
        private TableLayoutPanel _rootLayout;
        private TableLayoutPanel _topLayout;
        private bool _adBlockEnabled = true;
        private bool _privacyFirewallEnabled = true;
        private bool _passwordSavingEnabled = true;
        private int _nextIslandId = 1;
        private int _activeIslandId;
        private DateTime _startedUtc = DateTime.UtcNow;
        private bool _restoringSession;
        private bool _cleaningUp;
        private int _lastClickedTabIndex = -1;
        private bool _fullScreenActive;
        private FormBorderStyle _previousBorderStyle;
        private FormWindowState _previousWindowState;
        private Rectangle _previousBounds;
        private bool _previousTopMost;
        private string _preparedUpdateInstallerPath;
        private UpdateManifest _preparedUpdateManifest;
        private CancellationTokenSource _updateCts;
        private bool _updateDownloading;
        private bool _updateAvailable;
        private static readonly HttpClient _httpClient;

        static BrowserForm()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromMinutes(30);
            _httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd("GanBrowser/" + VersionInfo.CurrentVersion);
        }

        public BrowserForm()
        {
            SetStyle(
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.Opaque |
                ControlStyles.UserPaint |
                ControlStyles.ResizeRedraw, true);
            UpdateStyles();
            Text = "Gan Browser";
            MinimumSize = new Size(760, 540);
            Size = new Size(1280, 780);
            BackColor = Theme.Window;
            ForeColor = Theme.Text;
            Font = new Font("Segoe UI", 9f);
            DoubleBuffered = true;
            KeyPreview = true;
            Application.AddMessageFilter(this);
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }

            BuildLayout();
            SizeChanged += delegate { ApplyResponsiveMode(); };
            FormClosing += BrowserFormClosing;
            FormClosed += delegate
            {
                Application.RemoveMessageFilter(this);
                DisposeWebViewsForShutdown();
            };
            Load += async delegate
            {
                NativeChrome.ApplyDarkFrame(this);
                await InitializeAsync();
            };
        }

        private void BuildLayout()
        {
            _rootLayout = new TableLayoutPanel();
            _rootLayout.Dock = DockStyle.Fill;
            _rootLayout.BackColor = Theme.Window;
            _rootLayout.ColumnCount = 2;
            _rootLayout.RowCount = 3;
            _rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 46));
            _rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            _rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 82));
            _rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            _rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
            Controls.Add(_rootLayout);

            Panel side = new Panel();
            side.Dock = DockStyle.Fill;
            side.Padding = new Padding(5, 12, 5, 5);
            side.BackColor = Theme.Sidebar;
            _rootLayout.Controls.Add(side, 0, 0);
            _rootLayout.SetRowSpan(side, 3);

            AddSideButton(side, "YT", "YouTube", "https://www.youtube.com/");
            AddSideButton(side, "TW", "Twitch", "https://www.twitch.tv/");
            AddSideButton(side, "DC", "Discord", "https://discord.com/app");
            AddSideButton(side, "GH", "GitHub", "https://github.com/");
            AddSideButton(side, "CWS", "Chrome Web Store", ChromeStoreUrl);

            _topLayout = new TableLayoutPanel();
            _topLayout.Dock = DockStyle.Fill;
            _topLayout.BackColor = Theme.Topbar;
            _topLayout.Padding = new Padding(6, 4, 8, 4);
            _topLayout.RowCount = 3;
            _topLayout.ColumnCount = 1;
            _topLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
            _topLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            _topLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            _rootLayout.Controls.Add(_topLayout, 1, 0);

            _tabStrip.Dock = DockStyle.Fill;
            _tabStrip.WrapContents = false;
            _tabStrip.AutoScroll = false;
            _tabStrip.BackColor = Theme.Topbar;
            _topLayout.Controls.Add(_tabStrip, 0, 0);

            TableLayoutPanel nav = new TableLayoutPanel();
            nav.Dock = DockStyle.Fill;
            nav.ColumnCount = 3;
            nav.RowCount = 1;
            nav.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220));
            nav.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            nav.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 350));
            _topLayout.Controls.Add(nav, 0, 1);

            FlowLayoutPanel leftNav = new FlowLayoutPanel();
            leftNav.Dock = DockStyle.Fill;
            leftNav.WrapContents = false;
            leftNav.BackColor = Theme.Topbar;
            nav.Controls.Add(leftNav, 0, 0);

            ConfigureButton(_back, "<", 38, "Volver");
            ConfigureButton(_forward, ">", 38, "Avanzar");
            ConfigureButton(_reload, "Reload", 66, "Recargar pagina");
            ConfigureButton(_newTab, "+", 38, "Nueva pestana");
            leftNav.Controls.AddRange(new Control[] { _back, _forward, _reload, _newTab });

            Panel addressShell = new Panel();
            addressShell.Dock = DockStyle.Fill;
            addressShell.Padding = new Padding(10, 4, 10, 0);
            addressShell.Margin = new Padding(6, 1, 8, 1);
            addressShell.BackColor = Theme.Address;
            addressShell.Paint += PaintAddressShell;
            addressShell.Resize += delegate { addressShell.Invalidate(); };
            nav.Controls.Add(addressShell, 1, 0);

            _address.BorderStyle = BorderStyle.None;
            _address.Dock = DockStyle.Fill;
            _address.BackColor = Theme.Address;
            _address.ForeColor = Theme.Text;
            _address.Font = new Font("Segoe UI", 10f);
            addressShell.Controls.Add(_address);

            FlowLayoutPanel actions = new FlowLayoutPanel();
            actions.Dock = DockStyle.Fill;
            actions.FlowDirection = FlowDirection.RightToLeft;
            actions.WrapContents = false;
            actions.BackColor = Theme.Topbar;
            nav.Controls.Add(actions, 2, 0);

            _memoryLabel.Width = 92;
            _memoryLabel.Height = 24;
            _memoryLabel.Margin = new Padding(2, 1, 4, 1);
            _memoryLabel.TextAlign = ContentAlignment.MiddleCenter;
            _memoryLabel.ForeColor = Theme.Text;
            _memoryLabel.BackColor = Theme.Panel;
            _memoryLabel.Font = new Font("Segoe UI", 8.25f, FontStyle.Bold);
            ConfigureButton(_memoryLimitButton, "Limit 768", 88, "Limitador de memoria");
            ConfigureButton(_shield, "Block Ads On", 96, "Activar o desactivar el bloqueador de anuncios");
            ConfigureButton(_menuButton, "☰", 38, "Menu principal");
            _menuButton.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
            actions.Controls.AddRange(new Control[] { _menuButton, _memoryLimitButton, _shield, _memoryLabel });

            _bookmarksBar.Dock = DockStyle.Fill;
            _bookmarksBar.WrapContents = false;
            _bookmarksBar.AutoScroll = false;
            _bookmarksBar.BackColor = Theme.Topbar;
            _bookmarksBar.Padding = new Padding(0, 2, 0, 0);
            _topLayout.Controls.Add(_bookmarksBar, 0, 2);

            _tabs.Dock = DockStyle.Fill;
            _tabs.Appearance = TabAppearance.FlatButtons;
            _tabs.SizeMode = TabSizeMode.Fixed;
            _tabs.ItemSize = new Size(0, 1);
            _tabs.Padding = new Point(0, 0);
            _tabs.Margin = new Padding(0);
            _tabs.BackColor = Theme.Window;
            _rootLayout.Controls.Add(_tabs, 1, 1);

            _status.Dock = DockStyle.Fill;
            _status.Padding = new Padding(12, 3, 12, 0);
            _status.BackColor = Theme.Topbar;
            _status.ForeColor = Theme.Muted;
            _status.Font = new Font("Segoe UI", 8.25f);
            _rootLayout.Controls.Add(_status, 1, 2);

            WireEvents();
            ApplyResponsiveMode();
            RebuildTabStrip();
        }

        private void WireEvents()
        {
            _back.Click += delegate
            {
                WebView2 web = ActiveWebView();
                if (web != null && web.CanGoBack)
                {
                    web.GoBack();
                }
            };

            _forward.Click += delegate
            {
                WebView2 web = ActiveWebView();
                if (web != null && web.CanGoForward)
                {
                    web.GoForward();
                }
            };

            _reload.Click += delegate
            {
                WebView2 web = ActiveWebView();
                if (web != null)
                {
                    web.Reload();
                }
            };

            _newTab.Click += async delegate { await CreateTabAsync(HomeUrl); };
            _tabStripNewTab.Click += async delegate { await CreateTabAsync(HomeUrl); };
            _shield.Click += delegate
            {
                _adBlockEnabled = !_adBlockEnabled;
                _appSettings.AdBlockEnabled = _adBlockEnabled;
                _appSettings.Save();
                SetYouTubeShieldsEnabled(_adBlockEnabled);
                UpdateStatus();
            };
            _extensions.Click += async delegate { await ShowExtensionMenuAsync(); };
            _menuButton.Click += delegate { ShowMainMenu(); };
            _memoryLimitButton.Click += delegate
            {
                ShowGxControl();
            };

            _address.KeyDown += delegate(object sender, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    NavigateAddress();
                }
            };
            _address.MouseDown += delegate(object sender, MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Left && e.Clicks >= 3)
                {
                    BeginInvoke((MethodInvoker)delegate { _address.SelectAll(); });
                }
            };

            KeyDown += async delegate(object sender, KeyEventArgs e)
            {
                if (e.Control && e.KeyCode == Keys.T)
                {
                    e.SuppressKeyPress = true;
                    await CreateTabAsync(HomeUrl);
                }

                if (e.Control && e.KeyCode == Keys.N)
                {
                    e.SuppressKeyPress = true;
                    if (e.Shift)
                    {
                        MessageBox.Show(this, "Modo privado aun no esta implementado.", "Gan Browser");
                    }
                    else
                    {
                        StartNewWindow();
                    }
                }

                if (e.Control && e.KeyCode == Keys.W)
                {
                    e.SuppressKeyPress = true;
                    CloseTab(_tabs.SelectedTab);
                }

                if (e.Control && e.KeyCode == Keys.J)
                {
                    e.SuppressKeyPress = true;
                    NavigateInternal("downloads");
                }

                if (e.Control && e.KeyCode == Keys.H)
                {
                    e.SuppressKeyPress = true;
                    NavigateInternal("history");
                }

                if (e.Control && e.KeyCode == Keys.D)
                {
                    e.SuppressKeyPress = true;
                    AddCurrentBookmark();
                }

                if (e.Alt && e.KeyCode == Keys.T)
                {
                    e.SuppressKeyPress = true;
                    _activeIslandId = _nextIslandId++;
                    _islandColors[_activeIslandId] = ColorFromText(GetTabUrl(ActiveTab()));
                    await CreateTabAsync(HomeUrl);
                    _activeIslandId = 0;
                }

                if (e.Control && e.KeyCode == Keys.L)
                {
                    e.SuppressKeyPress = true;
                    _address.Focus();
                    _address.SelectAll();
                }

                if (e.Control && e.KeyCode == Keys.F)
                {
                    e.SuppressKeyPress = true;
                    ExecuteFind();
                }

                if (e.Alt && e.KeyCode == Keys.P)
                {
                    e.SuppressKeyPress = true;
                    NavigateInternal("settings");
                }

                if (e.KeyCode == Keys.F12)
                {
                    e.SuppressKeyPress = true;
                    WebView2 web = ActiveWebView();
                    if (web != null && web.CoreWebView2 != null)
                    {
                        web.CoreWebView2.OpenDevToolsWindow();
                    }
                }
            };

            _tabs.SelectedIndexChanged += async delegate
            {
                BrowserTab selected = ActiveTab();
                if (selected != null)
                {
                    selected.LastActiveUtc = DateTime.UtcNow;
                    if (selected.IsSuspended)
                    {
                        await RestoreSuspendedTabAsync(selected);
                    }
                }
                SyncAddress();
                RebuildTabStrip();
                ApplyTabResourcePolicy();
                UpdateStatus();
                SaveSession();
            };

            _memoryTimer.Interval = 5000;
            _memoryTimer.Tick += delegate
            {
                UpdateMemoryMonitor();
                EnforceMemoryLimit();
                EnforceLowResourceLimit();
                SuspendIdleTabs();
            };
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (HandleBrowserShortcut(keyData))
            {
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr GetParent(IntPtr hWnd);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern short GetKeyState(int nVirtKey);

        private bool IsActiveWindow()
        {
            IntPtr foreground = GetForegroundWindow();
            if (foreground == IntPtr.Zero) return false;
            if (foreground == this.Handle) return true;

            IntPtr parent = foreground;
            while (parent != IntPtr.Zero)
            {
                if (parent == this.Handle) return true;
                parent = GetParent(parent);
            }
            return false;
        }

        private static Keys GetModifierKeys()
        {
            Keys modifiers = Keys.None;
            if (GetKeyState(0x11) < 0) modifiers |= Keys.Control;
            if (GetKeyState(0x10) < 0) modifiers |= Keys.Shift;
            if (GetKeyState(0x12) < 0) modifiers |= Keys.Alt;
            return modifiers;
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_ERASEBKGND = 0x0014;
            if (m.Msg == WM_ERASEBKGND)
            {
                using (Graphics g = Graphics.FromHdc(m.WParam))
                using (Brush b = new SolidBrush(Theme.Window))
                {
                    g.FillRectangle(b, ClientRectangle);
                }
                m.Result = (System.IntPtr)1;
                return;
            }
            base.WndProc(ref m);
        }

        public bool PreFilterMessage(ref Message message)
        {
            const int WmKeyDown = 0x0100;
            const int WmSysKeyDown = 0x0104;
            if ((message.Msg != WmKeyDown && message.Msg != WmSysKeyDown) || !IsActiveWindow())
            {
                return false;
            }

            Keys keyData = (Keys)((int)message.WParam) | GetModifierKeys();
            if (!IsBrowserShortcut(keyData))
            {
                return false;
            }

            BeginInvoke((MethodInvoker)delegate { HandleBrowserShortcut(keyData); });
            return true;
        }

        private bool HandleBrowserShortcut(Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.N))
            {
                StartNewWindow();
                return true;
            }
            if (keyData == (Keys.Control | Keys.Shift | Keys.N))
            {
                MessageBox.Show(this, "Modo privado aun no esta implementado.", "Gan Browser");
                return true;
            }
            if (keyData == (Keys.Control | Keys.W))
            {
                CloseTab(_tabs.SelectedTab);
                return true;
            }
            if (keyData == (Keys.Control | Keys.T))
            {
                Task ignored = CreateTabAsync(HomeUrl);
                return true;
            }
            if (keyData == (Keys.Alt | Keys.T))
            {
                Task ignored = CreateNewIslandTabAsync();
                return true;
            }
            if (keyData == (Keys.Control | Keys.L))
            {
                _address.Focus();
                _address.SelectAll();
                return true;
            }
            if (keyData == (Keys.Control | Keys.J))
            {
                NavigateInternal("downloads");
                return true;
            }
            if (keyData == (Keys.Control | Keys.H))
            {
                NavigateInternal("history");
                return true;
            }
            if (keyData == (Keys.Control | Keys.D))
            {
                AddCurrentBookmark();
                return true;
            }
            if (keyData == (Keys.Control | Keys.Shift | Keys.L))
            {
                Task ignored = FillCurrentSiteFromVaultAsync();
                return true;
            }
            if (keyData == (Keys.Control | Keys.F))
            {
                ExecuteFind();
                return true;
            }
            if (keyData == (Keys.Control | Keys.R))
            {
                WebView2 web = ActiveWebView();
                if (web != null) web.Reload();
                return true;
            }
            if (keyData == (Keys.Alt | Keys.P))
            {
                NavigateInternal("settings");
                return true;
            }
            if (keyData == Keys.F12)
            {
                WebView2 web = ActiveWebView();
                if (web != null && web.CoreWebView2 != null) web.CoreWebView2.OpenDevToolsWindow();
                return true;
            }
            return false;
        }

        private static bool IsBrowserShortcut(Keys keyData)
        {
            return keyData == (Keys.Control | Keys.T) ||
                keyData == (Keys.Control | Keys.W) ||
                keyData == (Keys.Control | Keys.L) ||
                keyData == (Keys.Control | Keys.J) ||
                keyData == (Keys.Control | Keys.H) ||
                keyData == (Keys.Control | Keys.D) ||
                keyData == (Keys.Control | Keys.Shift | Keys.L) ||
                keyData == (Keys.Control | Keys.F) ||
                keyData == (Keys.Control | Keys.R) ||
                keyData == (Keys.Control | Keys.N) ||
                keyData == (Keys.Control | Keys.Shift | Keys.N) ||
                keyData == (Keys.Alt | Keys.T) ||
                keyData == (Keys.Alt | Keys.P) ||
                keyData == Keys.F12;
        }

        private async Task CreateNewIslandTabAsync()
        {
            _activeIslandId = _nextIslandId++;
            _islandColors[_activeIslandId] = ColorFromText(GetTabUrl(ActiveTab()));
            await CreateTabAsync(HomeUrl);
            _activeIslandId = 0;
        }

        private async Task InitializeAsync()
        {
            AppPaths.Ensure();
            string webView2Version;
            if (!RuntimePrerequisites.TryGetWebView2Version(out webView2Version))
            {
                Logger.Error("WebView2 Runtime is missing or unavailable.");
                MessageBox.Show(this, RuntimePrerequisites.MissingWebView2Message(), "Falta WebView2 Runtime",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
                return;
            }
            Logger.Info("Using WebView2 Runtime " + webView2Version);
            _appSettings = AppSettings.Load();
            Theme.ApplyTheme(_appSettings.SelectedTheme, _appSettings.ThemeMode);
            ApplyThemeToControls();
            ApplyLayoutDimensions();

            // Cargar preferencias del usuario
            _adBlockEnabled = _appSettings.AdBlockEnabled;
            _privacyFirewallEnabled = _appSettings.PrivacyFirewallEnabled;
            _passwordSavingEnabled = _appSettings.PasswordSavingEnabled;
            _gxControl.RamLimiterEnabled = _appSettings.RamLimiterEnabled;
            _gxControl.MemoryLimitMb = _appSettings.MemoryLimitMb;
            _gxControl.HardMemoryLimit = _appSettings.HardMemoryLimit;
            _gxControl.HotTabsKillerEnabled = _appSettings.HotTabsKillerEnabled;
            _gxControl.HotTabsMode = _appSettings.HotTabsMode;
            _gxControl.CpuLimiterEnabled = _appSettings.CpuLimiterEnabled;
            _gxControl.CpuLimitPercent = _appSettings.CpuLimitPercent;
            _gxControl.NetworkLimiterEnabled = _appSettings.NetworkLimiterEnabled;
            _gxControl.NetworkProfile = _appSettings.NetworkProfile;
            _gxControl.LowResourcesModeEnabled = _appSettings.LowResourcesModeEnabled;
            _gxControl.MaxActiveTabs = _appSettings.MaxActiveTabs;

            LoadBookmarks();
            LoadPasswordVault();
            LoadPlaylist();
            RebuildBookmarksBar();
            EnsureDefaultFilters();
            _adBlocker.Load(AppPaths.Filters);

            CoreWebView2EnvironmentOptions options = new CoreWebView2EnvironmentOptions();
            options.AreBrowserExtensionsEnabled = true;
            options.AdditionalBrowserArguments = "--disable-features=HeavyAdPrivacyMitigations,Heartbeat --disable-telemetry --disable-breakpad --no-report-upload --telemetry-disable";
            try
            {
                _environment = await CoreWebView2Environment.CreateAsync(null, AppPaths.Profile, options);
            }
            catch (Exception ex)
            {
                Logger.Error("Could not initialize WebView2 Runtime: " + ex.Message);
                MessageBox.Show(this, RuntimePrerequisites.MissingWebView2Message() + "\n\nDetalle: " + ex.Message,
                    "No se pudo iniciar WebView2", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
                return;
            }

            // Restaurar sesión previa si existe
            SessionData session = _appSettings.RestorePreviousSession ? SessionManager.LoadSession() : null;
            if (session != null && session.Tabs.Count > 0)
            {
                _restoringSession = true;
                Logger.Info("Restoring session with " + session.Tabs.Count + " tabs.");
                if (session.X >= 0 && session.Y >= 0)
                {
                    Location = new Point(session.X, session.Y);
                    Size = new Size(session.Width, session.Height);
                }
                if (session.Maximized)
                {
                    WindowState = FormWindowState.Maximized;
                }

                foreach (System.Collections.Generic.KeyValuePair<int, Color> kv in session.IslandColors)
                {
                    _islandColors[kv.Key] = kv.Value;
                }
                foreach (int islandId in session.CollapsedIslands)
                {
                    if (islandId > 0) _collapsedIslands.Add(islandId);
                }

                int maxIslandId = 0;
                foreach (var tab in session.Tabs)
                {
                    if (tab.IslandId > maxIslandId) maxIslandId = tab.IslandId;
                }
                _nextIslandId = maxIslandId + 1;

                for (int i = 0; i < session.Tabs.Count; i++)
                {
                    var tabData = session.Tabs[i];
                    if (i != session.ActiveIndex)
                    {
                        BrowserTab restored = CreateSuspendedTab(tabData.Url, tabData.Title, tabData.IslandId);
                        restored.IsCompact = tabData.IsCompact;
                    }
                    else
                    {
                        await CreateTabAsync(tabData.Url);
                        BrowserTab lastTab = _tabs.TabPages[_tabs.TabPages.Count - 1].Tag as BrowserTab;
                        if (lastTab != null)
                        {
                            lastTab.IslandId = tabData.IslandId;
                            lastTab.IsCompact = tabData.IsCompact;
                        }
                    }
                }

                int targetIndex = Math.Min(session.ActiveIndex, _tabs.TabPages.Count - 1);
                _tabs.SelectedIndex = Math.Max(0, targetIndex);
                _restoringSession = false;
                SaveSession();
            }
            else
            {
                await CreateTabAsync(HomeUrl);
            }

            await ShowUpdateNoticeIfNeededAsync();
            _memoryTimer.Start();
            UpdateMemoryMonitor();
            UpdateStatus();
        }

        private async Task ShowUpdateNoticeIfNeededAsync()
        {
            _updateManifest = await UpdateManifest.LoadLatestAsync();
            Version installedVersion;
            Version remoteVersion;
            if (_appSettings.AutoCheckUpdates &&
                Version.TryParse(VersionInfo.CurrentVersion, out installedVersion) &&
                Version.TryParse(_updateManifest.Version, out remoteVersion) &&
                remoteVersion > installedVersion)
            {
                // Auto-descarga silenciosa en segundo plano sin molestar al usuario
                BeginInvoke((MethodInvoker)(async delegate
                {
                    await PrepareUpdateSilentAsync(_updateManifest);
                }));
                return;
            }
            if (string.Equals(_appSettings.LastSeenVersion, _updateManifest.Version, StringComparison.Ordinal))
            {
                return;
            }

            await CreateTabAsync(UpdatedUrl);
            _appSettings.LastSeenVersion = _updateManifest.Version;
            _appSettings.Save();
        }

        private async Task<BrowserTab> CreateTabAsync(string url)
        {
            TabPage page = new TabPage("Nueva pestana");
            page.BackColor = Theme.Window;
            page.Margin = new Padding(0);
            page.Padding = new Padding(0);

            WebView2 web = new WebView2();
            web.Dock = DockStyle.Fill;
            web.DefaultBackgroundColor = Theme.Window;
            page.Controls.Add(web);
            BrowserTab tab = new BrowserTab(page, web);
            tab.IslandId = _activeIslandId;
            if (tab.IslandId > 0 && !_islandColors.ContainsKey(tab.IslandId))
            {
                _islandColors[tab.IslandId] = ColorFromText(url);
            }
            page.Tag = tab;

            _tabs.TabPages.Add(page);
            RebuildTabStrip();

            await web.EnsureCoreWebView2Async(_environment);
            ConfigureWebView(page, web);
            await PrepareNewTabSurfaceAsync(web);
            _tabs.SelectedTab = page;

            if (!string.IsNullOrWhiteSpace(url) && !string.Equals(url, HomeUrl, StringComparison.OrdinalIgnoreCase))
            {
                Navigate(web, url);
            }
            else
            {
                _address.Text = HomeUrl;
            }

            ApplyTabResourcePolicy();
            EnforceLowResourceLimit();
            SaveSession();

            if (url == "about:blank")
            {
                _address.Text = string.Empty;
                BeginInvoke((MethodInvoker)delegate
                {
                    _address.Focus();
                    _address.SelectAll();
                });
            }

            return tab;
        }

        private async Task PrepareNewTabSurfaceAsync(WebView2 web)
        {
            TaskCompletionSource<bool> ready = new TaskCompletionSource<bool>();
            EventHandler<CoreWebView2NavigationCompletedEventArgs> completed = null;
            completed = delegate
            {
                ready.TrySetResult(true);
            };

            web.CoreWebView2.NavigationCompleted += completed;
            web.NavigateToString(InternalPages.HomeHtml(_appSettings));
            await Task.WhenAny(ready.Task, Task.Delay(2000));
            web.CoreWebView2.NavigationCompleted -= completed;
        }

        private void ConfigureWebView(TabPage page, WebView2 web)
        {
            web.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
            web.CoreWebView2.Settings.AreDevToolsEnabled = true;
            web.CoreWebView2.Settings.IsStatusBarEnabled = false;
            web.CoreWebView2.Settings.IsZoomControlEnabled = true;
            web.CoreWebView2.Settings.IsPasswordAutosaveEnabled = _passwordSavingEnabled;
            web.CoreWebView2.Settings.IsGeneralAutofillEnabled = _passwordSavingEnabled;
            web.CoreWebView2.Profile.IsPasswordAutosaveEnabled = _passwordSavingEnabled;
            web.CoreWebView2.Profile.PreferredTrackingPreventionLevel = CoreWebView2TrackingPreventionLevel.Balanced;
            Logger.Info("Password autosave=" + web.CoreWebView2.Profile.IsPasswordAutosaveEnabled +
                " profile=" + web.CoreWebView2.Profile.ProfilePath);
            web.CoreWebView2.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);
            web.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(YouTubeShieldsScript(_adBlockEnabled));
            web.CoreWebView2.WebResourceRequested += WebResourceRequested;
            web.CoreWebView2.WebMessageReceived += WebMessageReceived;
            web.CoreWebView2.NewWindowRequested += NewWindowRequested;
            web.CoreWebView2.DownloadStarting += DownloadStarting;
            web.CoreWebView2.NavigationStarting += NavigationStarting;
            web.CoreWebView2.NavigationCompleted += NavigationCompletedHandler;
            web.CoreWebView2.DocumentTitleChanged += DocumentTitleChangedHandler;
            web.CoreWebView2.FaviconChanged += FaviconChangedHandler;
            web.CoreWebView2.ProcessFailed += Web_ProcessFailed;
            web.CoreWebView2.ContainsFullScreenElementChanged += ContainsFullScreenElementChanged;

            try
            {
                var controllerField = typeof(WebView2).GetField("_coreWebView2Controller", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (controllerField != null)
                {
                    var controller = controllerField.GetValue(web) as Microsoft.Web.WebView2.Core.CoreWebView2Controller;
                    if (controller != null)
                    {
                        controller.AcceleratorKeyPressed += (s, args) =>
                        {
                            if (args.KeyEventKind == Microsoft.Web.WebView2.Core.CoreWebView2KeyEventKind.KeyDown ||
                                args.KeyEventKind == Microsoft.Web.WebView2.Core.CoreWebView2KeyEventKind.SystemKeyDown)
                            {
                                Keys key = (Keys)args.VirtualKey;
                                Keys modifiers = Keys.None;
                                if (GetKeyState(0x11) < 0) modifiers |= Keys.Control;
                                if (GetKeyState(0x10) < 0) modifiers |= Keys.Shift;
                                if (GetKeyState(0x12) < 0) modifiers |= Keys.Alt;

                                Keys keyData = key | modifiers;
                                if (IsBrowserShortcut(keyData))
                                {
                                    args.Handled = true;
                                    BeginInvoke((MethodInvoker)delegate { HandleBrowserShortcut(keyData); });
                                }
                            }
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error registering AcceleratorKeyPressed: " + ex.Message);
            }
        }

        private void ContainsFullScreenElementChanged(object sender, object e)
        {
            CoreWebView2 core = sender as CoreWebView2;
            WebView2 web = WebViewForCore(core);
            if (core == null || web == null)
            {
                return;
            }

            if (core.ContainsFullScreenElement)
            {
                if (web == ActiveWebView())
                {
                    SetFullScreenMode(true);
                }
            }
            else if (_fullScreenActive)
            {
                SetFullScreenMode(false);
            }
        }

        private void SetFullScreenMode(bool enabled)
        {
            TableLayoutPanel root = _tabs.Parent as TableLayoutPanel;
            if (root == null || enabled == _fullScreenActive)
            {
                return;
            }

            if (enabled)
            {
                _previousBorderStyle = FormBorderStyle;
                _previousWindowState = WindowState;
                _previousBounds = Bounds;
                _previousTopMost = TopMost;
                _fullScreenActive = true;

                for (int i = 0; i < root.Controls.Count; i++)
                {
                    Control control = root.Controls[i];
                    control.Visible = control == _tabs;
                }
                root.ColumnStyles[0].Width = 0;
                root.RowStyles[0].Height = 0;
                root.RowStyles[2].Height = 0;
                FormBorderStyle = FormBorderStyle.None;
                WindowState = FormWindowState.Normal;
                Bounds = Screen.FromControl(this).Bounds;
                TopMost = true;
                WebView2 activeWeb = ActiveWebView();
                if (activeWeb != null)
                {
                    activeWeb.Focus();
                }
                return;
            }

            _fullScreenActive = false;
            TopMost = _previousTopMost;
            FormBorderStyle = _previousBorderStyle;
            Bounds = _previousBounds;
            WindowState = _previousWindowState;
            ApplyLayoutDimensions();
            for (int i = 0; i < root.Controls.Count; i++)
            {
                root.Controls[i].Visible = true;
            }
            ApplyResponsiveMode();
            WebView2 active = ActiveWebView();
            if (active != null)
            {
                active.Focus();
            }
        }

        private async void NewWindowRequested(object sender, CoreWebView2NewWindowRequestedEventArgs e)
        {
            CoreWebView2Deferral deferral = e.GetDeferral();
            try
            {
                if (!e.IsUserInitiated)
                {
                    e.Handled = true;
                    BrowserTab active = ActiveTab();
                    if (active != null)
                    {
                        active.BlockedPopups++;
                        active.LastBlockedRequest = "Ventana emergente no iniciada por el usuario";
                    }
                    UpdateStatus();
                    return;
                }

            BrowserTab newTab = await CreateTabAsync(null);
            e.NewWindow = newTab.WebView.CoreWebView2;
            }
            catch
            {
                e.Handled = true;
            }
            finally
            {
                deferral.Complete();
            }
        }

        private void NavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            CoreWebView2 sourceCore = sender as CoreWebView2;
            WebView2 sourceWeb = WebViewForCore(sourceCore);
            TabPage sourcePage = sourceWeb == null ? null : PageForWebView(sourceWeb);
            BrowserTab sourceTab = sourcePage == null ? null : sourcePage.Tag as BrowserTab;
            Uri startingUri;
            if (sourceTab != null)
            {
                sourceTab.NavigationNotice = string.Empty;
                sourceTab.BlockedRequests = 0;
                sourceTab.BlockedPopups = 0;
                sourceTab.LastBlockedRequest = string.Empty;
                sourceTab.LastNavigationHost = Uri.TryCreate(e.Uri, UriKind.Absolute, out startingUri)
                    ? startingUri.Host.ToLowerInvariant()
                    : string.Empty;
                sourceTab.SiteCompatibilityMode = IsYouTubeHost(sourceTab.LastNavigationHost) ||
                    IsHostOrSubdomain(sourceTab.LastNavigationHost, "crunchyroll.com");
                if (sourceTab.SiteCompatibilityMode)
                {
                    sourceTab.NavigationNotice = IsYouTubeHost(sourceTab.LastNavigationHost)
                        ? "Modo compatibilidad YouTube: Gan Guard activo."
                        : "Modo compatibilidad Crunchyroll: Gan Guard pausado para este sitio.";
                }
                if (IsYouTubeHost(sourceTab.LastNavigationHost) && sourceTab.WebView != null && sourceTab.WebView.CoreWebView2 != null)
                {
                    sourceTab.WebView.CoreWebView2.ExecuteScriptAsync(YouTubeShieldsScript(_adBlockEnabled));
                }
            }

            if (_privacyFirewallEnabled)
            {
                string clean = _privacyFirewall.StripTrackingParameters(e.Uri);
                if (!string.Equals(clean, e.Uri, StringComparison.Ordinal))
                {
                    e.Cancel = true;
                    CoreWebView2 web = sender as CoreWebView2;
                    if (web != null)
                    {
                        web.Navigate(clean);
                    }
                    return;
                }
            }

            UpdateStatus();
        }

        private static string Base64Decode(string value)
        {
            try
            {
                if (string.IsNullOrEmpty(value)) return "";
                byte[] bytes = Convert.FromBase64String(value);
                return Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                return "";
            }
        }

        private void WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            string message;
            try
            {
                message = e.TryGetWebMessageAsString();
            }
            catch
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            CoreWebView2 core = sender as CoreWebView2;
            const string vaultFillPrefix = "ganvault:fill:";
            if (message.StartsWith(vaultFillPrefix, StringComparison.Ordinal))
            {
                int index;
                if (int.TryParse(message.Substring(vaultFillPrefix.Length), out index))
                {
                    Task ignored = FillVaultCredentialFromPageAsync(core, e.Source, index);
                }
                return;
            }

            if (message.StartsWith("gxlight:", StringComparison.Ordinal) && !IsTrustedInternalMessageSource(core, e.Source))
            {
                return;
            }

            const string navigatePrefix = "gxlight:navigate:";
            if (message.StartsWith(navigatePrefix, StringComparison.Ordinal))
            {
                string url = message.Substring(navigatePrefix.Length);
                WebView2 web = WebViewForCore(core) ?? ActiveWebView();
                if (web != null && web.CoreWebView2 != null)
                {
                    Navigate(web, url);
                }
                return;
            }

            if (string.Equals(message, "gxlight:update:prepare", StringComparison.Ordinal))
            {
                Task ignored = CheckForUpdatesAsync(true);
                return;
            }

            const string passwordOpenPrefix = "gxlight:passwords:open:";
            if (message.StartsWith(passwordOpenPrefix, StringComparison.Ordinal))
            {
                NavigateActive(Base64Decode(message.Substring(passwordOpenPrefix.Length)));
                return;
            }

            const string passwordViewPrefix = "gxlight:passwords:view:";
            if (message.StartsWith(passwordViewPrefix, StringComparison.Ordinal))
            {
                int index;
                if (int.TryParse(message.Substring(passwordViewPrefix.Length), out index))
                {
                    Task ignored = ViewVaultCredentialAsync(index);
                }
                return;
            }

            if (string.Equals(message, "gxlight:passwords:import", StringComparison.Ordinal))
            {
                ImportPasswords();
                NavigateInternal("settings");
                return;
            }

            if (string.Equals(message, "gxlight:passwords:export", StringComparison.Ordinal))
            {
                ExportPasswords();
                return;
            }

            if (string.Equals(message, "gxlight:passwords:template", StringComparison.Ordinal))
            {
                ExportPasswordTemplate();
                return;
            }

            if (string.Equals(message, "gxlight:passwords:fill", StringComparison.Ordinal))
            {
                Task ignored = FillCurrentSiteFromVaultAsync();
                return;
            }

            const string deletePrefix = "gxlight:bookmarks:delete:";
            if (message.StartsWith(deletePrefix, StringComparison.Ordinal))
            {
                string data = message.Substring(deletePrefix.Length);
                string[] parts = data.Split('|');
                if (parts.Length >= 3)
                {
                    string title = Base64Decode(parts[0]);
                    string url = Base64Decode(parts[1]);
                    string folder = Base64Decode(parts[2]);

                    _bookmarks.RemoveAll(delegate(BookmarkEntry b)
                    {
                        return string.Equals(b.Title, title, StringComparison.OrdinalIgnoreCase) &&
                               string.Equals(b.Url, url, StringComparison.OrdinalIgnoreCase) &&
                               string.Equals(b.Folder, folder, StringComparison.OrdinalIgnoreCase);
                    });

                    SaveBookmarks();
                    RebuildBookmarksBar();
                    NavigateInternal("bookmarks");
                }
                return;
            }

            const string movePrefix = "gxlight:bookmarks:move:";
            if (message.StartsWith(movePrefix, StringComparison.Ordinal))
            {
                string data = message.Substring(movePrefix.Length);
                string[] parts = data.Split('|');
                if (parts.Length >= 4)
                {
                    string title = Base64Decode(parts[0]);
                    string url = Base64Decode(parts[1]);
                    string folder = Base64Decode(parts[2]);
                    string newFolder = Base64Decode(parts[3]);

                    for (int i = 0; i < _bookmarks.Count; i++)
                    {
                        BookmarkEntry b = _bookmarks[i];
                        if (string.Equals(b.Title, title, StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(b.Url, url, StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(b.Folder, folder, StringComparison.OrdinalIgnoreCase))
                        {
                            b.Folder = newFolder;
                            break;
                        }
                    }

                    SaveBookmarks();
                    RebuildBookmarksBar();
                    NavigateInternal("bookmarks");
                }
                return;
            }

            const string createFolderPrefix = "gxlight:bookmarks:create-folder:";
            if (message.StartsWith(createFolderPrefix, StringComparison.Ordinal))
            {
                string base64Folder = message.Substring(createFolderPrefix.Length);
                string folderName = Base64Decode(base64Folder);
                if (!string.IsNullOrWhiteSpace(folderName))
                {
                    bool exists = false;
                    for (int i = 0; i < _bookmarks.Count; i++)
                    {
                        if (string.Equals(_bookmarks[i].Folder, folderName, StringComparison.OrdinalIgnoreCase))
                        {
                            exists = true;
                            break;
                        }
                    }

                    if (!exists)
                    {
                        BookmarkEntry folderPlaceholder = new BookmarkEntry();
                        folderPlaceholder.Title = folderName;
                        folderPlaceholder.Url = "";
                        folderPlaceholder.Folder = folderName;
                        folderPlaceholder.CreatedUtc = DateTime.UtcNow;
                        _bookmarks.Add(folderPlaceholder);
                        SaveBookmarks();
                        RebuildBookmarksBar();
                    }
                    NavigateInternal("bookmarks");
                }
                return;
            }

            const string deleteBatchPrefix = "gxlight:bookmarks:delete-batch:";
            if (message.StartsWith(deleteBatchPrefix, StringComparison.Ordinal))
            {
                string data = message.Substring(deleteBatchPrefix.Length);
                string[] entries = data.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                for (int j = 0; j < entries.Length; j++)
                {
                    string[] parts = entries[j].Split('|');
                    if (parts.Length >= 3)
                    {
                        string title = Base64Decode(parts[0]);
                        string url = Base64Decode(parts[1]);
                        string folder = Base64Decode(parts[2]);

                        _bookmarks.RemoveAll(delegate(BookmarkEntry b)
                        {
                            return string.Equals(b.Title, title, StringComparison.OrdinalIgnoreCase) &&
                                   string.Equals(b.Url, url, StringComparison.OrdinalIgnoreCase) &&
                                   string.Equals(b.Folder, folder, StringComparison.OrdinalIgnoreCase);
                        });
                    }
                }

                SaveBookmarks();
                RebuildBookmarksBar();
                NavigateInternal("bookmarks");
                return;
            }

            const string deleteAllPrefix = "gxlight:bookmarks:delete-all";
            if (string.Equals(message, deleteAllPrefix, StringComparison.Ordinal))
            {
                _bookmarks.Clear();
                SaveBookmarks();
                RebuildBookmarksBar();
                NavigateInternal("bookmarks");
                return;
            }

            const string playlistOpenPrefix = "gxlight:playlist:open:";
            if (message.StartsWith(playlistOpenPrefix, StringComparison.Ordinal))
            {
                NavigateActive(Base64Decode(message.Substring(playlistOpenPrefix.Length)));
                return;
            }

            const string downloadOpenPrefix = "gxlight:download:open:";
            if (message.StartsWith(downloadOpenPrefix, StringComparison.Ordinal))
            {
                OpenDownloadedFile(Base64Decode(message.Substring(downloadOpenPrefix.Length)));
                return;
            }

            const string playlistDeletePrefix = "gxlight:playlist:delete:";
            if (message.StartsWith(playlistDeletePrefix, StringComparison.Ordinal))
            {
                string url = Base64Decode(message.Substring(playlistDeletePrefix.Length));
                _playlist.RemoveAll(delegate(PlaylistEntry item)
                {
                    return string.Equals(item.Url, url, StringComparison.OrdinalIgnoreCase);
                });
                SavePlaylist();
                NavigateInternal("playlist");
                return;
            }

            const string settingsTogglePrefix = "gxlight:settings:toggle:";
            if (message.StartsWith(settingsTogglePrefix, StringComparison.Ordinal))
            {
                string data = message.Substring(settingsTogglePrefix.Length);
                int colon = data.IndexOf(':');
                if (colon > 0)
                {
                    string key = data.Substring(0, colon);
                    bool val = bool.Parse(data.Substring(colon + 1));

                    if (key == "ShowBookmarksBar")
                    {
                        _appSettings.ShowBookmarksBar = val;
                        ApplyLayoutDimensions();
                    }
                    else if (key == "ShowPageIcons")
                    {
                        _appSettings.ShowPageIcons = val;
                        RebuildTabStrip();
                    }
                    else if (key == "CompactIconTabs")
                    {
                        _appSettings.CompactIconTabs = val;
                        RebuildTabStrip();
                    }
                    else if (key == "RamLimiterEnabled")
                    {
                        _appSettings.RamLimiterEnabled = val;
                        _gxControl.RamLimiterEnabled = val;
                    }
                    else if (key == "HardMemoryLimit")
                    {
                        _appSettings.HardMemoryLimit = val;
                        _gxControl.HardMemoryLimit = val;
                    }
                    else if (key == "LowResourcesModeEnabled")
                    {
                        _appSettings.LowResourcesModeEnabled = val;
                        _gxControl.LowResourcesModeEnabled = val;
                    }
                    else if (key == "AdBlockEnabled")
                    {
                        _appSettings.AdBlockEnabled = val;
                        _adBlockEnabled = val;
                        _shield.Accent = _adBlockEnabled ? Theme.Accent : Theme.Warning;
                        _shield.Text = _adBlockEnabled ? (Width < 980 ? "Ads On" : "Block Ads On") : (Width < 980 ? "Ads Off" : "Block Ads Off");
                    }
                    else if (key == "PrivacyFirewallEnabled")
                    {
                        _appSettings.PrivacyFirewallEnabled = val;
                        _privacyFirewallEnabled = val;
                    }
                    else if (key == "PasswordSavingEnabled")
                    {
                        _appSettings.PasswordSavingEnabled = val;
                        _passwordSavingEnabled = val;
                    }
                    else if (key == "RestorePreviousSession")
                    {
                        _appSettings.RestorePreviousSession = val;
                    }
                    else if (key == "AskSavePathBeforeDownload")
                    {
                        _appSettings.AskSavePathBeforeDownload = val;
                    }
                    else if (key == "AutoCheckUpdates")
                    {
                        _appSettings.AutoCheckUpdates = val;
                    }

                    _appSettings.Save();
                }
                return;
            }

            const string settingsSelectPrefix = "gxlight:settings:select:";
            if (message.StartsWith(settingsSelectPrefix, StringComparison.Ordinal))
            {
                string data = message.Substring(settingsSelectPrefix.Length);
                int colon = data.IndexOf(':');
                if (colon > 0)
                {
                    string key = data.Substring(0, colon);
                    string val = data.Substring(colon + 1);

                    if (key == "MaxActiveTabs")
                    {
                        int limit;
                        if (int.TryParse(val, out limit))
                        {
                            _appSettings.MaxActiveTabs = limit;
                            _gxControl.MaxActiveTabs = limit;
                        }
                    }
                    else if (key == "DefaultSearchEngine")
                    {
                        _appSettings.DefaultSearchEngine = val;
                    }

                    _appSettings.Save();
                }
                return;
            }

            const string settingsThemePrefix = "gxlight:settings:theme:";
            if (message.StartsWith(settingsThemePrefix, StringComparison.Ordinal))
            {
                string themeName = message.Substring(settingsThemePrefix.Length);
                _appSettings.SelectedTheme = themeName;
                _appSettings.Save();
                Theme.ApplyTheme(themeName, _appSettings.ThemeMode);
                ApplyThemeToControls();
                UpdateInternalPagesTheme();
                return;
            }

            const string settingsThemeModePrefix = "gxlight:settings:theme-mode:";
            if (message.StartsWith(settingsThemeModePrefix, StringComparison.Ordinal))
            {
                string themeMode = message.Substring(settingsThemeModePrefix.Length);
                _appSettings.ThemeMode = themeMode;
                _appSettings.Save();
                Theme.ApplyTheme(_appSettings.SelectedTheme, themeMode);
                ApplyThemeToControls();
                UpdateInternalPagesTheme();
                return;
            }

            const string settingsDownloadChangePrefix = "gxlight:settings:downloads:change";
            if (string.Equals(message, settingsDownloadChangePrefix, StringComparison.Ordinal))
            {
                using (FolderBrowserDialog dlg = new FolderBrowserDialog())
                {
                    dlg.Description = "Seleccionar carpeta de descargas";
                    dlg.SelectedPath = string.IsNullOrWhiteSpace(_appSettings.CustomDownloadsFolder) ?
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads") :
                        _appSettings.CustomDownloadsFolder;
                    if (dlg.ShowDialog(this) == DialogResult.OK)
                    {
                        _appSettings.CustomDownloadsFolder = dlg.SelectedPath;
                        _appSettings.Save();
                        core.PostWebMessageAsString("gxlight:settings:downloads:updated:" + dlg.SelectedPath);
                    }
                }
                return;
            }

            const string settingsResetPrefix = "gxlight:settings:reset";
            if (string.Equals(message, settingsResetPrefix, StringComparison.Ordinal))
            {
                _appSettings = new AppSettings();
                _appSettings.Save();
                
                _adBlockEnabled = _appSettings.AdBlockEnabled;
                _privacyFirewallEnabled = _appSettings.PrivacyFirewallEnabled;
                _passwordSavingEnabled = _appSettings.PasswordSavingEnabled;
                _gxControl.RamLimiterEnabled = _appSettings.RamLimiterEnabled;
                _gxControl.MemoryLimitMb = _appSettings.MemoryLimitMb;
                _gxControl.HardMemoryLimit = _appSettings.HardMemoryLimit;
                _gxControl.HotTabsKillerEnabled = _appSettings.HotTabsKillerEnabled;
                _gxControl.HotTabsMode = _appSettings.HotTabsMode;
                _gxControl.CpuLimiterEnabled = _appSettings.CpuLimiterEnabled;
                _gxControl.CpuLimitPercent = _appSettings.CpuLimitPercent;
                _gxControl.NetworkLimiterEnabled = _appSettings.NetworkLimiterEnabled;
                _gxControl.NetworkProfile = _appSettings.NetworkProfile;
                _gxControl.LowResourcesModeEnabled = _appSettings.LowResourcesModeEnabled;
                _gxControl.MaxActiveTabs = _appSettings.MaxActiveTabs;

                Theme.ApplyTheme(_appSettings.SelectedTheme, _appSettings.ThemeMode);
                ApplyThemeToControls();
                ApplyLayoutDimensions();

                NavigateInternal("settings");
                return;
            }
        }

        private bool IsTrustedInternalMessageSource(CoreWebView2 core, string source)
        {
            if (!string.IsNullOrWhiteSpace(source) &&
                (source.StartsWith("data:", StringComparison.OrdinalIgnoreCase) ||
                 source.StartsWith("about:", StringComparison.OrdinalIgnoreCase) ||
                 source.StartsWith("gxlight://", StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            WebView2 web = WebViewForCore(core);
            if (web == null || web != ActiveWebView())
            {
                return false;
            }

            return !string.IsNullOrWhiteSpace(_address.Text) &&
                _address.Text.StartsWith("gxlight://", StringComparison.OrdinalIgnoreCase);
        }

        private void WebResourceRequested(object sender, CoreWebView2WebResourceRequestedEventArgs e)
        {
            if (!_adBlockEnabled)
            {
                return;
            }

            Uri requestUri;
            if (!Uri.TryCreate(e.Request.Uri, UriKind.Absolute, out requestUri))
            {
                return;
            }

            CoreWebView2 core = sender as CoreWebView2;
            WebView2 active = WebViewForCore(core) ?? ActiveWebView();
            TabPage requestPage = active == null ? null : PageForWebView(active);
            BrowserTab tab = requestPage == null ? ActiveTab() : requestPage.Tag as BrowserTab;
            Uri documentUri = null;
            if (active != null && active.Source != null)
            {
                documentUri = active.Source;
            }
            if (tab != null && !string.IsNullOrWhiteSpace(tab.LastNavigationHost) &&
                (documentUri == null || !string.Equals(documentUri.Host, tab.LastNavigationHost, StringComparison.OrdinalIgnoreCase)))
            {
                Uri.TryCreate("https://" + tab.LastNavigationHost + "/", UriKind.Absolute, out documentUri);
            }

            if (tab != null && tab.SiteCompatibilityMode && !string.IsNullOrEmpty(tab.LastNavigationHost) && !IsYouTubeHost(tab.LastNavigationHost))
            {
                return;
            }

            bool shouldBlock = _adBlocker.ShouldBlock(requestUri, documentUri);
            if (!shouldBlock && _privacyFirewallEnabled)
            {
                shouldBlock = _privacyFirewall.ShouldBlock(requestUri, documentUri);
            }

            if (!shouldBlock && IsMediaCompatibilityRequest(e.ResourceContext, requestUri, documentUri))
            {
                return;
            }

            if (!shouldBlock)
            {
                return;
            }

            if (tab != null)
            {
                tab.BlockedRequests++;
                tab.LastBlockedRequest = requestUri.AbsoluteUri;
            }
            Logger.Info("Blocked request [" + e.ResourceContext + "]: " + requestUri.AbsoluteUri +
                " document=" + (documentUri == null ? "(unknown)" : documentUri.AbsoluteUri));

            e.Response = _environment.CreateWebResourceResponse(
                new MemoryStream(new byte[0]),
                204,
                "No Content",
                "Content-Type: text/plain");
            UpdateStatus();
        }

        private static bool IsMediaCompatibilityRequest(CoreWebView2WebResourceContext context, Uri requestUri, Uri documentUri)
        {
            if (requestUri == null || documentUri == null)
            {
                return false;
            }

            bool mediaContext = context == CoreWebView2WebResourceContext.Media ||
                context == CoreWebView2WebResourceContext.XmlHttpRequest ||
                context == CoreWebView2WebResourceContext.Fetch;
            if (!mediaContext)
            {
                return false;
            }

            string documentHost = documentUri.Host.ToLowerInvariant();
            string requestHost = requestUri.Host.ToLowerInvariant();
            if (IsYouTubeHost(documentHost))
            {
                return requestHost.EndsWith("googlevideo.com", StringComparison.Ordinal);
            }

            if (IsHostOrSubdomain(documentHost, "crunchyroll.com"))
            {
                return IsHostOrSubdomain(requestHost, "crunchyroll.com") ||
                    IsHostOrSubdomain(requestHost, "crunchyrollcdn.com") ||
                    IsHostOrSubdomain(requestHost, "vrv.co");
            }

            return false;
        }

        private static bool IsHostOrSubdomain(string host, string domain)
        {
            return !string.IsNullOrEmpty(host) &&
                (host == domain || host.EndsWith("." + domain, StringComparison.Ordinal));
        }

        private void DownloadStarting(object sender, CoreWebView2DownloadStartingEventArgs e)
        {
            string downloadsFolder = string.IsNullOrWhiteSpace(_appSettings.CustomDownloadsFolder)
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads")
                : _appSettings.CustomDownloadsFolder;

            string fileName = e.DownloadOperation.ResultFilePath == null
                ? "download"
                : Path.GetFileName(e.DownloadOperation.ResultFilePath);

            string result = Path.Combine(downloadsFolder, fileName);

            if (_appSettings.AskSavePathBeforeDownload)
            {
                using (SaveFileDialog dlg = new SaveFileDialog())
                {
                    dlg.FileName = fileName;
                    dlg.InitialDirectory = downloadsFolder;
                    dlg.Filter = "Todos los archivos (*.*)|*.*";
                    if (dlg.ShowDialog(this) == DialogResult.OK)
                    {
                        result = dlg.FileName;
                    }
                    else
                    {
                        e.Cancel = true;
                        return;
                    }
                }
            }

            e.ResultFilePath = result;
            e.Handled = false;

            DownloadEntry entry = new DownloadEntry();
            entry.FileName = Path.GetFileName(result);
            entry.Path = result;
            entry.Uri = e.DownloadOperation.Uri;
            entry.StartedUtc = DateTime.UtcNow;
            entry.State = "Downloading";
            _downloads.Insert(0, entry);

            e.DownloadOperation.StateChanged += delegate
            {
                entry.State = e.DownloadOperation.State.ToString();
            };
        }

        private void OpenDownloadedFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                MessageBox.Show(this, "El archivo todavia no esta disponible para abrir.", "Descargas",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo(path);
                startInfo.UseShellExecute = true;
                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                Logger.Error("Could not open downloaded file: " + ex.Message);
                MessageBox.Show(this, "No se pudo abrir el archivo." + Environment.NewLine + ex.Message, "Descargas",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void AddHistoryEntry(TabPage page, WebView2 web)
        {
            if (web == null || web.Source == null)
            {
                return;
            }

            string url = web.Source.ToString();
            if (url == "about:blank" || url.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            HistoryEntry entry = new HistoryEntry();
            entry.Title = string.IsNullOrWhiteSpace(page.Text) ? url : page.Text;
            entry.Url = url;
            entry.VisitedUtc = DateTime.UtcNow;
            _history.Insert(0, entry);

            BrowserTab tab = page.Tag as BrowserTab;
            if (tab != null)
            {
                tab.LastActiveUtc = DateTime.UtcNow;
            }

            while (_history.Count > 500)
            {
                _history.RemoveAt(_history.Count - 1);
            }
        }

        private void NavigateAddress()
        {
            NavigateActive(_address.Text);
        }

        private void NavigateActive(string url)
        {
            WebView2 web = ActiveWebView();
            if (web == null)
            {
                return;
            }

            Navigate(web, NormalizeInput(url));
        }

        private void Navigate(WebView2 web, string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return;

            string normalized = NormalizeInput(url);
            if (normalized.StartsWith("gxlight://", StringComparison.OrdinalIgnoreCase))
            {
                string pageName = normalized.Substring("gxlight://".Length).Trim().ToLowerInvariant();
                if (pageName == "home")
                {
                    web.NavigateToString(InternalPages.HomeHtml(_appSettings));
                    _address.Text = HomeUrl;
                }
                else if (pageName == "updated" || pageName == "novedades")
                {
                    web.NavigateToString(InternalPages.UpdateNoticeHtml(_updateManifest));
                    _address.Text = UpdatedUrl;
                }
                else if (pageName == "extensions")
                {
                    Task dummy = NavigateExtensionsPageAsync();
                }
                else if (pageName == "free-memory")
                {
                    FreeMemoryNow();
                    NavigateInternal("memory");
                }
                else
                {
                    NavigateInternal(pageName);
                }
                return;
            }

            web.CoreWebView2.Navigate(normalized);
        }

        private async void NavigateInternal(string pageName)
        {
            BrowserTab tab = ActiveTab();
            if (tab == null)
            {
                tab = await CreateTabAsync(HomeUrl);
            }

            if (tab.IsSuspended)
            {
                await RestoreSuspendedTabAsync(tab);
            }

            if (tab.WebView == null || tab.WebView.CoreWebView2 == null)
            {
                return;
            }

            tab.Page.Text = PageTitle(pageName);
            tab.WebView.NavigateToString(InternalPageHtml(pageName));
            _address.Text = "gxlight://" + pageName;
            RebuildTabStrip();
        }

        private async Task NavigateExtensionsPageAsync()
        {
            BrowserTab tab = ActiveTab();
            if (tab == null)
            {
                tab = await CreateTabAsync(HomeUrl);
            }

            if (tab.IsSuspended)
            {
                await RestoreSuspendedTabAsync(tab);
            }

            string html = await ExtensionsPageHtmlAsync();
            tab.Page.Text = "Extensions";
            tab.WebView.NavigateToString(html);
            _address.Text = "gxlight://extensions";
            RebuildTabStrip();
        }

        private static string PageTitle(string pageName)
        {
            switch (pageName)
            {
                case "home": return "Gan Browser";
                case "updated":
                case "novedades": return "Update notes";
                case "history": return "History";
                case "downloads": return "Downloads";
                case "passwords": return "Passwords";
                case "bookmarks": return "Bookmarks";
                case "playlist": return "Playlist";
                case "memory": return "Memory";
                case "shields": return "Gan Guard";
                case "settings": return "Settings";
                default: return "Gan Browser";
            }
        }

        private async Task ShowExtensionMenuAsync()
        {
            ContextMenuStrip menu = CreateContextMenu();
            menu.ShowImageMargin = false;
            menu.Items.Add("Importar extension desempaquetada...", null, async delegate { await ImportExtensionAsync(); });
            menu.Items.Add("Ver extensiones instaladas", null, async delegate { await ShowExtensionsAsync(); });
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Abrir Chrome Web Store", null, delegate { NavigateActive(ChromeStoreUrl); });
            menu.Show(_extensions, new Point(0, _extensions.Height + 4));
            await Task.FromResult(0);
        }

        private void ShowMainMenu()
        {
            ContextMenuStrip menu = CreateContextMenu();

            menu.Items.Add(CreateMenuItem("Nueva pestaña", "Ctrl+T", async delegate { await CreateTabAsync(HomeUrl); }));
            menu.Items.Add(CreateMenuItem("Nueva pestaña en isla", "Alt+T", async delegate { await CreateNewIslandTabAsync(); }));
            menu.Items.Add(CreateMenuItem("Nueva ventana", "Ctrl+N", delegate { StartNewWindow(); }));
            menu.Items.Add(CreateMenuItem("Nueva ventana privada", "Ctrl+Shift+N", delegate { MessageBox.Show(this, "El modo privado aún no está implementado.", "Gan Browser"); }));
            menu.Items.Add(new ToolStripSeparator());

            menu.Items.Add(CreateMenuItem("Historial", "Ctrl+H", delegate { NavigateInternal("history"); }));
            menu.Items.Add(CreateMenuItem("Descargas", "Ctrl+J", delegate { NavigateInternal("downloads"); }));

            ToolStripMenuItem playlist = new ToolStripMenuItem("Lista de reproducción (Playlist)");
            playlist.ForeColor = Theme.Text;
            playlist.DropDownItems.Add(CreateMenuItem("Agregar página actual", "", delegate { AddCurrentToPlaylist(); }));
            playlist.DropDownItems.Add(CreateMenuItem("Abrir Playlist", "", delegate { NavigateInternal("playlist"); }));
            menu.Items.Add(playlist);

            ToolStripMenuItem bookmarks = new ToolStripMenuItem("Marcadores");
            bookmarks.ForeColor = Theme.Text;
            bookmarks.DropDownItems.Add(CreateMenuItem("Añadir página actual", "Ctrl+D", delegate { AddCurrentBookmark(); }));
            bookmarks.DropDownItems.Add(CreateMenuItem("Administrar marcadores", "", delegate { NavigateInternal("bookmarks"); }));
            ToolStripMenuItem showBookmarksBar = CreateMenuItem("Mostrar barra de marcadores", "", delegate
            {
                SetBookmarksBarVisible(!_appSettings.ShowBookmarksBar);
            });
            showBookmarksBar.Checked = _appSettings.ShowBookmarksBar;
            bookmarks.DropDownItems.Add(showBookmarksBar);
            bookmarks.DropDownItems.Add(new ToolStripSeparator());
            bookmarks.DropDownItems.Add(CreateMenuItem("Importar marcadores HTML...", "", delegate { ImportBookmarks(); }));
            bookmarks.DropDownItems.Add(CreateMenuItem("Exportar marcadores HTML...", "", delegate { ExportBookmarks(); }));
            menu.Items.Add(bookmarks);

            ToolStripMenuItem tabSharing = new ToolStripMenuItem("Compartir / Exportar pestañas");
            tabSharing.ForeColor = Theme.Text;
            tabSharing.DropDownItems.Add(CreateMenuItem("Copiar todas las URLs", "", delegate { CopyAllTabUrls(); }));
            tabSharing.DropDownItems.Add(CreateMenuItem("Exportar pestañas a archivo de texto...", "", delegate { ExportTabsToTextFile(); }));
            tabSharing.DropDownItems.Add(CreateMenuItem("Importar pestañas desde archivo de texto...", "", delegate { ImportTabsFromTextFile(); }));
            menu.Items.Add(tabSharing);

            menu.Items.Add(CreateMenuItem("Extensiones", "", async delegate { await NavigateExtensionsPageAsync(); }));

            menu.Items.Add(new ToolStripSeparator());

            menu.Items.Add(CreateMenuItem("Suspender pestañas inactivas ahora", "", delegate { SuspendIdleTabsNow(); }));
            menu.Items.Add(CreateMenuItem("Gan Pulse / Limitadores", "", delegate { ShowGxControl(); }));
            menu.Items.Add(CreateMenuItem("Monitor de memoria", "", delegate { NavigateInternal("memory"); }));
            menu.Items.Add(CreateMenuItem("Gan Guard / Cortafuegos de Privacidad", "", delegate { NavigateInternal("shields"); }));

            ToolStripMenuItem restoreSession = CreateMenuItem("Guardar pestañas al cerrar", "", delegate
            {
                _appSettings.RestorePreviousSession = !_appSettings.RestorePreviousSession;
                _appSettings.Save();
                if (_appSettings.RestorePreviousSession) SaveSession();
                else SessionManager.DeleteSession();
            });
            restoreSession.Checked = _appSettings.RestorePreviousSession;
            menu.Items.Add(restoreSession);

            ToolStripMenuItem appearance = new ToolStripMenuItem("Apariencia de pestañas");
            appearance.ForeColor = Theme.Text;
            ToolStripMenuItem showIcons = CreateMenuItem("Ver iconos de las páginas", "", delegate
            {
                _appSettings.ShowPageIcons = !_appSettings.ShowPageIcons;
                if (!_appSettings.ShowPageIcons)
                {
                    _appSettings.CompactIconTabs = false;
                }
                _appSettings.Save();
                RebuildTabStrip();
            });
            showIcons.Checked = _appSettings.ShowPageIcons;
            ToolStripMenuItem compactTabs = CreateMenuItem("Pestañas compactas con iconos", "", delegate
            {
                _appSettings.CompactIconTabs = !_appSettings.CompactIconTabs;
                if (_appSettings.CompactIconTabs)
                {
                    _appSettings.ShowPageIcons = true;
                }
                _appSettings.Save();
                RebuildTabStrip();
            });
            compactTabs.Checked = _appSettings.CompactIconTabs;
            appearance.DropDownItems.Add(showIcons);
            appearance.DropDownItems.Add(compactTabs);

            ToolStripMenuItem tabSize = new ToolStripMenuItem("Tamaño de pestañas");
            tabSize.ForeColor = Theme.Text;
            AddTabWidthMenuItem(tabSize, "Automático", 0);
            AddTabWidthMenuItem(tabSize, "Pequeño", 92);
            AddTabWidthMenuItem(tabSize, "Mediano", 140);
            AddTabWidthMenuItem(tabSize, "Grande", 190);
            appearance.DropDownItems.Add(tabSize);
            menu.Items.Add(appearance);

            menu.Items.Add(new ToolStripSeparator());

            menu.Items.Add(CreateMenuItem("Buscar...", "Ctrl+F", delegate { ExecuteFind(); }));
            menu.Items.Add(CreateMenuItem("Configuración", "Alt+P", delegate { NavigateInternal("settings"); }));

            // Menu de actualizaciones dinamico
            if (_updateDownloading)
            {
                menu.Items.Add(CreateMenuItem("Descargando actualización...", "", delegate
                {
                    if (_updateCts != null && !_updateCts.IsCancellationRequested)
                    {
                        _updateCts.Cancel();
                        _status.Text = "Cancelando descarga...";
                    }
                }));
            }
            else if (_updateAvailable && !string.IsNullOrWhiteSpace(_preparedUpdateInstallerPath) && File.Exists(_preparedUpdateInstallerPath))
            {
                string preparedVersion = _preparedUpdateManifest == null ? string.Empty : " v" + _preparedUpdateManifest.Version;
                ToolStripMenuItem applyItem = CreateMenuItem("Reiniciar para aplicar" + preparedVersion, "", delegate { ApplyPreparedUpdateAndRestart(); });
                applyItem.Font = new Font(Font, FontStyle.Bold);
                applyItem.ForeColor = Color.LimeGreen;
                menu.Items.Add(applyItem);
            }
            else
            {
                menu.Items.Add(CreateMenuItem("Buscar actualizaciones", "", async delegate { await CheckForUpdatesAsync(true); }));
            }
            menu.Items.Add(CreateMenuItem("Descargar desde GitHub", "", delegate {
                try { Process.Start("https://github.com/wiimri/Gan-Browser-Releases/releases"); } catch { }
            }));
            menu.Items.Add(CreateMenuItem("Notas de actualización", "v" + _updateManifest.Version, delegate { NavigateActive(UpdatedUrl); }));
            menu.Items.Add(CreateMenuItem("Herramientas de desarrollo", "F12", delegate
            {
                WebView2 web = ActiveWebView();
                if (web != null && web.CoreWebView2 != null)
                {
                    web.CoreWebView2.OpenDevToolsWindow();
                }
            }));
            menu.Items.Add(CreateMenuItem("Salir", "", delegate { Close(); }));

            menu.Show(_menuButton, new Point(0, _menuButton.Height + 4));
        }

        private async Task CheckForUpdatesAsync(bool showCurrentMessage)
        {
            UpdateManifest latest = await UpdateManifest.LoadLatestAsync();
            _updateManifest = latest;
            Version installedVersion;
            Version remoteVersion;
            bool hasUpdate = Version.TryParse(VersionInfo.CurrentVersion, out installedVersion) &&
                Version.TryParse(latest.Version, out remoteVersion) &&
                remoteVersion > installedVersion;

            if (!hasUpdate)
            {
                if (showCurrentMessage)
                {
                    MessageBox.Show(this, "Gan Browser " + VersionInfo.CurrentVersion + " ya es la version mas reciente publicada.",
                        "Actualizaciones", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                return;
            }

            await PrepareUpdateAsync(latest, true);
        }

        private async Task PrepareUpdateSilentAsync(UpdateManifest manifest)
        {
            if (manifest == null || string.IsNullOrWhiteSpace(manifest.DownloadUrl))
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(_preparedUpdateInstallerPath) && File.Exists(_preparedUpdateInstallerPath))
            {
                _updateAvailable = true;
                return;
            }

            if (_updateDownloading)
            {
                return;
            }

            await PrepareUpdateAsync(manifest, false);
        }

        private async Task PrepareUpdateAsync(UpdateManifest manifest, bool userRequested)
        {
            if (manifest == null || string.IsNullOrWhiteSpace(manifest.DownloadUrl))
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(_preparedUpdateInstallerPath) && File.Exists(_preparedUpdateInstallerPath))
            {
                _updateAvailable = true;
                if (userRequested)
                {
                    PromptToApplyPreparedUpdate();
                }
                return;
            }

            if (_updateDownloading)
            {
                _status.Text = "Ya se esta descargando una actualizacion...";
                return;
            }

            string installerPath = Path.Combine(Path.GetTempPath(), "GanBrowser-Setup-" + manifest.Version + "-x64.exe");
            try
            {
                _updateDownloading = true;
                _updateCts = new CancellationTokenSource();
                CancellationToken ct = _updateCts.Token;

                CleanupOldInstallers(manifest.Version);
                _status.Text = "Descargando actualizacion " + manifest.Version + "... 0%";

                await DownloadUpdateFileWithProgressAsync(manifest.DownloadUrl, installerPath,
                    delegate (double progress)
                    {
                        string msg = "Descargando actualizacion " + manifest.Version + "... " + (int)Math.Round(progress) + "%";
                        BeginInvoke((MethodInvoker)delegate { _status.Text = msg; });
                    }, ct);

                if (ct.IsCancellationRequested)
                {
                    File.Delete(installerPath);
                    _status.Text = "Descarga de actualizacion cancelada.";
                    return;
                }

                bool? hashValid = await VerifyInstallerHashAsync(installerPath, manifest, ct);
                if (hashValid == false)
                {
                    File.Delete(installerPath);
                    _status.Text = "Actualizacion rechazada: hash no coincide.";
                    if (userRequested)
                    {
                        MessageBox.Show(this, "La actualizacion no supero la verificación SHA-256. No se aplicara el instalador.",
                            "Actualizacion rechazada", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    return;
                }

                if (hashValid == null)
                {
                    if (userRequested)
                    {
                        DialogResult dr = MessageBox.Show(this,
                            "No se pudo verificar la firma SHA-256 del instalador." + Environment.NewLine +
                            "Esto puede ocurrir si el manifiesto remoto aun no esta actualizado." + Environment.NewLine +
                            Environment.NewLine +
                            "¿Aplicar la actualizacion de todas formas?" + Environment.NewLine +
                            "(Si tienes dudas, abre GitHub y descarga manualmente.)",
                            "Verificacion no disponible", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                        if (dr != DialogResult.Yes)
                        {
                            File.Delete(installerPath);
                            _status.Text = "Actualizacion cancelada por seguridad.";
                            return;
                        }
                    }
                    else
                    {
                        _status.Text = "Actualizacion descargada pero no verificada (SHA-256 no disponible).";
                    }
                }

                _preparedUpdateInstallerPath = installerPath;
                _preparedUpdateManifest = manifest;
                _updateAvailable = true;
                _status.Text = "Actualizacion " + manifest.Version + " lista. Ve a Menu > Reiniciar para aplicar.";

                if (userRequested)
                {
                    PromptToApplyPreparedUpdate();
                }
            }
            catch (OperationCanceledException)
            {
                _status.Text = "Descarga de actualizacion cancelada.";
            }
            catch (Exception ex)
            {
                Logger.Error("Update download failed: " + ex.Message);
                _status.Text = "Error al descargar actualizacion.";
                if (userRequested)
                {
                    MessageBox.Show(this, "No se pudo descargar o preparar la actualizacion." + Environment.NewLine + ex.Message,
                        "Actualizaciones", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            finally
            {
                _updateDownloading = false;
                UpdateStatus();
            }
        }

        private async Task DownloadUpdateFileWithProgressAsync(string url, string destinationPath,
            Action<double> onProgress, CancellationToken ct)
        {
            const int maxAttempts = 3;
            Exception lastError = null;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    if (File.Exists(destinationPath))
                    {
                        File.Delete(destinationPath);
                    }

                    using (HttpResponseMessage response = await _httpClient.GetAsync(url,
                        HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false))
                    {
                        response.EnsureSuccessStatusCode();
                        long totalBytes = response.Content.Headers.ContentLength ?? -1;

                        using (Stream contentStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                        using (FileStream fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                        {
                            byte[] buffer = new byte[8192];
                            long readBytes = 0;
                            int bytesRead;

                            while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, ct).ConfigureAwait(false)) > 0)
                            {
                                await fileStream.WriteAsync(buffer, 0, bytesRead, ct).ConfigureAwait(false);
                                readBytes += bytesRead;

                                if (totalBytes > 0 && onProgress != null)
                                {
                                    double pct = (double)readBytes / totalBytes * 100.0;
                                    onProgress(pct);
                                }
                            }
                        }
                    }

                    if (!File.Exists(destinationPath) || new FileInfo(destinationPath).Length == 0)
                    {
                        throw new IOException("GitHub devolvio un archivo vacio.");
                    }

                    return;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    lastError = ex;
                    try { if (File.Exists(destinationPath)) File.Delete(destinationPath); }
                    catch { }

                    Logger.Warning("Update download attempt " + attempt + "/" + maxAttempts + " failed: " + ex.Message);
                }

                if (attempt < maxAttempts)
                {
                    await Task.Delay(attempt * 2500, ct).ConfigureAwait(false);
                }
            }

            throw new IOException("No se pudo descargar la actualizacion despues de " + maxAttempts + " intentos.", lastError);
        }

        private async Task<bool?> VerifyInstallerHashAsync(string installerPath, UpdateManifest manifest, CancellationToken ct)
        {
            if (!File.Exists(installerPath))
            {
                return false;
            }

            string expectedSha256 = null;

            if (!string.IsNullOrWhiteSpace(manifest.Sha256))
            {
                expectedSha256 = manifest.Sha256.Trim();
            }
            else if (!string.IsNullOrWhiteSpace(manifest.Sha256Url))
            {
                try
                {
                    using (HttpResponseMessage response = await _httpClient.GetAsync(manifest.Sha256Url, ct).ConfigureAwait(false))
                    {
                        response.EnsureSuccessStatusCode();
                        string content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        expectedSha256 = content.Trim().Split(' ')[0].Trim();
                    }
                }
                catch
                {
                }
            }

            if (string.IsNullOrWhiteSpace(expectedSha256))
            {
                return null;
            }

            using (SHA256 sha = SHA256.Create())
            using (FileStream stream = File.OpenRead(installerPath))
            {
                byte[] hash = await Task.Run(() => sha.ComputeHash(stream), ct).ConfigureAwait(false);
                string actual = BitConverter.ToString(hash).Replace("-", string.Empty);
                return string.Equals(expectedSha256, actual, StringComparison.OrdinalIgnoreCase);
            }
        }

        private static void CleanupOldInstallers(string currentVersion)
        {
            try
            {
                string tempDir = Path.GetTempPath();
                string[] oldInstallers = Directory.GetFiles(tempDir, "GanBrowser-Setup-*-x64.exe");
                foreach (string old in oldInstallers)
                {
                    if (!old.Contains("-" + currentVersion + "-"))
                    {
                        try { File.Delete(old); }
                        catch { }
                        string oldHash = old + ".sha256.txt";
                        try { if (File.Exists(oldHash)) File.Delete(oldHash); }
                        catch { }
                    }
                }
            }
            catch
            {
            }
        }

        private void PromptToApplyPreparedUpdate()
        {
            if (string.IsNullOrWhiteSpace(_preparedUpdateInstallerPath) || !File.Exists(_preparedUpdateInstallerPath))
            {
                return;
            }

            string version = _preparedUpdateManifest == null ? string.Empty : " " + _preparedUpdateManifest.Version;
            DialogResult result = MessageBox.Show(this,
                "La actualizacion" + version + " se descargo y verifico correctamente." + Environment.NewLine +
                "Gan Browser puede seguir abierto hasta que decidas aplicarla." + Environment.NewLine + Environment.NewLine +
                "¿Reiniciar y actualizar ahora?",
                "Actualizacion lista", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
            if (result == DialogResult.Yes)
            {
                ApplyPreparedUpdateAndRestart();
            }
        }

        private void ApplyPreparedUpdateAndRestart()
        {
            if (string.IsNullOrWhiteSpace(_preparedUpdateInstallerPath) || !File.Exists(_preparedUpdateInstallerPath))
            {
                MessageBox.Show(this, "El instalador preparado ya no esta disponible. Busca la actualizacion nuevamente.",
                    "Actualizaciones", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                // Crear backup de rollback antes de aplicar actualizacion
                CreateRollbackBackup();

                SaveSession();
                ProcessStartInfo startInfo = new ProcessStartInfo(_preparedUpdateInstallerPath);
                startInfo.UseShellExecute = true;
                startInfo.Arguments = "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /CLOSEAPPLICATIONS /FORCECLOSEAPPLICATIONS /RELAUNCH";
                Process.Start(startInfo);
                Close();
            }
            catch (Exception ex)
            {
                Logger.Error("Prepared update launch failed: " + ex.Message);
                MessageBox.Show(this, "No se pudo iniciar la actualizacion." + Environment.NewLine + ex.Message,
                    "Actualizaciones", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CreateRollbackBackup()
        {
            try
            {
                string installDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    "GanBrowser");
                if (!Directory.Exists(installDir))
                {
                    installDir = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                        "GXLightBrowser");
                }
                if (!Directory.Exists(installDir))
                {
                    return;
                }

                string rollbackDir = Path.Combine(AppPaths.AppData, "rollback-v" + VersionInfo.CurrentVersion);
                if (Directory.Exists(rollbackDir))
                {
                    Directory.Delete(rollbackDir, true);
                }

                Directory.CreateDirectory(rollbackDir);
                foreach (string file in Directory.GetFiles(installDir, "*", SearchOption.TopDirectoryOnly))
                {
                    try
                    {
                        string dest = Path.Combine(rollbackDir, Path.GetFileName(file));
                        File.Copy(file, dest, true);
                    }
                    catch
                    {
                    }
                }

                Logger.Info("Rollback backup created at " + rollbackDir);
            }
            catch (Exception ex)
            {
                Logger.Warning("Could not create rollback backup: " + ex.Message);
            }
        }

        private void AddTabWidthMenuItem(ToolStripMenuItem parent, string text, int width)
        {
            ToolStripMenuItem item = new ToolStripMenuItem(text);
            item.ForeColor = Theme.Text;
            item.Checked = _appSettings.TabWidth == width;
            item.Click += delegate
            {
                _appSettings.TabWidth = width;
                _appSettings.CompactIconTabs = false;
                _appSettings.Save();
                RebuildTabStrip();
            };
            parent.DropDownItems.Add(item);
        }

        private void SetPasswordSavingEnabled(bool enabled)
        {
            _passwordSavingEnabled = enabled;
            _appSettings.PasswordSavingEnabled = enabled;
            _appSettings.Save();

            for (int i = 0; i < _tabs.TabPages.Count; i++)
            {
                BrowserTab tab = _tabs.TabPages[i].Tag as BrowserTab;
                if (tab == null || tab.WebView == null || tab.WebView.CoreWebView2 == null)
                {
                    continue;
                }

                tab.WebView.CoreWebView2.Settings.IsPasswordAutosaveEnabled = enabled;
                tab.WebView.CoreWebView2.Settings.IsGeneralAutofillEnabled = enabled;
                tab.WebView.CoreWebView2.Profile.IsPasswordAutosaveEnabled = enabled;
            }

            NavigateInternal("settings");
        }

        private void ShowGxControl()
        {
            using (GxControlDialog dialog = new GxControlDialog(_gxControl))
            {
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    _appSettings.RamLimiterEnabled = _gxControl.RamLimiterEnabled;
                    _appSettings.MemoryLimitMb = _gxControl.MemoryLimitMb;
                    _appSettings.HardMemoryLimit = _gxControl.HardMemoryLimit;
                    _appSettings.HotTabsKillerEnabled = _gxControl.HotTabsKillerEnabled;
                    _appSettings.HotTabsMode = _gxControl.HotTabsMode;
                    _appSettings.CpuLimiterEnabled = _gxControl.CpuLimiterEnabled;
                    _appSettings.CpuLimitPercent = _gxControl.CpuLimitPercent;
                    _appSettings.NetworkLimiterEnabled = _gxControl.NetworkLimiterEnabled;
                    _appSettings.NetworkProfile = _gxControl.NetworkProfile;
                    _appSettings.LowResourcesModeEnabled = _gxControl.LowResourcesModeEnabled;
                    _appSettings.MaxActiveTabs = _gxControl.MaxActiveTabs;
                    _appSettings.Save();

                    EnforceMemoryLimit();
                    EnforceLowResourceLimit();
                    UpdateMemoryMonitor();
                }
            }
        }

        private void StartNewWindow()
        {
            try
            {
                Process.Start(Application.ExecutablePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "No se pudo abrir otra ventana");
            }
        }

        private void ExecuteFind()
        {
            WebView2 web = ActiveWebView();
            if (web != null && web.CoreWebView2 != null)
            {
                web.CoreWebView2.ExecuteScriptAsync("window.find(prompt('Buscar en la pagina') || '')");
            }
        }

        private async Task ImportExtensionAsync()
        {
            BrowserTab tab = ActiveTab();
            if (tab == null || tab.WebView.CoreWebView2 == null)
            {
                return;
            }

            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Selecciona una extension desempaquetada o una carpeta de extension de Chrome/Edge.";
                string chromePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Google", "Chrome", "User Data", "Default", "Extensions");
                if (Directory.Exists(chromePath))
                {
                    dialog.SelectedPath = chromePath;
                }

                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                try
                {
                    string imported = await _extensionImporter.ImportAsync(tab.WebView.CoreWebView2.Profile, dialog.SelectedPath);
                    MessageBox.Show(this, "Extension cargada: " + imported, "Gan Browser");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ex.Message, "No se pudo cargar la extension");
                }
            }
        }

        private async Task ShowExtensionsAsync()
        {
            BrowserTab tab = ActiveTab();
            if (tab == null || tab.WebView.CoreWebView2 == null)
            {
                return;
            }

            System.Collections.Generic.IReadOnlyList<CoreWebView2BrowserExtension> extensions =
                await tab.WebView.CoreWebView2.Profile.GetBrowserExtensionsAsync();
            if (extensions.Count == 0)
            {
                MessageBox.Show(this, "No hay extensiones cargadas todavia. Abre Extensions > Importar extension desempaquetada.", "Extensiones");
                return;
            }

            string text = string.Empty;
            for (int i = 0; i < extensions.Count; i++)
            {
                text += extensions[i].Name + " - " + (extensions[i].IsEnabled ? "activa" : "desactivada") + Environment.NewLine;
            }

            MessageBox.Show(this, text, "Extensiones");
        }

        private void AddSideButton(Control parent, string label, string tooltip, string url)
        {
            ChromeButton button = new ChromeButton();
            ConfigureButton(button, label, 34, tooltip);
            button.Height = 30;
            button.Left = 6;
            button.Top = 12 + parent.Controls.Count * 38;
            button.Click += delegate { NavigateActive(url); };
            button.MouseUp += async delegate(object sender, MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Middle)
                {
                    await CreateTabAsync(url);
                }
            };
            parent.Controls.Add(button);
        }

        private void ConfigureButton(ChromeButton button, string text, int width, string tooltip)
        {
            button.Text = text;
            button.Width = width;
            button.Height = 24;
            button.Margin = new Padding(2, 1, 4, 1);
            button.TabStop = false;
            _tips.SetToolTip(button, tooltip);
        }

        private void LoadBookmarks()
        {
            _bookmarks.Clear();
            if (!File.Exists(AppPaths.Bookmarks))
            {
                return;
            }

            string[] lines = File.ReadAllLines(AppPaths.Bookmarks, Encoding.UTF8);
            for (int i = 0; i < lines.Length; i++)
            {
                string[] parts = lines[i].Split('\t');
                if (parts.Length < 3)
                {
                    continue;
                }

                BookmarkEntry entry = new BookmarkEntry();
                entry.Title = DecodeField(parts[0]);
                entry.Url = DecodeField(parts[1]);
                entry.Folder = DecodeField(parts[2]);
                entry.CreatedUtc = parts.Length > 3 ? new DateTime(ParseLong(parts[3], DateTime.UtcNow.Ticks), DateTimeKind.Utc) : DateTime.UtcNow;
                _bookmarks.Add(entry);
            }
        }

        private void SaveBookmarks()
        {
            AppPaths.Ensure();
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < _bookmarks.Count; i++)
            {
                BookmarkEntry entry = _bookmarks[i];
                builder.Append(EncodeField(entry.Title)).Append('\t')
                    .Append(EncodeField(entry.Url)).Append('\t')
                    .Append(EncodeField(entry.Folder)).Append('\t')
                    .Append(entry.CreatedUtc.Ticks).AppendLine();
            }
            File.WriteAllText(AppPaths.Bookmarks, builder.ToString(), Encoding.UTF8);
        }

        private void LoadPlaylist()
        {
            _playlist.Clear();
            if (!File.Exists(AppPaths.Playlist))
            {
                return;
            }

            foreach (string line in File.ReadAllLines(AppPaths.Playlist, Encoding.UTF8))
            {
                string[] parts = line.Split('\t');
                if (parts.Length < 2) continue;
                PlaylistEntry item = new PlaylistEntry();
                item.Title = DecodeField(parts[0]);
                item.Url = DecodeField(parts[1]);
                item.AddedUtc = parts.Length > 2
                    ? new DateTime(ParseLong(parts[2], DateTime.UtcNow.Ticks), DateTimeKind.Utc)
                    : DateTime.UtcNow;
                _playlist.Add(item);
            }
        }

        private void SavePlaylist()
        {
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < _playlist.Count; i++)
            {
                builder.Append(EncodeField(_playlist[i].Title)).Append('\t')
                    .Append(EncodeField(_playlist[i].Url)).Append('\t')
                    .Append(_playlist[i].AddedUtc.Ticks).AppendLine();
            }
            File.WriteAllText(AppPaths.Playlist, builder.ToString(), Encoding.UTF8);
        }

        private void AddCurrentToPlaylist()
        {
            BrowserTab tab = ActiveTab();
            string url = GetTabUrl(tab);
            if (tab == null || string.IsNullOrWhiteSpace(url) || url.StartsWith("gxlight://", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show(this, "Abre un video o pagina multimedia antes de agregarla.", "Playlist");
                return;
            }

            if (_playlist.Exists(delegate(PlaylistEntry item) { return string.Equals(item.Url, url, StringComparison.OrdinalIgnoreCase); }))
            {
                MessageBox.Show(this, "Esta pagina ya esta en la Playlist.", "Playlist");
                return;
            }

            PlaylistEntry entry = new PlaylistEntry();
            entry.Title = GetTabTitle(tab);
            entry.Url = url;
            entry.AddedUtc = DateTime.UtcNow;
            _playlist.Insert(0, entry);
            SavePlaylist();
            NavigateInternal("playlist");
        }

        private void ShowFolderDropdownMenu(string folderName, Control owner)
        {
            ContextMenuStrip menu = CreateContextMenu();

            List<BookmarkEntry> folderItems = new List<BookmarkEntry>();
            for (int i = 0; i < _bookmarks.Count; i++)
            {
                BookmarkEntry entry = _bookmarks[i];
                if (string.Equals(entry.Folder, folderName, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(entry.Url))
                {
                    folderItems.Add(entry);
                }
            }

            if (folderItems.Count == 0)
            {
                menu.Items.Add("(Vacio)").Enabled = false;
            }
            else
            {
                for (int i = 0; i < folderItems.Count; i++)
                {
                    BookmarkEntry entry = folderItems[i];
                    string label = Trim(string.IsNullOrWhiteSpace(entry.Title) ? entry.Url : entry.Title, 30);
                    ToolStripMenuItem item = new ToolStripMenuItem(label);
                    item.Click += delegate { NavigateActive(entry.Url); };
                    menu.Items.Add(item);
                }
            }
            menu.Show(owner, new Point(0, owner.Height + 4));
        }

        private void ShowFolderContextMenu(string folderName, Control owner)
        {
            ContextMenuStrip menu = CreateContextMenu();

            menu.Items.Add("Eliminar carpeta y su contenido", null, delegate
            {
                if (MessageBox.Show(this, "¿Seguro que deseas eliminar la carpeta '" + folderName + "' y todos sus favoritos?", "Eliminar carpeta", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    _bookmarks.RemoveAll(delegate(BookmarkEntry b)
                    {
                        return string.Equals(b.Folder, folderName, StringComparison.OrdinalIgnoreCase);
                    });
                    SaveBookmarks();
                    RebuildBookmarksBar();
                }
            });
            menu.Show(owner, new Point(0, owner.Height + 4));
        }

        private void RebuildBookmarksBar()
        {
            _bookmarksBar.SuspendLayout();
            _bookmarksBar.Controls.Clear();

            List<string> folders = new List<string>();
            for (int i = 0; i < _bookmarks.Count; i++)
            {
                string f = _bookmarks[i].Folder;
                if (!string.IsNullOrWhiteSpace(f) &&
                    !string.Equals(f, "Favorites bar", StringComparison.OrdinalIgnoreCase) &&
                    !folders.Contains(f))
                {
                    folders.Add(f);
                }
            }

            for (int i = 0; i < folders.Count; i++)
            {
                string folderName = folders[i];
                ChromeButton folderBtn = new ChromeButton();
                ConfigureButton(folderBtn, "📁 " + Trim(folderName, 15), 110, "Carpeta: " + folderName);
                folderBtn.Height = 24;
                folderBtn.Margin = new Padding(0, 1, 6, 1);
                folderBtn.Click += delegate { ShowFolderDropdownMenu(folderName, folderBtn); };
                folderBtn.MouseUp += delegate(object sender, MouseEventArgs e)
                {
                    if (e.Button == MouseButtons.Right)
                    {
                        ShowFolderContextMenu(folderName, folderBtn);
                    }
                };
                _bookmarksBar.Controls.Add(folderBtn);
            }

            List<BookmarkEntry> rootBookmarks = new List<BookmarkEntry>();
            for (int i = 0; i < _bookmarks.Count; i++)
            {
                BookmarkEntry entry = _bookmarks[i];
                if ((string.IsNullOrWhiteSpace(entry.Folder) || string.Equals(entry.Folder, "Favorites bar", StringComparison.OrdinalIgnoreCase)) &&
                    !string.IsNullOrWhiteSpace(entry.Url))
                {
                    rootBookmarks.Add(entry);
                }
            }

            if (folders.Count == 0 && rootBookmarks.Count == 0)
            {
                ChromeButton add = new ChromeButton();
                ConfigureButton(add, "+ Bookmark", 100, "Guardar pagina actual en favoritos");
                add.Height = 24;
                add.Margin = new Padding(0, 1, 6, 1);
                add.Click += delegate { AddCurrentBookmark(); };
                _bookmarksBar.Controls.Add(add);
                _bookmarksBar.ResumeLayout();
                return;
            }

            int limit = Math.Min(rootBookmarks.Count, Width < 900 ? 5 : 10);
            for (int i = 0; i < limit; i++)
            {
                BookmarkEntry entry = rootBookmarks[i];
                ChromeButton button = new ChromeButton();
                ConfigureButton(button, Trim(string.IsNullOrWhiteSpace(entry.Title) ? entry.Url : entry.Title, 18), Width < 900 ? 92 : 132, entry.Url);
                button.Height = 24;
                button.Margin = new Padding(0, 1, 6, 1);
                button.Click += delegate { NavigateActive(entry.Url); };
                button.MouseUp += async delegate(object sender, MouseEventArgs e)
                {
                    if (e.Button == MouseButtons.Middle)
                    {
                        await CreateTabAsync(entry.Url);
                    }
                    else if (e.Button == MouseButtons.Right)
                    {
                        ShowBookmarkContextMenu(entry, button);
                    }
                };
                _bookmarksBar.Controls.Add(button);
            }

            ChromeButton manage = new ChromeButton();
            ConfigureButton(manage, "...", 34, "Administrar favoritos");
            manage.Height = 24;
            manage.Margin = new Padding(0, 1, 6, 1);
            manage.Click += delegate { NavigateInternal("bookmarks"); };
            _bookmarksBar.Controls.Add(manage);

            _bookmarksBar.ResumeLayout();
        }

        private void ShowBookmarkContextMenu(BookmarkEntry entry, Control owner)
        {
            ContextMenuStrip menu = CreateContextMenu();
            menu.Items.Add("Open", null, delegate { NavigateActive(entry.Url); });
            menu.Items.Add("Open in new tab", null, async delegate { await CreateTabAsync(entry.Url); });
            menu.Items.Add("Copy address", null, delegate { Clipboard.SetText(entry.Url); });
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Remove bookmark", null, delegate
            {
                _bookmarks.Remove(entry);
                SaveBookmarks();
                RebuildBookmarksBar();
            });
            menu.Show(owner, new Point(0, owner.Height + 4));
        }

        private void AddCurrentBookmark()
        {
            BrowserTab tab = ActiveTab();
            string url = GetTabUrl(tab);
            if (string.IsNullOrWhiteSpace(url))
            {
                return;
            }

            for (int i = 0; i < _bookmarks.Count; i++)
            {
                if (string.Equals(_bookmarks[i].Url, url, StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show(this, "Ese favorito ya existe.", "Bookmarks");
                    return;
                }
            }

            BookmarkEntry entry = new BookmarkEntry();
            entry.Title = GetTabTitle(tab);
            entry.Url = url;
            entry.Folder = "Favorites bar";
            entry.CreatedUtc = DateTime.UtcNow;
            _bookmarks.Insert(0, entry);
            SaveBookmarks();
            RebuildBookmarksBar();
            MessageBox.Show(this, "Favorito guardado en la barra.", "Bookmarks");
        }

        private void CopyAllTabUrls()
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < _tabs.TabPages.Count; i++)
                {
                    BrowserTab tab = _tabs.TabPages[i].Tag as BrowserTab;
                    if (tab != null)
                    {
                        string url = GetTabUrl(tab);
                        if (!string.IsNullOrWhiteSpace(url))
                        {
                            sb.AppendLine(url);
                        }
                    }
                }

                string urlsText = sb.ToString();
                if (string.IsNullOrWhiteSpace(urlsText))
                {
                    MessageBox.Show(this, "No hay pestañas abiertas con direcciones válidas.", "Copiar URLs", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                Clipboard.SetText(urlsText);
                _status.Text = "Se copiaron todas las URLs al portapapeles (" + _tabs.TabPages.Count + " pestañas).";
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Error al copiar las URLs: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ExportTabsToTextFile()
        {
            try
            {
                List<string> urls = new List<string>();
                for (int i = 0; i < _tabs.TabPages.Count; i++)
                {
                    BrowserTab tab = _tabs.TabPages[i].Tag as BrowserTab;
                    if (tab != null)
                    {
                        string url = GetTabUrl(tab);
                        if (!string.IsNullOrWhiteSpace(url))
                        {
                            urls.Add(url);
                        }
                    }
                }

                if (urls.Count == 0)
                {
                    MessageBox.Show(this, "No hay pestañas abiertas para exportar.", "Exportar pestañas", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                using (SaveFileDialog dialog = new SaveFileDialog())
                {
                    dialog.Filter = "Archivos de texto (*.txt)|*.txt|Todos los archivos (*.*)|*.*";
                    dialog.Title = "Exportar pestañas abiertas";
                    dialog.FileName = "pestañas_abiertas.txt";

                    if (dialog.ShowDialog(this) == DialogResult.OK)
                    {
                        File.WriteAllLines(dialog.FileName, urls, Encoding.UTF8);
                        _status.Text = "Se exportaron " + urls.Count + " pestañas exitosamente.";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Error al exportar las pestañas: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void ImportTabsFromTextFile()
        {
            try
            {
                using (OpenFileDialog dialog = new OpenFileDialog())
                {
                    dialog.Filter = "Archivos de texto (*.txt)|*.txt|Todos los archivos (*.*)|*.*";
                    dialog.Title = "Importar pestañas desde archivo";

                    if (dialog.ShowDialog(this) == DialogResult.OK)
                    {
                        string[] lines = File.ReadAllLines(dialog.FileName, Encoding.UTF8);
                        int count = 0;
                        foreach (string line in lines)
                        {
                            if (string.IsNullOrWhiteSpace(line))
                            {
                                continue;
                            }

                            string url = line.Trim();
                            if (url.IndexOf("://", StringComparison.Ordinal) < 0 && !url.StartsWith("about:", StringComparison.Ordinal) && !url.StartsWith("gxlight:", StringComparison.Ordinal))
                            {
                                url = "https://" + url;
                            }

                            Uri pageUri;
                            if (Uri.TryCreate(url, UriKind.Absolute, out pageUri))
                            {
                                await CreateTabAsync(url);
                                count++;
                            }
                        }

                        if (count > 0)
                        {
                            _status.Text = "Se importaron e iniciaron " + count + " pestañas.";
                        }
                        else
                        {
                            MessageBox.Show(this, "No se encontraron URLs válidas en el archivo.", "Importar pestañas", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Error al importar las pestañas: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ImportBookmarks()
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Title = "Importar bookmarks";
                dialog.Filter = "Bookmarks HTML (*.html;*.htm)|*.html;*.htm|All files (*.*)|*.*";
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                try
                {
                    int added = ImportBookmarksFromHtml(File.ReadAllText(dialog.FileName, Encoding.UTF8));
                    SaveBookmarks();
                    RebuildBookmarksBar();
                    MessageBox.Show(this, "Bookmarks importados: " + added, "Bookmarks");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ex.Message, "No se pudieron importar bookmarks");
                }
            }
        }

        private void ExportBookmarks()
        {
            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                dialog.Title = "Exportar bookmarks";
                dialog.Filter = "Bookmarks HTML (*.html)|*.html";
                dialog.FileName = "gx-light-bookmarks.html";
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                try
                {
                    File.WriteAllText(dialog.FileName, BuildBookmarksHtml(), Encoding.UTF8);
                    MessageBox.Show(this, "Bookmarks exportados.", "Bookmarks");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ex.Message, "No se pudieron exportar bookmarks");
                }
            }
        }

        private int ImportBookmarksFromHtml(string html)
        {
            int added = 0;
            Stack<string> folders = new Stack<string>();
            string pendingFolder = null;

            Regex folderRegex = new Regex("<H3[^>]*>(?<title>.*?)</H3>", RegexOptions.IgnoreCase);
            Regex linkRegex = new Regex("<A\\s+[^>]*HREF\\s*=\\s*(\"|')(?<url>.*?)\\1[^>]*>(?<title>.*?)</A>", RegexOptions.IgnoreCase);
            string[] lines = html.Replace("\r", string.Empty).Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;

                Match folderMatch = folderRegex.Match(line);
                if (folderMatch.Success)
                {
                    pendingFolder = CleanImportedText(folderMatch.Groups["title"].Value);
                }

                if (line.IndexOf("<DL", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    string folderToPush;
                    if (!string.IsNullOrWhiteSpace(pendingFolder))
                    {
                        folderToPush = JoinBookmarkFolder(folders.Count == 0 ? string.Empty : folders.Peek(), pendingFolder);
                    }
                    else
                    {
                        folderToPush = folders.Count == 0 ? "Favorites bar" : folders.Peek();
                    }
                    folders.Push(folderToPush);
                    pendingFolder = null;
                }

                if (line.IndexOf("</DL", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    if (folders.Count > 0)
                    {
                        folders.Pop();
                    }
                }

                Match linkMatch = linkRegex.Match(line);
                if (linkMatch.Success)
                {
                    string url = WebUtility.HtmlDecode(linkMatch.Groups["url"].Value);
                    if (string.IsNullOrWhiteSpace(url) || BookmarkExists(url))
                    {
                        continue;
                    }

                    BookmarkEntry entry = new BookmarkEntry();
                    entry.Url = url;
                    entry.Title = CleanImportedText(linkMatch.Groups["title"].Value);
                    entry.Folder = folders.Count > 0 ? folders.Peek() : "Imported";
                    entry.CreatedUtc = DateTime.UtcNow;
                    _bookmarks.Add(entry);
                    added++;
                }
            }

            return added;
        }

        private static string JoinBookmarkFolder(string parent, string child)
        {
            string cleanChild = string.IsNullOrWhiteSpace(child) ? "Imported" : child.Trim();
            if (string.IsNullOrWhiteSpace(parent) || string.Equals(parent, "Favorites bar", StringComparison.OrdinalIgnoreCase))
            {
                return cleanChild;
            }

            return parent.Trim() + " / " + cleanChild;
        }

        private string BuildBookmarksHtml()
        {
            StringBuilder html = new StringBuilder();
            html.AppendLine("<!DOCTYPE NETSCAPE-Bookmark-file-1>");
            html.AppendLine("<META HTTP-EQUIV=\"Content-Type\" CONTENT=\"text/html; charset=UTF-8\">");
            html.AppendLine("<TITLE>Gan Browser Bookmarks</TITLE>");
            html.AppendLine("<H1>Gan Browser Bookmarks</H1>");
            html.AppendLine("<DL><p>");

            List<string> folders = new List<string>();
            for (int i = 0; i < _bookmarks.Count; i++)
            {
                BookmarkEntry entry = _bookmarks[i];
                string folder = string.IsNullOrWhiteSpace(entry.Folder) ? "Favorites bar" : entry.Folder;
                if (!folders.Contains(folder))
                {
                    folders.Add(folder);
                }
            }

            for (int f = 0; f < folders.Count; f++)
            {
                string folder = folders[f];
                html.Append("    <DT><H3>").Append(EscapeHtml(folder)).AppendLine("</H3>");
                html.AppendLine("    <DL><p>");
                for (int i = 0; i < _bookmarks.Count; i++)
                {
                    BookmarkEntry entry = _bookmarks[i];
                    string entryFolder = string.IsNullOrWhiteSpace(entry.Folder) ? "Favorites bar" : entry.Folder;
                    if (!string.Equals(entryFolder, folder, StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(entry.Url))
                    {
                        continue;
                    }

                    long unix = (long)(entry.CreatedUtc - new DateTime(1970, 1, 1)).TotalSeconds;
                    html.Append("        <DT><A HREF=\"").Append(EscapeHtml(entry.Url)).Append("\" ADD_DATE=\"")
                        .Append(unix).Append("\">").Append(EscapeHtml(entry.Title)).AppendLine("</A>");
                }
                html.AppendLine("    </DL><p>");
            }
            html.AppendLine("</DL><p>");
            return html.ToString();
        }

        private bool BookmarkExists(string url)
        {
            for (int i = 0; i < _bookmarks.Count; i++)
            {
                if (string.Equals(_bookmarks[i].Url, url, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        private void LoadPasswordVault()
        {
            _passwordVault.Clear();
            if (!File.Exists(AppPaths.PasswordVault))
            {
                return;
            }

            string[] lines = File.ReadAllLines(AppPaths.PasswordVault, Encoding.UTF8);
            for (int i = 0; i < lines.Length; i++)
            {
                try
                {
                    byte[] protectedBytes = Convert.FromBase64String(lines[i]);
                    byte[] clearBytes = ProtectedData.Unprotect(protectedBytes, null, DataProtectionScope.CurrentUser);
                    string csv = Encoding.UTF8.GetString(clearBytes);
                    string[] parts = SplitCsvLine(csv);
                    if (parts.Length >= 4)
                    {
                        PasswordVaultEntry entry = new PasswordVaultEntry();
                        entry.Name = parts[0];
                        entry.Url = parts[1];
                        entry.Username = parts[2];
                        entry.SetPassword(parts[3]);
                        entry.Note = parts.Length > 4 ? parts[4] : string.Empty;
                        entry.ImportedUtc = parts.Length > 5 ? new DateTime(ParseLong(parts[5], DateTime.UtcNow.Ticks), DateTimeKind.Utc) : DateTime.UtcNow;
                        _passwordVault.Add(entry);
                    }
                }
                catch
                {
                }
            }
        }

        private void SavePasswordVault()
        {
            AppPaths.Ensure();
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < _passwordVault.Count; i++)
            {
                PasswordVaultEntry entry = _passwordVault[i];
                string csv = CsvEscape(entry.Name) + "," + CsvEscape(entry.Url) + "," + CsvEscape(entry.Username) + "," +
                    CsvEscape(entry.RevealPassword()) + "," + CsvEscape(entry.Note) + "," + entry.ImportedUtc.Ticks.ToString();
                byte[] clearBytes = Encoding.UTF8.GetBytes(csv);
                try
                {
                    byte[] protectedBytes = ProtectedData.Protect(clearBytes, null, DataProtectionScope.CurrentUser);
                    builder.AppendLine(Convert.ToBase64String(protectedBytes));
                }
                finally
                {
                    Array.Clear(clearBytes, 0, clearBytes.Length);
                }
            }
            File.WriteAllText(AppPaths.PasswordVault, builder.ToString(), Encoding.UTF8);
        }

        private void ImportPasswords()
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Title = "Importar contraseñas CSV/TXT";
                dialog.Filter = "CSV o TXT (*.csv;*.txt)|*.csv;*.txt|CSV files (*.csv)|*.csv|Text files (*.txt)|*.txt|All files (*.*)|*.*";
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                try
                {
                    string csv = File.ReadAllText(dialog.FileName, Encoding.UTF8);
                    PasswordCsvImportResult import = PasswordCsvImporter.Parse(csv);
                    int added = 0;
                    int duplicates = 0;
                    for (int i = 0; i < import.Entries.Count; i++)
                    {
                        PasswordVaultEntry entry = import.Entries[i];
                        if (PasswordVaultContains(entry))
                        {
                            duplicates++;
                            continue;
                        }
                        _passwordVault.Add(entry);
                        added++;
                    }

                    SavePasswordVault();
                    MessageBox.Show(this, "Contraseñas importadas a la boveda local: " + added + Environment.NewLine +
                        "Duplicadas omitidas: " + duplicates + Environment.NewLine +
                        "Filas omitidas por datos incompletos: " + import.SkippedRows + Environment.NewLine +
                        "Quedan cifradas con Windows DPAPI para este usuario y listas para el rellenado automatico.", "Contraseñas");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ex.Message, "No se pudieron importar passwords");
                }
            }
        }

        private async void ExportPasswords()
        {
            if (!await VerifyVaultAccessAsync("Exportar todas las contraseñas de Gan Browser"))
            {
                return;
            }

            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                dialog.Title = "Exportar passwords CSV";
                dialog.Filter = "CSV files (*.csv)|*.csv";
                dialog.FileName = "gx-light-passwords.csv";
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                try
                {
                    StringBuilder csv = new StringBuilder();
                    csv.AppendLine("name,url,username,password,note");
                    for (int i = 0; i < _passwordVault.Count; i++)
                    {
                        PasswordVaultEntry entry = _passwordVault[i];
                        csv.Append(CsvEscape(entry.Name)).Append(',')
                            .Append(CsvEscape(entry.Url)).Append(',')
                            .Append(CsvEscape(entry.Username)).Append(',')
                            .Append(CsvEscape(entry.RevealPassword())).Append(',')
                            .Append(CsvEscape(entry.Note)).AppendLine();
                    }
                    File.WriteAllText(dialog.FileName, csv.ToString(), Encoding.UTF8);
                    MessageBox.Show(this, "Passwords exportadas. Este CSV contiene secretos en texto visible.", "Passwords");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ex.Message, "No se pudieron exportar passwords");
                }
            }
        }

        private void ExportPasswordTemplate()
        {
            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                dialog.Title = "Exportar plantilla CSV";
                dialog.Filter = "CSV files (*.csv)|*.csv";
                dialog.FileName = "gx-light-password-template.csv";
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                File.WriteAllText(dialog.FileName, "name,url,username,password,note" + Environment.NewLine, Encoding.UTF8);
                MessageBox.Show(this, "Plantilla CSV exportada.", "Passwords");
            }
        }

        private async Task ViewVaultCredentialAsync(int index)
        {
            if (index < 0 || index >= _passwordVault.Count)
            {
                return;
            }

            if (!await VerifyVaultAccessAsync("Ver una contraseña guardada en Gan Browser"))
            {
                return;
            }

            using (CredentialViewerDialog dialog = new CredentialViewerDialog(_passwordVault[index]))
            {
                dialog.ShowDialog(this);
            }
        }

        private bool PasswordVaultContains(PasswordVaultEntry candidate)
        {
            for (int i = 0; i < _passwordVault.Count; i++)
            {
                PasswordVaultEntry existing = _passwordVault[i];
                if (string.Equals(existing.Url, candidate.Url, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(existing.Username, candidate.Username, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(existing.RevealPassword(), candidate.RevealPassword(), StringComparison.Ordinal))
                {
                    return true;
                }
            }
            return false;
        }

        private async Task FillCurrentSiteFromVaultAsync()
        {
            WebView2 web = ActiveWebView();
            if (web == null || web.CoreWebView2 == null || web.Source == null ||
                (web.Source.Scheme != Uri.UriSchemeHttp && web.Source.Scheme != Uri.UriSchemeHttps))
            {
                MessageBox.Show(this, "Abre primero el formulario de inicio de sesión que deseas rellenar.", "Bóveda de contraseñas");
                return;
            }

            List<PasswordVaultEntry> matches = VaultEntriesForHost(web.Source.Host);
            if (matches.Count == 0)
            {
                MessageBox.Show(this, "No hay credenciales de la bóveda para " + web.Source.Host + ".", "Bóveda de contraseñas");
                return;
            }

            PasswordVaultEntry selected = SelectVaultEntry(matches, web.Source.Host);
            if (selected == null || !await VerifyVaultAccessAsync("Rellenar credenciales en " + web.Source.Host))
            {
                return;
            }

            string username = Convert.ToBase64String(Encoding.UTF8.GetBytes(selected.Username ?? string.Empty));
            string password = Convert.ToBase64String(Encoding.UTF8.GetBytes(selected.RevealPassword()));
            string script = PasswordVaultSecurity.BuildFillScript(username, password);
            string result = await web.CoreWebView2.ExecuteScriptAsync(script);
            if (string.Equals(result, "\"filled\"", StringComparison.Ordinal))
            {
                MessageBox.Show(this, "Usuario y contraseña rellenados. Revisa el formulario antes de iniciar sesión.", "Bóveda de contraseñas");
            }
            else
            {
                MessageBox.Show(this, "No se encontraron campos compatibles de usuario y contraseña en esta página.", "Bóveda de contraseñas");
            }
        }

        private async Task FillVaultCredentialFromPageAsync(CoreWebView2 core, string source, int matchIndex)
        {
            WebView2 web = WebViewForCore(core);
            Uri sourceUri;
            if (web == null || web != ActiveWebView() || !IsActiveWindow() ||
                web.Source == null || !Uri.TryCreate(source, UriKind.Absolute, out sourceUri) ||
                !string.Equals(sourceUri.Host, web.Source.Host, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            List<PasswordVaultEntry> matches = VaultEntriesForHost(web.Source.Host);
            if (matchIndex < 0 || matchIndex >= matches.Count ||
                !await VerifyVaultAccessAsync("Rellenar contraseña en " + web.Source.Host))
            {
                return;
            }

            PasswordVaultEntry selected = matches[matchIndex];
            string username = Convert.ToBase64String(Encoding.UTF8.GetBytes(selected.Username ?? string.Empty));
            string password = Convert.ToBase64String(Encoding.UTF8.GetBytes(selected.RevealPassword()));
            string result = await core.ExecuteScriptAsync(PasswordVaultSecurity.BuildFillScript(username, password));
            if (!string.Equals(result, "\"filled\"", StringComparison.Ordinal))
            {
                MessageBox.Show(this, "El formulario cambió y no se pudo completar la contraseña.", "Bóveda de contraseñas");
            }
        }

        private async Task InstallVaultAssistAsync(CoreWebView2 core, WebView2 web)
        {
            if (core == null || web == null || web.Source == null ||
                (web.Source.Scheme != Uri.UriSchemeHttp && web.Source.Scheme != Uri.UriSchemeHttps))
            {
                return;
            }

            List<PasswordVaultEntry> matches = VaultEntriesForHost(web.Source.Host);
            if (matches.Count > 0)
            {
                await core.ExecuteScriptAsync(PasswordVaultSecurity.BuildAssistScript(matches));
            }
        }

        private async Task<bool> VerifyVaultAccessAsync(string reason)
        {
            bool verified = await WindowsHelloVerifier.VerifyAsync(reason);
            if (!verified)
            {
                MessageBox.Show(this, "Windows Hello/PIN no verificó tu identidad. La bóveda permanece bloqueada.", "Bóveda de contraseñas");
            }
            return verified;
        }

        private List<PasswordVaultEntry> VaultEntriesForHost(string host)
        {
            List<PasswordVaultEntry> matches = new List<PasswordVaultEntry>();
            for (int i = 0; i < _passwordVault.Count; i++)
            {
                if (PasswordVaultSecurity.MatchesExactHost(_passwordVault[i].Url, host))
                {
                    matches.Add(_passwordVault[i]);
                }
            }
            return matches;
        }

        private PasswordVaultEntry SelectVaultEntry(List<PasswordVaultEntry> matches, string host)
        {
            if (matches.Count == 1)
            {
                return matches[0];
            }

            using (CredentialSelectionDialog dialog = new CredentialSelectionDialog(matches, host))
            {
                return dialog.ShowDialog(this) == DialogResult.OK ? dialog.SelectedEntry : null;
            }
        }

        private static bool IsPasswordHeader(string[] parts)
        {
            return parts.Length >= 4 &&
                string.Equals(parts[0], "name", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(parts[1], "url", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(parts[2], "username", StringComparison.OrdinalIgnoreCase);
        }

        private void RebuildTabStrip()
        {
            _tabStrip.SuspendLayout();
            for (int i = _tabStrip.Controls.Count - 1; i >= 0; i--)
            {
                Control previousControl = _tabStrip.Controls[i];
                _tabStrip.Controls.RemoveAt(i);
                if (previousControl != _tabStripNewTab)
                {
                    previousControl.Dispose();
                }
            }

            int width = CalculateTabWidth();
            HashSet<int> renderedIslandBars = new HashSet<int>();
            for (int i = 0; i < _tabs.TabPages.Count; i++)
            {
                TabPage page = _tabs.TabPages[i];
                BrowserTab browserTab = page.Tag as BrowserTab;
                if (browserTab != null && browserTab.IslandId > 0 && renderedIslandBars.Add(browserTab.IslandId))
                {
                    AddIslandToggleButton(browserTab.IslandId);
                }
                if (browserTab != null && browserTab.IslandId > 0 && _collapsedIslands.Contains(browserTab.IslandId))
                {
                    continue;
                }
                ChromeButton tab = new ChromeButton();
                tab.Text = Trim(page.Text, Width < 900 ? 16 : 24);
                tab.Width = browserTab != null && browserTab.IsCompact ? 38 : width;
                tab.Height = 24;
                tab.Margin = new Padding(0, 1, 6, 1);
                tab.IsSelected = page == _tabs.SelectedTab;
                tab.IconOnly = _appSettings.CompactIconTabs || (browserTab != null && browserTab.IsCompact) || tab.Width <= 58;
                tab.ShowCloseGlyph = !tab.IconOnly && width >= 86;
                tab.IconImage = _appSettings.ShowPageIcons && browserTab != null ? browserTab.Favicon : null;
                tab.ShowIconPlaceholder = _appSettings.ShowPageIcons && browserTab != null && browserTab.Favicon == null;
                tab.IconPlaceholderColor = browserTab == null ? Theme.Muted : ColorFromText(GetTabUrl(browserTab));
                tab.IsMultiSelected = browserTab != null && browserTab.IsSelectedForIsland;
                if (browserTab != null && browserTab.IsSelectedForIsland)
                {
                    tab.Accent = Theme.Warning;
                }
                if (browserTab != null && browserTab.IslandId > 0)
                {
                    tab.ShowIslandStripe = true;
                    tab.IslandColor = GetIslandColor(browserTab);
                    tab.Accent = tab.IslandColor;
                }
                WireTabDragAndDrop(tab, browserTab);
                int index = i;
                tab.MouseUp += delegate(object sender, MouseEventArgs e)
                {
                    if (e.Button == MouseButtons.Middle || (e.Button == MouseButtons.Left && tab.IsCloseHit(e.Location)))
                    {
                        CloseTab(page);
                        return;
                    }

                    if (e.Button == MouseButtons.Left && index < _tabs.TabPages.Count)
                    {
                        if ((ModifierKeys & Keys.Shift) == Keys.Shift && browserTab != null)
                        {
                            SelectTabRange(_lastClickedTabIndex < 0 ? index : _lastClickedTabIndex, index);
                            RebuildTabStrip();
                            return;
                        }
                        if ((ModifierKeys & Keys.Control) == Keys.Control && browserTab != null)
                        {
                            browserTab.IsSelectedForIsland = !browserTab.IsSelectedForIsland;
                            _lastClickedTabIndex = index;
                            RebuildTabStrip();
                            return;
                        }

                        _lastClickedTabIndex = index;
                        _tabs.SelectedIndex = index;
                        return;
                    }

                    if (e.Button == MouseButtons.Right)
                    {
                        ShowTabContextMenu(page, tab);
                    }
                };
                _tips.SetToolTip(tab, page.Text);
                _tabStrip.Controls.Add(tab);
            }

            ConfigureButton(_tabStripNewTab, "+", 34, "Nueva pestana");
            _tabStripNewTab.Height = 24;
            _tabStripNewTab.Margin = new Padding(0, 1, 6, 1);
            _tabStrip.Controls.Add(_tabStripNewTab);

            _tabStrip.ResumeLayout();
        }

        private void AddIslandToggleButton(int islandId)
        {
            int count = 0;
            Color color = Theme.Accent;
            for (int i = 0; i < _tabs.TabPages.Count; i++)
            {
                BrowserTab member = _tabs.TabPages[i].Tag as BrowserTab;
                if (member != null && member.IslandId == islandId)
                {
                    count++;
                    color = GetIslandColor(member);
                }
            }

            ChromeButton island = new ChromeButton();
            island.Text = string.Empty;
            island.Width = Math.Min(40, 14 + Math.Min(5, count) * 6);
            island.Height = 24;
            island.Margin = new Padding(0, 1, 6, 1);
            island.IsIslandToggle = true;
            island.IslandMemberCount = count;
            island.IslandColor = color;
            island.Accent = color;
            island.AllowDrop = true;
            island.DragEnter += delegate(object sender, DragEventArgs e)
            {
                e.Effect = e.Data.GetDataPresent(typeof(BrowserTab)) ? DragDropEffects.Move : DragDropEffects.None;
            };
            island.DragDrop += delegate(object sender, DragEventArgs e)
            {
                BrowserTab dragged = e.Data.GetData(typeof(BrowserTab)) as BrowserTab;
                AddTabToIsland(dragged, islandId);
            };
            island.MouseUp += delegate(object sender, MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Left)
                {
                    ToggleIslandCollapsed(islandId);
                }
                else if (e.Button == MouseButtons.Right)
                {
                    ShowIslandContextMenu(islandId, island);
                }
            };
            _tips.SetToolTip(island, "Isla con " + count + " pestanas. Clic para " +
                (_collapsedIslands.Contains(islandId) ? "desplegar" : "colapsar") + ". Arrastra pestanas aqui.");
            _tabStrip.Controls.Add(island);
        }

        private void WireTabDragAndDrop(ChromeButton button, BrowserTab tab)
        {
            if (tab == null) return;

            Point dragStart = Point.Empty;
            button.AllowDrop = true;
            button.MouseDown += delegate(object sender, MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Left) dragStart = e.Location;
            };
            button.MouseMove += delegate(object sender, MouseEventArgs e)
            {
                if (e.Button != MouseButtons.Left || dragStart == Point.Empty) return;
                Rectangle dragBounds = new Rectangle(
                    dragStart.X - SystemInformation.DragSize.Width / 2,
                    dragStart.Y - SystemInformation.DragSize.Height / 2,
                    SystemInformation.DragSize.Width,
                    SystemInformation.DragSize.Height);
                if (!dragBounds.Contains(e.Location))
                {
                    dragStart = Point.Empty;
                    button.DoDragDrop(tab, DragDropEffects.Move);
                }
            };
            button.DragEnter += delegate(object sender, DragEventArgs e)
            {
                e.Effect = e.Data.GetDataPresent(typeof(BrowserTab)) ? DragDropEffects.Move : DragDropEffects.None;
            };
            button.DragDrop += delegate(object sender, DragEventArgs e)
            {
                BrowserTab dragged = e.Data.GetData(typeof(BrowserTab)) as BrowserTab;
                if (dragged == null || dragged == tab) return;
                if (tab.IslandId > 0)
                {
                    AddTabToIsland(dragged, tab.IslandId);
                }
                else
                {
                    CreateIslandFromTabs(dragged, tab);
                }
            };
        }

        private void AddTabToIsland(BrowserTab tab, int islandId)
        {
            if (tab == null || islandId <= 0) return;
            tab.IslandId = islandId;
            tab.IsSelectedForIsland = false;
            SaveSession();
            RebuildTabStrip();
        }

        private void CreateIslandFromTabs(BrowserTab first, BrowserTab second)
        {
            if (first == null || second == null || first == second) return;
            int islandId = _nextIslandId++;
            _islandColors[islandId] = ColorFromText(GetTabUrl(second));
            first.IslandId = islandId;
            second.IslandId = islandId;
            first.IsSelectedForIsland = false;
            second.IsSelectedForIsland = false;
            SaveSession();
            RebuildTabStrip();
        }

        private void SelectTabRange(int from, int to)
        {
            int start = Math.Max(0, Math.Min(from, to));
            int end = Math.Min(_tabs.TabPages.Count - 1, Math.Max(from, to));
            for (int i = start; i <= end; i++)
            {
                BrowserTab tab = _tabs.TabPages[i].Tag as BrowserTab;
                if (tab != null) tab.IsSelectedForIsland = true;
            }
            _lastClickedTabIndex = to;
        }

        private void ShowTabContextMenu(TabPage page, Control owner)
        {
            BrowserTab tab = page.Tag as BrowserTab;
            if (tab == null)
            {
                return;
            }

            ContextMenuStrip menu = CreateContextMenu();
            menu.Items.Add("New tab                                      Ctrl+T", null, async delegate { await CreateTabAsync(HomeUrl); });
            int selectedCount = SelectedTabsOr(null).Count;
            ToolStripMenuItem selectionInfo = new ToolStripMenuItem("Selected tabs: " + selectedCount);
            selectionInfo.Enabled = false;
            menu.Items.Add(selectionInfo);
            menu.Items.Add("Create tab island from selected             Alt+T", null, delegate { CreateIslandFromSelection(tab); });
            menu.Items.Add(tab.IsSelectedForIsland ? "Deselect tab" : "Select tab", null, delegate
            {
                tab.IsSelectedForIsland = !tab.IsSelectedForIsland;
                RebuildTabStrip();
            });
            menu.Items.Add("Clear tab selection", null, delegate { ClearTabSelection(); });
            ToolStripMenuItem tabSize = new ToolStripMenuItem("Tab size");
            AddTabWidthMenuItem(tabSize, "Automatic", 0);
            AddTabWidthMenuItem(tabSize, "Small", 92);
            AddTabWidthMenuItem(tabSize, "Medium", 140);
            AddTabWidthMenuItem(tabSize, "Large", 190);
            menu.Items.Add(tabSize);
            ToolStripMenuItem compact = new ToolStripMenuItem(tab.IsCompact ? "Expand this tab" : "Collapse this tab");
            compact.Checked = tab.IsCompact;
            compact.Click += delegate
            {
                tab.IsCompact = !tab.IsCompact;
                if (tab.IsCompact) _appSettings.ShowPageIcons = true;
                _appSettings.Save();
                SaveSession();
                RebuildTabStrip();
            };
            menu.Items.Add(compact);
            menu.Items.Add("Collapse selected tabs", null, delegate { SetSelectedTabsCompact(tab, true); });
            menu.Items.Add("Expand selected tabs", null, delegate { SetSelectedTabsCompact(tab, false); });
            ToolStripMenuItem compactAll = new ToolStripMenuItem("Compact all tabs");
            compactAll.Checked = _appSettings.CompactIconTabs;
            compactAll.Click += delegate
            {
                _appSettings.CompactIconTabs = !_appSettings.CompactIconTabs;
                if (_appSettings.CompactIconTabs) _appSettings.ShowPageIcons = true;
                _appSettings.Save();
                RebuildTabStrip();
            };
            menu.Items.Add(compactAll);
            if (tab.IslandId > 0)
            {
                menu.Items.Add("Add selected tabs to this island", null, delegate { AddSelectedTabsToIsland(tab.IslandId); });
                menu.Items.Add(_collapsedIslands.Contains(tab.IslandId) ? "Expand tab island" : "Collapse tab island", null,
                    delegate { ToggleIslandCollapsed(tab.IslandId); });
                menu.Items.Add("Remove tabs from island", null, delegate { DissolveIsland(tab.IslandId); });
            }
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Reload selected tabs", null, async delegate { await ReloadSelectedTabsAsync(tab); });
            menu.Items.Add("Copy page addresses", null, delegate { CopySelectedTabAddresses(tab); });
            menu.Items.Add("Duplicate selected tabs", null, async delegate { await DuplicateSelectedTabsAsync(tab); });
            menu.Items.Add("Suspend selected tabs", null, delegate { SuspendSelectedTabs(tab); });
            menu.Items.Add(tab.IsPinned ? "Unpin tab" : "Pin tab", null, delegate
            {
                tab.IsPinned = !tab.IsPinned;
                page.Text = tab.IsPinned ? "[Pinned] " + Trim(GetTabTitle(tab), 18) : GetTabTitle(tab);
                RebuildTabStrip();
            });
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Close selected tabs                          Ctrl+W", null, delegate { CloseSelectedTabs(tab); });
            menu.Items.Add("Close tabs to the right", null, delegate { CloseTabsToRight(page); });
            menu.Show(owner, new Point(0, owner.Height + 4));
        }

        private void ShowIslandContextMenu(int islandId, Control owner)
        {
            ContextMenuStrip menu = CreateContextMenu();
            menu.Items.Add("Add selected tabs to island", null, delegate { AddSelectedTabsToIsland(islandId); });
            menu.Items.Add("Expand tab island", null, delegate { ToggleIslandCollapsed(islandId); });
            menu.Items.Add("Remove tabs from island", null, delegate { DissolveIsland(islandId); });
            menu.Show(owner, new Point(0, owner.Height + 4));
        }

        private void ToggleIslandCollapsed(int islandId)
        {
            if (!_collapsedIslands.Add(islandId)) _collapsedIslands.Remove(islandId);
            SaveSession();
            RebuildTabStrip();
        }

        private void DissolveIsland(int islandId)
        {
            for (int i = 0; i < _tabs.TabPages.Count; i++)
            {
                BrowserTab tab = _tabs.TabPages[i].Tag as BrowserTab;
                if (tab != null && tab.IslandId == islandId) tab.IslandId = 0;
            }
            _collapsedIslands.Remove(islandId);
            _islandColors.Remove(islandId);
            SaveSession();
            RebuildTabStrip();
        }

        private void AddSelectedTabsToIsland(int islandId)
        {
            List<BrowserTab> selected = SelectedTabsOr(null);
            for (int i = 0; i < selected.Count; i++)
            {
                selected[i].IslandId = islandId;
                selected[i].IsSelectedForIsland = false;
            }
            SaveSession();
            RebuildTabStrip();
        }

        private List<BrowserTab> SelectedTabsOr(BrowserTab fallback)
        {
            List<BrowserTab> selected = new List<BrowserTab>();
            for (int i = 0; i < _tabs.TabPages.Count; i++)
            {
                BrowserTab tab = _tabs.TabPages[i].Tag as BrowserTab;
                if (tab != null && tab.IsSelectedForIsland)
                {
                    selected.Add(tab);
                }
            }

            if (selected.Count == 0 && fallback != null)
            {
                selected.Add(fallback);
            }

            return selected;
        }

        private void CreateIslandFromSelection(BrowserTab fallback)
        {
            List<BrowserTab> selected = SelectedTabsOr(null);
            if (selected.Count < 2)
            {
                MessageBox.Show(this,
                    "Selecciona al menos dos pestanas con Ctrl+clic o selecciona un rango con Shift+clic.",
                    "Crear isla de pestanas", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int islandId = _nextIslandId++;
            Color color = ColorFromText(GetTabUrl(selected[0]));
            _islandColors[islandId] = color;
            for (int i = 0; i < selected.Count; i++)
            {
                selected[i].IslandId = islandId;
                selected[i].IsSelectedForIsland = false;
            }
            _activeIslandId = islandId;
            _collapsedIslands.Add(islandId);
            _activeIslandId = 0;
            SaveSession();
            RebuildTabStrip();
        }

        private async Task ReloadSelectedTabsAsync(BrowserTab fallback)
        {
            List<BrowserTab> selected = SelectedTabsOr(fallback);
            for (int i = 0; i < selected.Count; i++)
            {
                BrowserTab tab = selected[i];
                if (tab.IsSuspended)
                {
                    await RestoreSuspendedTabAsync(tab);
                }
                else if (tab.WebView != null)
                {
                    tab.WebView.Reload();
                }
            }
        }

        private async Task DuplicateSelectedTabsAsync(BrowserTab fallback)
        {
            List<BrowserTab> selected = SelectedTabsOr(fallback);
            for (int i = 0; i < selected.Count; i++)
            {
                await CreateTabAsync(GetTabUrl(selected[i]));
            }
        }

        private void SuspendSelectedTabs(BrowserTab fallback)
        {
            List<BrowserTab> selected = SelectedTabsOr(fallback);
            for (int i = 0; i < selected.Count; i++)
            {
                BrowserTab tab = selected[i];
                if (tab != null && !tab.IsSuspended && tab.WebView != null)
                {
                    SuspendTab(tab);
                }
            }
        }

        private void SetSelectedTabsCompact(BrowserTab fallback, bool compact)
        {
            List<BrowserTab> selected = SelectedTabsOr(fallback);
            for (int i = 0; i < selected.Count; i++)
            {
                selected[i].IsCompact = compact;
            }
            if (compact)
            {
                _appSettings.ShowPageIcons = true;
                _appSettings.Save();
            }
            SaveSession();
            RebuildTabStrip();
        }

        private void CopySelectedTabAddresses(BrowserTab fallback)
        {
            List<BrowserTab> selected = SelectedTabsOr(fallback);
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < selected.Count; i++)
            {
                builder.AppendLine(GetTabUrl(selected[i]));
            }

            if (builder.Length > 0)
            {
                Clipboard.SetText(builder.ToString());
            }
        }

        private void CloseSelectedTabs(BrowserTab fallback)
        {
            List<BrowserTab> selected = SelectedTabsOr(fallback);
            for (int i = 0; i < selected.Count; i++)
            {
                if (selected[i].Page != null && _tabs.TabPages.Contains(selected[i].Page))
                {
                    CloseTab(selected[i].Page);
                }
            }
        }

        private void CloseTabsToRight(TabPage page)
        {
            int index = _tabs.TabPages.IndexOf(page);
            if (index < 0)
            {
                return;
            }

            List<TabPage> toClose = new List<TabPage>();
            for (int i = index + 1; i < _tabs.TabPages.Count; i++)
            {
                toClose.Add(_tabs.TabPages[i]);
            }

            for (int i = 0; i < toClose.Count; i++)
            {
                CloseTab(toClose[i]);
            }
        }

        private void ClearTabSelection()
        {
            for (int i = 0; i < _tabs.TabPages.Count; i++)
            {
                BrowserTab tab = _tabs.TabPages[i].Tag as BrowserTab;
                if (tab != null)
                {
                    tab.IsSelectedForIsland = false;
                }
            }
            RebuildTabStrip();
        }

        private void CloseTab(TabPage page)
        {
            if (page == null)
            {
                return;
            }

            if (_tabs.TabPages.Count <= 1)
            {
                BrowserTab only = page.Tag as BrowserTab;
                if (only != null)
                {
                    only.BlockedRequests = 0;
                    page.Text = "Nueva pestana";
                    if (only.WebView != null)
                    {
                        Navigate(only.WebView, HomeUrl);
                    }
                    else
                    {
                        only.SuspendedTitle = "Nueva pestana";
                        only.SuspendedUrl = HomeUrl;
                    }
                    RebuildTabStrip();
                    UpdateStatus();
                }
                return;
            }

            int closedIndex = _tabs.TabPages.IndexOf(page);
            BrowserTab tab = page.Tag as BrowserTab;
            int nextIndex = closedIndex < _tabs.TabPages.Count - 1 ? closedIndex + 1 : closedIndex - 1;
            TabPage nextPage = nextIndex >= 0 ? _tabs.TabPages[nextIndex] : null;

            if (nextPage != null)
            {
                _tabs.SelectedTab = nextPage;
                BrowserTab nextTab = nextPage.Tag as BrowserTab;
                if (nextTab != null && nextTab.WebView != null)
                {
                    nextTab.WebView.BringToFront();
                    nextTab.WebView.Focus();
                }
            }
            page.Visible = false;
            BeginInvoke((MethodInvoker)delegate { DisposeClosedTab(page, tab); });

            ApplyTabResourcePolicy();
            RebuildTabStrip();
            SyncAddress();
            UpdateStatus();
            SaveSession();
        }

        private void DisposeClosedTab(TabPage page, BrowserTab tab)
        {
            if (page == null || page.IsDisposed)
            {
                return;
            }

            _tabs.TabPages.Remove(page);
            if (tab != null && tab.WebView != null)
            {
                UnsubscribeWebViewEvents(tab.WebView);
                tab.WebView.Dispose();
                tab.WebView = null;
            }
            if (tab != null && tab.Favicon != null)
            {
                tab.Favicon.Dispose();
                tab.Favicon = null;
            }
            page.Dispose();
        }

        private void ApplyTabResourcePolicy()
        {
            for (int i = 0; i < _tabs.TabPages.Count; i++)
            {
                BrowserTab tab = _tabs.TabPages[i].Tag as BrowserTab;
                if (tab == null || tab.IsSuspended || tab.WebView == null || tab.WebView.CoreWebView2 == null)
                {
                    continue;
                }

                tab.WebView.CoreWebView2.MemoryUsageTargetLevel =
                    _tabs.TabPages[i] == _tabs.SelectedTab
                        ? CoreWebView2MemoryUsageTargetLevel.Normal
                        : CoreWebView2MemoryUsageTargetLevel.Low;
            }
        }

        private void UpdateMemoryMonitor()
        {
            long totalMb = EstimateBrowserMemoryMb();
            _memoryLabel.Text = totalMb + " MB";
            _memoryLimitButton.Text = _gxControl.RamLimiterEnabled ? "Pulse " + (_gxControl.MemoryLimitMb / 1024.0).ToString("0.0") + "G" : "Pulse Off";
            _memoryLabel.ForeColor = _gxControl.RamLimiterEnabled && totalMb > _gxControl.MemoryLimitMb ? Theme.Warning : Theme.Text;
        }

        private long EstimateBrowserMemoryMb()
        {
            long total = 0;
            try
            {
                Process current = Process.GetCurrentProcess();
                total += current.WorkingSet64;

                if (_environment != null)
                {
                    try
                    {
                        var infos = _environment.GetProcessInfos();
                        if (infos != null)
                        {
                            foreach (var info in infos)
                            {
                                try
                                {
                                    using (Process p = Process.GetProcessById((int)info.ProcessId))
                                    {
                                        total += p.WorkingSet64;
                                    }
                                }
                                catch
                                {
                                }
                            }
                        }
                    }
                    catch
                    {
                    }
                }
            }
            catch
            {
                total = Process.GetCurrentProcess().WorkingSet64;
            }

            return Math.Max(1, total / (1024 * 1024));
        }

        private void EnforceMemoryLimit()
        {
            if (!_gxControl.RamLimiterEnabled)
            {
                return;
            }

            long total = EstimateBrowserMemoryMb();
            if (total <= _gxControl.MemoryLimitMb)
            {
                return;
            }

            if (SuspendOldestInactiveTab())
            {
                total -= 100; // virtual decrement to prevent OS latency cascading suspensions
            }

            if (_gxControl.HardMemoryLimit)
            {
                while (total > _gxControl.MemoryLimitMb && SuspendOldestInactiveTab())
                {
                    total -= 100; // virtual decrement
                }
            }
        }

        private void SuspendIdleTabs()
        {
            DateTime cutoff = DateTime.UtcNow.AddMinutes(-5);
            for (int i = 0; i < _tabs.TabPages.Count; i++)
            {
                BrowserTab tab = _tabs.TabPages[i].Tag as BrowserTab;
                if (tab != null && _tabs.TabPages[i] != _tabs.SelectedTab && !tab.IsSuspended && tab.LastActiveUtc < cutoff)
                {
                    if (tab.WebView != null && tab.WebView.CoreWebView2 != null && tab.WebView.CoreWebView2.IsDocumentPlayingAudio)
                    {
                        continue;
                    }
                    if (_gxControl.HotTabsKillerEnabled)
                    {
                        SuspendTab(tab);
                    }
                }
            }
        }

        private void SuspendIdleTabsNow()
        {
            for (int i = 0; i < _tabs.TabPages.Count; i++)
            {
                BrowserTab tab = _tabs.TabPages[i].Tag as BrowserTab;
                if (tab != null && _tabs.TabPages[i] != _tabs.SelectedTab && !tab.IsSuspended)
                {
                    if (tab.WebView != null && tab.WebView.CoreWebView2 != null && tab.WebView.CoreWebView2.IsDocumentPlayingAudio)
                    {
                        continue;
                    }
                    SuspendTab(tab);
                }
            }
            UpdateMemoryMonitor();
        }

        private bool SuspendOldestInactiveTab()
        {
            BrowserTab oldest = null;
            for (int i = 0; i < _tabs.TabPages.Count; i++)
            {
                BrowserTab tab = _tabs.TabPages[i].Tag as BrowserTab;
                if (tab == null || tab.IsSuspended || _tabs.TabPages[i] == _tabs.SelectedTab)
                {
                    continue;
                }

                if (tab.WebView != null && tab.WebView.CoreWebView2 != null && tab.WebView.CoreWebView2.IsDocumentPlayingAudio)
                {
                    continue;
                }

                if (oldest == null || tab.LastActiveUtc < oldest.LastActiveUtc)
                {
                    oldest = tab;
                }
            }

            if (oldest != null)
            {
                SuspendTab(oldest);
                return true;
            }

            return false;
        }

        private void SuspendTab(BrowserTab tab)
        {
            if (tab == null || tab.IsSuspended || tab.WebView == null)
            {
                return;
            }

            Logger.Info("Suspending tab: " + tab.Page.Text);
            tab.SuspendedUrl = tab.WebView.Source == null || tab.WebView.Source.ToString() == "about:blank" ? HomeUrl : tab.WebView.Source.ToString();
            tab.SuspendedTitle = tab.Page.Text.Replace("[Crashed] ", "").Replace("[Suspended] ", "").Replace("[S] ", "");
            
            UnsubscribeWebViewEvents(tab.WebView);

            tab.Page.Controls.Clear();
            tab.WebView.Dispose();
            tab.WebView = null;
            tab.IsSuspended = true;

            SetupSuspensionPlaceholder(tab);
            tab.Page.Text = "[S] " + Trim(tab.SuspendedTitle, 18);
            RebuildTabStrip();
            SaveSession();
        }

        private async Task RestoreSuspendedTabAsync(BrowserTab tab)
        {
            if (tab == null || !tab.IsSuspended)
            {
                return;
            }

            tab.Page.Controls.Clear();
            WebView2 web = new WebView2();
            web.Dock = DockStyle.Fill;
            web.DefaultBackgroundColor = Theme.Window;
            tab.Page.Controls.Add(web);
            tab.WebView = web;
            tab.IsSuspended = false;
            tab.LastActiveUtc = DateTime.UtcNow;

            await web.EnsureCoreWebView2Async(_environment);
            ConfigureWebView(tab.Page, web);
            Navigate(web, string.IsNullOrWhiteSpace(tab.SuspendedUrl) ? HomeUrl : tab.SuspendedUrl);
            tab.Page.Text = string.IsNullOrWhiteSpace(tab.SuspendedTitle) ? "Restored" : tab.SuspendedTitle;
            RebuildTabStrip();
            EnforceLowResourceLimit();
            SaveSession();
        }

        private int CalculateTabWidth()
        {
            if (_appSettings.CompactIconTabs)
            {
                return 38;
            }

            HashSet<int> islands = new HashSet<int>();
            int visibleTabs = 0;
            int islandWidth = 0;
            for (int i = 0; i < _tabs.TabPages.Count; i++)
            {
                BrowserTab tab = _tabs.TabPages[i].Tag as BrowserTab;
                if (tab == null)
                {
                    visibleTabs++;
                    continue;
                }
                if (tab.IslandId > 0 && islands.Add(tab.IslandId))
                {
                    int members = CountIslandMembers(tab.IslandId);
                    islandWidth += Math.Min(40, 14 + Math.Min(5, members) * 6) + 6;
                }
                if (tab.IslandId <= 0 || !_collapsedIslands.Contains(tab.IslandId))
                {
                    visibleTabs++;
                }
            }

            int compactTabs = 0;
            for (int i = 0; i < _tabs.TabPages.Count; i++)
            {
                BrowserTab tab = _tabs.TabPages[i].Tag as BrowserTab;
                if (tab != null && tab.IsCompact &&
                    (tab.IslandId <= 0 || !_collapsedIslands.Contains(tab.IslandId)))
                {
                    compactTabs++;
                }
            }
            visibleTabs = Math.Max(0, visibleTabs - compactTabs);
            int available = Math.Max(80, _tabStrip.Width - islandWidth - 46 - compactTabs * 44);
            int automaticWidth = (available / Math.Max(1, visibleTabs)) - 6;
            automaticWidth = Math.Max(38, Math.Min(190, automaticWidth));
            return _appSettings.TabWidth >= 80 ? Math.Min(_appSettings.TabWidth, automaticWidth) : automaticWidth;
        }

        private int CountIslandMembers(int islandId)
        {
            int count = 0;
            for (int i = 0; i < _tabs.TabPages.Count; i++)
            {
                BrowserTab tab = _tabs.TabPages[i].Tag as BrowserTab;
                if (tab != null && tab.IslandId == islandId) count++;
            }
            return count;
        }

        private BrowserTab ActiveTab()
        {
            return _tabs.SelectedTab == null ? null : _tabs.SelectedTab.Tag as BrowserTab;
        }

        private WebView2 ActiveWebView()
        {
            BrowserTab tab = ActiveTab();
            return tab == null ? null : tab.WebView;
        }

        private WebView2 WebViewForCore(CoreWebView2 core)
        {
            if (core == null)
            {
                return null;
            }

            for (int i = 0; i < _tabs.TabPages.Count; i++)
            {
                BrowserTab tab = _tabs.TabPages[i].Tag as BrowserTab;
                if (tab != null && tab.WebView != null && tab.WebView.CoreWebView2 == core)
                {
                    return tab.WebView;
                }
            }

            return null;
        }

        private void SyncAddress()
        {
            BrowserTab tab = ActiveTab();
            if (tab != null && tab.IsSuspended)
            {
                if (tab.SuspendedUrl == "about:blank")
                {
                    _address.Text = string.Empty;
                }
                else
                {
                    _address.Text = string.IsNullOrWhiteSpace(tab.SuspendedUrl) ? HomeUrl : tab.SuspendedUrl;
                }
                return;
            }

            WebView2 web = ActiveWebView();
            if (web != null && web.Source != null)
            {
                string src = web.Source.ToString();
                if (src == "about:blank")
                {
                    if (tab != null && tab.SuspendedUrl == "about:blank")
                    {
                        _address.Text = string.Empty;
                    }
                    else
                    {
                        _address.Text = HomeUrl;
                    }
                }
                else
                {
                    _address.Text = src;
                }
            }
        }

        private void UpdateStatus()
        {
            BrowserTab tab = ActiveTab();
            int blocked = tab == null ? 0 : tab.BlockedRequests;
            _shield.Text = _adBlockEnabled ? "Block Ads On" : "Block Ads Off";
            _shield.Accent = _adBlockEnabled ? Theme.Accent : Theme.Warning;
            _shield.Invalidate();

            string updateInfo = string.Empty;
            if (_updateAvailable && _preparedUpdateManifest != null)
            {
                updateInfo = "   actualizacion " + _preparedUpdateManifest.Version + " lista";
                _menuButton.Text = "☰!";
            }
            else if (_updateDownloading)
            {
                updateInfo = "   descargando actualizacion...";
                _menuButton.Text = "☰↓";
            }
            else
            {
                _menuButton.Text = "☰";
            }

            _status.Text = "Bloqueador " + (_adBlockEnabled ? "activo" : "pausado") +
                "   reglas: " + _adBlocker.RuleCount +
                "   firewall: " + (_privacyFirewallEnabled ? "activo" : "pausado") +
                "   reglas firewall: " + _privacyFirewall.RuleCount +
                "   bloqueadas en pestana: " + blocked +
                (tab == null || tab.BlockedPopups == 0 ? string.Empty : "   popups bloqueados: " + tab.BlockedPopups) +
                (tab == null || string.IsNullOrWhiteSpace(tab.LastBlockedRequest) ? string.Empty : "   ultima: " + Trim(tab.LastBlockedRequest, 70)) +
                (tab == null || string.IsNullOrWhiteSpace(tab.NavigationNotice) ? string.Empty : "   " + tab.NavigationNotice) +
                updateInfo;
        }

        private void ApplyResponsiveMode()
        {
            bool compact = Width < 980;
            bool veryCompact = Width < 820;

            _reload.Text = compact ? "R" : "Reload";
            _reload.Width = compact ? 38 : 66;
            _newTab.Width = 38;
            _extensions.Text = compact ? "Ext" : "Extensions";
            _extensions.Width = compact ? 58 : 98;
            _shield.Text = _adBlockEnabled ? (compact ? "Ads On" : "Block Ads On") : (compact ? "Ads Off" : "Block Ads Off");
            _shield.Width = compact ? 68 : 96;

            _chromeStore.Visible = !veryCompact;
            RebuildTabStrip();
            RebuildBookmarksBar();
        }

        private string GetTabUrl(BrowserTab tab)
        {
            if (tab == null)
            {
                return HomeUrl;
            }

            if (tab.IsSuspended)
            {
                return string.IsNullOrWhiteSpace(tab.SuspendedUrl) ? HomeUrl : tab.SuspendedUrl;
            }

            if (tab.WebView != null && tab.WebView.Source != null && tab.WebView.Source.ToString() != "about:blank")
            {
                return tab.WebView.Source.ToString();
            }

            if (!string.IsNullOrWhiteSpace(tab.SuspendedUrl))
            {
                return tab.SuspendedUrl;
            }

            return HomeUrl;
        }

        private string GetTabTitle(BrowserTab tab)
        {
            if (tab == null || tab.Page == null || string.IsNullOrWhiteSpace(tab.Page.Text))
            {
                return "Gan Browser";
            }

            string title = tab.Page.Text;
            if (title.StartsWith("[Suspended] ", StringComparison.Ordinal))
            {
                title = title.Substring("[Suspended] ".Length);
            }
            if (title.StartsWith("[S] ", StringComparison.Ordinal))
            {
                title = title.Substring("[S] ".Length);
            }
            if (title.StartsWith("[Pinned] ", StringComparison.Ordinal))
            {
                title = title.Substring("[Pinned] ".Length);
            }

            return title;
        }

        private Color GetIslandColor(BrowserTab tab)
        {
            if (tab == null || tab.IslandId <= 0)
            {
                return Theme.Accent;
            }

            Color color;
            if (_islandColors.TryGetValue(tab.IslandId, out color))
            {
                return color;
            }

            color = ColorFromText(GetTabUrl(tab));
            _islandColors[tab.IslandId] = color;
            return color;
        }

        private static Color ColorFromText(string value)
        {
            string text = string.IsNullOrWhiteSpace(value) ? "gx-light" : value.ToLowerInvariant();
            int hash = 17;
            for (int i = 0; i < text.Length; i++)
            {
                hash = unchecked(hash * 31 + text[i]);
            }

            int palette = (hash & 0x7fffffff) % 8;
            Color[] colors = new Color[]
            {
                Color.FromArgb(255, 98, 132),
                Color.FromArgb(255, 159, 67),
                Color.FromArgb(72, 219, 251),
                Color.FromArgb(29, 209, 161),
                Color.FromArgb(254, 202, 87),
                Color.FromArgb(95, 39, 205),
                Color.FromArgb(238, 82, 83),
                Color.FromArgb(16, 172, 132)
            };
            return colors[palette];
        }

        private static string EncodeField(string value)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(value ?? string.Empty));
        }

        private static string DecodeField(string value)
        {
            try
            {
                return Encoding.UTF8.GetString(Convert.FromBase64String(value));
            }
            catch
            {
                return string.Empty;
            }
        }

        private static long ParseLong(string value, long fallback)
        {
            long parsed;
            return long.TryParse(value, out parsed) ? parsed : fallback;
        }

        private static string CleanImportedText(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "Imported";
            }

            string noTags = Regex.Replace(value, "<.*?>", string.Empty);
            return WebUtility.HtmlDecode(noTags).Trim();
        }

        private static string CsvEscape(string value)
        {
            string text = value ?? string.Empty;
            if (text.IndexOf('"') >= 0 || text.IndexOf(',') >= 0 || text.IndexOf('\n') >= 0 || text.IndexOf('\r') >= 0)
            {
                return "\"" + text.Replace("\"", "\"\"") + "\"";
            }
            return text;
        }

        private static string[] SplitCsvLine(string line)
        {
            List<string> values = new List<string>();
            StringBuilder current = new StringBuilder();
            bool inQuotes = false;
            for (int i = 0; i < line.Length; i++)
            {
                char ch = line[i];
                if (ch == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                    continue;
                }

                if (ch == ',' && !inQuotes)
                {
                    values.Add(current.ToString());
                    current.Length = 0;
                    continue;
                }

                current.Append(ch);
            }

            values.Add(current.ToString());
            return values.ToArray();
        }

        private string NormalizeInput(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return HomeUrl;
            }

            string value = input.Trim();
            if (string.Equals(value, HomeUrl, StringComparison.OrdinalIgnoreCase))
            {
                return HomeUrl;
            }

            if (string.Equals(value, UpdatedUrl, StringComparison.OrdinalIgnoreCase))
            {
                return UpdatedUrl;
            }

            if (value.IndexOf("://", StringComparison.Ordinal) >= 0)
            {
                return value;
            }

            if (value.IndexOf('.') >= 0 && value.IndexOf(' ') < 0)
            {
                return "https://" + value;
            }

            string searchEngine = (_appSettings != null && _appSettings.DefaultSearchEngine != null) ? _appSettings.DefaultSearchEngine : "DuckDuckGo";
            string searchUrl;
            switch (searchEngine.ToLowerInvariant())
            {
                case "google":
                    searchUrl = "https://www.google.com/search?q=";
                    break;
                case "bing":
                    searchUrl = "https://www.bing.com/search?q=";
                    break;
                case "yahoo":
                    searchUrl = "https://search.yahoo.com/search?p=";
                    break;
                default:
                    searchUrl = "https://duckduckgo.com/?q=";
                    break;
            }
            return searchUrl + Uri.EscapeDataString(value);
        }

        private static bool IsYouTubeHost(string host)
        {
            if (string.IsNullOrEmpty(host))
            {
                return false;
            }

            string value = host.ToLowerInvariant();
            return value == "youtube.com" || value.EndsWith(".youtube.com", StringComparison.Ordinal) ||
                value == "youtu.be" || value.EndsWith(".youtu.be", StringComparison.Ordinal);
        }

        private static string YouTubeShieldsScript(bool enabled)
        {
            if (!enabled)
            {
                return @"(() => {
  const isYT = /(\.|^)youtube\.com$/.test(location.hostname) || location.hostname === 'youtu.be';
  if (!isYT) return;
  window.__gxLightAdsEnabled = true;
  window.__gxLightYouTubeCompatibilityMode = true;
  window.__gxLightRunYouTubeShields = function(){};
})();";
            }

            return @"(function() {
    var isYT = /(\.|^)youtube\.com$/.test(location.hostname) || location.hostname === 'youtu.be';
    if (!isYT) return;
    window.__gxLightAdsEnabled = false;
    window.__gxLightYouTubeCompatibilityMode = true;

    var _parse = JSON.parse.bind(JSON);
    var _stringify = JSON.stringify;

    JSON.parse = function(s, r) {
        var v = _parse(s, r);
        if (v && typeof v === 'object') stripAds(v);
        return v;
    };

    var _rjson = Response.prototype.json;
    Response.prototype.json = function() {
        return _rjson.call(this).then(function(d) {
            if (d && typeof d === 'object') stripAds(d);
            return d;
        });
    };

    var _fetch = window.fetch;
    window.fetch = function(req, init) {
        return _fetch.call(window, req, init).then(function(r) {
            var u = (typeof req === 'string' ? req : (req && req.url)) || '';
            if (u.indexOf('/youtubei/v1/') < 0 && u.indexOf('player?') < 0 && u.indexOf('get_watch?') < 0) return r;
            var ct = (r.headers.get('content-type') || '') + '';
            if (ct.indexOf('json') < 0 && ct.indexOf('text') < 0 && ct.indexOf('javascript') < 0) return r;
            return r.clone().text().then(function(t) {
                try {
                    var d = _parse(t); stripAds(d);
                    return new Response(_stringify(d), { status: r.status, statusText: r.statusText, headers: r.headers });
                } catch(e) { return r; }
            });
        });
    };

    var _then = Promise.prototype.then;
    Promise.prototype.then = function(f, r) {
        if (typeof f === 'function') {
            var s = f.toString().replace(/\s/g, '');
            if (s.indexOf('onAbnormalityDetected') >= 0 || s.indexOf('abnormality') >= 0) return _then.call(this, function(){}, r);
        }
        return _then.call(this, f, r);
    };

    var _mapHas = Map.prototype.has;
    Map.prototype.has = function(k) {
        if (k === 'onSnackbarMessage' || k === 'onAdNoticeMessage') return false;
        return _mapHas.call(this, k);
    };

    function stripAds(o, d) {
        if (!o || typeof o !== 'object' || (d || 0) > 20) return;
        try {
            var keys = Object.keys(o);
            for (var i = 0; i < keys.length; i++) {
                var k = keys[i], kl = k.toLowerCase();
                if (kl.indexOf('ad') === 0 || kl.indexOf('ad') === 1 || kl.indexOf('adbreak') >= 0 || kl.indexOf('adslot') >= 0 || kl.indexOf('adplace') >= 0 || kl.indexOf('playerad') >= 0 || kl.indexOf('linearad') >= 0 || kl.indexOf('instreamad') >= 0 || kl.indexOf('adsequence') >= 0 || kl.indexOf('preroll') >= 0 || kl.indexOf('midroll') >= 0 || kl.indexOf('postroll') >= 0 || kl.indexOf('vast') >= 0 || kl.indexOf('vmap') >= 0 || kl.indexOf('sponsored') >= 0 || kl.indexOf('promoted') >= 0 || kl.indexOf('companionad') >= 0 || kl.indexOf('bannerad') >= 0 || kl.indexOf('overlayad') >= 0 || kl.indexOf('admodul') >= 0 || kl.indexOf('no_ads') >= 0 || kl.indexOf('adload') >= 0 || kl.indexOf('adinterrupt') >= 0 || kl.indexOf('adpod') >= 0 || kl.indexOf('adsystem') >= 0 || kl.indexOf('adwarning') >= 0 || kl.indexOf('adblock') >= 0 || kl.indexOf('adtext') >= 0 || kl.indexOf('adtitle') >= 0 || kl.indexOf('adimage') >= 0 || kl.indexOf('adoverlay') >= 0 || kl.indexOf('adunit') >= 0 || kl.indexOf('admeta') >= 0 || kl.indexOf('addata') >= 0) {
                    try { delete o[k]; } catch(e) {}
                }
            }
            for (var i = 0; i < keys.length; i++) {
                var v = o[keys[i]];
                if (v && typeof v === 'object') {
                    if (Array.isArray(v)) { for (var j = 0; j < v.length; j++) { if (v[j] && typeof v[j] === 'object') stripAds(v[j], (d||0)+1); } }
                    else { stripAds(v, (d||0)+1); }
                }
            }
        } catch(e) {}
    }

    function guard(n) {
        var v;
        try { Object.defineProperty(window, n, { configurable: true, enumerable: true, get: function(){return v;}, set: function(x){if(x&&typeof x==='object'){try{stripAds(x,0)}catch(e){}}v=x;} }); } catch(e) {}
    }
    guard('ytInitialPlayerResponse');
    guard('ytInitialData');

    function injectCSS() {
        if (document.getElementById('gxlight-css')) return;
        var s = document.createElement('style');
        s.id = 'gxlight-css';
        s.textContent = '#masthead-ad,#player-ads,ytd-ad-slot-renderer,ytd-companion-slot-renderer,ytd-promoted-video-renderer,ytd-display-ad-renderer,ytd-video-masthead-ad-v3-renderer,.ytp-ad-progress,.ytp-ad-progress-list,ytd-statement-banner-renderer,.ytp-ad-module,.video-ads,.ytp-ad-overlay-container,.ytp-ad-player-overlay,.ytp-ad-image-overlay,ytd-action-companion-ad-renderer,ytd-in-feed-ad-layout-renderer,ytd-player-legacy-desktop-watch-ads-renderer,ytd-engagement-panel-section-list-renderer[target-id=""engagement-panel-ads""],ytd-ad-slot-renderer{display:none!important;width:0!important;height:0!important;visibility:hidden!important;opacity:0!important;pointer-events:none!important;position:absolute!important;overflow:hidden!important;clip:rect(0,0,0,0)!important}';
        (document.head || document.documentElement).appendChild(s);
    }

    function killAds() {
        document.querySelectorAll('#masthead-ad,#player-ads,.ytp-ad-module,.video-ads,ytd-ad-slot-renderer,ytd-companion-slot-renderer,ytd-promoted-video-renderer,ytd-display-ad-renderer,ytd-video-masthead-ad-v3-renderer,.ytp-ad-progress,.ytp-ad-progress-list,ytd-statement-banner-renderer,.ytp-ad-overlay-container,.ytp-ad-player-overlay,.ytp-ad-image-overlay,ytd-action-companion-ad-renderer,ytd-in-feed-ad-layout-renderer,ytd-player-legacy-desktop-watch-ads-renderer,ytd-engagement-panel-section-list-renderer[target-id=""engagement-panel-ads""]').forEach(function(el){el.remove();});

        var skip = document.querySelector('.ytp-ad-skip-button,.ytp-ad-skip-button-modern,.ytp-skip-ad-button,button[aria-label^=""Skip""],button[aria-label^=""Saltar""],button[aria-label^=""Omitir""],button[aria-label*=""skip""],button[aria-label*=""Skip""]');
        if (skip && skip.getClientRects && skip.getClientRects().length > 0 && !skip.disabled) { try { skip.click(); } catch(e) {} }

        var player = document.querySelector('.html5-video-player');
        if (player && (player.classList.contains('ad-showing') || player.classList.contains('ad-interrupting'))) {
            var video = document.querySelector('video');
            if (video && video.duration > 0) {
                try {
                    video.currentTime = video.duration - 0.5;
                } catch(e) {}
            }
            try { player.classList.remove('ad-showing', 'ad-interrupting'); } catch(e) {}
        }
    }

    if (document.documentElement) {
        injectCSS();
        killAds();
        new MutationObserver(killAds).observe(document.documentElement, { childList: true, subtree: true, attributes: true, attributeFilter: ['class'] });
        setInterval(killAds, 500);
    } else {
        document.addEventListener('DOMContentLoaded', function() {
            injectCSS();
            killAds();
            new MutationObserver(killAds).observe(document.documentElement, { childList: true, subtree: true, attributes: true, attributeFilter: ['class'] });
            setInterval(killAds, 500);
        });
    }
    window.__gxLightRunYouTubeShields = killAds;
})();";
        }

        private void SetYouTubeShieldsEnabled(bool enabled)
        {
            string value = enabled ? "true" : "false";
            for (int i = 0; i < _tabs.TabPages.Count; i++)
            {
                BrowserTab tab = _tabs.TabPages[i].Tag as BrowserTab;
                if (tab == null || tab.WebView == null || tab.WebView.CoreWebView2 == null)
                {
                    continue;
                }
                tab.WebView.CoreWebView2.ExecuteScriptAsync(
                    "window.__gxLightAdsEnabled=" + value +
                    ";window.__gxLightRunYouTubeShields&&window.__gxLightRunYouTubeShields();");
            }
        }

        private string InternalPageHtml(string pageName)
        {
            switch (pageName)
            {
                case "home":
                    return InternalPages.HomeHtml(_appSettings);
                case "updated":
                case "novedades":
                    return InternalPages.UpdateNoticeHtml(_updateManifest);
                case "history":
                    return InternalPages.HtmlShell("History", InternalPages.HistoryHtml(_history));
                case "downloads":
                    return InternalPages.HtmlShell("Downloads", InternalPages.DownloadsHtml(_downloads));
                case "passwords":
                    return InternalPages.HtmlShell("Passwords and autofill", InternalPages.PasswordsHtml(_passwordSavingEnabled, _passwordVault));
                case "bookmarks":
                    return InternalPages.BookmarksHtml(_bookmarks);
                case "playlist":
                    return InternalPages.HtmlShell("Playlist", InternalPages.PlaylistHtml(_playlist));
                case "memory":
                    return InternalPages.HtmlShell("Memory monitor",
                        "<p>Estimated browser memory: <b>" + EstimateBrowserMemoryMb() + " MB</b></p>" +
                        "<p>RAM limiter: <b>" + (_gxControl.RamLimiterEnabled ? (_gxControl.MemoryLimitMb / 1024.0).ToString("0.0") + " GB" : "Off") + "</b></p>" +
                        "<p>Hard limit: <b>" + (_gxControl.HardMemoryLimit ? "On" : "Off") + "</b></p>" +
                        "<p>Hot tabs killer: <b>" + (_gxControl.HotTabsKillerEnabled ? _gxControl.HotTabsMode : "Off") + "</b></p>" +
                        "<p>CPU limiter policy: <b>" + (_gxControl.CpuLimiterEnabled ? _gxControl.CpuLimitPercent + "%" : "Off") + "</b></p>" +
                        "<p>Network limiter policy: <b>" + (_gxControl.NetworkLimiterEnabled ? _gxControl.NetworkProfile : "Off") + "</b></p>" +
                        "<p>Low Resource Mode: <b>" + (_gxControl.LowResourcesModeEnabled ? "On (Max active: " + _gxControl.MaxActiveTabs + ")" : "Off") + "</b></p>" +
                        "<p>Inactive tabs are moved to low-memory mode and can be suspended/discarded to free their WebView.</p>" +
                        InternalPages.GetMemoryProcessesHtml(_environment));
                case "shields":
                    return InternalPages.HtmlShell("Gan Guard y Cortafuegos de Privacidad",
                        "<p>Ad blocker: <b>" + (_adBlockEnabled ? "enabled" : "disabled") + "</b></p>" +
                        "<p>Privacy Firewall: <b>" + (_privacyFirewallEnabled ? "enabled" : "disabled") + "</b></p>" +
                        "<p>Rules: " + _adBlocker.RuleCount + " ad rules, " + _privacyFirewall.RuleCount + " firewall rules.</p>");
                case "settings":
                    return InternalPages.SettingsHtml(_appSettings, _passwordVault);
                default:
                    return InternalPages.HtmlShell("Gan Browser", "<p>Section not found.</p>");
            }
        }

        private async Task<string> ExtensionsPageHtmlAsync()
        {
            BrowserTab tab = ActiveTab();
            StringBuilder body = new StringBuilder();
            body.Append("<p>Import unpacked Chrome/Edge extensions from the main menu.</p>");
            body.Append("<p><a href='" + ChromeStoreUrl + "'>Chrome Web Store</a></p>");

            if (tab != null && tab.WebView != null && tab.WebView.CoreWebView2 != null)
            {
                System.Collections.Generic.IReadOnlyList<CoreWebView2BrowserExtension> extensions =
                    await tab.WebView.CoreWebView2.Profile.GetBrowserExtensionsAsync();
                if (extensions.Count == 0)
                {
                    body.Append("<p>No extensions loaded yet.</p>");
                }
                else
                {
                    body.Append("<table><tr><th>Name</th><th>Status</th><th>ID</th></tr>");
                    for (int i = 0; i < extensions.Count; i++)
                    {
                        body.Append("<tr><td>" + EscapeHtml(extensions[i].Name) + "</td><td>" +
                            (extensions[i].IsEnabled ? "Enabled" : "Disabled") + "</td><td>" +
                            EscapeHtml(extensions[i].Id) + "</td></tr>");
                    }
                    body.Append("</table>");
                }
            }
            return InternalPages.HtmlShell("Extensions", body.ToString());
        }

        private static string EscapeHtml(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
        }

        private static string Trim(string value, int max)
        {
            return value.Length <= max ? value : value.Substring(0, max - 1) + "...";
        }

        private static void PaintAddressShell(object sender, PaintEventArgs e)
        {
            Panel panel = (Panel)sender;
            using (Pen pen = new Pen(Color.FromArgb(70, 74, 88)))
            {
                Rectangle rect = new Rectangle(0, 0, panel.Width - 1, panel.Height - 1);
                e.Graphics.DrawRectangle(pen, rect);
            }
        }

        private static void EnsureDefaultFilters()
        {
            if (File.Exists(AppPaths.Filters))
            {
                return;
            }

            File.WriteAllText(AppPaths.Filters,
                "! Gan Browser local filters" + Environment.NewLine +
                "! Add EasyList/EasyPrivacy content here or run scripts\\Update-Filters.ps1" + Environment.NewLine,
                System.Text.Encoding.UTF8);
        }

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        private static extern bool SetProcessWorkingSetSize(IntPtr process, IntPtr minSize, IntPtr maxSize);

        private void NavigationCompletedHandler(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            CoreWebView2 core = sender as CoreWebView2;
            if (core == null) return;
            WebView2 web = WebViewForCore(core);
            if (web == null) return;
            TabPage page = PageForWebView(web);
            if (page == null) return;
            BrowserTab tab = page.Tag as BrowserTab;

            if (tab != null)
            {
                tab.NavigationNotice = string.Empty;
                if (e.HttpStatusCode == 403 && IsHostOrSubdomain(tab.LastNavigationHost, "crunchyroll.com"))
                {
                    tab.NavigationNotice = "Crunchyroll rechazo WebView2 (HTTP 403) con modo compatibilidad activo.";
                    Logger.Info(tab.NavigationNotice);
                }
                else if (!e.IsSuccess)
                {
                    tab.NavigationNotice = "Error de navegacion: " + e.WebErrorStatus;
                }
            }

            if (web.Source != null && IsYouTubeHost(web.Source.Host))
            {
                core.ExecuteScriptAsync("window.__gxLightRunYouTubeShields && window.__gxLightRunYouTubeShields();");
            }
            if (tab != null && web.Source != null &&
                (web.Source.Scheme == Uri.UriSchemeHttp || web.Source.Scheme == Uri.UriSchemeHttps))
            {
                int availableCredentials = VaultEntriesForHost(web.Source.Host).Count;
                if (availableCredentials > 0)
                {
                    tab.NavigationNotice = "Bóveda: selecciona una cuenta junto al campo de contraseña.";
                    Task ignored = InstallVaultAssistAsync(core, web);
                }
            }
            AddHistoryEntry(page, web);
            BrowserTab completedTab = page.Tag as BrowserTab;
            if (completedTab != null)
            {
                Task ignored = RefreshFaviconAsync(core, completedTab);
            }
            SyncAddress();
            UpdateStatus();
        }

        private void DocumentTitleChangedHandler(object sender, object e)
        {
            CoreWebView2 core = sender as CoreWebView2;
            if (core == null) return;
            WebView2 web = WebViewForCore(core);
            if (web == null) return;
            TabPage page = PageForWebView(web);
            if (page == null) return;

            string title = core.DocumentTitle;
            page.Text = string.IsNullOrWhiteSpace(title) ? "Pestana" : Trim(title, 30);
            RebuildTabStrip();
        }

        private async void FaviconChangedHandler(object sender, object e)
        {
            CoreWebView2 core = sender as CoreWebView2;
            WebView2 web = WebViewForCore(core);
            TabPage page = web == null ? null : PageForWebView(web);
            await RefreshFaviconAsync(core, page == null ? null : page.Tag as BrowserTab);
        }

        private async Task RefreshFaviconAsync(CoreWebView2 core, BrowserTab tab)
        {
            if (core == null || tab == null) return;
            bool loaded = false;
            try
            {
                using (Stream stream = await core.GetFaviconAsync(CoreWebView2FaviconImageFormat.Png))
                using (Image image = Image.FromStream(stream))
                {
                    SetTabFavicon(tab, image);
                }
                loaded = true;
                CacheTabFavicon(tab, core.Source);
                RebuildTabStrip();
            }
            catch (Exception ex)
            {
                Logger.Info("WebView2 favicon unavailable: " + ex.Message);
            }
            if (!loaded)
            {
                await RefreshFaviconFallbackAsync(core, tab);
            }
        }

        private async Task RefreshFaviconFallbackAsync(CoreWebView2 core, BrowserTab tab)
        {
            try
            {
                Uri pageUri = null;
                string source = null;
                try
                {
                    source = core.Source;
                }
                catch
                {
                }
                Uri.TryCreate(source, UriKind.Absolute, out pageUri);
                if (TryLoadCachedFavicon(tab, pageUri))
                {
                    RebuildTabStrip();
                    return;
                }

                List<Uri> candidates = new List<Uri>();
                string candidate = null;
                try
                {
                    candidate = core.FaviconUri;
                }
                catch
                {
                }

                if (!string.IsNullOrEmpty(candidate))
                {
                    if (candidate.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                    {
                        if (ProcessDataUriFavicon(candidate, pageUri, tab)) return;
                    }
                    else
                    {
                        Uri faviconUri;
                        if (Uri.TryCreate(candidate, UriKind.Absolute, out faviconUri))
                        {
                            candidates.Add(faviconUri);
                        }
                    }
                }

                try
                {
                    string iconResult = await core.ExecuteScriptAsync(
                        "(document.querySelector('link[rel~=\"icon\"]') || {}).href || ''");
                    string iconHref = DecodeScriptString(iconResult);
                    if (!string.IsNullOrWhiteSpace(iconHref))
                    {
                        if (iconHref.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                        {
                            if (ProcessDataUriFavicon(iconHref, pageUri, tab)) return;
                        }
                        else
                        {
                            Uri documentIcon;
                            if (Uri.TryCreate(iconHref, UriKind.Absolute, out documentIcon))
                            {
                                candidates.Add(documentIcon);
                            }
                        }
                    }
                }
                catch
                {
                }

                if (pageUri != null && (pageUri.Scheme == Uri.UriSchemeHttp || pageUri.Scheme == Uri.UriSchemeHttps))
                {
                    candidates.Add(new Uri(pageUri.GetLeftPart(UriPartial.Authority) + "/favicon.ico"));
                }

                for (int i = 0; i < candidates.Count; i++)
                {
                    Uri iconUri = candidates[i];
                    if (iconUri == null || (iconUri.Scheme != Uri.UriSchemeHttp && iconUri.Scheme != Uri.UriSchemeHttps))
                    {
                        continue;
                    }
                    if (await TryDownloadFaviconAsync(iconUri, pageUri, tab))
                    {
                        RebuildTabStrip();
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Info("Favicon fallback failed: " + ex.Message);
            }
        }

        private async Task<bool> TryDownloadFaviconAsync(Uri faviconUri, Uri pageUri, BrowserTab tab)
        {
            try
            {
                using (WebClient client = new WebClient())
                {
                    client.Headers[HttpRequestHeader.UserAgent] =
                        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36 Edge/120.0.0.0";
                    byte[] bytes = await client.DownloadDataTaskAsync(faviconUri);
                    using (MemoryStream stream = new MemoryStream(bytes))
                    using (Image image = Image.FromStream(stream))
                    {
                        SetTabFavicon(tab, image);
                    }
                }
                CacheTabFavicon(tab, pageUri == null ? null : pageUri.AbsoluteUri);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Info("Favicon candidate failed " + faviconUri + ": " + ex.Message);
                return false;
            }
        }

        private bool ProcessDataUriFavicon(string dataUri, Uri pageUri, BrowserTab tab)
        {
            byte[] bytes;
            if (TryParseDataUri(dataUri, out bytes))
            {
                try
                {
                    using (MemoryStream stream = new MemoryStream(bytes))
                    using (Image image = Image.FromStream(stream))
                    {
                        SetTabFavicon(tab, image);
                        CacheTabFavicon(tab, pageUri == null ? null : pageUri.AbsoluteUri);
                        RebuildTabStrip();
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Info("Failed to parse image from data URI: " + ex.Message);
                }
            }
            return false;
        }

        private static bool TryParseDataUri(string uriString, out byte[] bytes)
        {
            bytes = null;
            try
            {
                if (string.IsNullOrWhiteSpace(uriString) || !uriString.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
                int commaIndex = uriString.IndexOf(',');
                if (commaIndex < 0) return false;

                string header = uriString.Substring(0, commaIndex);
                string dataPart = uriString.Substring(commaIndex + 1);

                if (header.IndexOf(";base64", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    bytes = Convert.FromBase64String(dataPart.Trim());
                    return bytes.Length > 0;
                }
            }
            catch
            {
            }
            return false;
        }

        private static void SetTabFavicon(BrowserTab tab, Image image)
        {
            if (tab == null || image == null)
            {
                return;
            }
            Image previous = tab.Favicon;
            tab.Favicon = new Bitmap(image);
            if (previous != null)
            {
                previous.Dispose();
            }
        }

        private static string DecodeScriptString(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value == "null")
            {
                return string.Empty;
            }
            string text = value.Trim();
            if (text.Length >= 2 && text[0] == '"' && text[text.Length - 1] == '"')
            {
                text = text.Substring(1, text.Length - 2);
            }
            try
            {
                return Regex.Unescape(text.Replace("\\/", "/"));
            }
            catch
            {
                return text;
            }
        }

        private static string FaviconCachePath(Uri pageUri)
        {
            if (pageUri == null || string.IsNullOrWhiteSpace(pageUri.Host))
            {
                return null;
            }
            string host = Regex.Replace(pageUri.Host.ToLowerInvariant(), "[^a-z0-9.-]", "_");
            return Path.Combine(AppPaths.Favicons, host + ".png");
        }

        private static bool TryLoadCachedFavicon(BrowserTab tab, Uri pageUri)
        {
            try
            {
                string path = FaviconCachePath(pageUri);
                if (string.IsNullOrEmpty(path) || !File.Exists(path))
                {
                    return false;
                }
                using (Image image = Image.FromFile(path))
                {
                    SetTabFavicon(tab, image);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void CacheTabFavicon(BrowserTab tab, string pageUrl)
        {
            try
            {
                Uri pageUri;
                if (tab == null || tab.Favicon == null || string.IsNullOrWhiteSpace(pageUrl) ||
                    !Uri.TryCreate(pageUrl, UriKind.Absolute, out pageUri))
                {
                    return;
                }
                AppPaths.Ensure();
                string path = FaviconCachePath(pageUri);
                if (!string.IsNullOrEmpty(path))
                {
                    tab.Favicon.Save(path, System.Drawing.Imaging.ImageFormat.Png);
                }
            }
            catch
            {
            }
        }

        private TabPage PageForWebView(WebView2 web)
        {
            for (int i = 0; i < _tabs.TabPages.Count; i++)
            {
                BrowserTab tab = _tabs.TabPages[i].Tag as BrowserTab;
                if (tab != null && tab.WebView == web)
                {
                    return _tabs.TabPages[i];
                }
            }
            return null;
        }

        private void UnsubscribeWebViewEvents(WebView2 web)
        {
            if (web == null) return;
            try
            {
                if (web.CoreWebView2 != null)
                {
                    web.CoreWebView2.ProcessFailed -= Web_ProcessFailed;
                    web.CoreWebView2.WebResourceRequested -= WebResourceRequested;
                    web.CoreWebView2.NewWindowRequested -= NewWindowRequested;
                    web.CoreWebView2.DownloadStarting -= DownloadStarting;
                    web.CoreWebView2.NavigationStarting -= NavigationStarting;
                    web.CoreWebView2.NavigationCompleted -= NavigationCompletedHandler;
                    web.CoreWebView2.DocumentTitleChanged -= DocumentTitleChangedHandler;
                    web.CoreWebView2.FaviconChanged -= FaviconChangedHandler;
                    web.CoreWebView2.WebMessageReceived -= WebMessageReceived;
                    web.CoreWebView2.ContainsFullScreenElementChanged -= ContainsFullScreenElementChanged;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error unsubscribing events: " + ex.Message);
            }
        }

        private void Web_ProcessFailed(object sender, CoreWebView2ProcessFailedEventArgs e)
        {
            WebView2 web = sender as WebView2;
            if (web == null) return;

            Logger.Error("WebView2 process failed. Kind: " + e.ProcessFailedKind + ", Reason: " + e.Reason + ", ExitCode: " + e.ExitCode);

            BrowserTab tab = null;
            for (int i = 0; i < _tabs.TabPages.Count; i++)
            {
                BrowserTab t = _tabs.TabPages[i].Tag as BrowserTab;
                if (t != null && t.WebView == web)
                {
                    tab = t;
                    break;
                }
            }

            if (tab != null)
            {
                ShowCrashRecovery(tab, "El proceso de la pestaña falló (" + e.ProcessFailedKind + ").");
            }
        }

        private void ShowCrashRecovery(BrowserTab tab, string message)
        {
            if (tab == null || tab.WebView == null)
            {
                return;
            }

            tab.SuspendedUrl = tab.WebView.Source == null ? HomeUrl : tab.WebView.Source.ToString();
            tab.SuspendedTitle = tab.Page.Text.Replace("[Crashed] ", "").Replace("[Suspended] ", "").Replace("[S] ", "");

            UnsubscribeWebViewEvents(tab.WebView);

            tab.Page.Controls.Clear();
            tab.WebView.Dispose();
            tab.WebView = null;
            tab.IsSuspended = true;

            Panel placeholder = new Panel();
            placeholder.Dock = DockStyle.Fill;
            placeholder.BackColor = Theme.Window;

            Label title = new Label();
            title.Text = "La pagina ha fallado (Crash)";
            title.ForeColor = Theme.Warning;
            title.Font = new Font("Segoe UI", 16f, FontStyle.Bold);
            title.AutoSize = true;
            title.Left = 40;
            title.Top = 40;
            placeholder.Controls.Add(title);

            Label detail = new Label();
            detail.Text = message + "\nURL: " + tab.SuspendedUrl;
            detail.ForeColor = Theme.Muted;
            detail.AutoSize = true;
            detail.Left = 42;
            detail.Top = 82;
            placeholder.Controls.Add(detail);

            ChromeButton reload = new ChromeButton();
            reload.Text = "Recargar pagina";
            reload.Width = 150;
            reload.Height = 34;
            reload.Left = 42;
            reload.Top = 140;
            reload.Click += async delegate { await RestoreSuspendedTabAsync(tab); };
            placeholder.Controls.Add(reload);

            tab.Page.Controls.Add(placeholder);
            tab.Page.Text = "[Crashed] " + Trim(tab.SuspendedTitle, 18);
            RebuildTabStrip();
        }

        private void SetupSuspensionPlaceholder(BrowserTab tab)
        {
            Panel placeholder = new Panel();
            placeholder.Dock = DockStyle.Fill;
            placeholder.BackColor = Theme.Window;

            Label title = new Label();
            title.Text = "Pestana suspendida para ahorrar memoria";
            title.ForeColor = Theme.Text;
            title.Font = new Font("Segoe UI", 16f, FontStyle.Bold);
            title.AutoSize = true;
            title.Left = 40;
            title.Top = 40;
            placeholder.Controls.Add(title);

            Label urlLabel = new Label();
            urlLabel.Text = tab.SuspendedUrl;
            urlLabel.ForeColor = Theme.Muted;
            urlLabel.AutoSize = true;
            urlLabel.Left = 42;
            urlLabel.Top = 82;
            placeholder.Controls.Add(urlLabel);

            ChromeButton restore = new ChromeButton();
            restore.Text = "Restaurar pestana";
            restore.Width = 150;
            restore.Height = 34;
            restore.Left = 42;
            restore.Top = 118;
            restore.Click += async delegate { await RestoreSuspendedTabAsync(tab); };
            placeholder.Controls.Add(restore);

            tab.Page.Controls.Add(placeholder);
        }

        private BrowserTab CreateSuspendedTab(string url, string title, int islandId)
        {
            string cleanTitle = (title ?? "Pestana").Replace("[Suspended] ", "").Replace("[S] ", "");
            TabPage page = new TabPage("[S] " + Trim(cleanTitle, 18));
            page.BackColor = Theme.Window;

            BrowserTab tab = new BrowserTab(page, null);
            tab.IslandId = islandId;
            tab.IsSuspended = true;
            tab.SuspendedUrl = url;
            tab.SuspendedTitle = cleanTitle;
            page.Tag = tab;

            SetupSuspensionPlaceholder(tab);
            _tabs.TabPages.Add(page);
            Task ignored = RefreshSuspendedFaviconAsync(tab);
            RebuildTabStrip();
            if (!_restoringSession)
            {
                SaveSession();
            }
            return tab;
        }

        private async Task RefreshSuspendedFaviconAsync(BrowserTab tab)
        {
            if (tab == null || string.IsNullOrWhiteSpace(tab.SuspendedUrl)) return;
            try
            {
                Uri pageUri;
                if (!Uri.TryCreate(tab.SuspendedUrl, UriKind.Absolute, out pageUri) ||
                    (pageUri.Scheme != Uri.UriSchemeHttp && pageUri.Scheme != Uri.UriSchemeHttps))
                {
                    return;
                }
                if (TryLoadCachedFavicon(tab, pageUri))
                {
                    RebuildTabStrip();
                    return;
                }
                Uri faviconUri = new Uri(pageUri.GetLeftPart(UriPartial.Authority) + "/favicon.ico");
                if (await TryDownloadFaviconAsync(faviconUri, pageUri, tab))
                {
                    RebuildTabStrip();
                }
            }
            catch
            {
            }
        }

        private void SaveSession()
        {
            if (_restoringSession || !_appSettings.RestorePreviousSession)
            {
                return;
            }

            try
            {
                bool maximized = WindowState == FormWindowState.Maximized;
                int x = Location.X;
                int y = Location.Y;
                int width = Width;
                int height = Height;

                if (WindowState == FormWindowState.Minimized)
                {
                    x = -1; y = -1;
                }

                int activeIndex = _tabs.SelectedIndex;
                List<BrowserTab> tabsList = new List<BrowserTab>();
                for (int i = 0; i < _tabs.TabPages.Count; i++)
                {
                    BrowserTab tab = _tabs.TabPages[i].Tag as BrowserTab;
                    if (tab != null)
                    {
                        tabsList.Add(tab);
                    }
                }

                SessionManager.SaveSession(maximized, x, y, width, height, activeIndex, _islandColors, _collapsedIslands, tabsList);
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to save session from form: " + ex.Message);
            }
        }

        private void BrowserFormClosing(object sender, FormClosingEventArgs e)
        {
            if (_cleaningUp)
            {
                return;
            }

            _cleaningUp = true;
            _memoryTimer.Stop();
            if (_appSettings.RestorePreviousSession)
            {
                SaveSession();
            }
            else
            {
                SessionManager.DeleteSession();
            }
            _appSettings.PasswordSavingEnabled = _passwordSavingEnabled;
            _appSettings.Save();
        }

        private void DisposeWebViewsForShutdown()
        {
            for (int i = 0; i < _tabs.TabPages.Count; i++)
            {
                BrowserTab tab = _tabs.TabPages[i].Tag as BrowserTab;
                if (tab == null || tab.WebView == null)
                {
                    continue;
                }

                try
                {
                    UnsubscribeWebViewEvents(tab.WebView);
                    tab.WebView.Dispose();
                    tab.WebView = null;
                }
                catch (Exception ex)
                {
                    Logger.Error("Failed to close WebView cleanly: " + ex.Message);
                }
            }
        }

        private void EnforceLowResourceLimit()
        {
            if (!_gxControl.LowResourcesModeEnabled)
            {
                return;
            }

            int activeCount = 0;
            List<BrowserTab> activeTabs = new List<BrowserTab>();
            for (int i = 0; i < _tabs.TabPages.Count; i++)
            {
                BrowserTab tab = _tabs.TabPages[i].Tag as BrowserTab;
                if (tab != null && !tab.IsSuspended)
                {
                    activeCount++;
                    if (_tabs.TabPages[i] != _tabs.SelectedTab)
                    {
                        activeTabs.Add(tab);
                    }
                }
            }

            int limit = _gxControl.MaxActiveTabs;
            if (activeCount > limit && activeTabs.Count > 0)
            {
                activeTabs.Sort(delegate(BrowserTab a, BrowserTab b) {
                    return a.LastActiveUtc.CompareTo(b.LastActiveUtc);
                });

                int toSuspend = activeCount - limit;
                int suspendedCount = 0;
                for (int i = 0; i < activeTabs.Count && suspendedCount < toSuspend; i++)
                {
                    BrowserTab tab = activeTabs[i];
                    if (tab.WebView != null && tab.WebView.CoreWebView2 != null)
                    {
                        if (tab.WebView.CoreWebView2.IsDocumentPlayingAudio)
                        {
                            continue;
                        }
                    }
                    SuspendTab(tab);
                    suspendedCount++;
                }
            }
        }

        private void FreeMemoryNow()
        {
            Logger.Info("Manually requesting memory release...");
            SuspendIdleTabsNow();

            GC.Collect();
            GC.WaitForPendingFinalizers();

            try
            {
                SetProcessWorkingSetSize(Process.GetCurrentProcess().Handle, (IntPtr)(-1), (IntPtr)(-1));
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to trim working set: " + ex.Message);
            }

            Logger.Info("Memory release complete.");
        }

        private void ApplyLayoutDimensions()
        {
            if (_rootLayout == null || _fullScreenActive) return;

            int sidebarWidth = 46;
            int statusHeight = 22;
            
            int tabStripHeight = 26;
            int navHeight = 30;
            int bookmarksHeight = _appSettings.ShowBookmarksBar ? 28 : 0;
            int topbarPadding = _appSettings.ShowBookmarksBar ? 8 : 4;
            int topbarHeight = tabStripHeight + navHeight + bookmarksHeight + topbarPadding;

            _rootLayout.ColumnStyles[0].Width = sidebarWidth;
            _rootLayout.RowStyles[0].Height = topbarHeight;
            _rootLayout.RowStyles[2].Height = statusHeight;

            if (_topLayout != null)
            {
                _topLayout.RowStyles[0].Height = tabStripHeight;
                _topLayout.RowStyles[1].Height = navHeight;
                
                if (_appSettings.ShowBookmarksBar)
                {
                    _topLayout.Padding = new Padding(6, 4, 8, 4);
                    _topLayout.RowStyles[2].SizeType = SizeType.Absolute;
                    _topLayout.RowStyles[2].Height = bookmarksHeight;
                    _bookmarksBar.Visible = true;
                }
                else
                {
                    _topLayout.Padding = new Padding(6, 4, 8, 0);
                    _topLayout.RowStyles[2].SizeType = SizeType.Absolute;
                    _topLayout.RowStyles[2].Height = 0;
                    _bookmarksBar.Visible = false;
                }
            }
        }

        private void SetBookmarksBarVisible(bool visible)
        {
            _appSettings.ShowBookmarksBar = visible;
            _appSettings.Save();
            ApplyLayoutDimensions();
        }

        private void ApplyThemeToControls()
        {
            BackColor = Theme.Window;
            ForeColor = Theme.Text;

            if (_rootLayout != null) _rootLayout.BackColor = Theme.Window;
            if (_topLayout != null) _topLayout.BackColor = Theme.Topbar;
            
            if (_rootLayout != null && _rootLayout.Controls.Count > 0)
            {
                Control side = _rootLayout.Controls[0];
                if (side != null) side.BackColor = Theme.Sidebar;
            }

            _address.BackColor = Theme.Address;
            _address.ForeColor = Theme.Text;
            if (_address.Parent != null) _address.Parent.BackColor = Theme.Address;

            _status.BackColor = Theme.Topbar;
            _status.ForeColor = Theme.Muted;

            RebuildTabStrip();
            RebuildBookmarksBar();

            _shield.Accent = _adBlockEnabled ? Theme.Accent : Theme.Warning;

            if (_rootLayout != null)
            {
                _rootLayout.Invalidate(true);
            }
        }

        private void UpdateInternalPagesTheme()
        {
            string accentHex = Theme.AccentHex;
            System.Drawing.Color acc = Theme.Accent;
            string accentRgb = string.Format("{0},{1},{2}", acc.R, acc.G, acc.B);
            string bgHex = Theme.WindowHex;
            string panelBgHex = Theme.PanelHex;
            string textHex = Theme.TextHex;
            string mutedHex = Theme.MutedHex;
            string borderHex = Theme.BorderHex;

            string js = string.Format(
                "document.documentElement.setAttribute('data-theme-mode', '{0}'); " +
                "document.documentElement.style.setProperty('--accent', '{1}'); " +
                "document.documentElement.style.setProperty('--accent-rgb', '{2}'); " +
                "document.documentElement.style.setProperty('--bg', '{3}'); " +
                "document.documentElement.style.setProperty('--panel-bg', '{4}'); " +
                "document.documentElement.style.setProperty('--text', '{5}'); " +
                "document.documentElement.style.setProperty('--muted', '{6}'); " +
                "document.documentElement.style.setProperty('--border', '{7}');",
                (_appSettings.ThemeMode ?? "Dark").ToLowerInvariant(),
                accentHex,
                accentRgb,
                bgHex,
                panelBgHex,
                textHex,
                mutedHex,
                borderHex
            );

            for (int i = 0; i < _tabs.TabPages.Count; i++)
            {
                BrowserTab browserTab = _tabs.TabPages[i].Tag as BrowserTab;
                if (browserTab != null)
                {
                    string url = GetTabUrl(browserTab);
                    if (url.StartsWith("gxlight://settings", StringComparison.OrdinalIgnoreCase) ||
                        url.StartsWith("gxlight://home", StringComparison.OrdinalIgnoreCase))
                    {
                        if (browserTab.WebView != null && browserTab.WebView.CoreWebView2 != null)
                        {
                            browserTab.WebView.CoreWebView2.ExecuteScriptAsync(js);
                        }
                    }
                }
            }
        }

        private ContextMenuStrip CreateContextMenu()
        {
            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Renderer = new GxMenuRenderer();
            menu.BackColor = Theme.Panel;
            menu.ForeColor = Theme.Text;
            menu.ShowImageMargin = false;
            return menu;
        }

        private static ToolStripMenuItem CreateMenuItem(string text, string shortcut, EventHandler onClick)
        {
            ToolStripMenuItem item = new ToolStripMenuItem(text, null, onClick);
            if (!string.IsNullOrEmpty(shortcut))
            {
                item.ShortcutKeyDisplayString = shortcut;
            }
            item.ForeColor = Theme.Text;
            return item;
        }
    }

    internal class GxColorTable : ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground { get { return Theme.Panel; } }
        public override Color MenuBorder { get { return Color.FromArgb(45, 48, 60); } }
        public override Color MenuItemSelected { get { return Theme.Hover; } }
        public override Color MenuItemSelectedGradientBegin { get { return Theme.Hover; } }
        public override Color MenuItemSelectedGradientEnd { get { return Theme.Hover; } }
        public override Color MenuItemPressedGradientBegin { get { return Theme.Selected; } }
        public override Color MenuItemPressedGradientEnd { get { return Theme.Selected; } }
        public override Color ImageMarginGradientBegin { get { return Theme.Panel; } }
        public override Color ImageMarginGradientMiddle { get { return Theme.Panel; } }
        public override Color ImageMarginGradientEnd { get { return Theme.Panel; } }
        public override Color SeparatorDark { get { return Color.FromArgb(45, 48, 60); } }
        public override Color SeparatorLight { get { return Color.Transparent; } }
    }

    internal sealed class GxMenuRenderer : ToolStripProfessionalRenderer
    {
        public GxMenuRenderer() : base(new GxColorTable()) { }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            if (!e.Item.Enabled)
            {
                e.TextColor = Theme.Muted;
            }
            else
            {
                e.TextColor = Theme.Text;
            }
            base.OnRenderItemText(e);
        }
    }
}

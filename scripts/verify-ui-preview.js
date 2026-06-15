const path = require("path");
const fs = require("fs");
const { chromium } = require("playwright");

const root = path.resolve(__dirname, "..");
const preview = "file:///" + path.join(root, "docs", "ui-preview.html").replace(/\\/g, "/");
const output = path.join(root, "screenshots");
fs.mkdirSync(output, { recursive: true });

function readYouTubeShieldsScript() {
  const source = fs.readFileSync(path.join(root, "src", "BrowserForm.cs"), "utf8");
  const start = source.indexOf("private static string YouTubeShieldsScript(bool enabled)");
  if (start < 0) {
    throw new Error("Could not find YouTubeShieldsScript in BrowserForm.cs");
  }
  const body = source.slice(start);
  const match = body.match(/return @"([\s\S]*?)"\.Replace\("__GX_ADS_ENABLED__"/);
  if (!match) {
    throw new Error("Could not extract YouTubeShieldsScript from BrowserForm.cs");
  }
  return match[1].replace(/\"\"/g, "\"").replace("__GX_ADS_ENABLED__", "true");
}

function verifyInternalPageRoutes() {
  const browserForm = fs.readFileSync(path.join(root, "src", "BrowserForm.cs"), "utf8");
  const updateManifest = fs.readFileSync(path.join(root, "src", "UpdateManifest.cs"), "utf8");
  const internalPages = fs.readFileSync(path.join(root, "src", "InternalPages.cs"), "utf8");
  const installer = fs.readFileSync(path.join(root, "installer", "GXLightBrowser.iss"), "utf8");
  const versionInfo = fs.readFileSync(path.join(root, "src", "VersionInfo.cs"), "utf8");
  const updateJson = JSON.parse(fs.readFileSync(path.join(root, "update.json"), "utf8"));
  const buildInstaller = fs.readFileSync(path.join(root, "scripts", "Build-Installer.ps1"), "utf8");
  const requirements = [
    [browserForm.includes('Text = "Gan Browser"'), "Gan Browser window branding is missing"],
    [versionInfo.includes('CurrentVersion = "2.6"'), "Gan Browser 2.6 version is missing"],
    [versionInfo.includes('ReleaseName = "Gan Browser 2.6"'), "Gan Browser release name is missing"],
    [updateJson.sourceUrl === "https://github.com/wiimri/Gan-Browser", "update manifest repository is incorrect"],
    [updateJson.downloadUrl.endsWith("/GanBrowser-Setup-x64.exe"), "Gan permanent installer URL is incorrect"],
    [updateJson.sha256Url.endsWith("/GanBrowser-Setup-x64.sha256.txt"), "Gan installer SHA-256 URL is incorrect"],
    [buildInstaller.includes('"GXLightBrowser-Setup-x64.exe"'), "legacy permanent installer compatibility is missing"],
    [browserForm.includes('pageName == "updated"'), "gxlight://updated route is missing"],
    [browserForm.includes('case "home":'), "internal home fallback route is missing"],
    [browserForm.includes('case "updated":'), "internal update fallback route is missing"],
    [updateManifest.includes("DefaultChangelogUrl"), "remote changelog loading is missing"],
    [internalPages.includes("Bitacora de versiones"), "cumulative update notes UI is missing"],
    [browserForm.includes("ContainsFullScreenElementChanged"), "WebView2 fullscreen handling is missing"],
    [browserForm.includes("Collapse this tab"), "individual compact tab action is missing"],
    [browserForm.includes("SetSelectedTabsCompact"), "selected compact tab action is missing"],
    [browserForm.includes('ConfigureButton(_shield, "Block Ads On"'), "visible Block Ads button is missing"],
    [browserForm.includes("installInitialDataGuard('ytInitialPlayerResponse')"), "initial YouTube player response guard is missing"],
    [browserForm.includes("sanitizePlayerData"), "YouTube player response sanitizer is missing"],
    [browserForm.includes("IMessageFilter"), "WebView2 native shortcut handling is missing"],
    [browserForm.includes("PreFilterMessage"), "native keyboard message routing is missing"],
    [browserForm.includes("IsBrowserShortcut"), "browser shortcut routing is missing"],
    [browserForm.includes("PrepareUpdateAsync"), "background update preparation is missing"],
    [browserForm.includes("DownloadUpdateFileWithRetriesAsync"), "update downloads do not retry transient GitHub failures"],
    [browserForm.includes("new FileInfo(destinationPath).Length == 0"), "empty partial update downloads are not rejected"],
    [browserForm.includes("File.Delete(destinationPath)"), "partial update downloads are not cleaned up"],
    [browserForm.includes('"Mostrar barra de marcadores"'), "bookmarks bar visibility menu action is missing"],
    [browserForm.includes("SetBookmarksBarVisible"), "bookmarks bar visibility persistence is missing"],
    [browserForm.includes("_topLayout.RowStyles[2].Height = bookmarksHeight"), "bookmarks bar fixed-height layout is missing"],
    [browserForm.includes("_topLayout.Padding = new Padding(6, 4, 8, 0)"), "hidden bookmarks bar still reserves bottom padding"],
    [browserForm.includes("new BorderlessTabControl()"), "native TabControl border is still visible around web content"],
    [browserForm.includes("ShowBookmarksBar ? 28 : 0"), "bookmarks bar does not reserve enough height for its controls"],
    [browserForm.includes("e.Clicks >= 3"), "address bar triple-click selection is missing"],
    [browserForm.includes("_tabs.SelectedTab = page;") && browserForm.indexOf("_tabs.SelectedTab = page;") > browserForm.indexOf("await web.EnsureCoreWebView2Async(_environment);"), "new tabs are selected before WebView2 is ready"],
    [browserForm.includes("_tabs.SelectedTab = nextPage;"), "closing an active tab does not select its replacement first"],
    [browserForm.includes("BeginInvoke((MethodInvoker)delegate { DisposeClosedTab(page, tab); })"), "closed tabs are disposed before the replacement can paint"],
    [browserForm.includes("_newTab.Click += async delegate { await CreateTabAsync(HomeUrl); }"), "new tab button still exposes about:blank"],
    [!browserForm.includes('CreateTabAsync("about:blank")'), "normal new-tab flow still opens an empty about:blank page"],
    [browserForm.includes("PrepareNewTabSurfaceAsync"), "new WebViews are exposed before their first rendered surface"],
    [browserForm.includes("await Task.WhenAny(ready.Task, Task.Delay(2000))"), "new-tab surface preparation has no bounded render wait"],
    [browserForm.includes("ControlStyles.OptimizedDoubleBuffer"), "browser shell double buffering is missing"],
    [internalPages.includes("gxlight:download:open:"), "downloads do not open on double-click"],
    [browserForm.includes("string.IsNullOrWhiteSpace(manifest.Sha256Url) || !VerifyInstallerHash"), "updates without SHA-256 are not rejected"],
    [browserForm.includes("/RELAUNCH"), "update relaunch argument is missing"],
    [internalPages.includes("gxlight:update:prepare"), "update preparation action is missing"],
    [installer.includes("RestartApplications=yes"), "installer application restart support is missing"],
    [installer.includes("ShouldLaunchApp"), "installer relaunch condition is missing"],
    [!browserForm.includes("video.currentTime = video.duration"), "YouTube Shields still accelerates ads"],
    [!browserForm.includes("video.playbackRate = 16"), "YouTube Shields still changes playback speed"]
  ];
  for (const [passes, message] of requirements) {
    if (!passes) throw new Error(message);
  }
}

async function main() {
  verifyInternalPageRoutes();
  const browser = await chromium.launch({ channel: "msedge", headless: true });
  const cases = [
    { name: "desktop", width: 1280, height: 720 },
    { name: "compact", width: 760, height: 540 }
  ];

  const results = [];
  for (const testCase of cases) {
    const page = await browser.newPage({
      viewport: { width: testCase.width, height: testCase.height },
      deviceScaleFactor: 1
    });
    await page.goto(preview);

    const report = await page.evaluate(() => {
      const failures = [];
      const doc = document.documentElement;
      if (doc.scrollWidth > window.innerWidth) {
        failures.push(`horizontal overflow: ${doc.scrollWidth}px > ${window.innerWidth}px`);
      }

      const selectors = [".shell", ".top", ".nav", ".address", ".content", ".status"];
      for (const selector of selectors) {
        const node = document.querySelector(selector);
        if (!node) {
          failures.push(`missing ${selector}`);
          continue;
        }
        const rect = node.getBoundingClientRect();
        if (rect.left < -1 || rect.right > window.innerWidth + 1) {
          failures.push(`${selector} out of horizontal viewport`);
        }
        if (rect.top < -1 || rect.bottom > window.innerHeight + 1) {
          failures.push(`${selector} out of vertical viewport`);
        }
      }

      return {
        failures,
        scrollWidth: doc.scrollWidth,
        width: window.innerWidth,
        height: window.innerHeight
      };
    });

    await page.screenshot({
      path: path.join(output, `ui-preview-${testCase.name}.png`),
      fullPage: true
    });

    if (testCase.name === "desktop") {
      await page.locator(".island-bar").click();
      const visibleIslandMembers = await page.locator(".island-member:visible").count();
      if (visibleIslandMembers !== 2) {
        report.failures.push(`collapsed island did not expand: ${visibleIslandMembers}`);
      }
      const selectedOutline = await page.locator(".island-member.multi-selected:visible").count();
      if (selectedOutline !== 1) {
        report.failures.push(`multi-selection is not visibly represented: ${selectedOutline}`);
      }

      await page.locator('.shortcut[data-title="YouTube"]').click({ button: "middle" });
      const afterMiddle = await page.locator(".tab[data-tab]").count();
      if (afterMiddle !== 5) {
        report.failures.push(`middle-click shortcut did not create a tab: ${afterMiddle}`);
      }

      await page.locator('.tab[data-tab="Google"] .close').click();
      const hasGoogle = await page.locator('.tab[data-tab="Google"]').count();
      if (hasGoogle !== 0) {
        report.failures.push("tab close glyph did not close Google tab");
      }

      await page.locator('.tab[data-tab="YouTube"]').first().click({ button: "middle" });
      const youtubeCount = await page.locator('.tab[data-tab="YouTube"]').count();
      if (youtubeCount !== 1) {
        report.failures.push(`middle-click tab close removed wrong count: ${youtubeCount}`);
      }

      await page.locator('.favorite[data-title="GitHub"]').click({ button: "middle" });
      const githubCount = await page.locator('.tab[data-tab="GitHub"]').count();
      if (githubCount !== 1) {
        report.failures.push(`middle-click favorite did not create a tab: ${githubCount}`);
      }

      const transitionStates = [];
      for (let i = 0; i < 5; i += 1) {
        await page.evaluate((index) => makeTab(`Transition ${index}`), i);
        transitionStates.push(await page.locator(".content").evaluate((node) => ({
          width: node.getBoundingClientRect().width,
          height: node.getBoundingClientRect().height,
          state: node.dataset.transitionState
        })));
        await page.locator('.tab[data-tab^="Transition"]').last().locator(".close").click();
      }
      if (transitionStates.some((state) => state.width < 1 || state.height < 1 || state.state !== "content")) {
        report.failures.push(`tab transition exposed empty content: ${JSON.stringify(transitionStates)}`);
      }

      await page.evaluate(() => {
        document.querySelector(".tabs").classList.add("dense");
        for (let i = 0; i < 20; i += 1) makeTab(`Dense ${i}`);
      });
      const denseReport = await page.locator(".tabs").evaluate((tabs) => {
        const entries = [...tabs.querySelectorAll(".tab[data-tab]")];
        return {
          overflow: tabs.scrollWidth > tabs.clientWidth,
          missingIcons: entries.filter((tab) => !tab.querySelector(".favicon")).length,
          narrowest: Math.min(...entries.map((tab) => tab.getBoundingClientRect().width))
        };
      });
      if (denseReport.overflow || denseReport.missingIcons || denseReport.narrowest < 37) {
        report.failures.push(`dense tabs failed: ${JSON.stringify(denseReport)}`);
      }

      await page.locator(".menu-button").click();
      const menuVisible = await page.locator(".menu-preview").isVisible();
      const menuText = await page.locator(".menu-preview").innerText();
      if (!menuVisible || menuText.indexOf("History") < 0 || menuText.indexOf("Downloads") < 0 || menuText.indexOf("Bookmarks") < 0 || menuText.indexOf("Extensions") < 0) {
        report.failures.push("main menu did not expose required sections");
      }
    }

    await page.close();
    results.push({ ...testCase, ...report });
  }

  const youtubePage = await browser.newPage();
  await youtubePage.route("https://www.youtube.com/mock-ad-page", async (route) => {
    await route.fulfill({
      status: 200,
      contentType: "text/html",
      body: `<!doctype html><html><body>
        <div class="html5-video-player ad-showing"><video></video></div>
        <div id="player-ads">ad</div>
        <ytd-promoted-video-renderer>promoted</ytd-promoted-video-renderer>
        <div class="ytp-ad-overlay-container">overlay</div>
        <button class="ytp-ad-skip-button" onclick="document.body.dataset.skip='yes'">Skip</button>
      </body></html>`
    });
  });
  await youtubePage.route("https://www.youtube.com/youtubei/v1/player", async (route) => {
    await route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify({
        videoDetails: { videoId: "content-video" },
        adPlacements: [{ adPlacementRenderer: { config: "ad" } }],
        playerAds: [{ instreamVideoAdRenderer: { id: "ad" } }],
        adSlots: [{ adSlotRenderer: { slotId: "ad" } }]
      })
    });
  });
  await youtubePage.addInitScript(readYouTubeShieldsScript());
  await youtubePage.goto("https://www.youtube.com/mock-ad-page");
  await youtubePage.waitForTimeout(800);
  await youtubePage.evaluate(() => {
    document.querySelector(".html5-video-player").classList.remove("ad-showing");
    window.__gxLightRunYouTubeShields();
  });
  const youtubeReport = await youtubePage.evaluate(async () => {
    const parsed = JSON.parse(JSON.stringify({
      videoDetails: { videoId: "content-video" },
      adPlacements: [{ adPlacementRenderer: {} }],
      playerAds: [{}],
      nested: { adSlots: [{}], keep: true }
    }));
    window.ytInitialPlayerResponse = {
      videoDetails: { videoId: "initial-content" },
      adPlacements: [{}],
      playerAds: [{}]
    };
    const fetched = await fetch("/youtubei/v1/player").then((response) => response.json());
    window.__gxLightAdsEnabled = false;
    const disabledParsed = JSON.parse(JSON.stringify({ adPlacements: [{}], videoDetails: { videoId: "keep" } }));
    return {
      name: "youtube-shields",
      width: window.innerWidth,
      height: window.innerHeight,
      failures: [
        document.body.dataset.skip === "yes" ? null : "skip button was not clicked",
        document.querySelector("#player-ads") ? "player ad container was not removed" : null,
        document.querySelector("ytd-promoted-video-renderer") ? "promoted video renderer was not removed" : null,
        document.querySelector(".ytp-ad-overlay-container") ? "ad overlay was not removed" : null,
        document.querySelector("video").muted ? "video remained muted after the ad" : null,
        document.querySelector("video").playbackRate !== 1 ? "video playback rate was not restored" : null,
        parsed.adPlacements || parsed.playerAds || parsed.nested.adSlots ? "JSON.parse retained player ad data" : null,
        window.ytInitialPlayerResponse.adPlacements || window.ytInitialPlayerResponse.playerAds ? "initial player response retained ad data" : null,
        fetched.adPlacements || fetched.playerAds || fetched.adSlots ? "fetch player response retained ad data" : null,
        fetched.videoDetails && fetched.videoDetails.videoId === "content-video" ? null : "fetch sanitizer removed normal video data",
        disabledParsed.adPlacements ? null : "Block Ads Off did not disable player response sanitation"
      ].filter(Boolean)
    };
  });
  await youtubePage.close();
  results.push(youtubeReport);

  const youtubeFocusPage = await browser.newPage();
  await youtubeFocusPage.route("https://www.youtube.com/mock-comments", async (route) => {
    await route.fulfill({
      status: 200,
      contentType: "text/html",
      body: `<!doctype html><html><body>
        <div class="html5-video-player"></div>
        <button class="ytp-ad-skip-button" style="display:none">Skip</button>
        <div id="comment" contenteditable="true"></div>
      </body></html>`
    });
  });
  await youtubeFocusPage.addInitScript(readYouTubeShieldsScript());
  await youtubeFocusPage.goto("https://www.youtube.com/mock-comments");
  await youtubeFocusPage.locator("#comment").focus();
  await youtubeFocusPage.keyboard.type("comentario de prueba");
  await youtubeFocusPage.waitForTimeout(1300);
  const focusReport = await youtubeFocusPage.evaluate(() => ({
    name: "youtube-comment-focus",
    width: window.innerWidth,
    height: window.innerHeight,
    failures: [
      document.activeElement && document.activeElement.id === "comment" ? null : "comment editor lost focus",
      document.querySelector("#comment").textContent === "comentario de prueba" ? null : "comment text was interrupted"
    ].filter(Boolean)
  }));
  await youtubeFocusPage.close();
  results.push(focusReport);

  const nonYouTubePage = await browser.newPage();
  const nonYouTubeErrors = [];
  nonYouTubePage.on("pageerror", (error) => nonYouTubeErrors.push(error.message));
  await nonYouTubePage.route("https://www.crunchyroll.com/mock-page", async (route) => {
    await route.fulfill({
      status: 200,
      contentType: "text/html",
      body: "<!doctype html><html><body><main>Crunchyroll compatibility mock</main></body></html>"
    });
  });
  await nonYouTubePage.addInitScript(readYouTubeShieldsScript());
  await nonYouTubePage.goto("https://www.crunchyroll.com/mock-page");
  const nonYouTubeReport = await nonYouTubePage.evaluate((errors) => ({
    name: "non-youtube-isolation",
    width: window.innerWidth,
    height: window.innerHeight,
    failures: [
      window.__gxLightYouTubeShieldsInstalled ? "YouTube Shields was installed outside YouTube" : null,
      errors.length ? `page errors: ${errors.join("; ")}` : null
    ].filter(Boolean)
  }), nonYouTubeErrors);
  await nonYouTubePage.close();
  results.push(nonYouTubeReport);

  await browser.close();

  const failed = results.filter((result) => result.failures.length > 0);
  console.log(JSON.stringify(results, null, 2));
  if (failed.length > 0) {
    process.exitCode = 1;
  }
}

main().catch((error) => {
  console.error(error);
  process.exit(1);
});

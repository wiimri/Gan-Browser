using System;
using System.IO;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GXLightBrowser
{
    [DataContract]
    internal sealed class UpdateManifest
    {
        public const string ManifestUrl = "https://raw.githubusercontent.com/wiimri/Gan-Browser/main/update.json";
        public const string DefaultChangelogUrl = "https://raw.githubusercontent.com/wiimri/Gan-Browser/main/CHANGELOG.md";

        private static readonly HttpClient _http = new HttpClient();

        static UpdateManifest()
        {
            _http.Timeout = TimeSpan.FromSeconds(5);
            _http.DefaultRequestHeaders.UserAgent.TryParseAdd("GanBrowser/" + VersionInfo.CurrentVersion);
        }

        [DataMember(Name = "version")]
        public string Version { get; set; }

        [DataMember(Name = "releaseName")]
        public string ReleaseName { get; set; }

        [DataMember(Name = "publishedAt")]
        public string PublishedAt { get; set; }

        [DataMember(Name = "downloadUrl")]
        public string DownloadUrl { get; set; }

        [DataMember(Name = "sha256Url")]
        public string Sha256Url { get; set; }

        [DataMember(Name = "sha256")]
        public string Sha256 { get; set; }

        [DataMember(Name = "sourceUrl")]
        public string SourceUrl { get; set; }

        [DataMember(Name = "changelogUrl")]
        public string ChangelogUrl { get; set; }

        [DataMember(Name = "highlights")]
        public string[] Highlights { get; set; }

        public string ChangelogMarkdown { get; set; }

        public static UpdateManifest LocalFallback()
        {
            return new UpdateManifest
            {
                Version = VersionInfo.CurrentVersion,
                ReleaseName = VersionInfo.ReleaseName,
                PublishedAt = "2026-06-11",
                DownloadUrl = "https://github.com/wiimri/Gan-Browser/releases",
                Sha256Url = string.Empty,
                Sha256 = string.Empty,
                SourceUrl = BrandInfo.RepositoryUrl,
                ChangelogUrl = DefaultChangelogUrl,
                ChangelogMarkdown = string.Empty,
                Highlights = VersionInfo.Highlights()
            };
        }

        public static async Task<UpdateManifest> LoadLatestAsync(CancellationToken ct = default(CancellationToken))
        {
            try
            {
                using (HttpResponseMessage response = await _http.GetAsync(ManifestUrl, ct).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();
                    using (Stream stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                    {
                        DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(UpdateManifest));
                        UpdateManifest manifest = serializer.ReadObject(stream) as UpdateManifest;
                        if (!IsUsable(manifest))
                        {
                            return LocalFallback();
                        }

                        manifest.ChangelogMarkdown = await DownloadChangelogAsync(manifest.ChangelogUrl, ct)
                            .ConfigureAwait(false);
                        return manifest;
                    }
                }
            }
            catch
            {
                return LocalFallback();
            }
        }

        private static async Task<string> DownloadChangelogAsync(string changelogUrl, CancellationToken ct)
        {
            try
            {
                string url = string.IsNullOrWhiteSpace(changelogUrl) ? DefaultChangelogUrl : changelogUrl;
                using (HttpResponseMessage response = await _http.GetAsync(url, ct).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();
                    return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                }
            }
            catch
            {
                return string.Empty;
            }
        }

        private static bool IsUsable(UpdateManifest manifest)
        {
            return manifest != null &&
                !string.IsNullOrWhiteSpace(manifest.Version) &&
                !string.IsNullOrWhiteSpace(manifest.ReleaseName) &&
                manifest.Highlights != null &&
                manifest.Highlights.Length > 0;
        }
    }
}

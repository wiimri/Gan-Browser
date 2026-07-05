using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GXLightBrowser
{
    internal sealed class AdBlocker
    {
        private readonly List<FilterRule> _blockRules = new List<FilterRule>();
        private readonly List<FilterRule> _allowRules = new List<FilterRule>();
        private readonly HashSet<string> _blockDomains = new HashSet<string>();
        private readonly HashSet<string> _allowDomains = new HashSet<string>();

        public int RuleCount
        {
            get { return _blockRules.Count + _allowRules.Count + _blockDomains.Count + _allowDomains.Count; }
        }

        public void Load(string filterPath)
        {
            _blockRules.Clear();
            _allowRules.Clear();
            _blockDomains.Clear();
            _allowDomains.Clear();

            foreach (string line in BuiltInRules())
            {
                AddRule(line);
            }

            if (File.Exists(filterPath))
            {
                foreach (string line in File.ReadAllLines(filterPath, Encoding.UTF8))
                {
                    AddRule(line);
                }
            }
        }

        public bool ShouldBlock(Uri requestUri, Uri documentUri)
        {
            if (requestUri == null)
            {
                return false;
            }

            string scheme = requestUri.Scheme.ToLowerInvariant();
            if (scheme != "http" && scheme != "https")
            {
                return false;
            }

            string request = requestUri.AbsoluteUri.ToLowerInvariant();
            string host = requestUri.Host.ToLowerInvariant();
            string documentHost = documentUri == null ? string.Empty : documentUri.Host.ToLowerInvariant();

            if (IsCriticalYouTubeBlock(request, host))
            {
                return true;
            }

            if (IsSiteCompatibilityAllow(request, host, documentHost))
            {
                return false;
            }

            for (int i = 0; i < _allowRules.Count; i++)
            {
                if (_allowRules[i].Matches(request, host, documentHost))
                {
                    return false;
                }
            }

            if (HasDomainMatch(_allowDomains, host))
            {
                return false;
            }

            if (HasDomainMatch(_blockDomains, host))
            {
                return true;
            }

            for (int i = 0; i < _blockRules.Count; i++)
            {
                if (_blockRules[i].Matches(request, host, documentHost))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsSiteCompatibilityAllow(string request, string host, string documentHost)
        {
            if (IsHostOrSubdomain(documentHost, "youtube.com") ||
                IsHostOrSubdomain(documentHost, "youtu.be") ||
                IsHostOrSubdomain(documentHost, "youtube-nocookie.com"))
            {
                if (IsHostOrSubdomain(host, "googlevideo.com"))
                {
                    return true;
                }

                if (IsHostOrSubdomain(host, "ytimg.com"))
                {
                    return true;
                }

                if (IsHostOrSubdomain(host, "youtubei.googleapis.com") &&
                    (request.IndexOf("/youtubei/v1/player", StringComparison.Ordinal) >= 0 ||
                     request.IndexOf("/youtubei/v1/next", StringComparison.Ordinal) >= 0))
                {
                    return true;
                }

                if (IsHostOrSubdomain(host, "youtube.com") &&
                    (request.IndexOf("/youtubei/v1/player", StringComparison.Ordinal) >= 0 ||
                     request.IndexOf("/youtubei/v1/next", StringComparison.Ordinal) >= 0 ||
                     request.IndexOf("/s/player/", StringComparison.Ordinal) >= 0))
                {
                    return true;
                }
            }

            if (IsHostOrSubdomain(documentHost, "crunchyroll.com"))
            {
                return IsHostOrSubdomain(host, "crunchyroll.com") ||
                    IsHostOrSubdomain(host, "crunchyrollcdn.com") ||
                    IsHostOrSubdomain(host, "vrv.co");
            }

            return false;
        }

        private static bool IsCriticalYouTubeBlock(string request, string host)
        {
            if (!IsHostOrSubdomain(host, "youtube.com") &&
                !IsHostOrSubdomain(host, "youtubei.googleapis.com"))
            {
                return false;
            }

            return request.IndexOf("/youtubei/v1/ads/", StringComparison.Ordinal) >= 0 ||
                request.IndexOf("/youtubei/v1/attestation/", StringComparison.Ordinal) >= 0 ||
                request.IndexOf("/api/stats/ads", StringComparison.Ordinal) >= 0 ||
                request.IndexOf("/api/stats/qoe", StringComparison.Ordinal) >= 0 ||
                request.IndexOf("/api/stats/delayplay", StringComparison.Ordinal) >= 0 ||
                request.IndexOf("/pagead/", StringComparison.Ordinal) >= 0 ||
                request.IndexOf("/get_midroll", StringComparison.Ordinal) >= 0 ||
                request.IndexOf("/ptracking", StringComparison.Ordinal) >= 0;
        }

        private static bool IsHostOrSubdomain(string host, string domain)
        {
            if (string.IsNullOrEmpty(host))
            {
                return false;
            }

            return host == domain || host.EndsWith("." + domain, StringComparison.Ordinal);
        }

        private void AddRule(string rawLine)
        {
            if (string.IsNullOrWhiteSpace(rawLine))
            {
                return;
            }

            string line = rawLine.Trim();
            if (line.Length == 0 || line[0] == '!' || line[0] == '[' || line.StartsWith("#"))
            {
                return;
            }

            int optionsIndex = line.IndexOf('$');
            if (optionsIndex >= 0)
            {
                line = line.Substring(0, optionsIndex);
            }

            if (line.IndexOf("##", StringComparison.Ordinal) >= 0 ||
                line.IndexOf("#@#", StringComparison.Ordinal) >= 0 ||
                line.IndexOf("#?#", StringComparison.Ordinal) >= 0)
            {
                return;
            }

            bool allow = line.StartsWith("@@");
            if (allow)
            {
                line = line.Substring(2);
            }

            FilterRule rule = FilterRule.TryCreate(line);
            if (rule == null)
            {
                return;
            }

            if (allow)
            {
                if (rule.IsDomainRule)
                {
                    _allowDomains.Add(rule.Pattern);
                }
                else
                {
                    _allowRules.Add(rule);
                }
            }
            else
            {
                if (rule.IsDomainRule)
                {
                    _blockDomains.Add(rule.Pattern);
                }
                else
                {
                    _blockRules.Add(rule);
                }
            }
        }

        private static bool HasDomainMatch(HashSet<string> domains, string host)
        {
            if (domains.Count == 0 || string.IsNullOrEmpty(host))
            {
                return false;
            }

            string current = host;
            while (!string.IsNullOrEmpty(current))
            {
                if (domains.Contains(current))
                {
                    return true;
                }

                int dot = current.IndexOf('.');
                if (dot < 0 || dot == current.Length - 1)
                {
                    break;
                }

                current = current.Substring(dot + 1);
            }

            return false;
        }

        private static IEnumerable<string> BuiltInRules()
        {
            yield return "||doubleclick.net^";
            yield return "||googlesyndication.com^";
            yield return "||googleadservices.com^";
            yield return "||adservice.google.com^";
            yield return "||adsystem.com^";
            yield return "||adnxs.com^";
            yield return "||ads-twitter.com^";
            yield return "||facebook.net^";
            yield return "||scorecardresearch.com^";
            yield return "||taboola.com^";
            yield return "||outbrain.com^";
            yield return "||pagead2.googlesyndication.com^";
            yield return "||googleads.g.doubleclick.net^";
            yield return "||2mdn.net^";
            yield return "||adform.net^";
            yield return "||criteo.com^";
            yield return "||criteo.net^";
            yield return "youtube.com/pagead/";
            yield return "youtube.com/api/stats/ads";
            yield return "youtube.com/get_midroll";
            yield return "youtube.com/ptracking";
            yield return "youtube.com/youtubei/v1/log_event";
            yield return "youtube.com/youtubei/v1/ads/";
            yield return "youtube.com/youtubei/v1/attestation/";
            yield return "youtube.com/youtubei/v1/player/get_download_playback";
            yield return "youtube.com/api/stats/qoe";
            yield return "youtube.com/api/stats/delayplay";
            yield return "youtube.com/api/stats/atr";
            yield return "youtubei.googleapis.com/youtubei/v1/ads/";
            yield return "youtubei.googleapis.com/youtubei/v1/attestation/";
            yield return "/pagead/";
            yield return "/ads/";
            yield return "/adserver/";
            yield return "banner_ad";
        }

        private sealed class FilterRule
        {
            private readonly string _pattern;
            private readonly bool _domainRule;

            private FilterRule(string pattern, bool domainRule)
            {
                _pattern = pattern;
                _domainRule = domainRule;
            }

            public string Pattern
            {
                get { return _pattern; }
            }

            public bool IsDomainRule
            {
                get { return _domainRule; }
            }

            public static FilterRule TryCreate(string line)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    return null;
                }

                string normalized = line.Trim().ToLowerInvariant();
                if (normalized.Length < 3)
                {
                    return null;
                }

                if (normalized.StartsWith("||"))
                {
                    normalized = normalized.Substring(2);
                    string trimmed = TrimAtSeparator(normalized);
                    if (normalized.Contains("/") && normalized.IndexOf('/') < normalized.Length - 1)
                    {
                        normalized = normalized.Replace("^", "/").Replace("*", string.Empty);
                        return new FilterRule(normalized, false);
                    }
                    else
                    {
                        normalized = trimmed.TrimStart('.');
                        if (normalized.Length == 0)
                        {
                            return null;
                        }
                        return new FilterRule(normalized, true);
                    }
                }

                if (normalized.StartsWith("|"))
                {
                    normalized = normalized.TrimStart('|');
                }

                normalized = normalized.Replace("^", "/").Replace("*", string.Empty);
                if (normalized.Length < 3)
                {
                    return null;
                }

                return new FilterRule(normalized, false);
            }

            public bool Matches(string request, string host, string documentHost)
            {
                if (_domainRule)
                {
                    return host == _pattern || host.EndsWith("." + _pattern, StringComparison.Ordinal);
                }

                return request.IndexOf(_pattern, StringComparison.Ordinal) >= 0;
            }

            private static string TrimAtSeparator(string value)
            {
                int slash = value.IndexOf('/');
                int caret = value.IndexOf('^');
                int index = -1;

                if (slash >= 0)
                {
                    index = slash;
                }
                if (caret >= 0 && (index < 0 || caret < index))
                {
                    index = caret;
                }

                return index >= 0 ? value.Substring(0, index) : value;
            }
        }
    }
}

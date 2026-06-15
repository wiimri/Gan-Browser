using System;
using System.Collections.Generic;
using System.Text;

namespace GXLightBrowser
{
    internal static class PasswordVaultSecurity
    {
        public static bool MatchesExactHost(string entryUrl, string targetHost)
        {
            Uri entryUri;
            return !string.IsNullOrWhiteSpace(targetHost) &&
                Uri.TryCreate(entryUrl, UriKind.Absolute, out entryUri) &&
                (entryUri.Scheme == Uri.UriSchemeHttp || entryUri.Scheme == Uri.UriSchemeHttps) &&
                string.Equals(entryUri.Host, targetHost, StringComparison.OrdinalIgnoreCase);
        }

        public static string BuildFillScript(string usernameBase64, string passwordBase64)
        {
            return "(function(){" +
                "const dec=v=>new TextDecoder().decode(Uint8Array.from(atob(v),c=>c.charCodeAt(0)));" +
                "const user=dec('" + usernameBase64 + "'),pass=dec('" + passwordBase64 + "');" +
                "const visible=e=>!!(e&&e.offsetParent!==null&&!e.disabled&&!e.readOnly);" +
                "const pw=[...document.querySelectorAll('input[type=password]')].find(visible);" +
                "if(!pw)return 'missing';" +
                "const form=pw.form||document;" +
                "const users=[...form.querySelectorAll('input[type=email],input[autocomplete=username],input[name*=user i],input[name*=email i],input[type=text]')].filter(visible);" +
                "const set=(el,val)=>{const p=Object.getPrototypeOf(el),d=Object.getOwnPropertyDescriptor(p,'value');if(d&&d.set)d.set.call(el,val);else el.value=val;el.dispatchEvent(new Event('input',{bubbles:true}));el.dispatchEvent(new Event('change',{bubbles:true}));};" +
                "if(users.length)set(users[0],user);set(pw,pass);pw.focus();return 'filled';})()";
        }

        public static string BuildAssistScript(List<PasswordVaultEntry> entries)
        {
            StringBuilder accounts = new StringBuilder("[");
            for (int i = 0; i < entries.Count; i++)
            {
                if (i > 0) accounts.Append(',');
                accounts.Append("{i:").Append(i)
                    .Append(",u:'").Append(Convert.ToBase64String(Encoding.UTF8.GetBytes(entries[i].Username ?? string.Empty)))
                    .Append("',n:'").Append(Convert.ToBase64String(Encoding.UTF8.GetBytes(entries[i].Name ?? string.Empty)))
                    .Append("'}");
            }
            accounts.Append(']');

            return "(function(){" +
                "if(window.__ganVaultAssistInstalled)return;window.__ganVaultAssistInstalled=true;" +
                "const accounts=" + accounts + ";" +
                "const dec=v=>new TextDecoder().decode(Uint8Array.from(atob(v),c=>c.charCodeAt(0)));" +
                "const visible=e=>!!(e&&e.offsetParent!==null&&!e.disabled&&!e.readOnly);" +
                "const set=(el,val)=>{const p=Object.getPrototypeOf(el),d=Object.getOwnPropertyDescriptor(p,'value');if(d&&d.set)d.set.call(el,val);else el.value=val;el.dispatchEvent(new Event('input',{bubbles:true}));el.dispatchEvent(new Event('change',{bubbles:true}));};" +
                "const findUser=pw=>{const form=pw.form||document;return [...form.querySelectorAll('input[type=email],input[autocomplete=username],input[name*=user i],input[name*=email i],input[type=text]')].find(visible);};" +
                "const install=()=>{const pw=[...document.querySelectorAll('input[type=password]')].find(visible);if(!pw||pw.dataset.ganVaultReady)return;pw.dataset.ganVaultReady='1';" +
                "const user=findUser(pw);if(user&&accounts.length>0&&!user.value)set(user,dec(accounts[0].u));" +
                "const box=document.createElement('div');box.style.cssText='display:flex;gap:6px;flex-wrap:wrap;margin:6px 0;font:12px Segoe UI,Arial,sans-serif;z-index:2147483647';" +
                "accounts.forEach(a=>{const b=document.createElement('button');b.type='button';b.textContent='Gan: '+(dec(a.u)||dec(a.n)||\"cuenta guardada\");b.style.cssText='border:1px solid #667085;background:#20232d;color:#fff;border-radius:6px;padding:7px 10px;cursor:pointer';" +
                "b.addEventListener('click',()=>{if(user)set(user,dec(a.u));window.chrome.webview.postMessage('ganvault:fill:'+a.i);});box.appendChild(b);});" +
                "pw.insertAdjacentElement('afterend',box);};install();new MutationObserver(install).observe(document.documentElement,{childList:true,subtree:true});})()";
        }
    }
}

using System;

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
    }
}

using System;
using GXLightBrowser;

internal static class PasswordVaultSecurityProbe
{
    private static int Main()
    {
        if (!PasswordVaultSecurity.MatchesExactHost("https://accounts.example.com/login", "accounts.example.com"))
        {
            Console.Error.WriteLine("Expected exact password vault host match.");
            return 1;
        }
        if (!PasswordVaultSecurity.MatchesExactHost("https://desafiolatam.com/login", "cursos.desafiolatam.com"))
        {
            Console.Error.WriteLine("Password vault should match safe subdomains of an imported site.");
            return 1;
        }
        if (!PasswordVaultSecurity.MatchesExactHost("https://www.desafiolatam.com/login", "cursos.desafiolatam.com"))
        {
            Console.Error.WriteLine("Password vault should match safe subdomains when the imported site uses www.");
            return 1;
        }
        if (PasswordVaultSecurity.MatchesExactHost("https://accounts.example.com/login", "evil.example.com") ||
            PasswordVaultSecurity.MatchesExactHost("javascript:alert(1)", "accounts.example.com") ||
            PasswordVaultSecurity.MatchesExactHost("https://evil-desafiolatam.com", "cursos.desafiolatam.com"))
        {
            Console.Error.WriteLine("Password vault accepted an unsafe host match.");
            return 1;
        }

        string script = PasswordVaultSecurity.BuildFillScript("dXNlcg==", "cGFzcw==");
        if (script.IndexOf(".submit(", StringComparison.OrdinalIgnoreCase) >= 0 ||
            script.IndexOf("requestSubmit", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            Console.Error.WriteLine("Password fill script must never submit a form.");
            return 1;
        }

        System.Collections.Generic.List<PasswordVaultEntry> entries = new System.Collections.Generic.List<PasswordVaultEntry>();
        PasswordVaultEntry assistEntry = new PasswordVaultEntry();
        assistEntry.Name = "Example";
        assistEntry.Username = "user@example.com";
        entries.Add(assistEntry);
        string assist = PasswordVaultSecurity.BuildAssistScript(entries);
        if (assist.IndexOf("user@example.com", StringComparison.Ordinal) >= 0 ||
            assist.IndexOf("ganvault:fill:", StringComparison.Ordinal) < 0 ||
            assist.IndexOf("input[type=password]", StringComparison.Ordinal) < 0)
        {
            Console.Error.WriteLine("Password assist must expose only encoded account labels and require selection.");
            return 1;
        }

        PasswordVaultEntry entry = new PasswordVaultEntry();
        entry.SetPassword("secret-probe");
        if (entry.RevealPassword() != "secret-probe" ||
            typeof(PasswordVaultEntry).GetProperty("Password") != null)
        {
            Console.Error.WriteLine("Password vault entries must retain secrets only in protected memory.");
            return 1;
        }

        Console.WriteLine("Password vault security probe passed.");
        return 0;
    }
}

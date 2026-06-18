using System;
using GXLightBrowser;

internal static class PasswordCsvImporterProbe
{
    private static int Main()
    {
        string opera = "\uFEFFname,url,username,password\n" +
            "\"Facebook\",\"https://www.facebook.com/\",\"user@example.com\",\"secret,with,commas\"\n";
        PasswordCsvImportResult operaResult = PasswordCsvImporter.Parse(opera);
        if (operaResult.Entries.Count != 1 ||
            operaResult.Entries[0].Username != "user@example.com" ||
            operaResult.Entries[0].RevealPassword() != "secret,with,commas")
        {
            Console.Error.WriteLine("Opera/Chromium CSV import failed.");
            return 1;
        }

        string chromiumVariant = "origin_url,action_url,username_value,password_value,note\n" +
            "https://example.com,https://example.com/login,me,pw,\"line one\nline two\"\n";
        PasswordCsvImportResult variantResult = PasswordCsvImporter.Parse(chromiumVariant);
        if (variantResult.Entries.Count != 1 ||
            variantResult.Entries[0].Url != "https://example.com" ||
            variantResult.Entries[0].RevealPassword() != "pw" ||
            variantResult.Entries[0].Note.IndexOf("line two", StringComparison.Ordinal) < 0)
        {
            Console.Error.WriteLine("Chromium variant CSV import failed.");
            return 1;
        }

        string txt = "url\tusername\tpassword\nhttps://cursos.desafiolatam.com\talumno@correo.cl\tclave";
        PasswordCsvImportResult txtResult = PasswordCsvImporter.Parse(txt);
        if (txtResult.Entries.Count != 1 ||
            txtResult.Entries[0].Username != "alumno@correo.cl" ||
            txtResult.Entries[0].RevealPassword() != "clave")
        {
            Console.Error.WriteLine("TXT/TSV password import failed.");
            return 1;
        }

        Console.WriteLine("Password CSV importer probe passed.");
        return 0;
    }
}

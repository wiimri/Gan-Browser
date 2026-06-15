using System;
using System.Drawing;
using System.Windows.Forms;

namespace GXLightBrowser
{
    internal sealed class CredentialViewerDialog : Form
    {
        public CredentialViewerDialog(PasswordVaultEntry entry)
        {
            Text = "Credencial desbloqueada";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(560, 245);
            BackColor = Theme.Window;
            ForeColor = Theme.Text;
            Font = new Font("Segoe UI", 9f);

            Label warning = CreateLabel("Windows Hello verifico tu identidad. Cierra esta ventana cuando termines.", 18, 16, 520);
            warning.ForeColor = Theme.Warning;
            Controls.Add(warning);

            Controls.Add(CreateLabel("Sitio", 18, 54, 90));
            TextBox site = CreateValue(entry.Url, 112, 50, 425);
            Controls.Add(site);

            Controls.Add(CreateLabel("Usuario", 18, 94, 90));
            TextBox username = CreateValue(entry.Username, 112, 90, 425);
            Controls.Add(username);

            Controls.Add(CreateLabel("Contraseña", 18, 134, 90));
            TextBox password = CreateValue(entry.RevealPassword(), 112, 130, 425);
            password.UseSystemPasswordChar = true;
            Controls.Add(password);

            CheckBox reveal = new CheckBox();
            reveal.Text = "Mostrar contraseña";
            reveal.Left = 112;
            reveal.Top = 168;
            reveal.Width = 180;
            reveal.ForeColor = Theme.Text;
            reveal.CheckedChanged += delegate { password.UseSystemPasswordChar = !reveal.Checked; };
            Controls.Add(reveal);

            Button close = new Button();
            close.Text = "Cerrar";
            close.DialogResult = DialogResult.OK;
            close.Left = 437;
            close.Top = 196;
            close.Width = 100;
            Controls.Add(close);
            AcceptButton = close;
            CancelButton = close;
        }

        private static Label CreateLabel(string text, int left, int top, int width)
        {
            Label label = new Label();
            label.Text = text;
            label.Left = left;
            label.Top = top;
            label.Width = width;
            label.AutoSize = false;
            return label;
        }

        private static TextBox CreateValue(string value, int left, int top, int width)
        {
            TextBox box = new TextBox();
            box.Text = value ?? string.Empty;
            box.Left = left;
            box.Top = top;
            box.Width = width;
            box.ReadOnly = true;
            box.BackColor = Theme.Address;
            box.ForeColor = Theme.Text;
            box.BorderStyle = BorderStyle.FixedSingle;
            return box;
        }
    }
}

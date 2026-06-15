using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace GXLightBrowser
{
    internal sealed class CredentialSelectionDialog : Form
    {
        private readonly ListBox _accounts = new ListBox();
        private readonly List<PasswordVaultEntry> _entries;

        public PasswordVaultEntry SelectedEntry
        {
            get
            {
                int index = _accounts.SelectedIndex;
                return index >= 0 && index < _entries.Count ? _entries[index] : null;
            }
        }

        public CredentialSelectionDialog(List<PasswordVaultEntry> entries, string host)
        {
            _entries = entries;
            Text = "Seleccionar credencial";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(470, 300);
            BackColor = Theme.Window;
            ForeColor = Theme.Text;
            Font = new Font("Segoe UI", 9f);

            Label title = new Label();
            title.Text = "Credenciales disponibles para " + host;
            title.Left = 18;
            title.Top = 18;
            title.Width = 430;
            Controls.Add(title);

            _accounts.Left = 18;
            _accounts.Top = 50;
            _accounts.Width = 430;
            _accounts.Height = 190;
            _accounts.BackColor = Theme.Panel;
            _accounts.ForeColor = Theme.Text;
            for (int i = 0; i < entries.Count; i++)
            {
                string username = string.IsNullOrWhiteSpace(entries[i].Username) ? "(sin usuario)" : entries[i].Username;
                _accounts.Items.Add(username + "  -  " + (entries[i].Name ?? host));
            }
            if (_accounts.Items.Count > 0)
            {
                _accounts.SelectedIndex = 0;
            }
            Controls.Add(_accounts);

            Button fill = new Button();
            fill.Text = "Continuar";
            fill.DialogResult = DialogResult.OK;
            fill.Left = 238;
            fill.Top = 252;
            fill.Width = 100;
            Controls.Add(fill);

            Button cancel = new Button();
            cancel.Text = "Cancelar";
            cancel.DialogResult = DialogResult.Cancel;
            cancel.Left = 348;
            cancel.Top = 252;
            cancel.Width = 100;
            Controls.Add(cancel);

            AcceptButton = fill;
            CancelButton = cancel;
        }
    }
}

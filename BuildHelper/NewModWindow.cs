using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BuildHelper
{
    public partial class NewModWindow : Form
    {
        public class CreateModEventArgs : EventArgs
        {
            public CreateModEventArgs(string username, string modName)
            {
                Username = username;
                ModName = modName;
            }

            public string Username { get; }
            public string ModName { get; }
        }
        public delegate void CreateModDelegate(object sender, CreateModEventArgs eventArgs);

        public event CreateModDelegate OnCreateClick;

        public NewModWindow()
        {
            InitializeComponent();
        }

        private void ShowValidationError(string errorMessage)
        {
            MessageBox.Show(this, errorMessage, "Validation Error", MessageBoxButtons.OK);
        }

        private void CreateMod_Click(object sender, EventArgs e)
        {
            var _username = username.Text.Trim();
            var _modName = modName.Text.Trim();

            if (_username.Length == 0)
            {
                ShowValidationError("Username is empty!");
                return;
            }

            if (_modName.Length == 0)
            {
                ShowValidationError("Mod name is empty!");
                return;
            }

            OnCreateClick(this, new CreateModEventArgs(_username, _modName));
        }
    }
}

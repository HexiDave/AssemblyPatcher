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
    public partial class MainWindow : Form
    {
        NewModWindow newModWindow;
        SubnauticaFinder subnauticaFinder = new SubnauticaFinder();
        ModProjectCreator modProjectCreator = new ModProjectCreator();

        public MainWindow()
        {
            InitializeComponent();
            subnauticaFinder.OnLogEvent += OnLogEvent;
            modProjectCreator.OnLogEvent += OnLogEvent;
        }

        private void OnLogEvent(object sender, LogEventArgs eventArgs)
        {
            log.AppendText(eventArgs.LogMessage + "\n");
        }

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.Run(new MainWindow());
        }

        private void NewModToolStripMenuItem_Click(object sender, EventArgs e)
        {
            newModWindow = new NewModWindow();
            newModWindow.OnCreateClick += NewModWindow_OnCreateClick;
            newModWindow.ShowDialog(this);
        }

        private void NewModWindow_OnCreateClick(object sender, NewModWindow.CreateModEventArgs eventArgs)
        {
            newModWindow.Close();
            newModWindow = null;

            // TODO: Separate thread
            log.Text = "Beginning mod project creation...\n";
            if (modProjectCreator.Create(eventArgs.Username, eventArgs.ModName))
            {
                log.AppendText("\n\nSuccess! Reload the solution now to see the new project.");
            }
            else
            {
                log.AppendText("\n\nMod project creation failed.");
            }
        }

        private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void ClearToolStripMenuItem_Click(object sender, EventArgs e)
        {
            log.Text = "";
        }

        private void InitializeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            subnauticaFinder.Start();
        }
    }
}

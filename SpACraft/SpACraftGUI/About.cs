using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace ServerGUI
{
    public sealed partial class About : Form
    {
        public About(string version)
        {
            InitializeComponent();
            this.Text = "About SpACraft";
            this.labelProductName.Text = "SpACraft";
            this.labelVersion.Text = "Version: " + version;
            this.labelCompanyName.Text = "By heldplayer and mbl111";
            this.textBoxDescription.Text = "A minecraft classic server software developed by heldplayer and mbl111 for use by specialattack.net";
        }

        private void tableLayoutPanel_Paint(object sender, PaintEventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start("https://github.com/heldplayer/SpACraft/issues/new");
            }
            catch { }
        }

        private void copyright_Click(object sender, EventArgs e)
        {

        }

        private void labelVersion_Click(object sender, EventArgs e)
        {

        }

        private void textBoxDescription_TextChanged(object sender, EventArgs e)
        {

        }

        private void About_Load(object sender, EventArgs e)
        {

        }
    }
}

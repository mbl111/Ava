using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SpACraft
{
    public partial class ProfileViewer : Form
    {

        Player player;

        public ProfileViewer(Server server)
        {
            InitializeComponent();
            this.comboBox2.Items.AddRange(Server.Players);
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.player = (Player)this.comboBox2.SelectedItem;
            Title = this.player.Info.item;
            this.Rank = this.player.Info.rank;
        }

        private void Map_TextChanged(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {

        }

        private void Kickslabel_Click(object sender, EventArgs e)
        {

        }

        private void kicksbox_TextChanged(object sender, EventArgs e)
        {

        }
    }
}

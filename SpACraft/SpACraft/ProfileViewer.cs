using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SpACraft.Utils;

namespace SpACraft
{
    public partial class ProfileViewer : Form
    {

        Player player;

        public ProfileViewer()
        {
            InitializeComponent();
            this.comboBox2.Items.AddRange(Server.Players);
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.player = (Player)this.comboBox2.SelectedItem;
            update_window();
        }

        private void update_window()
        {
            this.rank.Text = this.player.Info.Rank.name;
            this.iptext.Text = this.player.Info.LastIP+"";
            this.textBox1.Text = this.player.Info.LastLoginDate.Month +"/"+this.player.Info.LastLoginDate.Day +"/"+this.player.Info.LastLoginDate.Year;
            this.textBox2.Text = (System.DateTime.Now.Subtract(this.player.LoginTime) + "");
            this.kicksbox.Text = this.player.kickCount+"";
            this.Displayname.Text = this.player.Info.DisplayedName;
            this.loginmessage.Text = this.player.Info.LoginMessage;
            this.logoutmessage.Text = this.player.Info.LogoutMessage;
            this.playertitle.Text = this.player.Info.Title;
            this.titlecolor.SelectedIndex = Color.ParseToIndex(this.player.Info.TitleColor);
        }

        private void logLine(string message)
        {
            feedback.AppendText(message + Environment.NewLine);
            feedback.ScrollToCaret();
        }

        private void upd_details_Click(object sender, EventArgs e)
        {
            this.player.Info.DisplayedName = this.Displayname.Text;
            this.player.Info.LoginMessage = this.loginmessage.Text;
            this.player.Info.LogoutMessage = this.logoutmessage.Text;
            this.player.Info.Title = this.playertitle.Text;
            this.player.Info.TitleColor = Color.Parse(this.titlecolor.SelectedIndex);
            logLine("Updated Player Info for: " + this.player.Name);
            logLine("Display Name: " + this.player.Info.DisplayedName);
            logLine("Login Message: " + this.player.Info.LoginMessage);
            logLine("Logout Message: " + this.player.Info.LogoutMessage);
            logLine("Player Title: " + this.player.Info.Title);
            logLine("Title Color: " + this.player.Info.TitleColor);
        }

        private void label5_Click(object sender, EventArgs e)
        {

        }
    }
}

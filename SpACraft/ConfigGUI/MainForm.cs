using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ConfigGUI
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
            ColorPicker picker = new ColorPicker("Auto");
            picker.ShowDialog();
            button1.Text = picker.ColorIndex + "";
        }

        private void button1_Click(object sender, EventArgs e)
        {
            ColorPicker picker = new ColorPicker("Clicked");
            picker.ShowDialog();
            button1.Text = picker.ColorIndex + "";
        }
    }
}

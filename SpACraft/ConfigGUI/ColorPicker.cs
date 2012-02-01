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
    public partial class ColorPicker : Form
    {
        public int ColorIndex;

        public ColorPicker(String title)
        {
            InitializeComponent();
            Text = title;
            initButtons();
            initClickActions();
        }

        public ColorPicker()
        {
            InitializeComponent();
            Text = "Colors";
            initButtons();
        }

        public void initButtons()
        {
            //Button Names
            this.button1.Text = "0 - Black";
            this.button2.Text = "1 - Navy";
            this.button3.Text = "2 - Green";
            this.button4.Text = "3 - Teal";
            this.button5.Text = "4 - Dark Red";
            this.button6.Text = "5 - Purple";
            this.button7.Text = "6 - Gold";
            this.button8.Text = "7 - Light Grey";
            this.button9.Text = "8 - Dark Grey";
            this.button10.Text = "9 - Blue";
            this.button11.Text = "a - Lime";
            this.button12.Text = "b - Aqua";
            this.button13.Text = "c - Red";
            this.button14.Text = "d - Magenta";
            this.button15.Text = "e - Yellow";
            this.button16.Text = "f - White";
            //Background Colors
            this.button1.BackColor = Color.Black;
            this.button2.BackColor = Color.Navy;
            this.button3.BackColor = Color.Green;
            this.button4.BackColor = Color.Teal;
            this.button5.BackColor = Color.DarkRed;
            this.button6.BackColor = Color.Purple;
            this.button7.BackColor = Color.Gold;
            this.button8.BackColor = Color.LightGray;
            this.button9.BackColor = Color.DarkGray;
            this.button10.BackColor = Color.Blue;
            this.button11.BackColor = Color.Lime;
            this.button12.BackColor = Color.Aqua;
            this.button13.BackColor = Color.Red;
            this.button14.BackColor = Color.Magenta;
            this.button15.BackColor = Color.Yellow;
            this.button16.BackColor = Color.White;
            //Text colors
            this.button1.ForeColor = Color.White;
            this.button2.ForeColor = Color.White;
            this.button3.ForeColor = Color.White;
            this.button4.ForeColor = Color.White;
            this.button5.ForeColor = Color.White;
            this.button6.ForeColor = Color.White;
            this.button7.ForeColor = Color.Black;
            this.button8.ForeColor = Color.Black;
            this.button9.ForeColor = Color.White;
            this.button10.ForeColor = Color.Black;
            this.button11.ForeColor = Color.Black;
            this.button12.ForeColor = Color.Black;
            this.button13.ForeColor = Color.Black;
            this.button14.ForeColor = Color.Black;
            this.button15.ForeColor = Color.Black;
            this.button16.ForeColor = Color.Black;
        }

        public void initClickActions()
        {
            button1.Click += delegate { ColorIndex = 0; DialogResult = DialogResult.OK; Close(); };
            button2.Click += delegate { ColorIndex = 1; DialogResult = DialogResult.OK; Close(); };
            button3.Click += delegate { ColorIndex = 2; DialogResult = DialogResult.OK; Close(); };
            button4.Click += delegate { ColorIndex = 3; DialogResult = DialogResult.OK; Close(); };
            button5.Click += delegate { ColorIndex = 4; DialogResult = DialogResult.OK; Close(); };
            button6.Click += delegate { ColorIndex = 5; DialogResult = DialogResult.OK; Close(); };
            button7.Click += delegate { ColorIndex = 6; DialogResult = DialogResult.OK; Close(); };
            button8.Click += delegate { ColorIndex = 7; DialogResult = DialogResult.OK; Close(); };
            button9.Click += delegate { ColorIndex = 8; DialogResult = DialogResult.OK; Close(); };
            button10.Click += delegate { ColorIndex = 9; DialogResult = DialogResult.OK; Close(); };
            button11.Click += delegate { ColorIndex = 10; DialogResult = DialogResult.OK; Close(); };
            button12.Click += delegate { ColorIndex = 11; DialogResult = DialogResult.OK; Close(); };
            button13.Click += delegate { ColorIndex = 12; DialogResult = DialogResult.OK; Close(); };
            button14.Click += delegate { ColorIndex = 13; DialogResult = DialogResult.OK; Close(); };
            button15.Click += delegate { ColorIndex = 14; DialogResult = DialogResult.OK; Close(); };
            button16.Click += delegate { ColorIndex = 15; DialogResult = DialogResult.OK; Close(); };
        }

        private void button1_Click(object sender, EventArgs e)
        {
            
        }
    }
}

namespace ServerGUI
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.Windows.Forms.Label lblServerURL;
            System.Windows.Forms.Label lblOnlinePlayers;
            this.ServerURL = new System.Windows.Forms.TextBox();
            this.onlinePlayers = new System.Windows.Forms.ListBox();
            this.ConsoleOutput = new System.Windows.Forms.TextBox();
            this.ConsoleInput = new SpACraft.ServerGUI.ConsoleBox();
            this.btnPlay = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            this.About = new System.Windows.Forms.Button();
            this.button3 = new System.Windows.Forms.Button();
            this.Player_Profile = new System.Windows.Forms.Button();
            lblServerURL = new System.Windows.Forms.Label();
            lblOnlinePlayers = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // lblServerURL
            // 
            lblServerURL.AutoSize = true;
            lblServerURL.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            lblServerURL.Location = new System.Drawing.Point(12, 15);
            lblServerURL.Name = "lblServerURL";
            lblServerURL.Size = new System.Drawing.Size(77, 13);
            lblServerURL.TabIndex = 0;
            lblServerURL.Text = "Server URL:";
            // 
            // lblOnlinePlayers
            // 
            lblOnlinePlayers.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            lblOnlinePlayers.AutoSize = true;
            lblOnlinePlayers.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            lblOnlinePlayers.Location = new System.Drawing.Point(735, 15);
            lblOnlinePlayers.Name = "lblOnlinePlayers";
            lblOnlinePlayers.Size = new System.Drawing.Size(48, 13);
            lblOnlinePlayers.TabIndex = 2;
            lblOnlinePlayers.Text = "Players";
            // 
            // ServerURL
            // 
            this.ServerURL.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.ServerURL.Location = new System.Drawing.Point(95, 12);
            this.ServerURL.Name = "ServerURL";
            this.ServerURL.ReadOnly = true;
            this.ServerURL.Size = new System.Drawing.Size(537, 20);
            this.ServerURL.TabIndex = 1;
            this.ServerURL.Text = "Waiting for first hartbeat...";
            // 
            // onlinePlayers
            // 
            this.onlinePlayers.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.onlinePlayers.FormattingEnabled = true;
            this.onlinePlayers.Location = new System.Drawing.Point(638, 42);
            this.onlinePlayers.Name = "onlinePlayers";
            this.onlinePlayers.Size = new System.Drawing.Size(145, 394);
            this.onlinePlayers.TabIndex = 3;
            this.onlinePlayers.SelectedIndexChanged += new System.EventHandler(this.onlinePlayers_SelectedIndexChanged);
            // 
            // ConsoleOutput
            // 
            this.ConsoleOutput.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.ConsoleOutput.Location = new System.Drawing.Point(12, 42);
            this.ConsoleOutput.Multiline = true;
            this.ConsoleOutput.Name = "ConsoleOutput";
            this.ConsoleOutput.ReadOnly = true;
            this.ConsoleOutput.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.ConsoleOutput.Size = new System.Drawing.Size(620, 397);
            this.ConsoleOutput.TabIndex = 4;
            // 
            // ConsoleInput
            // 
            this.ConsoleInput.Location = new System.Drawing.Point(15, 445);
            this.ConsoleInput.Name = "ConsoleInput";
            this.ConsoleInput.Size = new System.Drawing.Size(768, 20);
            this.ConsoleInput.TabIndex = 5;
            this.ConsoleInput.Text = "Starting the server, please wait...";
            // 
            // btnPlay
            // 
            this.btnPlay.Location = new System.Drawing.Point(638, 12);
            this.btnPlay.Name = "btnPlay";
            this.btnPlay.Size = new System.Drawing.Size(64, 20);
            this.btnPlay.TabIndex = 6;
            this.btnPlay.Text = "Play";
            this.btnPlay.UseVisualStyleBackColor = true;
            this.btnPlay.Click += new System.EventHandler(this.btnPlay_Click);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(15, 472);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(105, 23);
            this.button1.TabIndex = 7;
            this.button1.Text = "Maps";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // About
            // 
            this.About.Location = new System.Drawing.Point(230, 472);
            this.About.Name = "About";
            this.About.Size = new System.Drawing.Size(91, 23);
            this.About.TabIndex = 8;
            this.About.Text = "About";
            this.About.UseVisualStyleBackColor = true;
            this.About.Click += new System.EventHandler(this.About_Click_1);
            // 
            // button3
            // 
            this.button3.Location = new System.Drawing.Point(126, 472);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(98, 23);
            this.button3.TabIndex = 9;
            this.button3.Text = "Op/Admin Chat";
            this.button3.UseVisualStyleBackColor = true;
            // 
            // Player_Profile
            // 
            this.Player_Profile.Location = new System.Drawing.Point(638, 471);
            this.Player_Profile.Name = "Player_Profile";
            this.Player_Profile.Size = new System.Drawing.Size(144, 23);
            this.Player_Profile.TabIndex = 10;
            this.Player_Profile.Text = "Player Profile";
            this.Player_Profile.UseVisualStyleBackColor = true;
            this.Player_Profile.Click += new System.EventHandler(this.Player_Profile_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(795, 497);
            this.Controls.Add(this.Player_Profile);
            this.Controls.Add(this.button3);
            this.Controls.Add(this.About);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.btnPlay);
            this.Controls.Add(this.ConsoleInput);
            this.Controls.Add(this.ConsoleOutput);
            this.Controls.Add(this.onlinePlayers);
            this.Controls.Add(lblOnlinePlayers);
            this.Controls.Add(this.ServerURL);
            this.Controls.Add(lblServerURL);
            this.Name = "MainForm";
            this.Text = "SpACraft";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox ServerURL;
        private System.Windows.Forms.ListBox onlinePlayers;
        private System.Windows.Forms.TextBox ConsoleOutput;
        private SpACraft.ServerGUI.ConsoleBox ConsoleInput;
        private System.Windows.Forms.Button btnPlay;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button About;
        private System.Windows.Forms.Button button3;
        private System.Windows.Forms.Button Player_Profile;
    }
}


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
            this.ServerURL.TextChanged += new System.EventHandler(this.ServerURL_TextChanged);
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
            this.ConsoleInput.OnCommand += new System.Action(this.ConsoleInput_OnCommand);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(795, 477);
            this.Controls.Add(this.ConsoleInput);
            this.Controls.Add(this.ConsoleOutput);
            this.Controls.Add(this.onlinePlayers);
            this.Controls.Add(lblOnlinePlayers);
            this.Controls.Add(this.ServerURL);
            this.Controls.Add(lblServerURL);
            this.Name = "MainForm";
            this.Text = "SpACraft";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox ServerURL;
        private System.Windows.Forms.ListBox onlinePlayers;
        private System.Windows.Forms.TextBox ConsoleOutput;
        private SpACraft.ServerGUI.ConsoleBox ConsoleInput;
    }
}


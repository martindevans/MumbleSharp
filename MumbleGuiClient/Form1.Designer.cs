﻿namespace MumbleGuiClient
{
    partial class Form1
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
            this.components = new System.ComponentModel.Container();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.tbLog = new System.Windows.Forms.RichTextBox();
            this.tbSendMessage = new System.Windows.Forms.TextBox();
            this.btnSend = new System.Windows.Forms.Button();
            this.tvUsers = new System.Windows.Forms.TreeView();
            this.mumbleUpdater = new System.Windows.Forms.Timer(this.components);
            this.button1 = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.button1);
            this.splitContainer1.Panel1.Controls.Add(this.tbLog);
            this.splitContainer1.Panel1.Controls.Add(this.tbSendMessage);
            this.splitContainer1.Panel1.Controls.Add(this.btnSend);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.tvUsers);
            this.splitContainer1.Size = new System.Drawing.Size(621, 242);
            this.splitContainer1.SplitterDistance = 374;
            this.splitContainer1.TabIndex = 0;
            // 
            // tbLog
            // 
            this.tbLog.Location = new System.Drawing.Point(0, 0);
            this.tbLog.Name = "tbLog";
            this.tbLog.Size = new System.Drawing.Size(276, 216);
            this.tbLog.TabIndex = 0;
            this.tbLog.Text = "";
            // 
            // tbSendMessage
            // 
            this.tbSendMessage.Location = new System.Drawing.Point(3, 219);
            this.tbSendMessage.Name = "tbSendMessage";
            this.tbSendMessage.Size = new System.Drawing.Size(273, 20);
            this.tbSendMessage.TabIndex = 1;
            // 
            // btnSend
            // 
            this.btnSend.Location = new System.Drawing.Point(279, 219);
            this.btnSend.Name = "btnSend";
            this.btnSend.Size = new System.Drawing.Size(92, 20);
            this.btnSend.TabIndex = 2;
            this.btnSend.Text = "send";
            this.btnSend.UseVisualStyleBackColor = true;
            this.btnSend.Click += new System.EventHandler(this.btnSend_Click);
            // 
            // tvUsers
            // 
            this.tvUsers.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tvUsers.Location = new System.Drawing.Point(0, 0);
            this.tvUsers.Name = "tvUsers";
            this.tvUsers.Size = new System.Drawing.Size(243, 242);
            this.tvUsers.TabIndex = 0;
            this.tvUsers.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.tvUsers_MouseDoubleClick);
            // 
            // mumbleUpdater
            // 
            this.mumbleUpdater.Enabled = true;
            this.mumbleUpdater.Interval = 10;
            this.mumbleUpdater.Tick += new System.EventHandler(this.mumbleUpdater_Tick);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(282, 12);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(89, 23);
            this.button1.TabIndex = 3;
            this.button1.Text = "record";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // Form1
            // 
            this.AcceptButton = this.btnSend;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(621, 242);
            this.Controls.Add(this.splitContainer1);
            this.Name = "Form1";
            this.Text = "MumbleSharp Server - GUI version";
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel1.PerformLayout();
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.RichTextBox tbLog;
        private System.Windows.Forms.TextBox tbSendMessage;
        private System.Windows.Forms.Button btnSend;
        private System.Windows.Forms.TreeView tvUsers;
        private System.Windows.Forms.Timer mumbleUpdater;
        private System.Windows.Forms.Button button1;
    }
}


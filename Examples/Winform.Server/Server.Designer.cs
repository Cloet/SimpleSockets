namespace Winform.Server
{
	partial class Server
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
			this.lstClients = new System.Windows.Forms.ListView();
			this.Number = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.LocalIPv4 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.RemoteIPv4 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.LocalIPv6 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.RemoteIPv6 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.richTextBox1 = new System.Windows.Forms.RichTextBox();
			this.btnSendFile = new System.Windows.Forms.Button();
			this.label5 = new System.Windows.Forms.Label();
			this.txtFileDestination = new System.Windows.Forms.TextBox();
			this.btnBrowseFile = new System.Windows.Forms.Button();
			this.label6 = new System.Windows.Forms.Label();
			this.txtSourceFile = new System.Windows.Forms.TextBox();
			this.btnSendFolder = new System.Windows.Forms.Button();
			this.label4 = new System.Windows.Forms.Label();
			this.txtDestinatioinFolder = new System.Windows.Forms.TextBox();
			this.btnBrowseFolder = new System.Windows.Forms.Button();
			this.label3 = new System.Windows.Forms.Label();
			this.txtSourceFolder = new System.Windows.Forms.TextBox();
			this.richTextBox2 = new System.Windows.Forms.RichTextBox();
			this.rtMessageCustom = new System.Windows.Forms.Label();
			this.btnSendCustom = new System.Windows.Forms.Button();
			this.rtHeader = new System.Windows.Forms.RichTextBox();
			this.label2 = new System.Windows.Forms.Label();
			this.btnMessage = new System.Windows.Forms.Button();
			this.rtMessage = new System.Windows.Forms.RichTextBox();
			this.label1 = new System.Windows.Forms.Label();
			this.label7 = new System.Windows.Forms.Label();
			this.label8 = new System.Windows.Forms.Label();
			this.txtClient = new System.Windows.Forms.TextBox();
			this.SuspendLayout();
			// 
			// lstClients
			// 
			this.lstClients.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.lstClients.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.Number,
            this.LocalIPv4,
            this.RemoteIPv4,
            this.LocalIPv6,
            this.RemoteIPv6});
			this.lstClients.Location = new System.Drawing.Point(-1, -1);
			this.lstClients.Name = "lstClients";
			this.lstClients.Size = new System.Drawing.Size(918, 175);
			this.lstClients.TabIndex = 0;
			this.lstClients.UseCompatibleStateImageBehavior = false;
			this.lstClients.View = System.Windows.Forms.View.Details;
			// 
			// Number
			// 
			this.Number.Text = "ID";
			this.Number.Width = 58;
			// 
			// LocalIPv4
			// 
			this.LocalIPv4.Text = "Local IPv4";
			this.LocalIPv4.Width = 130;
			// 
			// RemoteIPv4
			// 
			this.RemoteIPv4.Text = "Remote IPv4";
			this.RemoteIPv4.Width = 130;
			// 
			// LocalIPv6
			// 
			this.LocalIPv6.Text = "LocalIPv6";
			this.LocalIPv6.Width = 130;
			// 
			// RemoteIPv6
			// 
			this.RemoteIPv6.Text = "Remote IPv6";
			this.RemoteIPv6.Width = 135;
			// 
			// richTextBox1
			// 
			this.richTextBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.richTextBox1.Location = new System.Drawing.Point(-1, 196);
			this.richTextBox1.Name = "richTextBox1";
			this.richTextBox1.ReadOnly = true;
			this.richTextBox1.Size = new System.Drawing.Size(1047, 301);
			this.richTextBox1.TabIndex = 1;
			this.richTextBox1.Text = "";
			// 
			// btnSendFile
			// 
			this.btnSendFile.Location = new System.Drawing.Point(1358, 467);
			this.btnSendFile.Name = "btnSendFile";
			this.btnSendFile.Size = new System.Drawing.Size(61, 23);
			this.btnSendFile.TabIndex = 40;
			this.btnSendFile.Text = "Send";
			this.btnSendFile.UseVisualStyleBackColor = true;
			this.btnSendFile.Click += new System.EventHandler(this.BtnSendFile_Click);
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Location = new System.Drawing.Point(1052, 452);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(79, 13);
			this.label5.TabIndex = 39;
			this.label5.Text = "Destination File";
			// 
			// txtFileDestination
			// 
			this.txtFileDestination.Location = new System.Drawing.Point(1055, 468);
			this.txtFileDestination.Name = "txtFileDestination";
			this.txtFileDestination.Size = new System.Drawing.Size(297, 20);
			this.txtFileDestination.TabIndex = 38;
			// 
			// btnBrowseFile
			// 
			this.btnBrowseFile.Location = new System.Drawing.Point(1358, 430);
			this.btnBrowseFile.Name = "btnBrowseFile";
			this.btnBrowseFile.Size = new System.Drawing.Size(61, 23);
			this.btnBrowseFile.TabIndex = 37;
			this.btnBrowseFile.Text = "...";
			this.btnBrowseFile.UseVisualStyleBackColor = true;
			this.btnBrowseFile.Click += new System.EventHandler(this.BtnBrowseFile_Click);
			// 
			// label6
			// 
			this.label6.AutoSize = true;
			this.label6.Location = new System.Drawing.Point(1052, 415);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(60, 13);
			this.label6.TabIndex = 36;
			this.label6.Text = "Source File";
			// 
			// txtSourceFile
			// 
			this.txtSourceFile.Location = new System.Drawing.Point(1055, 431);
			this.txtSourceFile.Name = "txtSourceFile";
			this.txtSourceFile.ReadOnly = true;
			this.txtSourceFile.Size = new System.Drawing.Size(297, 20);
			this.txtSourceFile.TabIndex = 35;
			// 
			// btnSendFolder
			// 
			this.btnSendFolder.Location = new System.Drawing.Point(1358, 377);
			this.btnSendFolder.Name = "btnSendFolder";
			this.btnSendFolder.Size = new System.Drawing.Size(61, 23);
			this.btnSendFolder.TabIndex = 34;
			this.btnSendFolder.Text = "Send";
			this.btnSendFolder.UseVisualStyleBackColor = true;
			this.btnSendFolder.Click += new System.EventHandler(this.BtnSendFolder_Click);
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(1052, 362);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(92, 13);
			this.label4.TabIndex = 33;
			this.label4.Text = "Destination Folder";
			// 
			// txtDestinatioinFolder
			// 
			this.txtDestinatioinFolder.Location = new System.Drawing.Point(1055, 378);
			this.txtDestinatioinFolder.Name = "txtDestinatioinFolder";
			this.txtDestinatioinFolder.Size = new System.Drawing.Size(297, 20);
			this.txtDestinatioinFolder.TabIndex = 32;
			// 
			// btnBrowseFolder
			// 
			this.btnBrowseFolder.Location = new System.Drawing.Point(1358, 340);
			this.btnBrowseFolder.Name = "btnBrowseFolder";
			this.btnBrowseFolder.Size = new System.Drawing.Size(61, 23);
			this.btnBrowseFolder.TabIndex = 31;
			this.btnBrowseFolder.Text = "...";
			this.btnBrowseFolder.UseVisualStyleBackColor = true;
			this.btnBrowseFolder.Click += new System.EventHandler(this.BtnBrowseFolder_Click);
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(1052, 325);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(73, 13);
			this.label3.TabIndex = 30;
			this.label3.Text = "Source Folder";
			// 
			// txtSourceFolder
			// 
			this.txtSourceFolder.Location = new System.Drawing.Point(1055, 341);
			this.txtSourceFolder.Name = "txtSourceFolder";
			this.txtSourceFolder.ReadOnly = true;
			this.txtSourceFolder.Size = new System.Drawing.Size(297, 20);
			this.txtSourceFolder.TabIndex = 29;
			// 
			// richTextBox2
			// 
			this.richTextBox2.Location = new System.Drawing.Point(1133, 177);
			this.richTextBox2.Name = "richTextBox2";
			this.richTextBox2.Size = new System.Drawing.Size(278, 96);
			this.richTextBox2.TabIndex = 28;
			this.richTextBox2.Text = "";
			// 
			// rtMessageCustom
			// 
			this.rtMessageCustom.AutoSize = true;
			this.rtMessageCustom.Location = new System.Drawing.Point(1130, 161);
			this.rtMessageCustom.Name = "rtMessageCustom";
			this.rtMessageCustom.Size = new System.Drawing.Size(50, 13);
			this.rtMessageCustom.TabIndex = 27;
			this.rtMessageCustom.Text = "Message";
			// 
			// btnSendCustom
			// 
			this.btnSendCustom.Location = new System.Drawing.Point(1055, 279);
			this.btnSendCustom.Name = "btnSendCustom";
			this.btnSendCustom.Size = new System.Drawing.Size(356, 23);
			this.btnSendCustom.TabIndex = 26;
			this.btnSendCustom.Text = "Send";
			this.btnSendCustom.UseVisualStyleBackColor = true;
			this.btnSendCustom.Click += new System.EventHandler(this.BtnSendCustom_Click);
			// 
			// rtHeader
			// 
			this.rtHeader.Location = new System.Drawing.Point(1055, 177);
			this.rtHeader.Name = "rtHeader";
			this.rtHeader.Size = new System.Drawing.Size(72, 96);
			this.rtHeader.TabIndex = 25;
			this.rtHeader.Text = "";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(1052, 161);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(42, 13);
			this.label2.TabIndex = 24;
			this.label2.Text = "Header";
			// 
			// btnMessage
			// 
			this.btnMessage.Location = new System.Drawing.Point(1055, 125);
			this.btnMessage.Name = "btnMessage";
			this.btnMessage.Size = new System.Drawing.Size(356, 23);
			this.btnMessage.TabIndex = 23;
			this.btnMessage.Text = "Send";
			this.btnMessage.UseVisualStyleBackColor = true;
			this.btnMessage.Click += new System.EventHandler(this.BtnMessage_Click);
			// 
			// rtMessage
			// 
			this.rtMessage.Location = new System.Drawing.Point(1055, 23);
			this.rtMessage.Name = "rtMessage";
			this.rtMessage.Size = new System.Drawing.Size(356, 96);
			this.rtMessage.TabIndex = 22;
			this.rtMessage.Text = "";
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(1052, 7);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(50, 13);
			this.label1.TabIndex = 21;
			this.label1.Text = "Message";
			// 
			// label7
			// 
			this.label7.AutoSize = true;
			this.label7.Location = new System.Drawing.Point(5, 180);
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size(30, 13);
			this.label7.TabIndex = 41;
			this.label7.Text = "Logs";
			// 
			// label8
			// 
			this.label8.AutoSize = true;
			this.label8.Location = new System.Drawing.Point(923, 9);
			this.label8.Name = "label8";
			this.label8.Size = new System.Drawing.Size(71, 13);
			this.label8.TabIndex = 42;
			this.label8.Text = "Choose client";
			// 
			// txtClient
			// 
			this.txtClient.Location = new System.Drawing.Point(923, 25);
			this.txtClient.Name = "txtClient";
			this.txtClient.Size = new System.Drawing.Size(100, 20);
			this.txtClient.TabIndex = 43;
			// 
			// Server
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(1423, 498);
			this.Controls.Add(this.txtClient);
			this.Controls.Add(this.label8);
			this.Controls.Add(this.label7);
			this.Controls.Add(this.btnSendFile);
			this.Controls.Add(this.label5);
			this.Controls.Add(this.txtFileDestination);
			this.Controls.Add(this.btnBrowseFile);
			this.Controls.Add(this.label6);
			this.Controls.Add(this.txtSourceFile);
			this.Controls.Add(this.btnSendFolder);
			this.Controls.Add(this.label4);
			this.Controls.Add(this.txtDestinatioinFolder);
			this.Controls.Add(this.btnBrowseFolder);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.txtSourceFolder);
			this.Controls.Add(this.richTextBox2);
			this.Controls.Add(this.rtMessageCustom);
			this.Controls.Add(this.btnSendCustom);
			this.Controls.Add(this.rtHeader);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.btnMessage);
			this.Controls.Add(this.rtMessage);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.richTextBox1);
			this.Controls.Add(this.lstClients);
			this.Name = "Server";
			this.Text = "Form1";
			this.Load += new System.EventHandler(this.Server_Load);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.ListView lstClients;
		private System.Windows.Forms.ColumnHeader Number;
		private System.Windows.Forms.ColumnHeader LocalIPv4;
		private System.Windows.Forms.ColumnHeader RemoteIPv4;
		private System.Windows.Forms.ColumnHeader LocalIPv6;
		private System.Windows.Forms.ColumnHeader RemoteIPv6;
		private System.Windows.Forms.RichTextBox richTextBox1;
		private System.Windows.Forms.Button btnSendFile;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.TextBox txtFileDestination;
		private System.Windows.Forms.Button btnBrowseFile;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.TextBox txtSourceFile;
		private System.Windows.Forms.Button btnSendFolder;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.TextBox txtDestinatioinFolder;
		private System.Windows.Forms.Button btnBrowseFolder;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.TextBox txtSourceFolder;
		private System.Windows.Forms.RichTextBox richTextBox2;
		private System.Windows.Forms.Label rtMessageCustom;
		private System.Windows.Forms.Button btnSendCustom;
		private System.Windows.Forms.RichTextBox rtHeader;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Button btnMessage;
		private System.Windows.Forms.RichTextBox rtMessage;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label7;
		private System.Windows.Forms.Label label8;
		private System.Windows.Forms.TextBox txtClient;
	}
}


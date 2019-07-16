namespace Winform.Client
{
	partial class Client
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
			this.richTextBox1 = new System.Windows.Forms.RichTextBox();
			this.label1 = new System.Windows.Forms.Label();
			this.rtMessage = new System.Windows.Forms.RichTextBox();
			this.btnMessage = new System.Windows.Forms.Button();
			this.btnSendCustom = new System.Windows.Forms.Button();
			this.rtHeader = new System.Windows.Forms.RichTextBox();
			this.label2 = new System.Windows.Forms.Label();
			this.richTextBox2 = new System.Windows.Forms.RichTextBox();
			this.rtMessageCustom = new System.Windows.Forms.Label();
			this.txtSourceFolder = new System.Windows.Forms.TextBox();
			this.label3 = new System.Windows.Forms.Label();
			this.btnBrowseFolder = new System.Windows.Forms.Button();
			this.btnSendFolder = new System.Windows.Forms.Button();
			this.label4 = new System.Windows.Forms.Label();
			this.txtDestinatioinFolder = new System.Windows.Forms.TextBox();
			this.btnSendFile = new System.Windows.Forms.Button();
			this.label5 = new System.Windows.Forms.Label();
			this.txtFileDestination = new System.Windows.Forms.TextBox();
			this.btnBrowseFile = new System.Windows.Forms.Button();
			this.label6 = new System.Windows.Forms.Label();
			this.txtSourceFile = new System.Windows.Forms.TextBox();
			this.SuspendLayout();
			// 
			// richTextBox1
			// 
			this.richTextBox1.Location = new System.Drawing.Point(0, -2);
			this.richTextBox1.Name = "richTextBox1";
			this.richTextBox1.ReadOnly = true;
			this.richTextBox1.Size = new System.Drawing.Size(879, 512);
			this.richTextBox1.TabIndex = 0;
			this.richTextBox1.Text = "";
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(885, 9);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(50, 13);
			this.label1.TabIndex = 1;
			this.label1.Text = "Message";
			// 
			// rtMessage
			// 
			this.rtMessage.Location = new System.Drawing.Point(888, 25);
			this.rtMessage.Name = "rtMessage";
			this.rtMessage.Size = new System.Drawing.Size(356, 96);
			this.rtMessage.TabIndex = 2;
			this.rtMessage.Text = "";
			// 
			// btnMessage
			// 
			this.btnMessage.Location = new System.Drawing.Point(888, 127);
			this.btnMessage.Name = "btnMessage";
			this.btnMessage.Size = new System.Drawing.Size(356, 23);
			this.btnMessage.TabIndex = 3;
			this.btnMessage.Text = "Send";
			this.btnMessage.UseVisualStyleBackColor = true;
			this.btnMessage.Click += new System.EventHandler(this.BtnMessage_Click);
			// 
			// btnSendCustom
			// 
			this.btnSendCustom.Location = new System.Drawing.Point(888, 281);
			this.btnSendCustom.Name = "btnSendCustom";
			this.btnSendCustom.Size = new System.Drawing.Size(356, 23);
			this.btnSendCustom.TabIndex = 6;
			this.btnSendCustom.Text = "Send";
			this.btnSendCustom.UseVisualStyleBackColor = true;
			this.btnSendCustom.Click += new System.EventHandler(this.BtnSendCustom_Click);
			// 
			// rtHeader
			// 
			this.rtHeader.Location = new System.Drawing.Point(888, 179);
			this.rtHeader.Name = "rtHeader";
			this.rtHeader.Size = new System.Drawing.Size(72, 96);
			this.rtHeader.TabIndex = 5;
			this.rtHeader.Text = "";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(885, 163);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(42, 13);
			this.label2.TabIndex = 4;
			this.label2.Text = "Header";
			// 
			// richTextBox2
			// 
			this.richTextBox2.Location = new System.Drawing.Point(966, 179);
			this.richTextBox2.Name = "richTextBox2";
			this.richTextBox2.Size = new System.Drawing.Size(278, 96);
			this.richTextBox2.TabIndex = 8;
			this.richTextBox2.Text = "";
			// 
			// rtMessageCustom
			// 
			this.rtMessageCustom.AutoSize = true;
			this.rtMessageCustom.Location = new System.Drawing.Point(963, 163);
			this.rtMessageCustom.Name = "rtMessageCustom";
			this.rtMessageCustom.Size = new System.Drawing.Size(50, 13);
			this.rtMessageCustom.TabIndex = 7;
			this.rtMessageCustom.Text = "Message";
			// 
			// txtSourceFolder
			// 
			this.txtSourceFolder.Location = new System.Drawing.Point(888, 343);
			this.txtSourceFolder.Name = "txtSourceFolder";
			this.txtSourceFolder.ReadOnly = true;
			this.txtSourceFolder.Size = new System.Drawing.Size(297, 20);
			this.txtSourceFolder.TabIndex = 9;
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(885, 327);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(73, 13);
			this.label3.TabIndex = 10;
			this.label3.Text = "Source Folder";
			// 
			// btnBrowseFolder
			// 
			this.btnBrowseFolder.Location = new System.Drawing.Point(1191, 342);
			this.btnBrowseFolder.Name = "btnBrowseFolder";
			this.btnBrowseFolder.Size = new System.Drawing.Size(61, 23);
			this.btnBrowseFolder.TabIndex = 11;
			this.btnBrowseFolder.Text = "...";
			this.btnBrowseFolder.UseVisualStyleBackColor = true;
			this.btnBrowseFolder.Click += new System.EventHandler(this.BtnBrowseFolder_Click);
			// 
			// btnSendFolder
			// 
			this.btnSendFolder.Location = new System.Drawing.Point(1191, 379);
			this.btnSendFolder.Name = "btnSendFolder";
			this.btnSendFolder.Size = new System.Drawing.Size(61, 23);
			this.btnSendFolder.TabIndex = 14;
			this.btnSendFolder.Text = "Send";
			this.btnSendFolder.UseVisualStyleBackColor = true;
			this.btnSendFolder.Click += new System.EventHandler(this.BtnSendFolder_Click);
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(885, 364);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(92, 13);
			this.label4.TabIndex = 13;
			this.label4.Text = "Destination Folder";
			// 
			// txtDestinatioinFolder
			// 
			this.txtDestinatioinFolder.Location = new System.Drawing.Point(888, 380);
			this.txtDestinatioinFolder.Name = "txtDestinatioinFolder";
			this.txtDestinatioinFolder.Size = new System.Drawing.Size(297, 20);
			this.txtDestinatioinFolder.TabIndex = 12;
			// 
			// btnSendFile
			// 
			this.btnSendFile.Location = new System.Drawing.Point(1191, 469);
			this.btnSendFile.Name = "btnSendFile";
			this.btnSendFile.Size = new System.Drawing.Size(61, 23);
			this.btnSendFile.TabIndex = 20;
			this.btnSendFile.Text = "Send";
			this.btnSendFile.UseVisualStyleBackColor = true;
			this.btnSendFile.Click += new System.EventHandler(this.BtnSendFile_Click);
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Location = new System.Drawing.Point(885, 454);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(79, 13);
			this.label5.TabIndex = 19;
			this.label5.Text = "Destination File";
			// 
			// txtFileDestination
			// 
			this.txtFileDestination.Location = new System.Drawing.Point(888, 470);
			this.txtFileDestination.Name = "txtFileDestination";
			this.txtFileDestination.Size = new System.Drawing.Size(297, 20);
			this.txtFileDestination.TabIndex = 18;
			// 
			// btnBrowseFile
			// 
			this.btnBrowseFile.Location = new System.Drawing.Point(1191, 432);
			this.btnBrowseFile.Name = "btnBrowseFile";
			this.btnBrowseFile.Size = new System.Drawing.Size(61, 23);
			this.btnBrowseFile.TabIndex = 17;
			this.btnBrowseFile.Text = "...";
			this.btnBrowseFile.UseVisualStyleBackColor = true;
			this.btnBrowseFile.Click += new System.EventHandler(this.BtnBrowseFile_Click);
			// 
			// label6
			// 
			this.label6.AutoSize = true;
			this.label6.Location = new System.Drawing.Point(885, 417);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(60, 13);
			this.label6.TabIndex = 16;
			this.label6.Text = "Source File";
			// 
			// txtSourceFile
			// 
			this.txtSourceFile.Location = new System.Drawing.Point(888, 433);
			this.txtSourceFile.Name = "txtSourceFile";
			this.txtSourceFile.ReadOnly = true;
			this.txtSourceFile.Size = new System.Drawing.Size(297, 20);
			this.txtSourceFile.TabIndex = 15;
			// 
			// Client
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(1256, 509);
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
			this.Name = "Client";
			this.Text = "Form1";
			this.Load += new System.EventHandler(this.Client_Load);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.RichTextBox richTextBox1;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.RichTextBox rtMessage;
		private System.Windows.Forms.Button btnMessage;
		private System.Windows.Forms.Button btnSendCustom;
		private System.Windows.Forms.RichTextBox rtHeader;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.RichTextBox richTextBox2;
		private System.Windows.Forms.Label rtMessageCustom;
		private System.Windows.Forms.TextBox txtSourceFolder;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Button btnBrowseFolder;
		private System.Windows.Forms.Button btnSendFolder;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.TextBox txtDestinatioinFolder;
		private System.Windows.Forms.Button btnSendFile;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.TextBox txtFileDestination;
		private System.Windows.Forms.Button btnBrowseFile;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.TextBox txtSourceFile;
	}
}


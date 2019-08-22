namespace ParrallelSending
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
			this.rtxtServer = new System.Windows.Forms.RichTextBox();
			this.label1 = new System.Windows.Forms.Label();
			this.rtxtClients = new System.Windows.Forms.RichTextBox();
			this.label2 = new System.Windows.Forms.Label();
			this.textBox1 = new System.Windows.Forms.TextBox();
			this.button1 = new System.Windows.Forms.Button();
			this.label3 = new System.Windows.Forms.Label();
			this.button2 = new System.Windows.Forms.Button();
			this.button3 = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// rtxtServer
			// 
			this.rtxtServer.Location = new System.Drawing.Point(12, 89);
			this.rtxtServer.Name = "rtxtServer";
			this.rtxtServer.Size = new System.Drawing.Size(494, 429);
			this.rtxtServer.TabIndex = 0;
			this.rtxtServer.Text = "";
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(13, 70);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(73, 13);
			this.label1.TabIndex = 1;
			this.label1.Text = "Server Output";
			// 
			// rtxtClients
			// 
			this.rtxtClients.Location = new System.Drawing.Point(619, 89);
			this.rtxtClients.Name = "rtxtClients";
			this.rtxtClients.Size = new System.Drawing.Size(494, 429);
			this.rtxtClients.TabIndex = 2;
			this.rtxtClients.Text = "";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(616, 70);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(73, 13);
			this.label2.TabIndex = 3;
			this.label2.Text = "Clients Output";
			// 
			// textBox1
			// 
			this.textBox1.Location = new System.Drawing.Point(131, 9);
			this.textBox1.Name = "textBox1";
			this.textBox1.Size = new System.Drawing.Size(188, 20);
			this.textBox1.TabIndex = 4;
			// 
			// button1
			// 
			this.button1.Location = new System.Drawing.Point(325, 7);
			this.button1.Name = "button1";
			this.button1.Size = new System.Drawing.Size(75, 23);
			this.button1.TabIndex = 5;
			this.button1.Text = "Send";
			this.button1.UseVisualStyleBackColor = true;
			this.button1.Click += new System.EventHandler(this.Button1_Click);
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(13, 12);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(112, 13);
			this.label3.TabIndex = 6;
			this.label3.Text = "Message To all clients";
			// 
			// button2
			// 
			this.button2.Location = new System.Drawing.Point(1037, 9);
			this.button2.Name = "button2";
			this.button2.Size = new System.Drawing.Size(75, 23);
			this.button2.TabIndex = 7;
			this.button2.Text = "Start Clients";
			this.button2.UseVisualStyleBackColor = true;
			this.button2.Click += new System.EventHandler(this.Button2_Click);
			// 
			// button3
			// 
			this.button3.Location = new System.Drawing.Point(557, 7);
			this.button3.Name = "button3";
			this.button3.Size = new System.Drawing.Size(75, 23);
			this.button3.TabIndex = 8;
			this.button3.Text = "Start Server";
			this.button3.UseVisualStyleBackColor = true;
			this.button3.Click += new System.EventHandler(this.Button3_Click);
			// 
			// Form1
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(1125, 530);
			this.Controls.Add(this.button3);
			this.Controls.Add(this.button2);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.button1);
			this.Controls.Add(this.textBox1);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.rtxtClients);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.rtxtServer);
			this.Name = "Form1";
			this.Text = "Form1";
			this.Load += new System.EventHandler(this.Form1_Load);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.RichTextBox rtxtServer;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.RichTextBox rtxtClients;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.TextBox textBox1;
		private System.Windows.Forms.Button button1;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Button button2;
		private System.Windows.Forms.Button button3;
	}
}


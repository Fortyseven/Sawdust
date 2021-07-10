using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Runtime.CompilerServices;
using System.Reflection;

namespace Aural_Probe
{
	/// <summary>
	/// Summary description for AboutForm.
	/// </summary>
	public class AboutForm : System.Windows.Forms.Form
	{
		//private System.Windows.Forms.PictureBox pictureBox1;
		//private System.Windows.Forms.Label label1;
		//private System.Windows.Forms.Label label2;
		//private System.Windows.Forms.LinkLabel linkLabel1;
		private System.Windows.Forms.PictureBox pictureBox2;
		private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
		private System.Windows.Forms.LinkLabel linkLabel3;
		private System.Windows.Forms.Button button1;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public AboutForm()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			//
			// TODO: Add any constructor code after InitializeComponent call
			//
			//linkLabel2.Links.Add(0,5,"mailto:michaeld@4bitterguys.com");
			//linkLabel3.Links.Add(0,30,"http://michael.4bitterguys.com");
			this.label3.Text = "Aural Probe v" + Assembly.GetExecutingAssembly().GetName().Version;
		}

		private void InitializeComponent()
		{
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AboutForm));
            this.pictureBox2 = new System.Windows.Forms.PictureBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.linkLabel3 = new System.Windows.Forms.LinkLabel();
            this.button1 = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).BeginInit();
            this.SuspendLayout();
            // 
            // pictureBox2
            // 
            this.pictureBox2.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.pictureBox2.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox2.Image")));
            this.pictureBox2.Location = new System.Drawing.Point(8, 8);
            this.pictureBox2.Name = "pictureBox2";
            this.pictureBox2.Size = new System.Drawing.Size(128, 128);
            this.pictureBox2.TabIndex = 0;
            this.pictureBox2.TabStop = false;
            // 
            // label3
            // 
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold);
            this.label3.Location = new System.Drawing.Point(144, 24);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(304, 16);
            this.label3.TabIndex = 0;
            // 
            // label4
            // 
            this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F);
            this.label4.Location = new System.Drawing.Point(144, 40);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(152, 16);
            this.label4.TabIndex = 1;
            this.label4.Text = "by Michael Davies";
            // 
            // linkLabel3
            // 
            this.linkLabel3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Underline, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.linkLabel3.ForeColor = System.Drawing.Color.Blue;
            this.linkLabel3.LinkArea = new System.Windows.Forms.LinkArea(0, 33);
            this.linkLabel3.Location = new System.Drawing.Point(144, 72);
            this.linkLabel3.Name = "linkLabel3";
            this.linkLabel3.Size = new System.Drawing.Size(184, 20);
            this.linkLabel3.TabIndex = 3;
            this.linkLabel3.TabStop = true;
            this.linkLabel3.Text = "http://auralprobe.sourceforge.net";
            this.linkLabel3.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabel3_LinkClicked);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(240, 112);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(72, 24);
            this.button1.TabIndex = 4;
            this.button1.Text = "OK";
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // AboutForm
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.ClientSize = new System.Drawing.Size(322, 143);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.pictureBox2);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.linkLabel3);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "AboutForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "About Aural Probe";
            this.TopMost = true;
            this.Load += new System.EventHandler(this.AboutForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).EndInit();
            this.ResumeLayout(false);

		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		private void AboutForm_Load(object sender, System.EventArgs e)
		{
		
		}

		private void button1_Click(object sender, System.EventArgs e)
		{
			Close();
		}

		private void linkLabel3_LinkClicked(object sender, System.Windows.Forms.LinkLabelLinkClickedEventArgs e)
		{
			System.Diagnostics.Process.Start("http://auralprobe.sourceforge.net");
		}

	}
}

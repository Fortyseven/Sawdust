using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Threading;

namespace Aural_Probe
{
	/// <summary>
	/// Summary description for ProgressBar.
	/// </summary>
	public class ProgressBar : System.Windows.Forms.Form
	{
		private System.Windows.Forms.Button button1;
		public System.Windows.Forms.ProgressBar progressBar1;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		#region Public Delegates

		// delegates used to call MainForm functions from worker thread
		public delegate void DelegateUpdate();
        public delegate void DelegateUpdateLabel(string text);
        public delegate void DelegateUpdateMaximumAndStep(int nMaximum, int nStep);
        public delegate void DelegateThreadFinished();

		#endregion

		// worker thread
		Thread m_WorkerThread;

		MainForm m_mainForm;

		// events used to stop worker thread
		ManualResetEvent m_EventStopThread;
		ManualResetEvent m_EventThreadStopped;

		bool m_bUseCache;

		// Delegate instances used to cal user interface functions 
		// from worker thread:
        public DelegateUpdate m_DelegateUpdateForm;
        public DelegateUpdateLabel m_DelegateUpdateLabel;
        public DelegateUpdateMaximumAndStep m_DelegateUpdateMaximumAndStep;
		public System.Windows.Forms.Label labelStatus;
		public DelegateThreadFinished m_DelegateThreadFinished;

        public void InitForm()
        {
            labelStatus.Text = "Please wait...";
            progressBar1.Minimum = 0;
            progressBar1.Step = 1;
            progressBar1.Value = 0;
        }

        public void UpdateMaximumAndStep(int nMaximum, int nStep)
        {
            if (nMaximum >= 0)
                progressBar1.Maximum = nMaximum;
            if (nStep >= 0)
                progressBar1.Step = nStep;
        }

        public void UpdateLabel(string text)
        {
            labelStatus.Text = text;
        }

        public void UpdateForm()
		{
			try
			{
				progressBar1.PerformStep();
				if (m_bUseCache)
					labelStatus.Text = m_mainForm.lnSamples + " sample(s) loaded from cache";
				else
					labelStatus.Text = m_mainForm.lnSamples + " sample(s) found";
			}
			catch (System.Exception e)
			{
				MessageBox.Show("ProgressBar::UpdateForm " + e.ToString(), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
		}

		// Set initial state of controls.
		// Called from worker thread using delegate and Control.Invoke
		private void ThreadFinished()
		{
			try
			{
				DialogResult = DialogResult.OK;
				Close();
			}
			catch (System.Exception e)
			{
				MessageBox.Show("ProgressBar::ThreadFinished " + e.ToString(), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);			
			}
		}

		// Worker thread function.
		// Called indirectly from btnStartThread_Click
		private void WorkerThreadFunction()
		{
			try
			{
				ScanFoldersProcess process = new ScanFoldersProcess(m_EventStopThread, m_EventThreadStopped, m_bUseCache, this, m_mainForm);
				process.Run();
			}
			catch (System.Exception e)
			{
				MessageBox.Show("ProgressBar::WorkerThreadFunction " + e.ToString(), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);						
			}
		}

		// Stop worker thread if it is running.
		// Called when user presses Stop button of form is closed.
		private void StopThread()
		{
			try
			{
				if ( m_WorkerThread != null  &&  m_WorkerThread.IsAlive )  // thread is active
				{
					// set event "Stop"
					m_EventStopThread.Set();

					// wait when thread  will stop or finish
					while (m_WorkerThread.IsAlive)
					{
						// We cannot use here infinite wait because our thread
						// makes syncronous calls to main form, this will cause deadlock.
						// Instead of this we wait for event some appropriate time
						// (and by the way give time to worker thread) and
						// process events. These events may contain Invoke calls.
						if ( WaitHandle.WaitAll(
							(new ManualResetEvent[] {m_EventThreadStopped}), 
							100,
							true) )
						{
							break;
						}

						Application.DoEvents();
					}
				}

				ThreadFinished();		// set initial state of buttons
			}
			catch (System.Exception e)
			{
				MessageBox.Show("ProgressBar::StopThread " + e.ToString(), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);							
			}
		}

		public ProgressBar(MainForm mainForm)
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			m_mainForm = mainForm;

			// initialize delegates
            m_DelegateUpdateMaximumAndStep = new DelegateUpdateMaximumAndStep(this.UpdateMaximumAndStep);
            m_DelegateUpdateLabel = new DelegateUpdateLabel(this.UpdateLabel);
			m_DelegateUpdateForm = new DelegateUpdate(this.UpdateForm);
			m_DelegateThreadFinished = new DelegateThreadFinished(this.ThreadFinished);

			// initialize events
			m_EventStopThread = new ManualResetEvent(false);
			m_EventThreadStopped = new ManualResetEvent(false);

			m_bUseCache = false;

		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
            m_EventStopThread.Close();
            m_EventThreadStopped.Close();
            if (disposing)
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.button1 = new System.Windows.Forms.Button();
			this.progressBar1 = new System.Windows.Forms.ProgressBar();
			this.labelStatus = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// button1
			// 
			this.button1.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
			this.button1.Location = new System.Drawing.Point(104, 72);
			this.button1.Name = "button1";
			this.button1.Size = new System.Drawing.Size(80, 24);
			this.button1.TabIndex = 0;
			this.button1.Text = "Cancel";
			this.button1.Click += new System.EventHandler(this.button1_Click);
			// 
			// progressBar1
			// 
			this.progressBar1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this.progressBar1.Location = new System.Drawing.Point(8, 16);
			this.progressBar1.Name = "progressBar1";
			this.progressBar1.Size = new System.Drawing.Size(272, 20);
			this.progressBar1.TabIndex = 1;
			// 
			// labelStatus
			// 
			this.labelStatus.Location = new System.Drawing.Point(8, 40);
			this.labelStatus.Name = "labelStatus";
			this.labelStatus.Size = new System.Drawing.Size(272, 16);
			this.labelStatus.TabIndex = 2;
			this.labelStatus.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// ProgressBar
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(290, 111);
			this.Controls.Add(this.labelStatus);
			this.Controls.Add(this.progressBar1);
			this.Controls.Add(this.button1);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "ProgressBar";
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Scanning Folders...";
			this.Activated += new System.EventHandler(this.OnActivated);
			this.Enter += new System.EventHandler(this.OnEnter);
			this.ResumeLayout(false);

		}
		#endregion

		private void OnEnter(object sender, System.EventArgs e)
		{
		}

		private void OnActivated(object sender, System.EventArgs e)
		{
		}

		public void Restart(bool bUseCache)
		{
			try
			{
				// reset events
				m_EventStopThread.Reset();
				m_EventThreadStopped.Reset();
				m_bUseCache = bUseCache;
				button1.Enabled = true;

                InitForm();

				// create worker thread instance
				m_WorkerThread = new Thread(new ThreadStart(this.WorkerThreadFunction));

				m_WorkerThread.Name = "Worker Thread Sample";	// looks nice in Output window

				m_WorkerThread.Start();
			}
			catch (System.Exception e)
			{
				MessageBox.Show("ProgressBar::Restart " + e.ToString(), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);			
			}
		}

		private void button1_Click(object sender, System.EventArgs e)
		{
			try
			{
				button1.Enabled = false;
				if ( m_WorkerThread != null  &&  m_WorkerThread.IsAlive )  // thread is active
				{
					// set event "Stop"
					m_EventStopThread.Set();

					// wait when thread  will stop or finish
					while (m_WorkerThread.IsAlive)
					{
						// We cannot use here infinite wait because our thread
						// makes syncronous calls to main form, this will cause deadlock.
						// Instead of this we wait for event some appropriate time
						// (and by the way give time to worker thread) and
						// process events. These events may contain Invoke calls.
						if ( WaitHandle.WaitAll(
							(new ManualResetEvent[] {m_EventThreadStopped}), 
							100,
							true) )
						{
							break;
						}

						Application.DoEvents();
					}
				}
			}
			catch (System.Exception ex)
			{
				MessageBox.Show("ProgressBar::button1_Click " + ex.ToString(), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);										
			}
		}
	}
}

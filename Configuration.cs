using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Aural_Probe
{
	/// <summary>
	/// Summary description for Configuration.
	/// </summary>
	public class ConfigurationForm : System.Windows.Forms.Form
	{
		private System.Windows.Forms.Button buttonOK;
		private System.Windows.Forms.Button buttonCancel;
		private System.Windows.Forms.ListBox listCategories;
		private System.Windows.Forms.TextBox textName;
		private System.Windows.Forms.TextBox textWildcard;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.GroupBox groupCategories;
		private System.Windows.Forms.GroupBox groupDirectories;
		private System.Windows.Forms.GroupBox groupGeneral;
		private System.Windows.Forms.TextBox textDirectories;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

        public string[] categoryName;
        public string[,] categorySearchStrings;
        public bool[] categoryUseRegularExpressions;
        public string[] searchDirectories;		
		public int lnNumCategories;
		public int[] lnNumCategorySearchStrings;
		public int lnNumSearchDirectories;
		private System.Windows.Forms.Button buttonDelete;
		private System.Windows.Forms.Button buttonInsert;
		private System.Windows.Forms.Button buttonReplace;
		public bool bDataDirty;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.CheckBox checkBoxWAV;
		private System.Windows.Forms.CheckBox checkBoxAIFF;
		private System.Windows.Forms.CheckBox checkBoxFLAC;
		private System.Windows.Forms.CheckBox checkBoxMP3;
		private System.Windows.Forms.CheckBox checkBoxWMA;
		private System.Windows.Forms.CheckBox checkBoxOGG;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog1;
        private System.Windows.Forms.FontDialog fontDialog1;
        private System.Windows.Forms.GroupBox groupBox2;
		private System.Windows.Forms.TextBox textBoxDefaultFavorites;
		private System.Windows.Forms.CheckBox checkBoxAutoplay;
		private System.Windows.Forms.NumericUpDown numericAutoplayRepeats;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.CheckBox checkBoxRescanPrompt;
		private System.Windows.Forms.CheckBox checkBoxIncludeFilePaths;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.NumericUpDown numericSampleDisplaySizeW;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.NumericUpDown numericSampleDisplaySizeH;
		private System.Windows.Forms.GroupBox groupBox3;
		private System.Windows.Forms.ComboBox comboOutputDevice;
		private System.Windows.Forms.CheckBox checkBoxAlwaysOnTop;
        private CheckBox checkBoxUseRegularExpressions;
        private LinkLabel linkLabelUseRegularExpressionsHelp;
        private Button buttonMoveUp;
        private Button buttonMoveDown;
        private TextBox textSampleFont;
        private Label label6;
        private CheckBox checkBoxMinimizeToSystemTray;
        public bool bNeedToRescanFolders;
		
		public ConfigurationForm()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			categoryName = new string[MainForm.configFile.kMaxCategories];
			categorySearchStrings = new string[MainForm.configFile.kMaxCategories,MainForm.configFile.kMaxSearchStringsPerCategory];
            categoryUseRegularExpressions = new bool[MainForm.configFile.kMaxCategories];
            searchDirectories = new string[MainForm.configFile.kMaxDirectories];
			lnNumCategorySearchStrings = new int[MainForm.configFile.kMaxCategories];
		}

		public void InitSoundOutputUI()
		{
			// Set up device output combo box
			FMOD.RESULT result;
			comboOutputDevice.Items.Clear();
			int nCurrentDriver = 0;
			int numDrivers = 0;
			StringBuilder driverName = new StringBuilder(256);
			result = MainForm.systemFMOD.getDriver(ref nCurrentDriver);
			MainForm.ERRCHECK(result);
			
			// hack to select primary sound device - will this work???
			if (nCurrentDriver == -1)
				nCurrentDriver = 0;

			result = MainForm.systemFMOD.getNumDrivers(ref numDrivers);
			MainForm.ERRCHECK(result);
			for (int count = 0; count < numDrivers; count++)
			{
                FMOD.GUID guid = new FMOD.GUID();
				result = MainForm.systemFMOD.getDriverInfo(count, driverName, driverName.Capacity, ref guid);
				MainForm.ERRCHECK(result);
				comboOutputDevice.Items.Add(driverName.ToString());
			}
			comboOutputDevice.SelectedIndex = nCurrentDriver;

		}

		public void ChangeSoundOutput()
		{
			int nSelectedOutputDevice = comboOutputDevice.SelectedIndex;
			if (nSelectedOutputDevice == 0)
				nSelectedOutputDevice = -1;
			if (nSelectedOutputDevice < comboOutputDevice.Items.Count)
			{
                if (MainForm.sound != null)
                    MainForm.sound.release();
                MainForm.sound = null;

				MainForm.systemFMOD.close();
				MainForm.systemFMOD.setDriver(nSelectedOutputDevice);
				FMOD.RESULT result = MainForm.systemFMOD.init(32, FMOD.INITFLAGS.NORMAL, (IntPtr)null);
				MainForm.ERRCHECK(result);
			}
		}

		public void InitFromConfigFile(object sender, System.EventArgs e)
		{
			numericSampleDisplaySizeW.Value = MainForm.configFile.lnSampleDisplaySizeW;
			numericSampleDisplaySizeH.Value = MainForm.configFile.lnSampleDisplaySizeH;
            textSampleFont.Font = MainForm.configFile.sampleFont;
            textSampleFont.Text = textSampleFont.Font.Name + " " + textSampleFont.Font.Size.ToString();
            numericAutoplayRepeats.Value = MainForm.configFile.lnAutoplayRepeats;

			listCategories.Items.Clear();
			lnNumCategories = MainForm.configFile.lnNumCategories;
			for (int i = 0; i < MainForm.configFile.lnNumCategories; ++i)
			{
				categoryName[i] = MainForm.configFile.categoryName[i];
				lnNumCategorySearchStrings[i] = MainForm.configFile.lnNumCategorySearchStrings[i];
				string categoryListName = "";
				categoryListName += MainForm.configFile.categoryName[i] + "\t";
                for (int j = 0, k = MainForm.configFile.lnNumCategorySearchStrings[i]; j < k; j++)
                {
                    categorySearchStrings[i, j] = MainForm.configFile.categorySearchStrings[i, j];
                    categoryListName += MainForm.configFile.categorySearchStrings[i, j];
                    if (j < k - 1)
                        categoryListName += ",";
                }
                categoryUseRegularExpressions[i] = MainForm.configFile.categoryUseRegularExpressions[i];
                listCategories.Items.Add(categoryListName);
			}
			listCategories.SelectedIndex = MainForm.configFile.lnNumCategories > 0 ? 0 : -1;
			checkBoxRescanPrompt.Checked = MainForm.configFile.lbRescanPrompt;
			checkBoxIncludeFilePaths.Checked = MainForm.configFile.lbIncludeFilePaths;
			checkBoxAutoplay.Checked = MainForm.configFile.lbAutoplay;
			checkBoxAlwaysOnTop.Checked = MainForm.configFile.lbAlwaysOnTop;
            checkBoxMinimizeToSystemTray.Checked = MainForm.configFile.lbMinimizeToSystemTray;
            numericAutoplayRepeats.Enabled = checkBoxAutoplay.Checked;

			checkBoxWAV.Checked = MainForm.configFile.lbWAV;
			checkBoxAIFF.Checked = MainForm.configFile.lbAIFF;
			checkBoxFLAC.Checked = MainForm.configFile.lbFLAC;
			checkBoxMP3.Checked = MainForm.configFile.lbMP3;
			checkBoxWMA.Checked = MainForm.configFile.lbWMA;
			checkBoxOGG.Checked = MainForm.configFile.lbOGG;

			textDirectories.Clear();
			lnNumSearchDirectories = MainForm.configFile.lnNumSearchDirectories;
			string[] dirsTemp = new string[lnNumSearchDirectories];
			for (int i = 0; i < MainForm.configFile.lnNumSearchDirectories; ++i)
			{
				dirsTemp[i] = searchDirectories[i] = MainForm.configFile.searchDirectories[i];
			}
			textDirectories.Lines = dirsTemp;

			textBoxDefaultFavorites.Text = MainForm.configFile.defaultFavoritesDirectory;

			InitSoundOutputUI();

			bDataDirty = false;
			bNeedToRescanFolders = false;
		}

		public void SaveToConfigFile()
		{
			if (bDataDirty)
			{
				MainForm.configFile.lnSampleDisplaySizeW = (int)numericSampleDisplaySizeW.Value;
				MainForm.configFile.lnSampleDisplaySizeH = (int)numericSampleDisplaySizeH.Value;
                MainForm.configFile.sampleFont = textSampleFont.Font;
                MainForm.configFile.lnAutoplayRepeats = (int)numericAutoplayRepeats.Value;

				MainForm.configFile.lnNumCategories = lnNumCategories;
				for (int i = 0; i < MainForm.configFile.lnNumCategories; ++i)
				{
					MainForm.configFile.categoryName[i] = categoryName[i];
					MainForm.configFile.lnNumCategorySearchStrings[i] = lnNumCategorySearchStrings[i];
					for (int j = 0; j < lnNumCategorySearchStrings[i]; ++j)
					{
						MainForm.configFile.categorySearchStrings[i,j] = categorySearchStrings[i,j];
					}
                    MainForm.configFile.categoryUseRegularExpressions[i] = categoryUseRegularExpressions[i];
				}

				// do directories
				string directories = textDirectories.Text;
				string delimStr = ",;\n\t\r";
				char[] delimiter = delimStr.ToCharArray();
				string[] split = null;
				split = directories.Split(delimiter, MainForm.configFile.kMaxSearchStringsPerCategory);
				lnNumSearchDirectories = 0;
                
                // update new search directories
                string invalidDirectories = "";
                for (int i = 0; i < split.Length; ++i)
				{
					if (split[i].Length > 0)
					{
                        if (!Directory.Exists(split[i]))
                            invalidDirectories += split[i] + "\n";

						MainForm.configFile.searchDirectories[lnNumSearchDirectories] = searchDirectories[lnNumSearchDirectories] = split[i];
						lnNumSearchDirectories++;
					}
				}
                if (invalidDirectories != "")
                    MessageBox.Show("Warning! The following search folders do not exist:\n\n" + invalidDirectories, "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Warning);

				MainForm.configFile.lnNumSearchDirectories = lnNumSearchDirectories;
				MainForm.configFile.lbRescanPrompt = checkBoxRescanPrompt.Checked;
				MainForm.configFile.lbIncludeFilePaths = checkBoxIncludeFilePaths.Checked;
				MainForm.configFile.lbAutoplay = checkBoxAutoplay.Checked;
				MainForm.configFile.lbAlwaysOnTop = checkBoxAlwaysOnTop.Checked;
                MainForm.configFile.lbMinimizeToSystemTray = checkBoxMinimizeToSystemTray.Checked;
                MainForm.configFile.defaultFavoritesDirectory = textBoxDefaultFavorites.Text;

				MainForm.configFile.lbWAV = MainForm.configFile.lbWAV = checkBoxWAV.Checked;	
				MainForm.configFile.lbAIFF = MainForm.configFile.lbAIFF = checkBoxAIFF.Checked;	
				MainForm.configFile.lbFLAC = MainForm.configFile.lbFLAC = checkBoxFLAC.Checked;	
				MainForm.configFile.lbMP3 = MainForm.configFile.lbMP3 = checkBoxMP3.Checked;	
				MainForm.configFile.lbWMA = MainForm.configFile.lbWMA = checkBoxWMA.Checked;	
				MainForm.configFile.lbOGG = MainForm.configFile.lbOGG = checkBoxOGG.Checked;	

				MainForm.configFile.defaultSoundDevice = comboOutputDevice.Text;

				MainForm.configFile.Save();

				bDataDirty = false;
			}
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

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            this.buttonOK = new System.Windows.Forms.Button();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.groupCategories = new System.Windows.Forms.GroupBox();
            this.buttonMoveDown = new System.Windows.Forms.Button();
            this.buttonMoveUp = new System.Windows.Forms.Button();
            this.linkLabelUseRegularExpressionsHelp = new System.Windows.Forms.LinkLabel();
            this.checkBoxUseRegularExpressions = new System.Windows.Forms.CheckBox();
            this.textName = new System.Windows.Forms.TextBox();
            this.listCategories = new System.Windows.Forms.ListBox();
            this.buttonInsert = new System.Windows.Forms.Button();
            this.buttonDelete = new System.Windows.Forms.Button();
            this.textWildcard = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.buttonReplace = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.groupDirectories = new System.Windows.Forms.GroupBox();
            this.textDirectories = new System.Windows.Forms.TextBox();
            this.groupGeneral = new System.Windows.Forms.GroupBox();
            this.textSampleFont = new System.Windows.Forms.TextBox();
            this.numericSampleDisplaySizeH = new System.Windows.Forms.NumericUpDown();
            this.label6 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.numericSampleDisplaySizeW = new System.Windows.Forms.NumericUpDown();
            this.checkBoxRescanPrompt = new System.Windows.Forms.CheckBox();
            this.checkBoxIncludeFilePaths = new System.Windows.Forms.CheckBox();
            this.numericAutoplayRepeats = new System.Windows.Forms.NumericUpDown();
            this.checkBoxAutoplay = new System.Windows.Forms.CheckBox();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.checkBoxAlwaysOnTop = new System.Windows.Forms.CheckBox();
            this.checkBoxWAV = new System.Windows.Forms.CheckBox();
            this.checkBoxAIFF = new System.Windows.Forms.CheckBox();
            this.checkBoxFLAC = new System.Windows.Forms.CheckBox();
            this.checkBoxMP3 = new System.Windows.Forms.CheckBox();
            this.checkBoxWMA = new System.Windows.Forms.CheckBox();
            this.checkBoxOGG = new System.Windows.Forms.CheckBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
            this.fontDialog1 = new System.Windows.Forms.FontDialog();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.textBoxDefaultFavorites = new System.Windows.Forms.TextBox();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.comboOutputDevice = new System.Windows.Forms.ComboBox();
            this.checkBoxMinimizeToSystemTray = new System.Windows.Forms.CheckBox();
            this.groupCategories.SuspendLayout();
            this.groupDirectories.SuspendLayout();
            this.groupGeneral.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericSampleDisplaySizeH)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericSampleDisplaySizeW)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericAutoplayRepeats)).BeginInit();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.SuspendLayout();
            // 
            // buttonOK
            // 
            this.buttonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonOK.Location = new System.Drawing.Point(508, 16);
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.Size = new System.Drawing.Size(72, 24);
            this.buttonOK.TabIndex = 6;
            this.buttonOK.Text = "&OK";
            this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
            // 
            // buttonCancel
            // 
            this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonCancel.Location = new System.Drawing.Point(508, 48);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(72, 24);
            this.buttonCancel.TabIndex = 7;
            this.buttonCancel.Text = "&Cancel";
            this.buttonCancel.Click += new System.EventHandler(this.button1_Click);
            // 
            // groupCategories
            // 
            this.groupCategories.Controls.Add(this.buttonMoveDown);
            this.groupCategories.Controls.Add(this.buttonMoveUp);
            this.groupCategories.Controls.Add(this.linkLabelUseRegularExpressionsHelp);
            this.groupCategories.Controls.Add(this.checkBoxUseRegularExpressions);
            this.groupCategories.Controls.Add(this.textName);
            this.groupCategories.Controls.Add(this.listCategories);
            this.groupCategories.Controls.Add(this.buttonInsert);
            this.groupCategories.Controls.Add(this.buttonDelete);
            this.groupCategories.Controls.Add(this.textWildcard);
            this.groupCategories.Controls.Add(this.label2);
            this.groupCategories.Controls.Add(this.buttonReplace);
            this.groupCategories.Controls.Add(this.label1);
            this.groupCategories.Location = new System.Drawing.Point(8, 134);
            this.groupCategories.Name = "groupCategories";
            this.groupCategories.Size = new System.Drawing.Size(572, 170);
            this.groupCategories.TabIndex = 1;
            this.groupCategories.TabStop = false;
            this.groupCategories.Text = "Categories:";
            this.groupCategories.Enter += new System.EventHandler(this.groupCategories_Enter);
            // 
            // buttonMoveDown
            // 
            this.buttonMoveDown.Location = new System.Drawing.Point(329, 138);
            this.buttonMoveDown.Name = "buttonMoveDown";
            this.buttonMoveDown.Size = new System.Drawing.Size(18, 24);
            this.buttonMoveDown.TabIndex = 8;
            this.buttonMoveDown.Text = "↓";
            this.buttonMoveDown.Click += new System.EventHandler(this.buttonMoveDown_Click);
            // 
            // buttonMoveUp
            // 
            this.buttonMoveUp.Location = new System.Drawing.Point(305, 138);
            this.buttonMoveUp.Name = "buttonMoveUp";
            this.buttonMoveUp.Size = new System.Drawing.Size(18, 24);
            this.buttonMoveUp.TabIndex = 7;
            this.buttonMoveUp.Text = "↑";
            this.buttonMoveUp.Click += new System.EventHandler(this.buttonMoveUp_Click);
            // 
            // linkLabelUseRegularExpressionsHelp
            // 
            this.linkLabelUseRegularExpressionsHelp.AutoSize = true;
            this.linkLabelUseRegularExpressionsHelp.ImageAlign = System.Drawing.ContentAlignment.TopLeft;
            this.linkLabelUseRegularExpressionsHelp.Location = new System.Drawing.Point(447, 107);
            this.linkLabelUseRegularExpressionsHelp.Name = "linkLabelUseRegularExpressionsHelp";
            this.linkLabelUseRegularExpressionsHelp.Size = new System.Drawing.Size(13, 13);
            this.linkLabelUseRegularExpressionsHelp.TabIndex = 6;
            this.linkLabelUseRegularExpressionsHelp.TabStop = true;
            this.linkLabelUseRegularExpressionsHelp.Text = "?";
            this.linkLabelUseRegularExpressionsHelp.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabelUseRegularExpressionsHelp_LinkClicked);
            // 
            // checkBoxUseRegularExpressions
            // 
            this.checkBoxUseRegularExpressions.Checked = true;
            this.checkBoxUseRegularExpressions.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxUseRegularExpressions.Location = new System.Drawing.Point(305, 106);
            this.checkBoxUseRegularExpressions.Name = "checkBoxUseRegularExpressions";
            this.checkBoxUseRegularExpressions.Size = new System.Drawing.Size(152, 16);
            this.checkBoxUseRegularExpressions.TabIndex = 5;
            this.checkBoxUseRegularExpressions.Text = "Use regular expressions";
            // 
            // textName
            // 
            this.textName.Location = new System.Drawing.Point(305, 32);
            this.textName.Name = "textName";
            this.textName.Size = new System.Drawing.Size(259, 20);
            this.textName.TabIndex = 2;
            // 
            // listCategories
            // 
            this.listCategories.HorizontalScrollbar = true;
            this.listCategories.IntegralHeight = false;
            this.listCategories.Location = new System.Drawing.Point(8, 16);
            this.listCategories.Name = "listCategories";
            this.listCategories.Size = new System.Drawing.Size(291, 146);
            this.listCategories.TabIndex = 0;
            this.listCategories.SelectedIndexChanged += new System.EventHandler(this.listCategories_SelectedIndexChanged);
            // 
            // buttonInsert
            // 
            this.buttonInsert.Location = new System.Drawing.Point(353, 138);
            this.buttonInsert.Name = "buttonInsert";
            this.buttonInsert.Size = new System.Drawing.Size(60, 24);
            this.buttonInsert.TabIndex = 9;
            this.buttonInsert.Text = "&Insert";
            this.buttonInsert.Click += new System.EventHandler(this.buttonInsert_Click);
            // 
            // buttonDelete
            // 
            this.buttonDelete.Location = new System.Drawing.Point(485, 138);
            this.buttonDelete.Name = "buttonDelete";
            this.buttonDelete.Size = new System.Drawing.Size(60, 24);
            this.buttonDelete.TabIndex = 11;
            this.buttonDelete.Text = "&Delete";
            this.buttonDelete.Click += new System.EventHandler(this.buttonDelete_Click);
            // 
            // textWildcard
            // 
            this.textWildcard.Location = new System.Drawing.Point(305, 80);
            this.textWildcard.Name = "textWildcard";
            this.textWildcard.Size = new System.Drawing.Size(259, 20);
            this.textWildcard.TabIndex = 4;
            // 
            // label2
            // 
            this.label2.Location = new System.Drawing.Point(305, 64);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(152, 16);
            this.label2.TabIndex = 3;
            this.label2.Text = "Search Criteria:";
            // 
            // buttonReplace
            // 
            this.buttonReplace.Location = new System.Drawing.Point(419, 138);
            this.buttonReplace.Name = "buttonReplace";
            this.buttonReplace.Size = new System.Drawing.Size(60, 24);
            this.buttonReplace.TabIndex = 10;
            this.buttonReplace.Text = "&Replace";
            this.buttonReplace.Click += new System.EventHandler(this.buttonReplace_Click);
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(305, 16);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(152, 16);
            this.label1.TabIndex = 1;
            this.label1.Text = "Name:";
            // 
            // groupDirectories
            // 
            this.groupDirectories.Controls.Add(this.textDirectories);
            this.groupDirectories.Location = new System.Drawing.Point(8, 310);
            this.groupDirectories.Name = "groupDirectories";
            this.groupDirectories.Size = new System.Drawing.Size(486, 112);
            this.groupDirectories.TabIndex = 2;
            this.groupDirectories.TabStop = false;
            this.groupDirectories.Text = "Search Folders:";
            // 
            // textDirectories
            // 
            this.textDirectories.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textDirectories.Location = new System.Drawing.Point(8, 16);
            this.textDirectories.Multiline = true;
            this.textDirectories.Name = "textDirectories";
            this.textDirectories.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.textDirectories.Size = new System.Drawing.Size(470, 88);
            this.textDirectories.TabIndex = 0;
            this.textDirectories.TextChanged += new System.EventHandler(this.OnSearchFoldersChanged);
            this.textDirectories.KeyDown += new System.Windows.Forms.KeyEventHandler(this.textDirectories_KeyDown);
            // 
            // groupGeneral
            // 
            this.groupGeneral.Controls.Add(this.textSampleFont);
            this.groupGeneral.Controls.Add(this.numericSampleDisplaySizeH);
            this.groupGeneral.Controls.Add(this.label6);
            this.groupGeneral.Controls.Add(this.label3);
            this.groupGeneral.Controls.Add(this.numericSampleDisplaySizeW);
            this.groupGeneral.Controls.Add(this.checkBoxRescanPrompt);
            this.groupGeneral.Controls.Add(this.checkBoxIncludeFilePaths);
            this.groupGeneral.Controls.Add(this.numericAutoplayRepeats);
            this.groupGeneral.Controls.Add(this.checkBoxAutoplay);
            this.groupGeneral.Controls.Add(this.label4);
            this.groupGeneral.Controls.Add(this.label5);
            this.groupGeneral.Controls.Add(this.checkBoxMinimizeToSystemTray);
            this.groupGeneral.Controls.Add(this.checkBoxAlwaysOnTop);
            this.groupGeneral.Location = new System.Drawing.Point(8, 8);
            this.groupGeneral.Name = "groupGeneral";
            this.groupGeneral.Size = new System.Drawing.Size(484, 118);
            this.groupGeneral.TabIndex = 0;
            this.groupGeneral.TabStop = false;
            this.groupGeneral.Text = "General:";
            // 
            // textSampleFont
            // 
            this.textSampleFont.Location = new System.Drawing.Point(137, 34);
            this.textSampleFont.Name = "textSampleFont";
            this.textSampleFont.ReadOnly = true;
            this.textSampleFont.Size = new System.Drawing.Size(125, 20);
            this.textSampleFont.TabIndex = 7;
            this.textSampleFont.Click += new System.EventHandler(this.textFont_Click);
            // 
            // numericSampleDisplaySizeH
            // 
            this.numericSampleDisplaySizeH.Location = new System.Drawing.Point(79, 34);
            this.numericSampleDisplaySizeH.Maximum = new decimal(new int[] {
            255,
            0,
            0,
            0});
            this.numericSampleDisplaySizeH.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericSampleDisplaySizeH.Name = "numericSampleDisplaySizeH";
            this.numericSampleDisplaySizeH.Size = new System.Drawing.Size(48, 20);
            this.numericSampleDisplaySizeH.TabIndex = 3;
            this.numericSampleDisplaySizeH.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericSampleDisplaySizeH.ValueChanged += new System.EventHandler(this.numericSampleDisplaySizeH_ValueChanged);
            this.numericSampleDisplaySizeH.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.numericSampleDisplaySizeH_ValueChanged);
            this.numericSampleDisplaySizeH.Leave += new System.EventHandler(this.numericSampleDisplaySizeH_ValueChanged);
            // 
            // label6
            // 
            this.label6.Location = new System.Drawing.Point(134, 16);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(112, 16);
            this.label6.TabIndex = 6;
            this.label6.Text = "Sample Font:";
            // 
            // label3
            // 
            this.label3.Location = new System.Drawing.Point(16, 16);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(112, 16);
            this.label3.TabIndex = 0;
            this.label3.Text = "Sample Display Size:";
            // 
            // numericSampleDisplaySizeW
            // 
            this.numericSampleDisplaySizeW.Location = new System.Drawing.Point(17, 34);
            this.numericSampleDisplaySizeW.Maximum = new decimal(new int[] {
            255,
            0,
            0,
            0});
            this.numericSampleDisplaySizeW.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericSampleDisplaySizeW.Name = "numericSampleDisplaySizeW";
            this.numericSampleDisplaySizeW.Size = new System.Drawing.Size(48, 20);
            this.numericSampleDisplaySizeW.TabIndex = 1;
            this.numericSampleDisplaySizeW.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericSampleDisplaySizeW.ValueChanged += new System.EventHandler(this.numericSampleDisplaySizeW_ValueChanged);
            this.numericSampleDisplaySizeW.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.numericSampleDisplaySizeW_ValueChanged);
            this.numericSampleDisplaySizeW.Leave += new System.EventHandler(this.numericSampleDisplaySizeW_ValueChanged);
            // 
            // checkBoxRescanPrompt
            // 
            this.checkBoxRescanPrompt.Checked = true;
            this.checkBoxRescanPrompt.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxRescanPrompt.Location = new System.Drawing.Point(286, 15);
            this.checkBoxRescanPrompt.Name = "checkBoxRescanPrompt";
            this.checkBoxRescanPrompt.Size = new System.Drawing.Size(192, 16);
            this.checkBoxRescanPrompt.TabIndex = 8;
            this.checkBoxRescanPrompt.Text = "Prompt when rescanning folders";
            this.checkBoxRescanPrompt.CheckedChanged += new System.EventHandler(this.checkBoxRescanPrompt_CheckedChanged);
            // 
            // checkBoxIncludeFilePaths
            // 
            this.checkBoxIncludeFilePaths.Checked = true;
            this.checkBoxIncludeFilePaths.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxIncludeFilePaths.Location = new System.Drawing.Point(286, 39);
            this.checkBoxIncludeFilePaths.Name = "checkBoxIncludeFilePaths";
            this.checkBoxIncludeFilePaths.Size = new System.Drawing.Size(192, 16);
            this.checkBoxIncludeFilePaths.TabIndex = 9;
            this.checkBoxIncludeFilePaths.Text = "Include file paths when searching";
            this.checkBoxIncludeFilePaths.CheckedChanged += new System.EventHandler(this.checkBoxIncludeFilePaths_CheckedChanged);
            // 
            // numericAutoplayRepeats
            // 
            this.numericAutoplayRepeats.Location = new System.Drawing.Point(353, 61);
            this.numericAutoplayRepeats.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericAutoplayRepeats.Name = "numericAutoplayRepeats";
            this.numericAutoplayRepeats.Size = new System.Drawing.Size(48, 20);
            this.numericAutoplayRepeats.TabIndex = 11;
            this.numericAutoplayRepeats.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericAutoplayRepeats.ValueChanged += new System.EventHandler(this.numericAutoplayRepeats_ValueChanged);
            this.numericAutoplayRepeats.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.numericAutoplayRepeats_ValueChanged);
            this.numericAutoplayRepeats.Leave += new System.EventHandler(this.numericAutoplayRepeats_ValueChanged);
            // 
            // checkBoxAutoplay
            // 
            this.checkBoxAutoplay.Checked = true;
            this.checkBoxAutoplay.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxAutoplay.Location = new System.Drawing.Point(286, 63);
            this.checkBoxAutoplay.Name = "checkBoxAutoplay";
            this.checkBoxAutoplay.Size = new System.Drawing.Size(72, 16);
            this.checkBoxAutoplay.TabIndex = 10;
            this.checkBoxAutoplay.Text = "Autoplay";
            this.checkBoxAutoplay.CheckedChanged += new System.EventHandler(this.checkBoxAutoplay_CheckedChanged);
            // 
            // label4
            // 
            this.label4.Location = new System.Drawing.Point(406, 64);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(56, 16);
            this.label4.TabIndex = 12;
            this.label4.Text = "time(s)";
            // 
            // label5
            // 
            this.label5.Location = new System.Drawing.Point(66, 36);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(16, 16);
            this.label5.TabIndex = 2;
            this.label5.Text = "x";
            // 
            // checkBoxAlwaysOnTop
            // 
            this.checkBoxAlwaysOnTop.Checked = true;
            this.checkBoxAlwaysOnTop.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxAlwaysOnTop.Location = new System.Drawing.Point(16, 64);
            this.checkBoxAlwaysOnTop.Name = "checkBoxAlwaysOnTop";
            this.checkBoxAlwaysOnTop.Size = new System.Drawing.Size(152, 16);
            this.checkBoxAlwaysOnTop.TabIndex = 4;
            this.checkBoxAlwaysOnTop.Text = "Always on top";
            this.checkBoxAlwaysOnTop.CheckedChanged += new System.EventHandler(this.checkBoxAlwaysOnTop_CheckedChanged);
            // 
            // checkBoxWAV
            // 
            this.checkBoxWAV.Checked = true;
            this.checkBoxWAV.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxWAV.Location = new System.Drawing.Point(16, 24);
            this.checkBoxWAV.Name = "checkBoxWAV";
            this.checkBoxWAV.Size = new System.Drawing.Size(56, 16);
            this.checkBoxWAV.TabIndex = 0;
            this.checkBoxWAV.Text = "WAV";
            this.checkBoxWAV.CheckedChanged += new System.EventHandler(this.checkBoxWAV_CheckedChanged);
            // 
            // checkBoxAIFF
            // 
            this.checkBoxAIFF.Checked = true;
            this.checkBoxAIFF.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxAIFF.Location = new System.Drawing.Point(16, 48);
            this.checkBoxAIFF.Name = "checkBoxAIFF";
            this.checkBoxAIFF.Size = new System.Drawing.Size(56, 16);
            this.checkBoxAIFF.TabIndex = 1;
            this.checkBoxAIFF.Text = "AIFF";
            this.checkBoxAIFF.CheckedChanged += new System.EventHandler(this.checkBoxAIFF_CheckedChanged);
            // 
            // checkBoxFLAC
            // 
            this.checkBoxFLAC.Checked = true;
            this.checkBoxFLAC.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxFLAC.Location = new System.Drawing.Point(16, 72);
            this.checkBoxFLAC.Name = "checkBoxFLAC";
            this.checkBoxFLAC.Size = new System.Drawing.Size(56, 16);
            this.checkBoxFLAC.TabIndex = 2;
            this.checkBoxFLAC.Text = "FLAC";
            this.checkBoxFLAC.CheckedChanged += new System.EventHandler(this.checkBoxFLAC_CheckedChanged);
            // 
            // checkBoxMP3
            // 
            this.checkBoxMP3.Checked = true;
            this.checkBoxMP3.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxMP3.Location = new System.Drawing.Point(16, 96);
            this.checkBoxMP3.Name = "checkBoxMP3";
            this.checkBoxMP3.Size = new System.Drawing.Size(56, 16);
            this.checkBoxMP3.TabIndex = 3;
            this.checkBoxMP3.Text = "MP3";
            this.checkBoxMP3.CheckedChanged += new System.EventHandler(this.checkBoxMP3_CheckedChanged);
            // 
            // checkBoxWMA
            // 
            this.checkBoxWMA.Checked = true;
            this.checkBoxWMA.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxWMA.Location = new System.Drawing.Point(16, 120);
            this.checkBoxWMA.Name = "checkBoxWMA";
            this.checkBoxWMA.Size = new System.Drawing.Size(56, 16);
            this.checkBoxWMA.TabIndex = 4;
            this.checkBoxWMA.Text = "WMA";
            this.checkBoxWMA.CheckedChanged += new System.EventHandler(this.checkBoxWMA_CheckedChanged);
            // 
            // checkBoxOGG
            // 
            this.checkBoxOGG.Checked = true;
            this.checkBoxOGG.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxOGG.Location = new System.Drawing.Point(16, 144);
            this.checkBoxOGG.Name = "checkBoxOGG";
            this.checkBoxOGG.Size = new System.Drawing.Size(56, 16);
            this.checkBoxOGG.TabIndex = 5;
            this.checkBoxOGG.Text = "OGG";
            this.checkBoxOGG.CheckedChanged += new System.EventHandler(this.checkBoxOGG_CheckedChanged);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.checkBoxOGG);
            this.groupBox1.Controls.Add(this.checkBoxMP3);
            this.groupBox1.Controls.Add(this.checkBoxWAV);
            this.groupBox1.Controls.Add(this.checkBoxFLAC);
            this.groupBox1.Controls.Add(this.checkBoxWMA);
            this.groupBox1.Controls.Add(this.checkBoxAIFF);
            this.groupBox1.Location = new System.Drawing.Point(500, 310);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(80, 168);
            this.groupBox1.TabIndex = 5;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "File Types:";
            this.groupBox1.Enter += new System.EventHandler(this.groupBox1_Enter);
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.textBoxDefaultFavorites);
            this.groupBox2.Location = new System.Drawing.Point(8, 430);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(486, 48);
            this.groupBox2.TabIndex = 3;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Default Favorites Folder:";
            // 
            // textBoxDefaultFavorites
            // 
            this.textBoxDefaultFavorites.Location = new System.Drawing.Point(8, 17);
            this.textBoxDefaultFavorites.Name = "textBoxDefaultFavorites";
            this.textBoxDefaultFavorites.Size = new System.Drawing.Size(470, 20);
            this.textBoxDefaultFavorites.TabIndex = 0;
            this.textBoxDefaultFavorites.TextChanged += new System.EventHandler(this.textBoxDefaultFavorites_TextChanged);
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.comboOutputDevice);
            this.groupBox3.Location = new System.Drawing.Point(8, 486);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(572, 48);
            this.groupBox3.TabIndex = 4;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Sound Output:";
            // 
            // comboOutputDevice
            // 
            this.comboOutputDevice.Location = new System.Drawing.Point(8, 16);
            this.comboOutputDevice.Name = "comboOutputDevice";
            this.comboOutputDevice.Size = new System.Drawing.Size(556, 21);
            this.comboOutputDevice.TabIndex = 0;
            this.comboOutputDevice.SelectedIndexChanged += new System.EventHandler(this.comboOutputDevice_SelectedIndexChanged);
            // 
            // checkBoxMinimizeToSystemTray
            // 
            this.checkBoxMinimizeToSystemTray.Checked = true;
            this.checkBoxMinimizeToSystemTray.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxMinimizeToSystemTray.Location = new System.Drawing.Point(16, 86);
            this.checkBoxMinimizeToSystemTray.Name = "checkBoxMinimizeToSystemTray";
            this.checkBoxMinimizeToSystemTray.Size = new System.Drawing.Size(152, 16);
            this.checkBoxMinimizeToSystemTray.TabIndex = 5;
            this.checkBoxMinimizeToSystemTray.Text = "Minimize to system tray";
            this.checkBoxMinimizeToSystemTray.CheckedChanged += new System.EventHandler(this.checkBoxMinimizeToSystemTray_CheckedChanged);
            // 
            // ConfigurationForm
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.ClientSize = new System.Drawing.Size(588, 540);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.groupGeneral);
            this.Controls.Add(this.groupDirectories);
            this.Controls.Add(this.groupCategories);
            this.Controls.Add(this.buttonOK);
            this.Controls.Add(this.buttonCancel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ConfigurationForm";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Configuration";
            this.Load += new System.EventHandler(this.InitFromConfigFile);
            this.TextChanged += new System.EventHandler(this.OnSearchFoldersChanged);
            this.groupCategories.ResumeLayout(false);
            this.groupCategories.PerformLayout();
            this.groupDirectories.ResumeLayout(false);
            this.groupDirectories.PerformLayout();
            this.groupGeneral.ResumeLayout(false);
            this.groupGeneral.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericSampleDisplaySizeH)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericSampleDisplaySizeW)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericAutoplayRepeats)).EndInit();
            this.groupBox1.ResumeLayout(false);
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.ResumeLayout(false);

		}
		#endregion

		private void button1_Click(object sender, System.EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Close();
		}

		private void textBox1_TextChanged(object sender, System.EventArgs e)
		{
		
		}

		private void buttonOK_Click(object sender, System.EventArgs e)
		{
			if (bNeedToRescanFolders)
				DialogResult = DialogResult.Retry;
			else if (bDataDirty)
				DialogResult = DialogResult.OK;
			else
				DialogResult = DialogResult.Cancel;
			SaveToConfigFile();
			Close();
		}

		private void buttonInsert_Click(object sender, System.EventArgs e)
		{
			string name = textName.Text.ToString();
			string wildcard = textWildcard.Text.ToString();
            bool useRegularExpressions = checkBoxUseRegularExpressions.Checked;

            if (useRegularExpressions)
            {
                try
                {
                    Regex r = new Regex(wildcard);
                    Match m = r.Match("Test");
                }
                catch (ArgumentException)
                {
                    MessageBox.Show("Invalid regular expression. Please see the documentation for regular expression syntax.", "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

			if (name.Length > 0)
			{
				categoryName[lnNumCategories] = name;
                categoryUseRegularExpressions[lnNumCategories] = useRegularExpressions;
                if (useRegularExpressions)
                {
                    categorySearchStrings[lnNumCategories, 0] = wildcard;
                    lnNumCategorySearchStrings[lnNumCategories] = 1;
                }
                else 
                {
                    string delimStr = ",;";
                    char[] delimiter = delimStr.ToCharArray();
                    string[] split = null;
                    split = wildcard.Split(delimiter, MainForm.configFile.kMaxSearchStringsPerCategory);
                    int i = 0;
                    foreach (string s in split)
                    {
                        if (s.Length > 0)
                        {
                            categorySearchStrings[lnNumCategories, i] = s;
                            i++;
                        }
                    }
                    lnNumCategorySearchStrings[lnNumCategories] = i;
                }
				textName.Text = "";
				textWildcard.Text = "";
                checkBoxUseRegularExpressions.Checked = false;

				string categoryListName = "";
				categoryListName += categoryName[lnNumCategories] + "\t";
                for (int j = 0, k = lnNumCategorySearchStrings[lnNumCategories]; j < k; j++)
                {
                    categorySearchStrings[lnNumCategories, j] = categorySearchStrings[lnNumCategories, j];
                    categoryListName += categorySearchStrings[lnNumCategories, j];
                    if (j < k - 1)
                        categoryListName += ",";
                }
				listCategories.Items.Add(categoryListName);
				
				lnNumCategories++;
			}

			bDataDirty = true;
			bNeedToRescanFolders = true;
		}

		private void buttonDelete_Click(object sender, System.EventArgs e)
		{
			// Delete selected item from list!
			int index = listCategories.SelectedIndex;
			if (index > 0)
			{
				listCategories.Items.RemoveAt(index);
				lnNumCategorySearchStrings[index] = 0;
				lnNumCategories--;
				for (int i = index; i < lnNumCategories; i++)
				{
					lnNumCategorySearchStrings[i] = lnNumCategorySearchStrings[i+1];
					for (int j = 0; j < MainForm.configFile.kMaxSearchStringsPerCategory; ++j)
						categorySearchStrings[i,j] = categorySearchStrings[i+1,j];
					categoryName[i] = categoryName[i+1];
                    categoryUseRegularExpressions[i] = categoryUseRegularExpressions[i + 1];
                }
			}

			bDataDirty = true;
			bNeedToRescanFolders = true;
		}

		private void OnSearchFoldersChanged(object sender, System.EventArgs e)
		{
			bDataDirty = true;
			bNeedToRescanFolders = true;
		}

		private void OnSampleSizeChanged(object sender, System.EventArgs e)
		{
			bDataDirty = true;
		}

		private void groupCategories_Enter(object sender, System.EventArgs e)
		{
		
		}

		private void buttonReplace_Click(object sender, System.EventArgs e)
		{
			// Replace selected item in list!
			int index = listCategories.SelectedIndex;
			if (index <= 0)
				return; // should never get here

			string name = textName.Text.ToString();
			string wildcard = textWildcard.Text.ToString();
            bool useRegularExpressions = checkBoxUseRegularExpressions.Checked;

            if (useRegularExpressions)
            {
                try
                {
                    Regex r = new Regex(wildcard);
                    Match m = r.Match("Test");
                }
                catch (ArgumentException)
                {
                    MessageBox.Show("Invalid regular expression. Please see the documentation for regular expression syntax.", "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            if (name.Length > 0)
			{
				categoryName[index] = name;
                categoryUseRegularExpressions[index] = useRegularExpressions;
                if (useRegularExpressions)
                {
                    categorySearchStrings[index, 0] = wildcard;
                    lnNumCategorySearchStrings[index] = 1;
                }
                else
                {
                    string delimStr = ",;";
                    char[] delimiter = delimStr.ToCharArray();
                    string[] split = null;
                    split = wildcard.Split(delimiter, MainForm.configFile.kMaxSearchStringsPerCategory);
                    int i = 0;
                    foreach (string s in split)
                    {
                        if (s.Length > 0)
                        {
                            categorySearchStrings[index, i] = s;
                            i++;
                        }
                    }
                    lnNumCategorySearchStrings[index] = i;
                }
				textName.Text = "";
				textWildcard.Text = "";
                checkBoxUseRegularExpressions.Checked = false;

				string categoryListName = "";
				categoryListName += categoryName[index] + "\t";
                for (int j = 0, k = lnNumCategorySearchStrings[index]; j < k; j++)
                {
                    categorySearchStrings[index, j] = categorySearchStrings[index, j];
                    categoryListName += categorySearchStrings[index, j];
                    if (j < k - 1)
                        categoryListName += ",";
                }
                listCategories.Items[index] = ""; // clear the value before setting it, otherwise it won't update for case changes (eg. a to A)
                listCategories.Items[index] = categoryListName;
			}

			bDataDirty = true;
			bNeedToRescanFolders = true;
		}

		private void listCategories_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			// Replace selected item in list!
			int index = listCategories.SelectedIndex;
			if (index == -1)
				return; // should never get here

			// disable replace/delete buttons for All Samples and don't copy text to edit fields
			buttonReplace.Enabled = index != 0;
			buttonDelete.Enabled = index != 0;
            buttonMoveUp.Enabled = index > 1;
            buttonMoveDown.Enabled = index > 0 && index < listCategories.Items.Count - 1;

            if (index == 0)
			{
				textName.Text = "";
				textWildcard.Text = "";
                checkBoxUseRegularExpressions.Checked = false;
				return;
			}

			string wildcard = "";
            for (int j = 0, k = lnNumCategorySearchStrings[index]; j < k; j++)
            {
                wildcard += categorySearchStrings[index, j];
                if (j < k - 1)
                    wildcard += ",";
            }

			textName.Text = categoryName[index];
			textWildcard.Text = wildcard;
            checkBoxUseRegularExpressions.Checked = categoryUseRegularExpressions[index];
		}

		private void OnSampleSizeKeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
		{
			OnSampleSizeChanged(sender, e);
		}

		private void checkBoxRescanPrompt_CheckedChanged(object sender, System.EventArgs e)
		{
			bDataDirty = true;
		}

		private void checkBoxIncludeFilePaths_CheckedChanged(object sender, System.EventArgs e)
		{
			bDataDirty = true;
			bNeedToRescanFolders = true;
		}

		private void groupBox1_Enter(object sender, System.EventArgs e)
		{
		
		}

		private void textBoxDefaultFavorites_TextChanged(object sender, System.EventArgs e)
		{
			bDataDirty = true;
		}

		private void checkBoxAutoplay_CheckedChanged(object sender, System.EventArgs e)
		{
			bDataDirty = true;
			numericAutoplayRepeats.Enabled = checkBoxAutoplay.Checked;
		}

		private void numericAutoplayRepeats_ValueChanged(object sender, System.EventArgs e)
		{
			bDataDirty = true;
		}

		private void checkBoxWAV_CheckedChanged(object sender, System.EventArgs e)
		{
			bDataDirty = true;
			bNeedToRescanFolders = true;
		}

		private void checkBoxAIFF_CheckedChanged(object sender, System.EventArgs e)
		{
			bDataDirty = true;
			bNeedToRescanFolders = true;
		}

		private void checkBoxFLAC_CheckedChanged(object sender, System.EventArgs e)
		{
			bDataDirty = true;
			bNeedToRescanFolders = true;
		}

		private void checkBoxMP3_CheckedChanged(object sender, System.EventArgs e)
		{
			bDataDirty = true;
			bNeedToRescanFolders = true;
		}

		private void checkBoxWMA_CheckedChanged(object sender, System.EventArgs e)
		{
			bDataDirty = true;
			bNeedToRescanFolders = true;
		}

		private void checkBoxOGG_CheckedChanged(object sender, System.EventArgs e)
		{
			bDataDirty = true;
		}

		private void numericSampleDisplaySizeW_ValueChanged(object sender, System.EventArgs e)
		{
			bDataDirty = true;
		}

		private void checkBoxAlwaysOnTop_CheckedChanged(object sender, System.EventArgs e)
		{
			bDataDirty = true;
		}

        private void checkBoxMinimizeToSystemTray_CheckedChanged(object sender, EventArgs e)
        {
            bDataDirty = true;
        }

        private void comboOutputDevice_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			ChangeSoundOutput();
			bDataDirty = true;
		}

		private void numericSampleDisplaySizeH_ValueChanged(object sender, System.Windows.Forms.KeyPressEventArgs e)
		{
		
		}

		private void numericAutoplayRepeats_ValueChanged(object sender, System.Windows.Forms.KeyPressEventArgs e)
		{
		
		}

		private void numericSampleDisplaySizeH_ValueChanged(object sender, System.EventArgs e)
		{
			bDataDirty = true;
		}

		private void numericSampleDisplaySizeW_ValueChanged(object sender, System.Windows.Forms.KeyPressEventArgs e)
		{
		
		}

        private void textDirectories_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.A)
            {
                ((TextBox)sender).SelectAll();
                e.Handled = true;
            }
        }

        private void linkLabelUseRegularExpressionsHelp_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://msdn.microsoft.com/en-us/library/az24scfc%28v=vs.110%29.aspx");
        }

        public static void Swap<T>(ref T lhs, ref T rhs)
        {
            T temp = lhs;
            lhs = rhs;
            rhs = temp;
        }

        private void buttonMove(bool bUp)
        {
            // Move selected item in list up or down one
            int index = listCategories.SelectedIndex;
            if (bUp && index <= 1)
                return; // should never get here
            if (!bUp && index >= listCategories.Items.Count - 1)
                return; // should never get here

            int targetIndex = bUp ? index - 1 : index + 1;

            Swap(ref categoryName[targetIndex], ref categoryName[index]);
            Swap(ref categoryUseRegularExpressions[targetIndex], ref categoryUseRegularExpressions[index]);
            for (int i = 0; i < MainForm.configFile.kMaxSearchStringsPerCategory; ++i)
                Swap(ref categorySearchStrings[targetIndex, i], ref categorySearchStrings[index, i]);
            Swap(ref lnNumCategorySearchStrings[targetIndex], ref lnNumCategorySearchStrings[index]);

            string tmp = (string)listCategories.Items[targetIndex];
            listCategories.Items[targetIndex] = listCategories.Items[index]; // clear the value before setting it, otherwise it won't update for case changes (eg. a to A)
            listCategories.Items[index] = tmp;

            listCategories.SelectedIndex = targetIndex;

            bDataDirty = true;
            bNeedToRescanFolders = true;
        }
        private void buttonMoveUp_Click(object sender, EventArgs e)
        {
            buttonMove(true);
        }

        private void buttonMoveDown_Click(object sender, EventArgs e)
        {
            buttonMove(false);
        }

        private void textFont_Click(object sender, EventArgs e)
        {
            fontDialog1.Font = textSampleFont.Font;
            if (fontDialog1.ShowDialog() == DialogResult.OK)
            {
                textSampleFont.Font = fontDialog1.Font;
                textSampleFont.Text = textSampleFont.Font.Name + " " + textSampleFont.Font.Size.ToString();
                bDataDirty = true;
            }
        }
    }
}

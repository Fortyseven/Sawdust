using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using System.IO;
using System.Text;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using Microsoft.Win32;

namespace Aural_Probe
{
	/// <summary>
	/// Summary description for Form1.
	/// </summary>

	public class MainForm : System.Windows.Forms.Form
	{
		private System.Windows.Forms.StatusBar statusBar;
		private System.Windows.Forms.StatusBarPanel statusBarPanel;

		public static FMOD.System     systemFMOD  = null;
		public static FMOD.Sound      sound  = null;
		private FMOD.Channel    channel = null;
		private bool bFMODInitialised = false;
		public bool bAutoPlayNextSample = false;
		public int nAutoPlayRepeatsLeft = 0;
		private FMOD.CHANNEL_CALLBACK cbFMOD = null;

		private PersistWindowState m_windowState;
		
		public static string kSampleCacheFilename = "cache.dat";
		public const int kVersionedSampleCacheID = 1;

		public static ConfigFile configFile;
		public static FavoritesFile favoritesFile;
		
		public static int knMaxColors = 16;
		public int nColorInc;
		static public Color[,] colorList;

		public bool bUseCachedSamplesIfPossible;

		public string forceLoadFavoritesName = "";
		//public const int kMaxSamples = 32768;
		public int lnSamples;
		public bool lbDontPlayNextSample;
		public string[] sampleList;
		public int[] sampleColorIndex;
		public int[,] sampleIndices;
		public int[] sampleIndicesCount;
		public int[] sampleFavoritesCount;
		//public bool[] sampleFavorites;
		public int[] sampleBitField;
		int bitFavorite = 0;
		private System.Windows.Forms.ToolBarButton toolBarButtonLoadFavorites;
		private System.Windows.Forms.ToolBarButton toolBarButtonSaveFavorites;
		private System.Windows.Forms.ToolBarButton toolBarButtonResetFavorites;
		private System.Windows.Forms.ToolBarButton toolBarButton2;
		private System.Windows.Forms.ToolBarButton toolBarButton3;
		private System.Windows.Forms.ToolBarButton toolBarButtonPlayStop;
		private System.Timers.Timer timer;
		public System.Windows.Forms.TrackBar trackBarMasterVol;
		private System.Windows.Forms.Label labelVolume;
		private System.Windows.Forms.Label labelVolumeValue;
		private System.Windows.Forms.NotifyIcon notifyIcon1;
		private System.Windows.Forms.ContextMenu contextMenuNotify;
		private System.Windows.Forms.MenuItem menuItem1;
		private System.Windows.Forms.MenuItem menuItem2;
		int bitMissing = 1;
		public bool GetSampleFlag(int sample, int bit) { return (sampleBitField[sample] & (1 << bit)) != 0; }
		public void SetSampleFlag(int sample, int bit, bool val)
		{
			try
			{
				if (val == true)
					sampleBitField[sample] |= 1 << bit;
				else
					sampleBitField[sample] &= ~(1 << bit);
			}
			catch (System.Exception e)
			{
				MessageBox.Show("SetSampleFlag " + e.ToString(), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);			
			}
		}

        public int listSamplesSingleSelectedIndex; // when there are multiple selections, this is -1, otherwise it's listSamples.SelectedIndex
		public int[] listSamplesLastSelectedIndices; // remember the last selected indices to properly handle ListBox item invalidation

		static public string workingDirectory;

		public bool lbFavoritesOnly;
		public bool lbDirtyFavorites;

		ConfigurationForm configurationForm;
		AboutForm aboutForm;
		ProgressBar progressForm;

		private System.Windows.Forms.ToolBar toolBar1;
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.Splitter splitter1;
		private System.Windows.Forms.ListBox categoriesList;
		private System.Windows.Forms.ToolBarButton toolBarButtonConfiguration;
		private System.Windows.Forms.ToolBarButton toolBarButton;
		private System.Windows.Forms.ToolBarButton toolBarButtonRescanFolders;
		private System.Windows.Forms.ToolBarButton toolBarButton1;
		private System.Windows.Forms.ToolBarButton toolBarButtonAbout;
		private System.Windows.Forms.ImageList imageList1;
		private System.Windows.Forms.ToolBarButton toolBarButtonHelp;
		private System.Windows.Forms.PictureBox pictureStatus;
		private System.Windows.Forms.Label statusLabel;
		private SamplesListBox listSamples;
		private System.ComponentModel.IContainer components;
		private System.Windows.Forms.StatusBarPanel statusBarProperties;
		private System.Windows.Forms.ToolBarButton toolBarButtonFavoritesOnly;

		private ContextMenu sampleListMenu;  

		public MainForm()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			try
			{
				workingDirectory = Directory.GetCurrentDirectory();

				lbDirtyFavorites = false;

				m_windowState = new PersistWindowState(this);
				m_windowState.Parent = this;

				// set registry path in HKEY_CURRENT_USER
				m_windowState.RegistryPath = @"Software\Aural Probe"; 

				configFile = new ConfigFile();
				configFile.Load();

				favoritesFile = new FavoritesFile();

				configurationForm = new ConfigurationForm();

				aboutForm = new AboutForm();
			
				progressForm = new ProgressBar(this);

				nColorInc = 0;
				colorList = new Color[knMaxColors,2];
				for (int i = 0; i < knMaxColors; ++i)
				{
					float H = i * (360.0f / knMaxColors);
					float S = 0.25f;
					float V1 = 0.5f;
					float V2 = 1.0f;
					float R = 0.0f;
					float G = 0.0f;
					float B = 0.0f;
					HSVtoRGB(ref H, ref S, ref V1, ref R, ref G, ref B);
					colorList[i,0] = Color.FromArgb((int)(R * 255.0f), (int)(G * 255.0f), (int)(B * 255.0f));
					HSVtoRGB(ref H, ref S, ref V2, ref R, ref G, ref B);
					colorList[i,1] = Color.FromArgb((int)(R * 255.0f), (int)(G * 255.0f), (int)(B * 255.0f));
				}

				sampleIndicesCount = new int[configFile.kMaxCategories];
				sampleFavoritesCount = new int[configFile.kMaxCategories];


				cbFMOD = new FMOD.CHANNEL_CALLBACK(MainForm.soundEndedCallback);
				// Init context menu
				sampleListMenu = new ContextMenu(); 
				sampleListMenu.MenuItems.Add(0, 
					new MenuItem("Explore...\tEnter",new System.EventHandler(ExploreSamples))); 
				sampleListMenu.MenuItems.Add(1,
					new MenuItem("Copy\tCtrl+C",new System.EventHandler(CopySamples)));
				sampleListMenu.MenuItems.Add(2, 
					new MenuItem("Copy as path\tCtrl+Shift+C",new System.EventHandler(CopySamplesShortcut)));
				sampleListMenu.MenuItems.Add(3,
					new MenuItem("Favorite\tSpace", new System.EventHandler(AddRemoveFromFavorites)));
                sampleListMenu.MenuItems.Add(4,
                    new MenuItem("Delete\tDel", new System.EventHandler(DeleteSamples)));
                listSamples.ContextMenu = sampleListMenu;

				Application.ApplicationExit +=new EventHandler(Application_ApplicationExit);

				/* Register for system events to detect user trying 
				   to log off or low memory condition */
				SystemEvents.SessionEnding += new SessionEndingEventHandler(SystemEvents_SessionEnding);

				lbFavoritesOnly = false;

				bUseCachedSamplesIfPossible = true; // set to true once on startup

				try
				{
					uint            version = 0;
					FMOD.RESULT     result;

					/*
						Create a System object and initialize.
					*/
					result = FMOD.Factory.System_Create(ref systemFMOD);
					ERRCHECK(result);

					result = systemFMOD.getVersion(ref version);
					ERRCHECK(result);
					if (version < FMOD.VERSION.number)
					{
						MessageBox.Show("Error!  You are using an old version of FMOD " + version.ToString("X") + ".  This program requires " + FMOD.VERSION.number.ToString("X") + ".");
						Application.Exit();
					}

					TrySettingOutputDevice();

					result = systemFMOD.init(32, FMOD.INITFLAGS.NORMAL, (IntPtr)null);
					ERRCHECK(result);

					bFMODInitialised = true;
				}
				catch
				{
					MessageBox.Show("Error! Could not find FMOD DLL.", "FMOD DLL missing!",
						MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
					Environment.Exit(-1);
				}

				UpdateFormWithConfigurationData();
				Show();
				Refresh();

				RefreshForm();
			}
			catch (System.Exception e)
			{
				MessageBox.Show("MainForm::MainForm " + e.ToString(), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);				
			}
		}

		private void TrySettingOutputDevice()
		{
			int numDrivers = 0;
			StringBuilder driverName = new StringBuilder(256);
			FMOD.RESULT result;
			result = systemFMOD.getNumDrivers(ref numDrivers);
			ERRCHECK(result);
			for (int count = 0; count < numDrivers; count++)
			{
                FMOD.GUID guid = new FMOD.GUID();
				result = systemFMOD.getDriverInfo(count, driverName, driverName.Capacity, ref guid);
				ERRCHECK(result);
				if (driverName.ToString() == configFile.defaultSoundDevice)
				{
					result = systemFMOD.setDriver(count);
					ERRCHECK(result);
				}
			}
		}

		private bool UpdateFavoriteDataFromFavoritesFile(ref FavoritesFile file, bool bShowWarning)
		{
			try
			{
				if (lnSamples == 0 && file.bLoaded)
				{
					if (bShowWarning)
						MessageBox.Show("You must scan the search folders for samples before trying to load favorites.", "No samples loaded!",
							MessageBoxButtons.OK, MessageBoxIcon.Information);
					file.Reset(0);
					return false;
				}

				bool bError = false;
				string errorMessage = "The following favorites could not be located:\n\n";
				for (int i = 0; i < lnSamples; ++i)
					SetSampleFlag(i, bitFavorite, false);
				for (int i = 0; i < file.nFavorites; ++i)
				{
					// See if we can quickly find favorite
					if (file.favoriteIndex[i] < lnSamples &&
						file.favoriteName[i] == sampleList[file.favoriteIndex[i]])
					{
						// we have a winner!
						SetSampleFlag(file.favoriteIndex[i], bitFavorite, true);
					}
					else
					{
						// we have to find it manually as the index is a dud
						int j = 0;
						for (; j < lnSamples; ++j)
						{
							if (sampleList[j] == file.favoriteName[i])
							{
								// we found it!
								SetSampleFlag(file.favoriteIndex[i], bitFavorite, true);
								break;
							}
						}
						if (j == lnSamples) // we didn't find it!
						{
							errorMessage += file.favoriteName[i] + "\n";
							bError = true;
						}
					}
				}
				if (bError)
				{
					errorMessage += "\nThese samples have either been moved to a new location or deleted.";
					MessageBox.Show(errorMessage, "Some favorites missing!",
						MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				}
				return true;
			}
			catch (System.Exception e)
			{
				MessageBox.Show("UpdateFavoriteDataFromFavoritesFile " + e.ToString(), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);			
				return false;
			}
		}

		private void UpdateFavoritesFileFromFavoriteData(ref FavoritesFile file, bool bShowWarning)
		{
			try
			{
				if (sampleFavoritesCount[0] == 0)
				{
					if (bShowWarning)
						MessageBox.Show("There are no favorites to save.", "Nothing to save!",
							MessageBoxButtons.OK, MessageBoxIcon.Information);
					return;
				}

				file.Reset(sampleFavoritesCount[0]);
				int favoriteIndex = 0;
				for (int i = 0; i < lnSamples; ++i)
				{
					if (GetSampleFlag(i, bitFavorite))
					{
						file.favoriteIndex[favoriteIndex] = i;
						file.favoriteName[favoriteIndex] = sampleList[i];
						favoriteIndex++;
					}
				}
			}
			catch (System.Exception e)
			{
				MessageBox.Show("UpdateFavoritesFileFromFavoriteData " + e.ToString(), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);			
			}
		}

        private void ExploreSamples(object sender, EventArgs e)
		{
            try
            {
                StopSoundPlayback();

                int nCurrentCategory = categoriesList.SelectedIndex;
                if (nCurrentCategory < 0 || sampleIndicesCount[nCurrentCategory] == 0)
                    return;
                if (listSamples.SelectedIndices.Count == 0)
                    return;
                if (listSamples.SelectedIndices.Count > 1 && DialogResult.No == MessageBox.Show("Are you sure you want to open " + listSamples.SelectedIndices.Count.ToString() + " explorer windows?", "Explore multiple samples?", MessageBoxButtons.YesNo, MessageBoxIcon.Question))
                    return;
                for (int i = 0; i < listSamples.SelectedIndices.Count; ++i)
                {
                    int nCurrentSample = CalculateRealSampleIndex(listSamples.SelectedIndices[i]);
                    if (nCurrentSample < 0)
                        continue;
                    string sampleName = sampleList[sampleIndices[nCurrentCategory, nCurrentSample]];
                    System.Diagnostics.Process proc = new System.Diagnostics.Process();
                    proc.EnableRaisingEvents = false;
                    proc.StartInfo.FileName = "explorer";
                    proc.StartInfo.Arguments = "/n,/select," + sampleName;
                    proc.Start();
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("ExploreSamples " + ex.ToString(), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
		}

        private void DeleteSamples(object sender, EventArgs e)
        {
            StopSoundPlayback();

            try
            {
                int nCurrentCategory = categoriesList.SelectedIndex;
                if (nCurrentCategory < 0 || sampleIndicesCount[nCurrentCategory] == 0)
                    return;
                if (listSamples.SelectedIndices.Count == 0)
                    return;
                else if (listSamples.SelectedIndices.Count == 1)
                {
                    if (DialogResult.No == MessageBox.Show("Are you sure you want to permanently delete this sample from your computer? Deleted samples will remain in the samples list until you rescan search folders.", "Delete sample?", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2))
                        return;
                }
                else if (listSamples.SelectedIndices.Count > 1)
                {
                    if (DialogResult.No == MessageBox.Show("Are you sure you want to permanently delete " + listSamples.SelectedIndices.Count.ToString() + " samples from your computer? Deleted samples will remain in the samples list until you rescan search folders.", "Delete multiple samples?", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2))
                        return;
                }
                string deleteErrors = "";
                for (int i = 0; i < listSamples.SelectedIndices.Count; ++i)
                {
                    int nCurrentSample = CalculateRealSampleIndex(listSamples.SelectedIndices[i]);
                    if (nCurrentSample < 0)
                        continue;
                    string sampleName = sampleList[sampleIndices[nCurrentCategory, nCurrentSample]];
                    try
                    {
                        File.Delete(sampleName);
                        SetSampleFlag(nCurrentSample, bitMissing, true);
                        listSamples.Invalidate(listSamples.GetItemRectangle(listSamples.SelectedIndices[i]));
                    }
                    catch (System.Exception ex)
                    {
                        deleteErrors += ex.Message.ToString() + "\n";
                    }
                }

                if (deleteErrors != "")
                    MessageBox.Show("One or more errors were encountered during delete:\n" + deleteErrors, "Delete operation completed with errors", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("DeleteSamples " + ex.ToString(), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }
        
        private void CopySamples(object sender, EventArgs e)
		{
			try
			{
				int nCurrentCategory = categoriesList.SelectedIndex;
				if (nCurrentCategory < 0 || sampleIndicesCount[nCurrentCategory] == 0)
					return;
                if (listSamples.SelectedIndices.Count == 0)
                    return;
                DataObject objData = new DataObject();
                string[] filename = new string[listSamples.SelectedIndices.Count];
                for (int i = 0; i < listSamples.SelectedIndices.Count; ++i)
                {
                    int nCurrentSample = CalculateRealSampleIndex(listSamples.SelectedIndices[i]);
                    if (nCurrentSample < 0)
                        continue;
                    filename[i] = sampleList[sampleIndices[nCurrentCategory, nCurrentSample]];
                }
				objData.SetData(DataFormats.FileDrop, true, filename);  
				Clipboard.SetDataObject(objData, true);  
				statusBarPanel.Text = "Copied file(s) to clipboard.";
                statusBarPanel.ToolTipText = "";
			}
			catch (System.Exception ex)
			{
				MessageBox.Show("CopySamples " + ex.ToString(), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);			
			}
		}

		private void CopySamplesShortcut(object sender, EventArgs e)
		{
			try
			{
				int nCurrentCategory = categoriesList.SelectedIndex;
				if (nCurrentCategory < 0 || sampleIndicesCount[nCurrentCategory] == 0)
					return;
                if (listSamples.SelectedIndices.Count == 0)
                    return;
                string sampleNames = "";
                for (int i = 0; i < listSamples.SelectedIndices.Count; ++i)
                {
                    int nCurrentSample = CalculateRealSampleIndex(listSamples.SelectedIndices[i]);
                    if (nCurrentSample < 0)
                        continue;
                    sampleNames += sampleList[sampleIndices[nCurrentCategory, nCurrentSample]] + "\r\n";
                }
				Clipboard.SetDataObject(sampleNames,true);
				statusBarPanel.Text = "Copied file path(s) to clipboard.";
                statusBarPanel.ToolTipText = "";
			}
			catch (System.Exception ex)
			{
				MessageBox.Show("CopySamplesShortcut " + ex.ToString(), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);			
			}
		}

		private void AddRemoveFromFavorites(object sender, EventArgs e)
		{
			try
			{
				int nCurrentCategory = categoriesList.SelectedIndex;
				if (nCurrentCategory < 0 || sampleIndicesCount[nCurrentCategory] == 0)
					return;
                if (listSamples.SelectedIndices.Count == 0)
                    return;

                int nFavoriteSample = CalculateRealSampleIndex(listSamples.SelectedIndices[0]);
                if (nFavoriteSample < 0)
                    return;
                int favoriteSampleIndex = sampleIndices[nCurrentCategory, nFavoriteSample];
                bool isFavorite = GetSampleFlag(favoriteSampleIndex, bitFavorite);

                for (int i = 0; i < listSamples.SelectedIndices.Count; ++i)
                {
                    int nCurrentSample = CalculateRealSampleIndex(listSamples.SelectedIndices[i]);
                    if (nCurrentSample < 0)
                        continue;
                    int sampleIndex = sampleIndices[nCurrentCategory, nCurrentSample];
                    SetSampleFlag(sampleIndex, bitFavorite, !isFavorite);

                    lbDirtyFavorites = true;
                }
				UpdateSampleFavorites();
                if (lbFavoritesOnly)
                    UpdateAudioSamples();
                else
                {
                    for (int i = 0; i < listSamples.SelectedIndices.Count; ++i)
                        listSamples.Invalidate(listSamples.GetItemRectangle(listSamples.SelectedIndices[i]));
                }
                // update favorites checkbox state in popup menu
                if (listSamplesSingleSelectedIndex != -1)
                {
                    int nCurrentSample = CalculateRealSampleIndex(listSamplesSingleSelectedIndex);
                    if (nCurrentSample >= 0)
                    {
                        int sampleIndex = sampleIndices[nCurrentCategory, nCurrentSample];
                        sampleListMenu.MenuItems[3].Checked = GetSampleFlag(sampleIndex, bitFavorite);
                    }
                }
			}
			catch (System.Exception ex)
			{
				MessageBox.Show("AddRemoveFromFavorites " + ex.ToString(), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);			
			}
		}

		public void UpdateSampleFavorites()
		{
			try
			{
				for (int i = 0; i < configFile.lnNumCategories; ++i)
				{
					sampleFavoritesCount[i] = 0;
					for (int j = 0; j < sampleIndicesCount[i]; ++j)
					{
						int index = sampleIndices[i,j];
						if (GetSampleFlag(index, bitFavorite))
						{
							sampleFavoritesCount[i]++;
						}
					}
				}
				UpdateTitleBarText();
				UpdateCategoryList();
			}
			catch (System.Exception e)
			{
				MessageBox.Show("UpdateSampleFavorites " + e.ToString(), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
		}
		
		public void UpdateTitleBarText()
		{
			try
			{
				string[] splitFilename = favoritesFile.currentFavoritesFilename.Split('\\');
				string title;
                if (favoritesFile.bLoaded)
                    title = splitFilename[splitFilename.Length - 1];
				else
					title = "Untitled";
				if (lbDirtyFavorites && sampleFavoritesCount[0] > 0)
					title += "*";
                title += " - Aural Probe";

				Text = title;
			}
			catch (System.Exception e)
			{
				MessageBox.Show("UpdateTitleBarText " + e.ToString(), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);			
			}
		}

		public void HSVtoRGB(ref float H, ref float S, ref float V, ref float R, ref float G, ref float B)
		{
			try
			{
				// Build color list from HSV values
				int Hi = ((int)(H / 60.0f)) % 6;
				float f = (H / 60.0f) - Hi;
				float p = V * (1.0f - S);
				float q = V * (1.0f - (f * S));
				float t = V * (1.0f - ((1.0f - f) * S));
				if		(Hi == 0) { R = V; G = t; B = p; }
				else if (Hi == 1) { R = q; G = V; B = p; }
				else if (Hi == 2) { R = p; G = V; B = t; }
				else if (Hi == 3) { R = p; G = q; B = V; }
				else if (Hi == 4) { R = t; G = p; B = V; }
				else if (Hi == 5) { R = V; G = p; B = q; }
			}
			catch (System.Exception ex)
			{
				MessageBox.Show("HSVtoRGB " + ex.ToString(), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);							
			}
		}

		public void AllocateSampleData(int nSize)
		{
			try
			{
				sampleList = new string[nSize];
				sampleColorIndex = new int[nSize];
				sampleBitField = new int[nSize];
				sampleIndices = new int[configFile.kMaxCategories,nSize];
			}
			catch (System.Exception ex)
			{
				MessageBox.Show("AllocateSampleData " + nSize + " " + ex.ToString(), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);			
			}
		}

		public static void ERRCHECK(FMOD.RESULT result)
		{
            if (result != FMOD.RESULT.OK && result != FMOD.RESULT.ERR_INVALID_PARAM && result != FMOD.RESULT.ERR_FORMAT && result != FMOD.RESULT.ERR_FILE_BAD && result != FMOD.RESULT.ERR_TOOMANYCHANNELS)
			{
				MessageBox.Show("FMOD error! " + result + " - " + FMOD.Error.String(result));
                Environment.Exit(-1);
			}
		}

		public void UpdateAudioSamples()
		{
			try
			{
				int nCurrentCategory = categoriesList.SelectedIndex;
				if (nCurrentCategory < 0 || sampleIndicesCount[nCurrentCategory] == 0)
					return;
				int nCount = sampleIndicesCount[nCurrentCategory];
				listSamples.Items.Clear();
				for (int i = 0; i < nCount; ++i)
				{
					int nSampleIndex = sampleIndices[nCurrentCategory, i];
					if (!lbFavoritesOnly || GetSampleFlag(nSampleIndex, bitFavorite))
					{
						listSamples.Items.Add(i.ToString());
					}
				}
			}
			catch (System.Exception ex)
			{
				MessageBox.Show("UpdateAudioSamples " + ex.ToString(), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);			
			}
		}

		public string GetCategoryListName(int i)
		{
			try
			{
				return configFile.categoryName[i] + " (" + (lbFavoritesOnly ? sampleFavoritesCount[i] : sampleIndicesCount[i]).ToString() + ")";
			}
			catch (System.Exception e)
			{
				MessageBox.Show("GetCategoryListName " + i + " " + e.ToString(), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);			
				return "";
			}
		}

		public void UpdateCategoryList()
		{
			try
			{
				if (lbFavoritesOnly)
				{
					for (int i = 0; i < configFile.lnNumCategories; ++i)
						categoriesList.Items[i] = GetCategoryListName(i);
				}
			}
			catch (System.Exception ex)
			{
				MessageBox.Show("UpdateCategoryList " + ex.ToString(), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);			
			}
		}

		public void UpdateFormWithConfigurationData()
		{
			try
			{
				TopMost = configFile.lbAlwaysOnTop;

                bAutoPlayNextSample = false; // this gets set when playing a sound
				nAutoPlayRepeatsLeft = 0; // this gets set when playing a sound

				// Update the category list
				int oldIndex = categoriesList.SelectedIndex;
				int oldCategoriesSize = categoriesList.Items.Count;

				categoriesList.Items.Clear();
				for (int i = 0; i < configFile.lnNumCategories; ++i)
					categoriesList.Items.Add(GetCategoryListName(i));

				if (oldCategoriesSize == categoriesList.Items.Count)
					categoriesList.SelectedIndex = oldIndex;

				UpdateStatusBarAndLabel();
				listSamples.ItemHeight = configFile.lnSampleDisplaySizeH;
				listSamples.ColumnWidth = configFile.lnSampleDisplaySizeW;
                listSamples.Font = configFile.sampleFont;

                notifyIcon1.Visible = configFile.lbMinimizeToSystemTray;

				//sampleListMenu.MenuItems[3].Enabled = !lbFavoritesOnly; // disable favorite editing when in favorites view

				UpdateVolume();
			}
			catch (System.Exception e)
			{
				MessageBox.Show("UpdateFormWithConfigurationData " + e.ToString(), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
		}

		public void UpdateVolume()
		{
			float volumePercentage = trackBarMasterVol.Value / 100.0f;
			if (volumePercentage <= 0f)
				labelVolumeValue.Text = "Off";
			else
				labelVolumeValue.Text = (20f*Math.Log10(volumePercentage)).ToString("F1") + " dB";
			FMOD.ChannelGroup group = null;
			FMOD.RESULT result = systemFMOD.getMasterChannelGroup(ref group);
			group.setVolume(volumePercentage);

		}

		public void UpdateStatusBarAndLabel()
		{
			try
			{
				listSamples.Items.Clear();
                statusBarPanel.ToolTipText = "";
			
				if (configFile.lnNumSearchDirectories <= 0)
				{
					pictureStatus.Visible = true;
					statusLabel.Visible = true;
					statusLabel.Text = "You must configure your search folders.";
					statusBarPanel.Text = "Ready";
					statusBarProperties.Text = "";
				}
				else if (configFile.lnNumCategories <= 0)
				{
					pictureStatus.Visible = true;
					statusLabel.Visible = true;
					statusLabel.Text = "You must configure your audio sample categories.";
					statusBarPanel.Text = "No categories found";
					statusBarProperties.Text = "";
				}
				else if (lnSamples <= 0)
				{
					pictureStatus.Visible = true;
					statusLabel.Visible = true;
					statusLabel.Text = "No audio samples found.";
					statusBarPanel.Text = "No audio samples found";
					statusBarProperties.Text = "";
				}
				else if (categoriesList.SelectedIndex == -1)
				{
					pictureStatus.Visible = true;
					statusLabel.Visible = true;
					statusLabel.Text = "Select a category from the left.";
					statusBarPanel.Text = lnSamples.ToString() + " sample(s)";
					statusBarProperties.Text = "";
				}
				else
				{
					pictureStatus.Visible = false;
					statusLabel.Visible = false;
					statusBarPanel.Text = (lbFavoritesOnly ? sampleFavoritesCount[categoriesList.SelectedIndex] : sampleIndicesCount[categoriesList.SelectedIndex]).ToString() + "/" + lnSamples.ToString() + " sample(s)";
					statusBarProperties.Text = "";
				}
			}
			catch (System.Exception ex)
			{
				MessageBox.Show("UpdateStatusBarAndLabel " + ex.ToString(), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
		}

		public void ClearSamples()
		{
			try
			{
				for (int i = 0; i < configFile.kMaxCategories; ++i)
				{
					sampleIndicesCount[i] = 0;
				}
				for (int i = 0; i < lnSamples; ++i)
				{
					sampleList[i] = ""; // do I need to do this? :/
					sampleBitField[i] = 0;
				}
				lnSamples = 0;
			}
			catch (System.Exception ex)
			{
				MessageBox.Show("ClearSamples " + ex.ToString(), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
		}

		public bool PopulateCategoriesWithAudioSamples(bool bUseCache)
		{
			try
			{
                bool bWantsToUseCache = bUseCache;
				bUseCache = bUseCache && File.Exists(GetSampleCacheFilepath());
				if (configFile.lnNumSearchDirectories == 0)
				{
                    if (!bWantsToUseCache) // only show this message if we're not trying to use the cache, ie. a fresh install
					    MessageBox.Show("You must configure your search folders.", "No search folders found", MessageBoxButtons.OK, MessageBoxIcon.Information);
					return false;
				}
				else if (configFile.lnNumCategories == 0)
				{
                    if (!bWantsToUseCache) // only show this message if we're not trying to use the cache, ie. a fresh install
					    MessageBox.Show("You must configure your audio sample categories.", "No categories found", MessageBoxButtons.OK, MessageBoxIcon.Information);
					return false;
				}
				else 
				{
					if (bUseCache)
					{
						statusLabel.Text = "Please wait. Loading audio samples from cache...";
						Refresh();
						progressForm.Restart(bUseCache);
						DialogResult result = progressForm.ShowDialog(this);
					}
					else
					{
						if (!configFile.lbRescanPrompt || DialogResult.Yes == MessageBox.Show("Would you like to scan all search folders for audio samples now?", "Scan search folders for audio samples?", MessageBoxButtons.YesNo, MessageBoxIcon.Question))
						{
							statusLabel.Text = "Please wait. Scanning folders for audio samples...";
							Refresh();
							progressForm.Restart(bUseCache);
							DialogResult result = progressForm.ShowDialog(this);
							SaveAudioSampleCache();
						}
					}
				}
				return true;
			}
			catch (System.Exception ex)
			{
				MessageBox.Show("PopulateCategoriesWithAudioSamples " + ex.ToString(), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);				
				return false;
			}
		}

        static public string GetApplicationDataPath()
        {
            string applicationDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Aural Probe";
            return applicationDataPath;
        }

        static public string GetSampleCacheFilepath()
        {
            return GetApplicationDataPath() + "\\" + kSampleCacheFilename;
        }

		public bool SaveAudioSampleCache()
		{
			try
			{
                System.IO.Directory.CreateDirectory(GetApplicationDataPath());
				using(Stream myFileStream = File.OpenWrite(GetSampleCacheFilepath()))
				{
					BinaryFormatter serializer = new BinaryFormatter();
                    serializer.Serialize(myFileStream, kVersionedSampleCacheID);
					serializer.Serialize(myFileStream, lnSamples);
					for (int i = 0; i < lnSamples; ++i)
					{
						serializer.Serialize(myFileStream, sampleList[i]);
						serializer.Serialize(myFileStream, sampleColorIndex[i]);
					}
				}
				return true;
			}
			catch (Exception ex)
			{
				MessageBox.Show("Error! Could not save sample cache! " + ex.ToString(), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                try { File.Delete(GetSampleCacheFilepath()); } 
				catch
				{
                    MessageBox.Show("Error! Could not delete sample cache! " + ex.ToString(), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				}
				return false;
			}
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if (components != null) 
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.statusBar = new System.Windows.Forms.StatusBar();
            this.statusBarPanel = new System.Windows.Forms.StatusBarPanel();
            this.statusBarProperties = new System.Windows.Forms.StatusBarPanel();
            this.toolBar1 = new System.Windows.Forms.ToolBar();
            this.toolBarButtonRescanFolders = new System.Windows.Forms.ToolBarButton();
            this.toolBarButton2 = new System.Windows.Forms.ToolBarButton();
            this.toolBarButtonFavoritesOnly = new System.Windows.Forms.ToolBarButton();
            this.toolBarButtonLoadFavorites = new System.Windows.Forms.ToolBarButton();
            this.toolBarButtonSaveFavorites = new System.Windows.Forms.ToolBarButton();
            this.toolBarButtonResetFavorites = new System.Windows.Forms.ToolBarButton();
            this.toolBarButton = new System.Windows.Forms.ToolBarButton();
            this.toolBarButtonPlayStop = new System.Windows.Forms.ToolBarButton();
            this.toolBarButton1 = new System.Windows.Forms.ToolBarButton();
            this.toolBarButtonConfiguration = new System.Windows.Forms.ToolBarButton();
            this.toolBarButton3 = new System.Windows.Forms.ToolBarButton();
            this.toolBarButtonHelp = new System.Windows.Forms.ToolBarButton();
            this.toolBarButtonAbout = new System.Windows.Forms.ToolBarButton();
            this.imageList1 = new System.Windows.Forms.ImageList(this.components);
            this.panel1 = new System.Windows.Forms.Panel();
            this.statusLabel = new System.Windows.Forms.Label();
            this.pictureStatus = new System.Windows.Forms.PictureBox();
            this.listSamples = new SamplesListBox();
            this.splitter1 = new System.Windows.Forms.Splitter();
            this.categoriesList = new System.Windows.Forms.ListBox();
            this.timer = new System.Timers.Timer();
            this.trackBarMasterVol = new System.Windows.Forms.TrackBar();
            this.labelVolume = new System.Windows.Forms.Label();
            this.labelVolumeValue = new System.Windows.Forms.Label();
            this.notifyIcon1 = new System.Windows.Forms.NotifyIcon(this.components);
            this.contextMenuNotify = new System.Windows.Forms.ContextMenu();
            this.menuItem1 = new System.Windows.Forms.MenuItem();
            this.menuItem2 = new System.Windows.Forms.MenuItem();
            ((System.ComponentModel.ISupportInitialize)(this.statusBarPanel)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.statusBarProperties)).BeginInit();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureStatus)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.timer)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBarMasterVol)).BeginInit();
            this.SuspendLayout();
            // 
            // statusBar
            // 
            this.statusBar.Location = new System.Drawing.Point(0, 299);
            this.statusBar.Name = "statusBar";
            this.statusBar.Panels.AddRange(new System.Windows.Forms.StatusBarPanel[] {
            this.statusBarPanel,
            this.statusBarProperties});
            this.statusBar.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.statusBar.ShowPanels = true;
            this.statusBar.Size = new System.Drawing.Size(632, 24);
            this.statusBar.TabIndex = 0;
            this.statusBar.Text = "Ready";
            this.statusBar.DrawItem += new System.Windows.Forms.StatusBarDrawItemEventHandler(this.statusBar_DrawItem);
            // 
            // statusBarPanel
            // 
            this.statusBarPanel.AutoSize = System.Windows.Forms.StatusBarPanelAutoSize.Spring;
            this.statusBarPanel.Name = "statusBarPanel";
            this.statusBarPanel.Width = 385;
            this.statusBarPanel.Style = StatusBarPanelStyle.OwnerDraw;
            // 
            // statusBarProperties
            // 
            this.statusBarProperties.Name = "statusBarProperties";
            this.statusBarProperties.Width = 230;
            // 
            // toolBar1
            // 
            this.toolBar1.Appearance = System.Windows.Forms.ToolBarAppearance.Flat;
            this.toolBar1.AutoSize = false;
            this.toolBar1.Buttons.AddRange(new System.Windows.Forms.ToolBarButton[] {
            this.toolBarButtonRescanFolders,
            this.toolBarButton2,
            this.toolBarButtonFavoritesOnly,
            this.toolBarButtonLoadFavorites,
            this.toolBarButtonSaveFavorites,
            this.toolBarButtonResetFavorites,
            this.toolBarButton,
            this.toolBarButtonPlayStop,
            this.toolBarButton1,
            this.toolBarButtonConfiguration,
            this.toolBarButton3,
            this.toolBarButtonHelp,
            this.toolBarButtonAbout});
            this.toolBar1.ButtonSize = new System.Drawing.Size(16, 16);
            this.toolBar1.DropDownArrows = true;
            this.toolBar1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.toolBar1.ImageList = this.imageList1;
            this.toolBar1.Location = new System.Drawing.Point(0, 0);
            this.toolBar1.Name = "toolBar1";
            this.toolBar1.ShowToolTips = true;
            this.toolBar1.Size = new System.Drawing.Size(632, 38);
            this.toolBar1.TabIndex = 1;
            this.toolBar1.ButtonClick += new System.Windows.Forms.ToolBarButtonClickEventHandler(this.toolBar1_ButtonClick);
            // 
            // toolBarButtonRescanFolders
            // 
            this.toolBarButtonRescanFolders.ImageIndex = 2;
            this.toolBarButtonRescanFolders.Name = "toolBarButtonRescanFolders";
            this.toolBarButtonRescanFolders.Text = "Rescan";
            this.toolBarButtonRescanFolders.ToolTipText = "Rescan Search Folders";
            // 
            // toolBarButton2
            // 
            this.toolBarButton2.Name = "toolBarButton2";
            this.toolBarButton2.Style = System.Windows.Forms.ToolBarButtonStyle.Separator;
            // 
            // toolBarButtonFavoritesOnly
            // 
            this.toolBarButtonFavoritesOnly.ImageIndex = 4;
            this.toolBarButtonFavoritesOnly.Name = "toolBarButtonFavoritesOnly";
            this.toolBarButtonFavoritesOnly.Style = System.Windows.Forms.ToolBarButtonStyle.ToggleButton;
            this.toolBarButtonFavoritesOnly.Text = "Favorites";
            this.toolBarButtonFavoritesOnly.ToolTipText = "View Favorites";
            // 
            // toolBarButtonLoadFavorites
            // 
            this.toolBarButtonLoadFavorites.ImageIndex = 5;
            this.toolBarButtonLoadFavorites.Name = "toolBarButtonLoadFavorites";
            this.toolBarButtonLoadFavorites.Text = "Open...";
            this.toolBarButtonLoadFavorites.ToolTipText = "Open Favorites...";
            // 
            // toolBarButtonSaveFavorites
            // 
            this.toolBarButtonSaveFavorites.ImageIndex = 6;
            this.toolBarButtonSaveFavorites.Name = "toolBarButtonSaveFavorites";
            this.toolBarButtonSaveFavorites.Text = "Save...";
            this.toolBarButtonSaveFavorites.ToolTipText = "Save Favorites...";
            // 
            // toolBarButtonResetFavorites
            // 
            this.toolBarButtonResetFavorites.ImageIndex = 9;
            this.toolBarButtonResetFavorites.Name = "toolBarButtonResetFavorites";
            this.toolBarButtonResetFavorites.Text = "Reset";
            this.toolBarButtonResetFavorites.ToolTipText = "Reset Favorites";
            // 
            // toolBarButton
            // 
            this.toolBarButton.Name = "toolBarButton";
            this.toolBarButton.Style = System.Windows.Forms.ToolBarButtonStyle.Separator;
            // 
            // toolBarButtonPlayStop
            // 
            this.toolBarButtonPlayStop.Enabled = false;
            this.toolBarButtonPlayStop.ImageIndex = 7;
            this.toolBarButtonPlayStop.Name = "toolBarButtonPlayStop";
            this.toolBarButtonPlayStop.Text = "Play";
            // 
            // toolBarButton1
            // 
            this.toolBarButton1.Name = "toolBarButton1";
            this.toolBarButton1.Style = System.Windows.Forms.ToolBarButtonStyle.Separator;
            // 
            // toolBarButtonConfiguration
            // 
            this.toolBarButtonConfiguration.ImageIndex = 3;
            this.toolBarButtonConfiguration.Name = "toolBarButtonConfiguration";
            this.toolBarButtonConfiguration.Text = "Config...";
            this.toolBarButtonConfiguration.ToolTipText = "Configuration...";
            // 
            // toolBarButton3
            // 
            this.toolBarButton3.Name = "toolBarButton3";
            this.toolBarButton3.Style = System.Windows.Forms.ToolBarButtonStyle.Separator;
            // 
            // toolBarButtonHelp
            // 
            this.toolBarButtonHelp.ImageIndex = 1;
            this.toolBarButtonHelp.Name = "toolBarButtonHelp";
            this.toolBarButtonHelp.Text = "Help";
            this.toolBarButtonHelp.ToolTipText = "Help...";
            // 
            // toolBarButtonAbout
            // 
            this.toolBarButtonAbout.ImageIndex = 0;
            this.toolBarButtonAbout.Name = "toolBarButtonAbout";
            this.toolBarButtonAbout.Text = "About";
            this.toolBarButtonAbout.ToolTipText = "About...";
            // 
            // imageList1
            // 
            this.imageList1.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList1.ImageStream")));
            this.imageList1.TransparentColor = System.Drawing.Color.Transparent;
            this.imageList1.Images.SetKeyName(0, "");
            this.imageList1.Images.SetKeyName(1, "");
            this.imageList1.Images.SetKeyName(2, "");
            this.imageList1.Images.SetKeyName(3, "");
            this.imageList1.Images.SetKeyName(4, "");
            this.imageList1.Images.SetKeyName(5, "");
            this.imageList1.Images.SetKeyName(6, "");
            this.imageList1.Images.SetKeyName(7, "");
            this.imageList1.Images.SetKeyName(8, "");
            this.imageList1.Images.SetKeyName(9, "");
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.statusLabel);
            this.panel1.Controls.Add(this.pictureStatus);
            this.panel1.Controls.Add(this.listSamples);
            this.panel1.Controls.Add(this.splitter1);
            this.panel1.Controls.Add(this.categoriesList);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(0, 38);
            this.panel1.Name = "panel1";
            this.panel1.Padding = new System.Windows.Forms.Padding(1);
            this.panel1.Size = new System.Drawing.Size(632, 261);
            this.panel1.TabIndex = 5;
            // 
            // statusLabel
            // 
            this.statusLabel.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.statusLabel.BackColor = System.Drawing.Color.Black;
            this.statusLabel.ForeColor = System.Drawing.Color.Silver;
            this.statusLabel.Location = new System.Drawing.Point(304, 176);
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new System.Drawing.Size(176, 40);
            this.statusLabel.TabIndex = 6;
            this.statusLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.statusLabel.Visible = false;
            // 
            // pictureStatus
            // 
            this.pictureStatus.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.pictureStatus.BackColor = System.Drawing.Color.Black;
            this.pictureStatus.Image = ((System.Drawing.Image)(resources.GetObject("pictureStatus.Image")));
            this.pictureStatus.Location = new System.Drawing.Point(328, 48);
            this.pictureStatus.Name = "pictureStatus";
            this.pictureStatus.Size = new System.Drawing.Size(128, 128);
            this.pictureStatus.TabIndex = 5;
            this.pictureStatus.TabStop = false;
            this.pictureStatus.Visible = false;
            // 
            // listSamples
            // 
            this.listSamples.BackColor = System.Drawing.Color.Black;
            this.listSamples.ColumnWidth = 32;
            this.listSamples.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listSamples.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.listSamples.Font = new System.Drawing.Font("Lucida Console", 6F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.listSamples.ForeColor = System.Drawing.Color.Silver;
            this.listSamples.IntegralHeight = false;
            this.listSamples.ItemHeight = 32;
            this.listSamples.Location = new System.Drawing.Point(155, 1);
            this.listSamples.MultiColumn = true;
            this.listSamples.Name = "listSamples";
            this.listSamples.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
            this.listSamples.Size = new System.Drawing.Size(476, 259);
            this.listSamples.TabIndex = 1;
            this.listSamples.PreMouseDown += new System.EventHandler(this.listSamples_PreMouseDown);
            this.listSamples.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.listSamples_DrawItem);
            this.listSamples.SelectedIndexChanged += new System.EventHandler(this.listSamples_SelectedIndexChanged);
            this.listSamples.KeyDown += new System.Windows.Forms.KeyEventHandler(this.listSamples_OnKeyDown);
            this.listSamples.MouseMove += new System.Windows.Forms.MouseEventHandler(this.listSamples_OnMouseMove);
            // 
            // splitter1
            // 
            this.splitter1.Location = new System.Drawing.Point(151, 1);
            this.splitter1.Name = "splitter1";
            this.splitter1.Size = new System.Drawing.Size(4, 259);
            this.splitter1.TabIndex = 0;
            this.splitter1.TabStop = false;
            // 
            // categoriesList
            // 
            this.categoriesList.BackColor = System.Drawing.Color.Black;
            this.categoriesList.Dock = System.Windows.Forms.DockStyle.Left;
            this.categoriesList.ForeColor = System.Drawing.Color.Silver;
            this.categoriesList.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.categoriesList.IntegralHeight = false;
            this.categoriesList.Location = new System.Drawing.Point(1, 1);
            this.categoriesList.Name = "categoriesList";
            this.categoriesList.Size = new System.Drawing.Size(150, 259);
            this.categoriesList.TabIndex = 0;
            this.categoriesList.SelectedIndexChanged += new System.EventHandler(this.categoriesList_SelectedIndexChanged);
            // 
            // timer
            // 
            this.timer.Enabled = true;
            this.timer.Interval = 10D;
            this.timer.SynchronizingObject = this;
            this.timer.Elapsed += new System.Timers.ElapsedEventHandler(this.timer_Elapsed);
            // 
            // trackBarMasterVol
            // 
            this.trackBarMasterVol.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.trackBarMasterVol.AutoSize = false;
            this.trackBarMasterVol.LargeChange = 1;
            this.trackBarMasterVol.Location = new System.Drawing.Point(506, 22);
            this.trackBarMasterVol.Maximum = 100;
            this.trackBarMasterVol.Name = "trackBarMasterVol";
            this.trackBarMasterVol.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.trackBarMasterVol.Size = new System.Drawing.Size(120, 16);
            this.trackBarMasterVol.TabIndex = 7;
            this.trackBarMasterVol.Tag = "";
            this.trackBarMasterVol.TickFrequency = 10;
            this.trackBarMasterVol.TickStyle = System.Windows.Forms.TickStyle.None;
            this.trackBarMasterVol.Value = 100;
            this.trackBarMasterVol.Scroll += new System.EventHandler(this.trackBarMasterVol_Scroll);
            // 
            // labelVolume
            // 
            this.labelVolume.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.labelVolume.Location = new System.Drawing.Point(509, 7);
            this.labelVolume.Name = "labelVolume";
            this.labelVolume.Size = new System.Drawing.Size(67, 16);
            this.labelVolume.TabIndex = 8;
            this.labelVolume.Text = "Volume:";
            // 
            // labelVolumeValue
            // 
            this.labelVolumeValue.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.labelVolumeValue.Location = new System.Drawing.Point(567, 7);
            this.labelVolumeValue.Name = "labelVolumeValue";
            this.labelVolumeValue.Size = new System.Drawing.Size(56, 16);
            this.labelVolumeValue.TabIndex = 9;
            this.labelVolumeValue.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // notifyIcon1
            // 
            this.notifyIcon1.ContextMenu = this.contextMenuNotify;
            this.notifyIcon1.Icon = ((System.Drawing.Icon)(resources.GetObject("notifyIcon1.Icon")));
            this.notifyIcon1.Text = "Aural Probe";
            this.notifyIcon1.Visible = false;
            this.notifyIcon1.DoubleClick += new System.EventHandler(this.notifyIcon1_DoubleClick);
            // 
            // contextMenuNotify
            // 
            this.contextMenuNotify.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.menuItem1,
            this.menuItem2});
            this.contextMenuNotify.Popup += new System.EventHandler(this.contextMenuNotify_Popup);
            // 
            // menuItem1
            // 
            this.menuItem1.DefaultItem = true;
            this.menuItem1.Index = 0;
            this.menuItem1.Text = "Open Aural Probe";
            this.menuItem1.Click += new System.EventHandler(this.menuItem1_Click);
            // 
            // menuItem2
            // 
            this.menuItem2.Index = 1;
            this.menuItem2.Text = "Exit";
            this.menuItem2.Click += new System.EventHandler(this.menuItem2_Click);
            // 
            // MainForm
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.ClientSize = new System.Drawing.Size(632, 323);
            this.Controls.Add(this.labelVolumeValue);
            this.Controls.Add(this.trackBarMasterVol);
            this.Controls.Add(this.labelVolume);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.toolBar1);
            this.Controls.Add(this.statusBar);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MinimumSize = new System.Drawing.Size(640, 350);
            this.Name = "MainForm";
            this.Text = "Aural Probe";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.Resize += new System.EventHandler(this.MainForm_Resize);
            ((System.ComponentModel.ISupportInitialize)(this.statusBarPanel)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.statusBarProperties)).EndInit();
            this.panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureStatus)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.timer)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBarMasterVol)).EndInit();
            this.ResumeLayout(false);

		}
		#endregion

		static MainForm gMainForm;

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]

		static void Main(string[] args) 
		{
			if (args.Length == 2)
			{
				Directory.SetCurrentDirectory(args[1]);
			}

			gMainForm = new MainForm();
			if (args.Length == 2)
			{
				gMainForm.SetForceLoadFavorites(args[0]);
			}
			Application.Run(gMainForm);
		}

		private void RefreshForm()
		{
			try
			{
                StopSoundPlayback();

				FavoritesFile tempFavorites = new FavoritesFile();
				if (lnSamples > 0 && sampleFavoritesCount[0] > 0)
					UpdateFavoritesFileFromFavoriteData(ref tempFavorites, false);
				if (bUseCachedSamplesIfPossible && PopulateCategoriesWithAudioSamples(true))
				{
					bUseCachedSamplesIfPossible = false;
				}
				else
				{
					PopulateCategoriesWithAudioSamples(false);
				}
				if (lnSamples > 0 && sampleFavoritesCount[0] > 0)
				{
					UpdateFavoriteDataFromFavoritesFile(ref tempFavorites, false);
					UpdateSampleFavorites();
				}
				UpdateFormWithConfigurationData();
				UpdateAudioSamples();
				UpdateTitleBarText();
			}
			catch (System.Exception e)
			{
				MessageBox.Show("RefreshForm " + e.ToString(), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
		}

		public void SetForceLoadFavorites(string filename)
		{
			try
			{
				forceLoadFavoritesName = filename;
				if (forceLoadFavoritesName.Length > 0)
				{
					favoritesFile.Load(forceLoadFavoritesName);
					if (!UpdateFavoriteDataFromFavoritesFile(ref favoritesFile, true))
					{
						forceLoadFavoritesName = "";
						favoritesFile.Reset(0);
					}
					UpdateSampleFavorites();
					UpdateAudioSamples();
					lbDirtyFavorites = false;
				}
			}
			catch (System.Exception e)
			{
				MessageBox.Show("SetForceLoadFavorites " + filename + " " + e.ToString(), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
		}

		public bool SaveFavorites()
		{
			try
			{
				SaveFileDialog fdlg = new SaveFileDialog();
				fdlg.Title = "Save Favorites";
				if (configFile.defaultFavoritesDirectory.Length > 0)
					fdlg.InitialDirectory = configFile.defaultFavoritesDirectory;
				fdlg.OverwritePrompt = true;
				fdlg.Filter = "Aural Probe Favorites (*.apf)|*.apf|All files (*.*)|*.*";
				fdlg.FileName = favoritesFile.currentFavoritesFilename;
				
				DialogResult result = fdlg.ShowDialog();
				if (fdlg.FileName.Length > 0)
				{
					UpdateFavoritesFileFromFavoriteData(ref favoritesFile, true);
					favoritesFile.currentFavoritesFilename = fdlg.FileName;
					favoritesFile.Save();
					lbDirtyFavorites = false;
					UpdateFormWithConfigurationData();
					UpdateAudioSamples();
					UpdateTitleBarText();
					return true;
				}
			}
			catch (System.Exception e)
			{
				MessageBox.Show("SaveFavorites " + e.ToString(), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
			return false;
		}

        private void StopSoundPlayback()
        {
            if (channel != null)
            {
                bool bPlaying = false;
                channel.isPlaying(ref bPlaying);
                if (bPlaying)
                {
                    channel.stop();
                    bAutoPlayNextSample = false;
                }
            }
            if (sound != null)
            {
                FMOD.RESULT result;
                result = sound.release();
                ERRCHECK(result);
                sound = null;
            }
        }

		private void toolBar1_ButtonClick(object sender, System.Windows.Forms.ToolBarButtonClickEventArgs e)
		{
			try
			{
				if ( e.Button == toolBarButtonRescanFolders )
				{
					RefreshForm();
				} 
				else if (e.Button == toolBarButtonPlayStop)
				{
					bool bPlaying = false;
					if (channel != null)
						channel.isPlaying(ref bPlaying);
					if (bPlaying)
					{
						if (channel != null)
							channel.stop();
						toolBarButtonPlayStop.Enabled = listSamplesSingleSelectedIndex != -1;
						toolBarButtonPlayStop.Text = "Play";
						toolBarButtonPlayStop.ImageIndex = 7; // do work here
						bAutoPlayNextSample = false;
					}
					else
					{
						bAutoPlayNextSample = configFile.lbAutoplay;
						nAutoPlayRepeatsLeft = configFile.lnAutoplayRepeats;
						listSamples_SelectedIndexChanged(sender, e);
					}
				}
				else if (e.Button == toolBarButtonFavoritesOnly )
				{
                    StopSoundPlayback();

					lbFavoritesOnly = !lbFavoritesOnly;
					UpdateFormWithConfigurationData();
					UpdateAudioSamples();
				
					int nCurrentCategory = categoriesList.SelectedIndex;
					if (nCurrentCategory < 0 || sampleIndicesCount[nCurrentCategory] == 0)
						return;
				}
				else if (e.Button == toolBarButtonResetFavorites )
				{
                    StopSoundPlayback();

					if (sampleFavoritesCount[0] > 0 && 
						DialogResult.Yes == MessageBox.Show("Are you sure you want to reset the favorites?", "Reset favorites?", MessageBoxButtons.YesNo, MessageBoxIcon.Question))
					{
						favoritesFile.Reset(0);
						UpdateFavoriteDataFromFavoritesFile(ref favoritesFile, true);
						lbDirtyFavorites = false;
						UpdateSampleFavorites();
						UpdateAudioSamples();
					}
				}
				else if (e.Button == toolBarButtonLoadFavorites )
				{
                    StopSoundPlayback();

					if ((sampleFavoritesCount[0] == 0 || !lbDirtyFavorites) || DialogResult.Yes == MessageBox.Show("You will lose all changes made to the current favorites. Are you sure?", "Replace favorites?", MessageBoxButtons.YesNo, MessageBoxIcon.Question))
					{
						OpenFileDialog fdlg = new OpenFileDialog(); 
						fdlg.Title = "Open Favorites" ; 
						if (configFile.defaultFavoritesDirectory.Length > 0)
							fdlg.InitialDirectory = configFile.defaultFavoritesDirectory;
						fdlg.Filter = "Aural Probe Favorites (*.apf)|*.apf|All files (*.*)|*.*"; 
						fdlg.RestoreDirectory = true; 
						if(fdlg.ShowDialog() == DialogResult.OK && fdlg.FileName.Length > 0) 
						{
							favoritesFile.Load(fdlg.FileName);
							lbDirtyFavorites = false;
							UpdateFavoriteDataFromFavoritesFile(ref favoritesFile, true);
							UpdateSampleFavorites();
							UpdateAudioSamples();
						}
					}
				}
				else if (e.Button == toolBarButtonSaveFavorites )
				{
                    StopSoundPlayback();

					if (sampleFavoritesCount[0] == 0)
					{
						MessageBox.Show("There are no favorites to save.", "Save Favorites", MessageBoxButtons.OK, MessageBoxIcon.Information);
					}
					else
					{
						SaveFavorites();
					}
				}
				else if (e.Button == toolBarButtonConfiguration )
				{
                    StopSoundPlayback();

					DialogResult result = configurationForm.ShowDialog(this);
					if (result == DialogResult.Retry)
					{
						// we need to rescan folders!
						RefreshForm();
					}
					else if (result == DialogResult.OK)
					{
						// settings have changed but sample data + favorites are still valid
						UpdateFormWithConfigurationData();
						UpdateAudioSamples();
					}
					else
					{
						// do nothing
					}
					
				}
				else if ( e.Button == toolBarButtonHelp )
				{
                    StopSoundPlayback();

					try
					{
						System.Diagnostics.Process.Start("Aural Probe documentation.chm");
					}
					catch
					{
						MessageBox.Show("Error!  Could not find help.", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
					}
				
				}
				else if ( e.Button == toolBarButtonAbout )
				{
                    StopSoundPlayback();

					aboutForm.ShowDialog();
				}
			}
			catch (System.Exception ex)
			{
				MessageBox.Show("toolBar1_ButtonClick " + ex.ToString(), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);				
			}
		}

		private void categoriesList_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			try
			{
                listSamplesLastSelectedIndices = null;
				UpdateStatusBarAndLabel();
				Refresh();
				UpdateAudioSamples();

				bool bIsPlaying = false;
				if (channel != null)
					channel.isPlaying(ref bIsPlaying);
				if (!bIsPlaying)
				{
					toolBarButtonPlayStop.Enabled = false;
					toolBarButtonPlayStop.Text = "Play";
					toolBarButtonPlayStop.ImageIndex = 7;
				}
			}
			catch (System.Exception ex)
			{
				MessageBox.Show("categoriesList_SelectedIndexChanged " + ex.ToString(), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
		}

		public static FMOD.RESULT soundEndedCallback(IntPtr channelraw, FMOD.CHANNEL_CALLBACKTYPE type, IntPtr commanddata1, IntPtr commanddata2) 
		{
            // Stop
            gMainForm.toolBarButtonPlayStop.Enabled = gMainForm.listSamplesSingleSelectedIndex != -1;
            gMainForm.toolBarButtonPlayStop.Text = "Play";
            gMainForm.toolBarButtonPlayStop.ImageIndex = 7;

            return FMOD.RESULT.OK;
		}

		private void listSamples_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			nAutoPlayRepeatsLeft = configFile.lnAutoplayRepeats;
			listSamplesSelectedIndexChanged();
		}

        private void UpdateListSamplesSingleSelectedIndex()
        {
            if (listSamples.SelectedIndices.Count == 1)
                listSamplesSingleSelectedIndex = listSamples.SelectedIndices[0];
            else
                listSamplesSingleSelectedIndex = -1; // don't allow an active singular selection if multiple items are selected
        }

		public void listSamplesSelectedIndexChanged()
		{
			try
			{
                UpdateListSamplesSingleSelectedIndex();

                // Invalidate last list sample indices
                if (listSamplesLastSelectedIndices != null)
                {
                    for (int i = 0; i < listSamplesLastSelectedIndices.Length; ++i)
                        listSamples.Invalidate(listSamples.GetItemRectangle(listSamplesLastSelectedIndices[i]));
                }

                // Copy current list sample indices to last list sample indices
                listSamplesLastSelectedIndices = new int[listSamples.SelectedIndices.Count];
                for (int i = 0; i < listSamples.SelectedIndices.Count; ++i)
                    listSamplesLastSelectedIndices[i] = listSamples.SelectedIndices[i];

                // Invalidate current list sample indices
                for (int i = 0; i < listSamples.SelectedIndices.Count; ++i)
                    listSamples.Invalidate(listSamples.GetItemRectangle(listSamples.SelectedIndices[i]));

                // Update popup context menu and default Favorites checkbox state
                listSamples.ContextMenu = listSamples.SelectedIndices.Count > 0 ? sampleListMenu : null;
                sampleListMenu.MenuItems[3].Checked = false;

				int nCurrentCategory = categoriesList.SelectedIndex;
				if (nCurrentCategory < 0 || sampleIndicesCount[nCurrentCategory] == 0)
					return;
				int nCurrentSample = CalculateRealSampleIndex(listSamplesSingleSelectedIndex);
				if (nCurrentSample < 0)
				{
					toolBarButtonPlayStop.Enabled = false;
					toolBarButtonPlayStop.Text = "Play";
					toolBarButtonPlayStop.ImageIndex = 7;
					return;
				}

                int nSampleIndex = sampleIndices[nCurrentCategory, nCurrentSample];

                // update favorites checkbox state in popup menu if applicable
                if (listSamplesSingleSelectedIndex != -1)
                    sampleListMenu.MenuItems[3].Checked = GetSampleFlag(nSampleIndex, bitFavorite);

                string sampleName = sampleList[nSampleIndex];

                statusBarPanel.Text = sampleName;
                statusBarPanel.ToolTipText = sampleName;

                bool sampleFileExists = File.Exists(sampleName);
                SetSampleFlag(nSampleIndex, bitMissing, !sampleFileExists);
                if (!sampleFileExists)
                    return;

				try
				{
					FMOD.RESULT     result;
					if (channel != null)
						channel.stop();

                    if (sound != null)
                    {
                        result = sound.release();
                        ERRCHECK(result);
                        sound = null;
                    }

					result = systemFMOD.createSound(sampleName, FMOD.MODE.SOFTWARE | FMOD.MODE.CREATESTREAM, ref sound);
					ERRCHECK(result);
                    bool createSoundSucceeded = result == FMOD.RESULT.OK;

                    if (createSoundSucceeded)
                    {
                        result = sound.setMode(FMOD.MODE.LOOP_OFF);
                        ERRCHECK(result);
                        if (lbDontPlayNextSample)
                        {
                            lbDontPlayNextSample = false;
                        }
                        else
                        {
                            result = systemFMOD.playSound(FMOD.CHANNELINDEX.FREE, sound, false, ref channel);
                            ERRCHECK(result);

                            if (channel != null)
                                channel.setCallback(cbFMOD);
                            toolBarButtonPlayStop.Enabled = listSamplesSingleSelectedIndex != -1;
                            toolBarButtonPlayStop.Text = "Stop";
                            toolBarButtonPlayStop.ImageIndex = 8;
                            bAutoPlayNextSample = configFile.lbAutoplay;
                        }
                        FMOD.SOUND_TYPE stype = 0;
                        FMOD.SOUND_FORMAT sformat = 0;
                        int schannels = 0;
                        int sbits = 0;
                        float freq = 0;
                        float vol = 0;
                        float pan = 0;
                        int pri = 0;
                        uint length = 0;
                        result = sound.getFormat(ref stype, ref sformat, ref schannels, ref sbits);
                        ERRCHECK(result);
                        result = sound.getDefaults(ref freq, ref vol, ref pan, ref pri);
                        ERRCHECK(result);
                        result = sound.getLength(ref length, FMOD.TIMEUNIT.MS);
                        ERRCHECK(result);
                        string lengthstr;
                        lengthstr = (length / (float)1000).ToString() + "s";
                        statusBarProperties.Text =
                            (freq / 1000).ToString() + "KHz " +
                            sbits.ToString() + "-bit " +
                            (schannels > 1 ? "Stereo " : "Mono ") +
                            "(" + sformat.ToString() + "), " +
                            lengthstr;
                    }
                    else
                    {
                        SetSampleFlag(nSampleIndex, bitMissing, true);
                        listSamples.Invalidate(listSamples.GetItemRectangle(listSamplesSingleSelectedIndex));
                        
                        statusBarPanel.Text = sampleName;
                        statusBarProperties.Text = "ERROR: Unable to play sample.";
                    }
				}
				catch (System.Exception ex)
				{
					MessageBox.Show("Error!  Could not play sample. " + ex.ToString(), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				}
			}
			catch (System.Exception ex)
			{
				MessageBox.Show("listSamples_SelectedIndexChanged " + ex.ToString(), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
		}

		private void listSamples_DrawItem(object sender, System.Windows.Forms.DrawItemEventArgs e)
		{
			try
			{
				int nRealIndex = CalculateRealSampleIndex(e.Index);
				if (nRealIndex < 0)
					return;

				// Set the DrawMode property to draw fixed sized items.
				listSamples.DrawMode = DrawMode.OwnerDrawFixed;
				// Draw the background of the ListBox control for each item.
				e.DrawBackground();

				int nCurrentCategory = categoriesList.SelectedIndex;
				if (nCurrentCategory < 0 || sampleIndicesCount[nCurrentCategory] == 0)
					return;
				int nCurrentSample = nRealIndex;
				if (nCurrentSample < 0)
					return;
				int sampleIndex = sampleIndices[nCurrentCategory, nCurrentSample];

				Color color;
                if (listSamples.SelectedIndices.Contains(e.Index))
				{
                    color = colorList[sampleColorIndex[sampleIndex],1];
				}
				else
				{
					color = colorList[sampleColorIndex[sampleIndex],0];
				}
				if (GetSampleFlag(sampleIndex, bitMissing))
					color = Color.Red;

				SolidBrush brush = new SolidBrush(color);
				Pen favoritePen = new Pen(Color.White, 3);

				Brush textBrush = Brushes.Black;
				//Pen pen = new Pen(Color.Silver);
				int nBorder = 2;
				Rectangle boundedText;
				boundedText = e.Bounds;
				boundedText.X += nBorder * 2;
				boundedText.Y += nBorder * 2;
				boundedText.Width -= nBorder * 4;
				boundedText.Height -= nBorder * 4;
				//e.Graphics.DrawRectangle(pen, e.Bounds.X + nBorder, e.Bounds.Y + nBorder, e.Bounds.Width - (nBorder * 2) - 1, e.Bounds.Height - (nBorder * 2) - 1);
				e.Graphics.FillRectangle(brush, e.Bounds.X + nBorder, e.Bounds.Y + nBorder, e.Bounds.Width - (nBorder * 2), e.Bounds.Height - (nBorder * 2));
				if (GetSampleFlag(sampleIndex, bitFavorite))
					e.Graphics.DrawRectangle(favoritePen, e.Bounds.X + nBorder, e.Bounds.Y + nBorder, e.Bounds.Width - (nBorder * 2), e.Bounds.Height - (nBorder * 2));

				string sampleName = sampleList[sampleIndex];
				string[] split = sampleName.Split('\\');
				string name = split[split.Length - 1];

				e.Graphics.DrawString(name, e.Font, textBrush, boundedText,StringFormat.GenericDefault);
				//e.Graphics.DrawImage(imageList1.Images[0], e.Bounds.X, e.Bounds.Y, 32, 32);
				// If the ListBox has focus, draw a focus rectangle around the selected item.
				//e.DrawFocusRectangle();
			}
			catch (System.Exception ex)
			{
				MessageBox.Show("listSamples_DrawItem " + ex.ToString(), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
		}

		private void listSamples_OnKeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
		{
			try
			{
				if (e.KeyCode == Keys.Enter)
					ExploreSamples(sender, e);
				else if (e.Control && e.Shift && e.KeyCode == Keys.C)
					CopySamplesShortcut(sender, e);
				else if (e.Control && e.KeyCode == Keys.C)
					CopySamples(sender, e);
                else if (e.KeyCode == Keys.Space)
                {
                    AddRemoveFromFavorites(sender, e);
                    e.SuppressKeyPress = true; // prevents selection from being reset
                }
                if (e.KeyCode == Keys.Delete)
                    DeleteSamples(sender, e);
            }
			catch (System.Exception ex)
			{
				MessageBox.Show("listSamples_OnKeyDown " + ex.ToString(), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
		}

		public int CalculateRealSampleIndex(int listBoxIndex)
		{
			try
			{
				if (!lbFavoritesOnly)
					return listBoxIndex;
				else
				{
					int nCurrentCategory = categoriesList.SelectedIndex;
					if (nCurrentCategory < 0 || sampleIndicesCount[nCurrentCategory] == 0)
						return -1;
					int nFavoriteCount = 0;
					for (int i = 0; i < sampleIndicesCount[nCurrentCategory]; ++i)
					{
						int nSampleIndex = sampleIndices[nCurrentCategory, i];
						if (GetSampleFlag(nSampleIndex, bitFavorite))
						{
							if (nFavoriteCount == listBoxIndex)
								return i;
							nFavoriteCount++;
						}
					}
				}
			}
			catch (System.Exception ex)
			{
				MessageBox.Show("CalculateRealSampleIndex " + listBoxIndex + " " + ex.ToString(), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);			
			}
			return -1;				
		}

		private void SystemEvents_SessionEnding(object sender, SessionEndingEventArgs e) 
		{
			try
			{
				if (!lbDirtyFavorites || sampleFavoritesCount[0] == 0)
					return;

				// User is trying to log out. Prompt the user with choices
				DialogResult dr = MessageBox.Show(  "Do you want to save changes to your favorites before you logout?\n"+
					"Click Yes to save favorites and log out\n"+
					"Click No to logout without saving favorites\n"+
					"Click Cancel to cancel logout and manually close Aural Probe", "Save changes?",
					MessageBoxButtons.YesNoCancel, MessageBoxIcon.Exclamation  );

				// User promises to be good and manually stop the app from now on(yeah right)
				// Cancel the logout request, app continues
				if( dr == DialogResult.Cancel )
				{
					e.Cancel = true;
				}
					// Good user! Santa will bring lots of data this year
					// Save data and logout
				else if( dr == DialogResult.Yes )
				{
					e.Cancel = !SaveFavorites(); // if we cancel for some reason, don't abort program, let them save again!

				}
					// Bad user! doesn't care about poor data
				else if( dr == DialogResult.No )
				{
					e.Cancel = false;
					return;
				}
			}
			catch (System.Exception ex)
			{
				MessageBox.Show("SystemEvents_SessionEnding " + ex.ToString(), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}

		}

		private void Application_ApplicationExit(object sender, EventArgs e) 
		{
			try
			{
				if (!lbDirtyFavorites || sampleFavoritesCount[0] == 0)
					return;

				// User is trying to quit. Prompt the user with choices
				DialogResult dr = MessageBox.Show(  "Do you want to save changes to your favorites?\n",  "Save changes?",
					MessageBoxButtons.YesNo, MessageBoxIcon.Question  );

				if( dr == DialogResult.Yes )
				{
					SaveFavorites(); // if we cancel for some reason, don't abort program, let them save again!
				}
				else if( dr == DialogResult.No )
				{
					return;
				}
			}
			catch (System.Exception ex)
			{
				MessageBox.Show("Application_ApplicationExit " + ex.ToString(), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
		}

		private void timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			try
			{
				if (systemFMOD != null && bFMODInitialised && channel != null)
				{
					FMOD.RESULT result = systemFMOD.update();
					ERRCHECK(result);
				}
			}
			catch (System.Exception ex)
			{
				MessageBox.Show("timer_Elapsed " + ex.ToString(), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}

            // handle AutoPlay
            try
            {
                bool isPlaying = false;
                if (channel != null)
                    channel.isPlaying(ref isPlaying);
                if (gMainForm != null && gMainForm.bAutoPlayNextSample && gMainForm.listSamplesSingleSelectedIndex != -1 && (gMainForm.nAutoPlayRepeatsLeft > 1 || (gMainForm.listSamplesSingleSelectedIndex < gMainForm.listSamples.Items.Count - 1)) && !isPlaying)
                {
                    gMainForm.nAutoPlayRepeatsLeft--;
                    if (gMainForm.nAutoPlayRepeatsLeft > 0)
                    {
                        gMainForm.listSamplesSelectedIndexChanged();
                    }
                    else
                    {
                        gMainForm.nAutoPlayRepeatsLeft = MainForm.configFile.lnAutoplayRepeats;
                        int newSelectedIndex = listSamplesSingleSelectedIndex + 1;
                        if (newSelectedIndex < gMainForm.listSamples.Items.Count)
                        {
                            gMainForm.listSamples.SetSelected(listSamplesSingleSelectedIndex, false);
                            gMainForm.listSamples.SetSelected(newSelectedIndex, true);
                        }
                    }
                }
                else
                {
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("timer_Elapsed " + ex.ToString(), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }

		}

        private void listSamples_OnMouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
		{
			try
			{
				if (!SamplesListBox.s_allowLeftMouseDownEvent && e.Button == MouseButtons.Left)
				{
                    // Handle drag + drop - this is a synchronous event, and will block this thread until DoDragDrop is complete (by failing or succeeding)
                    int nCurrentCategory = categoriesList.SelectedIndex;
                    if (listSamples.SelectedIndices.Count == 0)
                        return;
                    string[] files = new String[listSamples.SelectedIndices.Count];
                    for (int i = 0; i < listSamples.SelectedIndices.Count; ++i)
                    {
                        int nCurrentSample = CalculateRealSampleIndex(listSamples.SelectedIndices[i]);
                        if (nCurrentSample < 0)
                            continue;
                        files[i] = sampleList[sampleIndices[nCurrentCategory, nCurrentSample]];
                    }
                    DataObject objData = new DataObject(DataFormats.FileDrop, files);
                    listSamples.DoDragDrop(objData, DragDropEffects.Copy);
				}
			}
			catch (System.Exception ex)
			{
				MessageBox.Show("OnMouseMove " + ex.ToString(), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
		
		}

		private void trackBarMasterVol_Scroll(object sender, System.EventArgs e)
		{
			UpdateVolume();
		}

		private void MainForm_Load(object sender, System.EventArgs e)
		{
		
		}

		private void MainForm_Resize(object sender, System.EventArgs e)
		{
			if (FormWindowState.Minimized == WindowState && configFile.lbMinimizeToSystemTray)
                    Hide();
        }

		private void notifyIcon1_DoubleClick(object sender, System.EventArgs e)
		{
			if (FormWindowState.Minimized == WindowState)
			{
                Show();
				WindowState = FormWindowState.Normal;
			}
		}

		private void contextMenuNotify_Popup(object sender, System.EventArgs e)
		{
		
		}

		private void menuItem2_Click(object sender, System.EventArgs e)
		{
			Application.Exit();
		}

		private void menuItem1_Click(object sender, System.EventArgs e)
		{
			if (FormWindowState.Minimized == WindowState)
			{
				Show();
				WindowState = FormWindowState.Normal;
			}
		}

        private void listSamples_PreMouseDown(object sender, EventArgs e)
        {
            System.Drawing.Point point = System.Windows.Forms.Cursor.Position;
            point = listSamples.PointToClient(point);
            int listBoxSampleIndex = listSamples.IndexFromPoint(point);
			if (listBoxSampleIndex != ListBox.NoMatches)
			{
                // only allow left mouse down event to go through to ListBox (which modifies the selection) when clicking on an unselected item, or if CTRL is held down (meaning the user is trying to turn off an actively selected element)
                SamplesListBox.s_allowLeftMouseDownEvent = !listSamples.SelectedIndices.Contains(listBoxSampleIndex) || Control.ModifierKeys == Keys.Control;
			}

        }

        private void statusBar_DrawItem(object sender, StatusBarDrawItemEventArgs e)
        {
			// Manually draw the status bar panel text here to avoid 127 character limit.
			// See http://msdn.microsoft.com/en-us/library/vstudio/we893ad3%28v=vs.80%29.aspx

            StringFormat textFormat = new StringFormat();
            textFormat.LineAlignment = StringAlignment.Center; // vertical middle
            textFormat.Alignment = StringAlignment.Near; // horizontal left
            
			string text = e.Panel.Text;
            int indexForward, indexBackward, finalIndex;
            while (e.Graphics.MeasureString(text, statusBar.Font).Width > e.Bounds.Width)
            {
                indexForward = text.IndexOf('\\');
                indexBackward = text.IndexOf('/');
                if (indexForward < 0 && indexBackward < 0)
                    break; // can't strip any more
                finalIndex = indexForward;
                if (finalIndex < 0 || (indexBackward >= 0 && indexBackward < finalIndex))
                    finalIndex = indexBackward;
                text = text.Substring(finalIndex + 1);
            }
            e.Graphics.DrawString(text, statusBar.Font, Brushes.Black, new RectangleF(e.Bounds.X, e.Bounds.Y, e.Bounds.Width, e.Bounds.Height), textFormat);
        }
	}
}

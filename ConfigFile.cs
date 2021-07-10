using System;
using System.IO;
using System.Windows.Forms;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace Aural_Probe
{
	/// <summary>
	/// Configuration file container.
	/// </summary>
	public class ConfigFile
	{
		private string kConfigFile = "Aural Probe.cfg";
		private string kDefaultFile = "Default.cfg";

		public int kMaxCategories = 256;
		public int kMaxSearchStringsPerCategory = 256;
		public int kMaxDirectories = 256;
		public int kVersionedConfigFileID = -666;

        // Version History
        // 0 = Unversioned config file
        // 1 = Initial first versioned config file, no change in data
        // 2 = Added default favorites directory
        // 3 = Added file types + autoplay
        // 4 = Added default sound device
        // 5 = Added always on top
        // 6 = Added use regular expression option (per category)
        // 7 = Added font
        // 8 = Added minimize to system tray
        // ...
        public int kCurrentConfigFileVersion = 8;

		public string[] categoryName;
		public string[,] categorySearchStrings;
        public bool[] categoryUseRegularExpressions;
		public string[] searchDirectories;
        public string[] searchDirectoriesScrubbed;
        public int lnNumCategories;
		public int[] lnNumCategorySearchStrings;
		public int lnNumSearchDirectories;
        public int lnNumSearchDirectoriesScrubbed;
		public int lnSampleDisplaySizeW;
		public int lnSampleDisplaySizeH;
        public System.Drawing.Font sampleFont;
        public bool lbRescanPrompt;
		public bool lbIncludeFilePaths;
		public bool lbAlwaysOnTop;
        public bool lbMinimizeToSystemTray;
        public string defaultFavoritesDirectory;
		public bool lbAutoplay;
		public int lnAutoplayRepeats;
		public bool lbWAV;
		public bool lbAIFF;
		public bool lbFLAC;
		public bool lbMP3;
		public bool lbWMA;
		public bool lbOGG;
		public string defaultSoundDevice;

		public ConfigFile()
		{
			categoryName = new string[kMaxCategories];
			categorySearchStrings = new string[kMaxCategories,kMaxSearchStringsPerCategory];
            categoryUseRegularExpressions = new bool[kMaxCategories];
            searchDirectories = new string[kMaxDirectories];
			lnNumCategorySearchStrings = new int[kMaxCategories];
			lbRescanPrompt = true;
			lbIncludeFilePaths = true;
			lbAlwaysOnTop = false;
            lbMinimizeToSystemTray = false;
            lbAutoplay = false;
			lnAutoplayRepeats = 4;
			lbWAV = true;
			lbAIFF = true;
			lbFLAC = true;
			lbMP3 = true;
			lbWMA = true;
			lbOGG = true;
			lnSampleDisplaySizeH = 32;
			lnSampleDisplaySizeH = 192;
            sampleFont = new System.Drawing.Font("Microsoft Sans Serif", 8.25F);

        }

		private void ReadDataFromFile(string filename)
		{
			try
			{
				using(Stream myFileStream = File.OpenRead(filename))
				{
					BinaryFormatter deserializer = new BinaryFormatter();
                    
					int nVersion = (int)deserializer.Deserialize(myFileStream);
					if (nVersion == kVersionedConfigFileID)
					{
						// We're reading a versioned config file, so read the actual version in now
						nVersion = (int)deserializer.Deserialize(myFileStream);
						lnNumCategories = (int)deserializer.Deserialize(myFileStream);
					}
					else
					{
						// Unversioned config file, so version is 0 and first int was actually number of categories
						lnNumCategories = nVersion;
						nVersion = 0;
					}

					for(int lnCategory = 0; lnCategory < lnNumCategories; lnCategory++ )
					{
						categoryName[lnCategory] = (string)deserializer.Deserialize(myFileStream);
						lnNumCategorySearchStrings[lnCategory] = (int)deserializer.Deserialize(myFileStream);
						for(int lnSS = 0; lnSS < lnNumCategorySearchStrings[lnCategory]; lnSS++ )
						{
							categorySearchStrings[lnCategory,lnSS] = (string)deserializer.Deserialize(myFileStream);
						}
                        if (nVersion >= 6)
                            categoryUseRegularExpressions[lnCategory] = (bool)deserializer.Deserialize(myFileStream);
                    }
					lnNumSearchDirectories = (int)deserializer.Deserialize(myFileStream);
					for(int lnSearchDirectory = 0; lnSearchDirectory < lnNumSearchDirectories; lnSearchDirectory++ )
					{
						searchDirectories[lnSearchDirectory] = (string)deserializer.Deserialize(myFileStream);
					}
					lnSampleDisplaySizeH = (int)deserializer.Deserialize(myFileStream);
                    if (nVersion >= 7)
                    {
                        sampleFont = (System.Drawing.Font)deserializer.Deserialize(myFileStream);
                    }
					lbRescanPrompt = (bool)deserializer.Deserialize(myFileStream);
					lbIncludeFilePaths = (bool)deserializer.Deserialize(myFileStream);
					if (nVersion >= 2)
					{
						defaultFavoritesDirectory = (string)deserializer.Deserialize(myFileStream);
					}
					else
					{
						defaultFavoritesDirectory = "";
					}
					if (nVersion >= 3)
					{
						lbAutoplay = (bool)deserializer.Deserialize(myFileStream);
						lnAutoplayRepeats = (int)deserializer.Deserialize(myFileStream);
						lbWAV = (bool)deserializer.Deserialize(myFileStream);
						lbAIFF = (bool)deserializer.Deserialize(myFileStream);
						lbFLAC = (bool)deserializer.Deserialize(myFileStream);
						lbMP3 = (bool)deserializer.Deserialize(myFileStream);
						lbWMA = (bool)deserializer.Deserialize(myFileStream);
						lbOGG = (bool)deserializer.Deserialize(myFileStream);
						lnSampleDisplaySizeW = (int)deserializer.Deserialize(myFileStream);
					}
					else
					{
						lbAutoplay = false;
						lnAutoplayRepeats = 4;
						lbWAV = true;
						lbAIFF = true;
						lbFLAC = true;
						lbMP3 = true;
						lbWMA = true;
						lbOGG = true;
						lnSampleDisplaySizeW = lnSampleDisplaySizeH * 6;
					}
					if (nVersion >= 4)
					{
						defaultSoundDevice = (string)deserializer.Deserialize(myFileStream);
					}
					else
					{
						defaultSoundDevice = "";
					}
					if (nVersion >= 5)
					{
						lbAlwaysOnTop = (bool)deserializer.Deserialize(myFileStream);
					}
					else
					{
						lbAlwaysOnTop = false;
					}
                    if (nVersion >= 8)
                    {
                        lbMinimizeToSystemTray = (bool)deserializer.Deserialize(myFileStream);
                    }
                    else
                    {
                        lbMinimizeToSystemTray = false;
                    }
                }
			}
			catch
			{
			}
		}

        public void UpdateScrubbedSearchDirectories()
        {
            System.Collections.ArrayList newList = new System.Collections.ArrayList();

            lnNumSearchDirectoriesScrubbed = 0;

            for (int i = 0; i < lnNumSearchDirectories; ++i)
            {
                string str = searchDirectories[i].ToLower();
                if (str == null)
                    continue;

                if (Directory.Exists(str))
                {
                    if (!newList.Contains(str))    // skip duplicates
                    {
                        bool bAlreadyContainedByOtherDirectory = false;
                        for (int j = 0; j < lnNumSearchDirectories; ++j)
                        {
                            if (i == j)
                                continue;

                            string newStr = searchDirectories[j].ToLower();
                            if (newStr == null || str == newStr)
                                continue;

                            if (str.IndexOf(newStr) != -1)
                            {
                                bAlreadyContainedByOtherDirectory = true;
                                break;
                            }
                        }
                        if (!bAlreadyContainedByOtherDirectory)
                        {
                            newList.Add(searchDirectories[i]);
                            lnNumSearchDirectoriesScrubbed++;
                        }
                    }
                }
            }

            searchDirectoriesScrubbed = (string[])newList.ToArray(typeof(string));
        }

        public string GetConfigFilePath()
        {
            return MainForm.GetApplicationDataPath() + "\\" + kConfigFile;
        }

        public void Load()
		{
			Directory.SetCurrentDirectory(MainForm.workingDirectory);
            bool bConfigExists = File.Exists(GetConfigFilePath());
			bool bOldConfigExists = File.Exists( kConfigFile );
			bool bDefaultExists = File.Exists( kDefaultFile );
			if (bConfigExists)
			{
                ReadDataFromFile(GetConfigFilePath());
			}
            else if (bOldConfigExists)
            {
                ReadDataFromFile(kConfigFile);
            }
            else if (bDefaultExists)
            {
                ReadDataFromFile(kDefaultFile);
                lnSampleDisplaySizeW = 192;
                lnSampleDisplaySizeH = 32;
            }
            else
            {
                // Set up defaults
                lnNumCategories = 1;
                categoryName[0] = "All Samples";
                lnNumSearchDirectories = 0;
                lnSampleDisplaySizeW = 192;
                lnSampleDisplaySizeH = 32;
            }

            UpdateScrubbedSearchDirectories();
		}

		public void Save()
		{
			try
            {
                System.IO.Directory.CreateDirectory(MainForm.GetApplicationDataPath());
                using (Stream myFileStream = File.OpenWrite(GetConfigFilePath()))
				{
					BinaryFormatter serializer = new BinaryFormatter();
                    serializer.Serialize(myFileStream, kVersionedConfigFileID);
					serializer.Serialize(myFileStream, kCurrentConfigFileVersion);
					serializer.Serialize(myFileStream, lnNumCategories);
					for(int lnCategory = 0; lnCategory < lnNumCategories; lnCategory++ )
					{
						serializer.Serialize(myFileStream, categoryName[lnCategory]);
						serializer.Serialize(myFileStream, lnNumCategorySearchStrings[lnCategory]);
						for(int lnSS = 0; lnSS < lnNumCategorySearchStrings[lnCategory]; lnSS++ )
						{
							serializer.Serialize(myFileStream, categorySearchStrings[lnCategory,lnSS].ToString());
						}
                        serializer.Serialize(myFileStream, categoryUseRegularExpressions[lnCategory]);
                    }
					serializer.Serialize(myFileStream, lnNumSearchDirectories);
					for(int lnSearchDirectory = 0; lnSearchDirectory < lnNumSearchDirectories; lnSearchDirectory++ )
					{
						serializer.Serialize(myFileStream, searchDirectories[lnSearchDirectory].ToString());
					}
					serializer.Serialize(myFileStream, lnSampleDisplaySizeH);
                    serializer.Serialize(myFileStream, sampleFont);
                    serializer.Serialize(myFileStream, lbRescanPrompt);
					serializer.Serialize(myFileStream, lbIncludeFilePaths);
					serializer.Serialize(myFileStream, defaultFavoritesDirectory);

					serializer.Serialize(myFileStream, lbAutoplay);
					serializer.Serialize(myFileStream, lnAutoplayRepeats);
					serializer.Serialize(myFileStream, lbWAV);
					serializer.Serialize(myFileStream, lbAIFF);
					serializer.Serialize(myFileStream, lbFLAC);
					serializer.Serialize(myFileStream, lbMP3);
					serializer.Serialize(myFileStream, lbWMA);
					serializer.Serialize(myFileStream, lbOGG);
					serializer.Serialize(myFileStream, lnSampleDisplaySizeW);
					serializer.Serialize(myFileStream, defaultSoundDevice);

					serializer.Serialize(myFileStream, lbAlwaysOnTop);
                    serializer.Serialize(myFileStream, lbMinimizeToSystemTray);
                }

                UpdateScrubbedSearchDirectories();
			}
			catch (Exception ex)
			{
                MessageBox.Show("Error! Could not save config file! " + ex.ToString(), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                try
                {
                    File.Delete(GetConfigFilePath());
                }
                catch (Exception ex2)
                {
                    MessageBox.Show("Error! Could not delete config file! " + ex2.ToString(), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }
			}
		}
	}
}

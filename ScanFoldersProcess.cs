using System;
using System.IO;
using System.Windows.Forms;
using System.Threading;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.RegularExpressions;

namespace Aural_Probe
{
	/// <summary>
	/// Class emulates long process which runs in worker thread
	/// and makes synchronous user UI operations.
	/// </summary>
	public class ScanFoldersProcess
	{
		static public string[] formatStrings =
		{
			"*.wav",
			"*.mp3",
			"*.wma",
			"*.ogg",
			"*.aif",
			"*.flac"
		};
		static public bool[] formatFlag =
		{
			true,
			true,
			true,
			true,
			true,
			true,
		};
		

		// Main thread sets this event to stop worker thread:
		ManualResetEvent m_EventStop;

		// Worker thread sets this event when it is stopped:
		ManualResetEvent m_EventStopped;

		// Reference to progress bar
		ProgressBar m_progressBar;

		bool m_bUseCache;

		// Reference to progress bar
		MainForm m_mainForm;

		bool bCancelled;

        int nFileCount = 0;
        int nDirectoryCount = 0;

		public ScanFoldersProcess(ManualResetEvent eventStop, 
			ManualResetEvent eventStopped,
			bool bUseCache,
			ProgressBar progressBar,
			MainForm mainForm)
		{
			bCancelled = false;
			m_bUseCache = bUseCache;
			m_EventStop = eventStop;
			m_EventStopped = eventStopped;
			m_mainForm = mainForm;
			m_progressBar = progressBar;
		}

        public bool IsDirectoryHidden(string dir)
        {
            DirectoryInfo info = new DirectoryInfo(dir);
            return info.Parent != null && (info.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden; // ignore root directories, which may show up as hidden
        }

        public int CountDirectoriesInDirectory(string dir)
		{
			try
			{
                if (IsDirectoryHidden(dir))
                    return 0;

                if (!Directory.Exists(dir))
                    return 0;

        		// check if thread is cancelled
                if (m_EventStop.WaitOne(0, true))
                {
                    bCancelled = true;
                    return 0;
                }

                nDirectoryCount++;
                if (!m_progressBar.IsDisposed && m_progressBar.IsHandleCreated)
                    m_progressBar.Invoke(m_progressBar.m_DelegateUpdateLabel, nDirectoryCount.ToString() + " folder(s) found");

				int nSubDirectories = 1;
				foreach (string s in Directory.GetDirectories(dir))
				{
                    if (!bCancelled)
					    nSubDirectories += CountDirectoriesInDirectory(s);
				}
				return nSubDirectories;
			}
			catch
			{
                // something went wrong at the OS level - perhaps the user doesn't have permissions.
				return 0;
			}
		}

		public int CountFilesInDirectory(string dir)
		{
			try
			{
                if (IsDirectoryHidden(dir))
                    return 0;

                if (!Directory.Exists(dir))
                    return 0;

                // check if thread is cancelled
                if (m_EventStop.WaitOne(0, true))
                {
                    bCancelled = true;
                    return 0;
                }

                int nFiles = Directory.GetFiles(dir).Length;
				foreach (string s in Directory.GetDirectories(dir))
				{
                    if (!bCancelled)
                        nFiles += CountFilesInDirectory(s);
				}

                nFileCount += nFiles;
                if (!m_progressBar.IsDisposed && m_progressBar.IsHandleCreated)
                    m_progressBar.Invoke(m_progressBar.m_DelegateUpdateLabel, nFileCount.ToString() + " file(s) found");
                
                return nFiles;
			}
			catch
			{
                // something went wrong at the OS level - perhaps the user doesn't have permissions.
				return 0;				
			}
		}

		public bool AddSample(string s, int nColorIndex)
		{
			try
			{
				if (bCancelled)
				{
					// inform main thread that this thread stopped
					m_EventStopped.Set();
					return false;
				}

				// check if thread is cancelled
				if ( m_EventStop.WaitOne(0, true) )
				{
					bCancelled = true;
					// clean up
					m_mainForm.ClearSamples();

					// inform main thread that this thread stopped
					m_EventStopped.Set();
					return false;
				}

                m_mainForm.sampleList[m_mainForm.lnSamples] = s;
                m_mainForm.sampleColorIndex[m_mainForm.lnSamples] = nColorIndex;
                
                if (!MainForm.configFile.lbIncludeFilePaths)
				{
					string[] sSplit = s.Split('\\');
                    s = sSplit[sSplit.Length - 1];
				}

				for (int i = 0; i < MainForm.configFile.lnNumCategories; ++i)
				{
					if (MainForm.configFile.lnNumCategorySearchStrings[i] == 0)
					{
						// Special case for "Everything" category
						m_mainForm.sampleIndices[i,m_mainForm.sampleIndicesCount[i]] = m_mainForm.lnSamples;
						++m_mainForm.sampleIndicesCount[i];
					}
					else
					{
                        if (MainForm.configFile.categoryUseRegularExpressions[i])
                        {
                            Regex regex = new Regex(MainForm.configFile.categorySearchStrings[i, 0], RegexOptions.IgnoreCase);
                            Match match = regex.Match(s);
                            if (match.Success)
                            {
                                m_mainForm.sampleIndices[i, m_mainForm.sampleIndicesCount[i]] = m_mainForm.lnSamples;
                                m_mainForm.sampleIndicesCount[i]++;
                            }
                        }
                        else
                        {
                            for (int j = 0; j < MainForm.configFile.lnNumCategorySearchStrings[i]; ++j)
                            {
                                if (s.IndexOf(MainForm.configFile.categorySearchStrings[i, j], StringComparison.OrdinalIgnoreCase) != -1)
                                {
                                    m_mainForm.sampleIndices[i, m_mainForm.sampleIndicesCount[i]] = m_mainForm.lnSamples;
                                    m_mainForm.sampleIndicesCount[i]++;
                                    break;
                                }
                            }
                        }
					}
				}
				m_mainForm.lnSamples++;
				return true;
			}
			catch (System.Exception e)
			{
				MessageBox.Show("AddSample " + s + " " + e.ToString(), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return false;				
			}
		}

		public bool PopulateSamplesFromCache()
		{
            if (File.Exists(MainForm.GetSampleCacheFilepath()))
			{
				try
				{
                    using (Stream myFileStream = File.OpenRead(MainForm.GetSampleCacheFilepath()))
					{
						BinaryFormatter deserializer = new BinaryFormatter();
                    
						int nCacheVersion = (int)deserializer.Deserialize(myFileStream);
						if (nCacheVersion == 1)
						{
							int nSamples = (int)deserializer.Deserialize(myFileStream);
							m_mainForm.AllocateSampleData(nSamples);
                            if (!m_progressBar.IsDisposed && m_progressBar.IsHandleCreated)
							    m_progressBar.Invoke(m_progressBar.m_DelegateUpdateMaximumAndStep, nSamples, nSamples / 20);
                            for (int i = 0; i < nSamples; ++i)
							{
								string sampleName = (string)deserializer.Deserialize(myFileStream);
								int sampleColorIndex = (int)deserializer.Deserialize(myFileStream);
								if (!AddSample(sampleName, sampleColorIndex))
									return false;
								if (!m_progressBar.IsDisposed && m_progressBar.IsHandleCreated && i % m_progressBar.progressBar1.Step == 0)
								    m_progressBar.Invoke(m_progressBar.m_DelegateUpdateForm, null);
							}
						}
					}
					return true;
				}
				catch (Exception ex)
				{
					MessageBox.Show("Error! Could not populate sample list from cache! " + ex.ToString(), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
					try
					{
                        File.Delete(MainForm.GetSampleCacheFilepath());
					} 
					catch (Exception ex2)
					{
                        MessageBox.Show("Could not delete sample cache! " + ex2.ToString(), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
					}
					m_mainForm.ClearSamples();
					return false;
				}
			}
			return false;
		}

		public bool PopulateSamplesFromDirectory(string dir)
		{
			try
			{
                if (IsDirectoryHidden(dir))
                    return true;

                if (!Directory.Exists(dir))
                    return false;

                if (bCancelled)
                    return false;

				m_mainForm.nColorInc = (m_mainForm.nColorInc + 1) % MainForm.knMaxColors;
				int i = 0;
				foreach (string formatString in formatStrings)
				{
					if (formatFlag[i])
					{
						foreach (string s in Directory.GetFiles(dir, formatString))
						{
							if (!AddSample(s, m_mainForm.nColorInc))
								return false;
						}
					}
					i++;
				}
				foreach (string s in Directory.GetDirectories(dir))
				{
                    if (!m_progressBar.IsDisposed && m_progressBar.IsHandleCreated)
					    m_progressBar.Invoke(m_progressBar.m_DelegateUpdateForm, null);
					bool bResult = PopulateSamplesFromDirectory(s);
					if (!bResult)
						return false;
				}
			}
			catch
			{
                // allow graceful fallthrough if we cannot access a protected folder
			}
			return true;
		}

		// Function runs in worker thread
		public void Run()
		{
            while (!m_progressBar.IsHandleCreated)
            {
            }

			try
			{
				bCancelled = false;

				formatFlag[0] = MainForm.configFile.lbWAV;
				formatFlag[1] = MainForm.configFile.lbMP3;
				formatFlag[2] = MainForm.configFile.lbWMA;
				formatFlag[3] = MainForm.configFile.lbOGG;
				formatFlag[4] = MainForm.configFile.lbAIFF;
				formatFlag[5] = MainForm.configFile.lbFLAC;

				m_mainForm.ClearSamples();
				if (m_bUseCache)
				{
					bool bResult = PopulateSamplesFromCache();
					if (!bResult)
					{
						if (!bCancelled)
							MessageBox.Show("Could not load all samples.", "Not all samples loaded!",
								MessageBoxButtons.OK, MessageBoxIcon.Exclamation);

                        if (!m_progressBar.IsDisposed && m_progressBar.IsHandleCreated)
						    m_progressBar.Invoke(m_progressBar.m_DelegateThreadFinished, null);

                        if (bCancelled)
                            m_mainForm.ClearSamples();

						return;
					}
				}
				else
				{
                    nDirectoryCount = 0; // reset
					nFileCount = 0; // reset
					for (int i = 0; i < MainForm.configFile.lnNumSearchDirectoriesScrubbed; ++i)
					{
						string dir = MainForm.configFile.searchDirectoriesScrubbed[i];
						nDirectoryCount += CountDirectoriesInDirectory(dir);
						nFileCount += CountFilesInDirectory(dir);
					}
                    if (!bCancelled)
					    m_mainForm.AllocateSampleData(nFileCount);
                    if (!m_progressBar.IsDisposed && m_progressBar.IsHandleCreated)
                        m_progressBar.Invoke(m_progressBar.m_DelegateUpdateMaximumAndStep, nDirectoryCount, -1);
                    for (int i = 0; i < MainForm.configFile.lnNumSearchDirectoriesScrubbed; ++i)
					{
						string dir = MainForm.configFile.searchDirectoriesScrubbed[i];
						if (dir.Length == 0)
							continue;

						bool bResult = PopulateSamplesFromDirectory(dir);
						if (!bResult)
						{
							if (!bCancelled)
								MessageBox.Show("Could not load all samples.", "Not all samples loaded!",
									MessageBoxButtons.OK, MessageBoxIcon.Exclamation);

                            if (!m_progressBar.IsDisposed && m_progressBar.IsHandleCreated)
							    m_progressBar.Invoke(m_progressBar.m_DelegateThreadFinished, null);

                            if (bCancelled)
                                m_mainForm.ClearSamples();
                            
                            return;
						}
					}
				}
                if (!m_progressBar.IsDisposed && m_progressBar.IsHandleCreated)
				    m_progressBar.Invoke(m_progressBar.m_DelegateThreadFinished, null);
			}
			catch (System.Exception e)
			{
				MessageBox.Show("Run " + e.ToString(), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
		}
	}
}

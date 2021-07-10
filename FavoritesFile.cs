using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace Aural_Probe
{
	/// <summary>
	/// Favorite file container.
	/// </summary>
	public class FavoritesFile
	{
		private static string kExtension = ".apf";
		private static string kDefaultFavoritesFilename = "Untitled";

		// Version History
		// 1 = Initial first versioned favorites file, reads sampleIndices array index (which may be invalid), then full sample filename
		// ...
		public int kCurrentConfigFileVersion = 1;


		// Version file data
		public string currentFavoritesFilename;
		public int nFavorites;
		public int[] favoriteIndex;
		public string[] favoriteName;
		public bool bLoaded;

		public static string GetDefaultFilename()
		{
			return kDefaultFavoritesFilename;
		}

		public FavoritesFile()
		{
			currentFavoritesFilename = GetDefaultFilename();
			nFavorites = 0;
			bLoaded = false;
		}

		public void Reset(int nSize)
		{
			bLoaded = false;
			currentFavoritesFilename = GetDefaultFilename();
			nFavorites = nSize;
			favoriteIndex = new int[nSize];
			favoriteName = new string[nSize];
		}

		public void Load(string filename)
		{
			if (File.Exists( filename ))
			{
				try
				{
					using(Stream myFileStream = File.OpenRead(filename))
					{
						currentFavoritesFilename = filename;
						BinaryFormatter deserializer = new BinaryFormatter();
                    
						int nVersion = (int)deserializer.Deserialize(myFileStream);
						nFavorites = (int)deserializer.Deserialize(myFileStream);
						favoriteIndex = new int[nFavorites];
						favoriteName = new string[nFavorites];
						for (int i = 0; i < nFavorites; ++i)
						{
							favoriteIndex[i] = (int)deserializer.Deserialize(myFileStream);
							favoriteName[i] = (string)deserializer.Deserialize(myFileStream);
						}
					}
					bLoaded = true;
				}
				catch
				{
				}			
			}
		}

		public void Save()
		{
			Save(currentFavoritesFilename);
		}

		public void SaveAs(string filename)
		{
			Save(filename);
		}

		// Internal save method
		private void Save(string filename)
		{
			if (filename.LastIndexOf(kExtension) != filename.Length - kExtension.Length)
			{
				// Filename does not have correct extension, let's add it
				filename += kExtension;
			}

			try
			{			
				using(Stream myFileStream = File.OpenWrite(filename))
				{
					BinaryFormatter serializer = new BinaryFormatter();
					serializer.Serialize(myFileStream, kCurrentConfigFileVersion);
					serializer.Serialize(myFileStream, nFavorites);
					for(int i = 0; i < nFavorites; i++ )
					{
						serializer.Serialize(myFileStream, favoriteIndex[i]);
						serializer.Serialize(myFileStream, favoriteName[i]);
					}
				}
				bLoaded = true;
			}
			catch
			{
			}
		}
	}
}

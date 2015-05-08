using System;
using System.IO.IsolatedStorage;
using System.Diagnostics;
using System.Collections.Generic;
using System.ComponentModel;

namespace mapapp 
{
    public enum DbState
    {
        Unknown = -1,
        Empty,
        Loading,
        Updating,
        Loaded
    }

    public class MapAppSettings : INotifyPropertyChanged, INotifyPropertyChanging
    {
        // Isolated Store instance
        IsolatedStorageSettings settingsStore;

        // Names of settings in store
        const string stDbName       = "dbname";
        const string stDbStatus     = "dbstat";
        const string stDbDate       = "dbdate";
        const string stLastUpdate   = "lastup";
        const string stVoterCount   = "numvoters";
        const string stUploadDir    = "uploadto";

        // Default values for settings
        const string    defaultDbName   = "VoterData.sdf";
        const DbState   defaultDbStat   = DbState.Unknown;
        DateTime  defaultDbDate   = new DateTime(2012, 1, 1);
        DateTime  defaultUpdate   = new DateTime(2012, 1, 1);
        const int       defaultCount    = 0;
        const string    defaultUpload   = "me/skydrive";

        Dictionary<string, Object> defaults;

        /// <summary>
        /// Constructor initializes AppSettings from settings store or defaults
        /// </summary>
        public MapAppSettings()
        {
            try
            {
                settingsStore = IsolatedStorageSettings.ApplicationSettings;
                defaults = new Dictionary<string, object>(8);
                defaults.Add(stDbName, defaultDbName);
                defaults.Add(stDbStatus, defaultDbStat);
                defaults.Add(stDbDate, defaultDbDate);
                defaults.Add(stLastUpdate, defaultUpdate);
                defaults.Add(stVoterCount, defaultCount);
                defaults.Add(stUploadDir, defaultUpload);
            }
            catch (Exception e)
            {
                Debug.WriteLine("Exception initializing settings in isolated store " + e.ToString());
            }
        }

        public void UpdateAll()
        {
            try
            {
                settingsStore.Save();
            }
            catch (Exception e)
            {
                Debug.WriteLine("Exception saving settings to isolated store " + e.ToString());
            }
        }

        /// <summary>
        /// Update a setting in the application settings store
        /// </summary>
        /// <param name="Name">Name of the setting to update</param>
        /// <param name="Value">New value to be saved for the named setting</param>
        /// <returns>true if Value is changed, otherwise false</returns>
        public bool UpdateSetting(string Name, Object Value)
        {
            bool changed = false;

            if (settingsStore.Contains(Name))
            {
                if (settingsStore[Name] != Value)
                {
                    NotifyPropertyChanging(Name);
                    settingsStore[Name] = Value;
                    NotifyPropertyChanged(Name);
                    changed = true;
                }
            }
            else
            {
                if (defaults[Name] != Value)
                {
                    NotifyPropertyChanging(Name);
                    settingsStore.Add(Name, Value);
                    NotifyPropertyChanged(Name);
                    changed = true;
                }
            }
            return changed;
        }

        /// <summary>
        /// Gets the value of a setting from the settings store or returns the default
        /// </summary>
        /// <typeparam name="valueType">Type of value to return</typeparam>
        /// <param name="Name">Name of the setting to retreive</param>
        /// <returns>Current value of setting or default if setting has not been set</returns>
        public valueType GetSetting<valueType>(string Name)
        {
            valueType Value;

            if (settingsStore.Contains(Name))
            {
                Value = (valueType)settingsStore[Name];
            }
            else
            {
                Value = (valueType)defaults[Name];
            }
            return Value;
        }

        /// <summary>
        /// SkyDrive folder ID for folder that update files should be uploaded to
        /// By default this will be the user's root folder until a voter data file
        /// is downloaded, then the folder that the voter data file was in becomes
        /// the update upload folder.
        /// </summary>
        public string UploadFolder
        {
            get { return GetSetting<string>(stUploadDir); }
            set
            {
                if (UpdateSetting(stUploadDir, value))
                    settingsStore.Save();
            }
        }

        /// <summary>
        /// Filename of the current Voters Database
        /// </summary>
        public string DbFileName
        {
            get { return GetSetting<string>(stDbName); }
            set { if (UpdateSetting(stDbName, value)) settingsStore.Save(); }
        }

        /// <summary>
        /// Current status of database
        /// </summary>
        public DbState DbStatus
        {
            get { return GetSetting<DbState>(stDbStatus); }
            set { if (UpdateSetting(stDbStatus, value)) settingsStore.Save(); }
        }

        /// <summary>
        /// Timestamp of current database
        /// This is the time the voters.xml file was created
        /// </summary>
        public DateTime DbDate
        {
            get { return GetSetting<DateTime>(stDbDate); }
            set { if (UpdateSetting(stDbDate, value)) settingsStore.Save(); }
        }

        /// <summary>
        /// Timestamp of last update successfully sent to SkyDrive
        /// </summary>
        public DateTime LastUpdateTimestamp
        {
            get { return GetSetting<DateTime>(stLastUpdate); }
            set { if (UpdateSetting(stLastUpdate, value)) settingsStore.Save(); }
        }

        /// <summary>
        /// Count of voters in database
        /// </summary>
        public int VoterCount
        {
            get { return GetSetting<int>(stVoterCount); }
            set { if (UpdateSetting(stVoterCount, value)) settingsStore.Save(); }
        }

        #region INotifyPropertyChanged Members
        public event PropertyChangedEventHandler PropertyChanged;

        // Used to notify the page that a data context property changed
        private void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                // PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                if (System.Threading.Thread.CurrentThread.ManagedThreadId == App.MainThread.ManagedThreadId)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                }
                else
                {
                    System.Windows.Deployment.Current.Dispatcher.BeginInvoke(new Action<string>(NotifyPropertyChanged), propertyName);
                }
            }
        }
        #endregion

        #region INotifyPropertyChanging Members
        public event PropertyChangingEventHandler PropertyChanging;

        // Used to notify the data context that a data context property is about to change
        private void NotifyPropertyChanging(string propertyName)
        {
            if (PropertyChanging != null)
            {
                PropertyChanging(this, new PropertyChangingEventArgs(propertyName));
            }
        }
        #endregion
    }
}

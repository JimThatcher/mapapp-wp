using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Collections.ObjectModel;
using System.Threading;
using mapapp.data;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using System.Xml.Linq;
using System.ComponentModel;
using System.Text;
using System.IO.IsolatedStorage;
using System.Windows.Resources;

namespace mapapp
{
    public partial class App : Application, INotifyPropertyChanged
    {
        private static VoterViewModel _voterViewModel = null;
        public MapAppSettings _settings = null;
        /// <summary>
        /// A static ViewModel used by the views to bind against.
        /// </summary>
        /// <returns>The VoterViewModel object.</returns>
        public static VoterViewModel VotersViewModel
        {
            get
            {
                // Delay creation of the view model until necessary
                if (_voterViewModel == null)
                    _voterViewModel = new VoterViewModel();
                return _voterViewModel;
            }
            set
            {
                if (_voterViewModel != value)
                {
                    _voterViewModel = value;
                }
            }
        }

        private VoterFileEntry _selectedVoter;

        public VoterFileEntry SelectedHouse
        {
            get { return _selectedVoter; }
            set
            {
                if (_selectedVoter != value)
                {
                    _selectedVoter = value;
                    NotifyPropertyChanged("SelectedHouse");
                }
            }
        }

        public bool UpdateVoter()
        {
            bool success = false;
            VoterFileDataContext _voterDB = new VoterFileDataContext(string.Format(VoterFileDataContext.DBConnectionString, _settings.DbFileName));
            if (_voterDB.DatabaseExists())
            {
                try
                {
                    IQueryable<VoterFileEntry> voterQuery = from voter in _voterDB.AllVoters where voter.VoterID == _selectedVoter.VoterID select voter;
                    VoterFileEntry voterToUpdate = voterQuery.FirstOrDefault();
                    voterToUpdate.Party = _selectedVoter.Party;
                    voterToUpdate.ResultOfContact = _selectedVoter.ResultOfContact;
                    voterToUpdate.Email = _selectedVoter.Email;
                    voterToUpdate.CellPhone = _selectedVoter.CellPhone;
                    voterToUpdate.IsSupporter = _selectedVoter.IsSupporter;
                    voterToUpdate.IsVolunteer = _selectedVoter.IsVolunteer;
                    voterToUpdate.IsUpdated = _selectedVoter.IsUpdated;
                    voterToUpdate.Comments = _selectedVoter.Comments;
                    voterToUpdate.ModifiedTime = DateTime.Now;
                    _selectedVoter.ModifiedTime = voterToUpdate.ModifiedTime;
                    _voterDB.SubmitChanges(System.Data.Linq.ConflictMode.ContinueOnConflict);
                    success = true;
                }
                catch (Exception ex)
                {
                    Log(" Error updating voter record: " + _selectedVoter.FullName + "  : " + ex.ToString());
                }
            }
            return success;
        }

        public string ReportUpdates()
        {
            // TODO: This is where to change the updates to write in CSV format to REST interface
            string _updatesFileName = "";
            VoterFileDataContext _voterDB = new VoterFileDataContext(string.Format(VoterFileDataContext.DBConnectionString, _settings.DbFileName));
            if (_voterDB.DatabaseExists())
            {
                try
                {
                    IEnumerable<VoterFileEntry> voters = from voter in _voterDB.AllVoters
                                                         where voter.IsUpdated == true
                                                         select voter;
                    App.Log("  ReportUpdates Query completed. Starting write to XML");

                    // XmlSerializer serializer = new XmlSerializer(typeof(VoterFileEntry));
                    IsolatedStorageFile _iso = IsolatedStorageFile.GetUserStoreForApplication();
                    DateTime _now = DateTime.Now;
                    string _dateString = string.Format("{0:0000}{1:00}{2:00}{3:00}{4:00}{5:00}", _now.Year, _now.Month, _now.Day, _now.Hour, _now.Minute, _now.Second);
                    _updatesFileName = string.Format("VoterUpdates{0}.xml", _dateString);
                    IsolatedStorageFileStream _outputStream = _iso.CreateFile(_updatesFileName);
                    StreamWriter _updateFileStream = new StreamWriter(_outputStream);
                    StringBuilder xml = new StringBuilder();
                    xml.Clear();
                    xml.AppendLine("<VoterUpdates>");
                    xml.AppendFormat("\t<ReportDate>{0}</ReportDate>{1}", DateTime.Now, System.Environment.NewLine);
                    xml.AppendLine("\t<VoterList>");
                    _updateFileStream.Write(xml.ToString());
                    xml.Clear();

                    foreach (VoterFileEntry voter in voters)
                    {
                        xml.AppendLine("\t\t<VoterUpdate>");
                        xml.AppendFormat("\t\t\t<ID>{0}</ID>{1}", voter.VoterID, System.Environment.NewLine);
                        xml.AppendFormat("\t\t\t<Party>{0}</Party>{1}", voter.Party, System.Environment.NewLine);
                        xml.AppendFormat("\t\t\t<ResultOfContact>{0}</ResultOfContact>{1}", voter.ResultOfContact, System.Environment.NewLine);
                        xml.AppendFormat("\t\t\t<Email>{0}</Email>{1}", voter.Email, System.Environment.NewLine);
                        xml.AppendFormat("\t\t\t<CellPhone>{0}</CellPhone>{1}", voter.CellPhone, System.Environment.NewLine);
                        xml.AppendFormat("\t\t\t<IsSupporter>{0}</IsSupporter>{1}", voter.IsSupporter, System.Environment.NewLine);
                        xml.AppendFormat("\t\t\t<IsVolunteer>{0}</IsVolunteer>{1}", voter.IsVolunteer, System.Environment.NewLine);
                        xml.AppendFormat("\t\t\t<IsModified>{0}</IsModified>{1}", voter.IsUpdated, System.Environment.NewLine);
                        xml.AppendFormat("\t\t\t<Comments>{0}</Comments>{1}", voter.Comments, System.Environment.NewLine);
                        xml.AppendFormat("\t\t\t<ModifiedTime>{0}</ModifiedTime>{1}", voter.ModifiedTime, System.Environment.NewLine);
                        xml.AppendLine("\t\t</VoterUpdate>");
                        _updateFileStream.Write(xml.ToString());
                        xml.Clear();
                    }
                    xml.AppendLine("\t</VoterList>");
                    xml.AppendLine("</VoterUpdates>");
                    _updateFileStream.Write(xml.ToString());
                    _updateFileStream.Flush();
                    _updateFileStream.Close();
                    xml.Clear();
                    Log("  Completed writing update file " + _updatesFileName);
                }
                catch (Exception ex)
                {
                    Log(" Error reporting changes: " +  ex.ToString());
                    _updatesFileName = "";
                }
            }
            return _updatesFileName;
        }

        // private static VoterFileDataContext _voterDB;

        Thread _dbLoadThread;
        /*
        private bool _dbLoaded;

        public bool IsDbLoaded
        {
            get { return _dbLoaded; }
            set
            {
                _dbLoaded = value;
                this.NotifyPropertyChanged("IsDbLoaded");
            }
        }
         */

        /// <summary>
        /// Provides easy access to the root frame of the Phone Application.
        /// </summary>
        /// <returns>The root frame of the Phone Application.</returns>
        public PhoneApplicationFrame RootFrame { get; private set; }


        public static App thisApp
        {
            get;
            set;
        }

        public static Thread MainThread
        {
            get;
            set;
        }

        /// <summary>
        /// Constructor for the Application object.
        /// </summary>
        public App()
        {
            _settings = new MapAppSettings();
            
            // Global handler for uncaught exceptions. 
            UnhandledException += Application_UnhandledException;

            // Standard Silverlight initialization
            InitializeComponent();

            // Phone-specific initialization
            InitializePhoneApplication();

            // Show graphics profiling information while debugging.
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // Display the current frame rate counters.
                Application.Current.Host.Settings.EnableFrameRateCounter = true;

                // Show the areas of the app that are being redrawn in each frame.
                //Application.Current.Host.Settings.EnableRedrawRegions = true;

                // Enable non-production analysis visualization mode, 
                // which shows areas of a page that are handed off to GPU with a colored overlay.
                //Application.Current.Host.Settings.EnableCacheVisualization = true;

                // Disable the application idle detection by setting the UserIdleDetectionMode property of the
                // application's PhoneApplicationService object to Disabled.
                // Caution:- Use this under debug mode only. Application that disables user idle detection will continue to run
                // and consume battery power when the user is not using the phone.
                PhoneApplicationService.Current.UserIdleDetectionMode = IdleDetectionMode.Disabled;
            }

            thisApp = this;
            MainThread = Thread.CurrentThread;
            // Create the database if it does not exist.

            // Changing from initially loading voters.xml from static resource to getting it from SkyDrive
            // _dbLoadThread = new Thread(LoadDatabase);
            // _dbLoadThread.Start("voters.xml");
            if (_settings.DbStatus != DbState.Loaded)
            {

            }
            /*
            // now we want to create the database at the same time we load the data into it.
            using (mapapp.data.VoterFileDataContext db = new mapapp.data.VoterFileDataContext(mapapp.data.VoterFileDataContext.DBConnectionString))
            {
                if (db.DatabaseExists() == false)
                {
                    //Create the database
                    db.CreateDatabase();
                }
            }
             * */
            Thread.Sleep(1500);
        }

        // TODO: This is _probably_ the place to change load to pull datafile from TEST interface
        public void LoadDatabaseFromXmlStream(Stream xmlFile)
        {
            if (_settings.DbStatus == DbState.Loaded)
            {
                // TODO: Handle other states
                return;
            }
            else
            {
                if (App.VotersViewModel != null)
                {
                    App.VotersViewModel.StreetList.Clear();
                    App.VotersViewModel.VoterList.Clear();
                    App.VotersViewModel = null;
                }
                if (_dbLoadThread == null)
                    _dbLoadThread = new Thread(LoadDatabase);
                _dbLoadThread.Start(xmlFile);
                // LoadDatabase(xmlFile);
            }
        }

        public void LoadDatabaseFromFile(string xmlFile)
        {
            if (_settings.DbStatus == DbState.Loaded)
            {
                // TODO: Handle other states
                return;
            }
            else
            {
                if (App.VotersViewModel != null)
                {
                    App.VotersViewModel.StreetList.Clear();
                    App.VotersViewModel.VoterList.Clear();
                    App.VotersViewModel = null;
                }
                if (_dbLoadThread == null)
                    _dbLoadThread = new Thread(LoadDatabase);

                IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication();
                IsolatedStorageFileStream voterFile = isf.OpenFile(xmlFile, FileMode.Open);
                Stream xmlVoterDataStream = null;
                if (xmlFile.EndsWith(".zip"))
                {
                    string voterFileName = "voters.xml";
                    if (voterFile.CanSeek)
                    {
                        byte[] fileFront = new byte[64];
                        int bytesRead = voterFile.Read(fileFront, 0, 64);
                        voterFile.Seek(0, SeekOrigin.Begin);
                        byte[] fileNameBytes = new byte[20];
                        int i = 0;
                        for (i = 0; i < 16; i++)
                        {
                            fileNameBytes[i] = fileFront[30 + i];
                            if (fileNameBytes[i] > 128)
                            {
                                fileNameBytes[i] = 0;
                                break;
                            }
                        }
                        voterFileName = Encoding.UTF8.GetString(fileNameBytes, 0, i);
                    }
                    StreamResourceInfo zipInfo = new StreamResourceInfo(voterFile, "application/zip");
                    StreamResourceInfo streamInfo = Application.GetResourceStream(zipInfo, new Uri(voterFileName, UriKind.Relative));
                    if (streamInfo != null)
                        xmlVoterDataStream = streamInfo.Stream;
                }
                else if (xmlFile.EndsWith(".xml"))
                {
                    xmlVoterDataStream = voterFile;
                }
                if (xmlVoterDataStream != null)
                    _dbLoadThread.Start(xmlVoterDataStream);
            }
        }


        public void LoadDatabase(Object xmlFile)
        {
            /*
            <pins>
              <pin>
                <address>1234 Anystreet</address>
                <address2></address2>
                <city>Sammamish</city>
                <state>WA</state>
                <zip>98074</zip>
                <phone></phone>
                <lastname>BOND</lastname>
                <firstname>JAMES</firstname>
                <email></email>
                <precinct>MICHAEL</precinct>
                <party>1</party>
                <pvscore><pri>2</pri><gen>4</gen></pvscore>
                <recid>WA00092212345</recid>
                <location>47.6161616,-122.0551111</location>
              </pin>
            </pins>
            */
            int nVoters = 0;
            // TODO: If we have an existing database, update voter records for existing voters and add new voter records 

            VoterFileDataContext _voterDB = new VoterFileDataContext(string.Format(VoterFileDataContext.DBConnectionString, _settings.DbFileName));
            if (_settings.DbStatus == DbState.Loaded)
            {
                // TODO: prompt user to reload/overwrite database
                // TODO: Read database status from app settings
                // IsDbLoaded = true;               
                return;
            }
            else
            {
                try
                {
                    // TODO: This is where to load voter list from REST interface instead of xml file
                    if (!_voterDB.DatabaseExists())
                    {
                        App.Log("Creating new database...");
                        _voterDB.CreateDatabase();
                        _settings.DbStatus = DbState.Empty;
                        App.Log("  Database created");
                    }

                    XDocument loadedData = null;

                    if (xmlFile is Stream)
                        loadedData = XDocument.Load(xmlFile as Stream);
                    else
                        loadedData = XDocument.Load(xmlFile as string);


                    IEnumerable<XElement> dbDate = loadedData.Descendants("timestamp");
                    if (dbDate != null)
                    {
                        string dbDateString = dbDate.First<XElement>().Value;
                        DateTime dbTimeStamp;
                        if (DateTime.TryParse(dbDateString, out dbTimeStamp))
                            _settings.DbDate = dbTimeStamp;
                    }
                    IEnumerable<XElement> votersNode = loadedData.Descendants("voters");
                    if (votersNode != null)
                    {

                        App.Log("  XML file loaded");
                        IEnumerable<VoterFileEntry> voters = from query in votersNode.Descendants("voter")
                                                             select new VoterFileEntry
                                                             {
                                                                 FirstName = (string)query.Element("firstname"),
                                                                 LastName = (string)query.Element("lastname"),
                                                                 Address = (string)query.Element("address"),
                                                                 Address2 = (string)query.Element("address2"),
                                                                 City = (string)query.Element("city"),
                                                                 State = (string)query.Element("state"),
                                                                 Zip = (string)query.Element("zip"),
                                                                 VoterIdString = (string)query.Element("recid"),
                                                                 PartyString = (string)query.Element("party"),
                                                                 Precinct = (string)query.Element("precinct"),
                                                                 PvpString = (string)query.Element("pvscore").Element("pri"),
                                                                 PvgString = (string)query.Element("pvscore").Element("gen"),
                                                                 Email = (string)query.Element("email"),
                                                                 Phone = (string)query.Element("phone"),
                                                                 Coordinates = (string)query.Element("location"),
                                                                 ModifiedTime = DateTime.Now
                                                             };
                        App.Log("  Query completed");
                        _settings.DbStatus = DbState.Loading;
                        foreach (VoterFileEntry voter in voters)
                        {
                            if (0.0 == voter.Latitude || 0.0 == voter.Longitude)
                            {
                                continue;
                            }
                            nVoters++;
                            _voterDB.AllVoters.InsertOnSubmit(voter);
                        }
                    }
                    App.Log("  Voters submitted to database: " + nVoters.ToString());
                    _voterDB.SubmitChanges(System.Data.Linq.ConflictMode.ContinueOnConflict);
                    App.Log("  Changes committed to database");
                    _settings.DbStatus = DbState.Loaded;
                }
                catch (Exception ex)
                {
                    App.Log("Exception loading XML to database: " + ex.ToString());
                }
            }
        }

        // Code to execute when the application is launching (eg, from Start)
        // This code will not execute when the application is reactivated
        private void Application_Launching(object sender, LaunchingEventArgs e)
        {
            // TODO: reload location of Car from isostore
        }

        // Code to execute when the application is activated (brought to foreground)
        // This code will not execute when the application is first launched
        private void Application_Activated(object sender, ActivatedEventArgs e)
        {
            // TODO: reload location of Car from isostore
        }

        // Code to execute when the application is deactivated (sent to background)
        // This code will not execute when the application is closing
        private void Application_Deactivated(object sender, DeactivatedEventArgs e)
        {
            // TODO: write location of Car to isostore
        }

        // Code to execute when the application is closing (eg, user hit Back)
        // This code will not execute when the application is deactivated
        private void Application_Closing(object sender, ClosingEventArgs e)
        {
            // TODO: write location of Car to isostore
        }

        // Code to execute if a navigation fails
        private void RootFrame_NavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // A navigation has failed; break into the debugger
                System.Diagnostics.Debugger.Break();
            }
        }

        // Code to execute on Unhandled Exceptions
        private void Application_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // An unhandled exception has occurred; break into the debugger
                System.Diagnostics.Debugger.Break();
            }
        }

        #region Phone application initialization

        // Avoid double-initialization
        private bool phoneApplicationInitialized = false;

        // Do not add any additional code to this method
        private void InitializePhoneApplication()
        {
            if (phoneApplicationInitialized)
                return;

            // Create the frame but don't set it as RootVisual yet; this allows the splash
            // screen to remain active until the application is ready to render.
            RootFrame = new PhoneApplicationFrame();
            RootFrame.Navigated += CompleteInitializePhoneApplication;

            // Handle navigation failures
            RootFrame.NavigationFailed += RootFrame_NavigationFailed;

            // Ensure we don't initialize again
            phoneApplicationInitialized = true;
        }

        // Do not add any additional code to this method
        private void CompleteInitializePhoneApplication(object sender, NavigationEventArgs e)
        {
            // Set the root visual to allow the application to render
            if (RootVisual != RootFrame)
                RootVisual = RootFrame;

            // Remove this handler since it is no longer needed
            RootFrame.Navigated -= CompleteInitializePhoneApplication;
        }

        #endregion

        public event PropertyChangedEventHandler PropertyChanged;

        // Used to notify the page that a data context property changed
        private void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public static void Log(string messageString)
        {
            System.Diagnostics.Debug.WriteLine(string.Format("({2:X}) {0:H:mm:ss.fff}: {1}", DateTime.Now, messageString, Thread.CurrentThread.ManagedThreadId));
        }
    }

    public class VoterViewModel : INotifyPropertyChanged
    {
        public VoterViewModel()
        {
            _nearbyVoters = new ObservableCollection<PushpinModel>();
        }

        private ObservableCollection<PushpinModel> _nearbyVoters = new ObservableCollection<PushpinModel>();

        public ObservableCollection<PushpinModel> VoterList
        {
            get
            {
                return _nearbyVoters;
            }
            set
            {
                _nearbyVoters = value;
                NotifyPropertyChanged("VoterList");
            }
        }

        private List<string> _streets = new List<string>();

        public List<string> StreetList
        {
            get
            {
                return _streets;
            }
            set
            {
                if (_streets != value)
                {
                    _streets = value;
                    NotifyPropertyChanged("StreetList");
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        // Used to notify the page that a data context property changed
        private void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
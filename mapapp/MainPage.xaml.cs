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
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using System.Device.Location;
using Microsoft.Phone.Controls.Maps;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using System.Xml.Linq;
using System.Windows.Resources;
using System.Windows.Threading;
using System.Threading;
using mapapp.data;
using Microsoft.Phone.Shell;

namespace mapapp
{
    
    public partial class MainPage : PhoneApplicationPage, INotifyPropertyChanged
    {
        
        internal const string Id = "AgTD8VcX7TSsg5eldqc4Pmhs0ST2L2GGYt4P9vPJZM9ijZTYsmzCmet-6Va1oX8d";
        private readonly CredentialsProvider _credentialsProvider = new ApplicationIdCredentialsProvider(Id);
        public static readonly GeoCoordinate DefaultLocation = new GeoCoordinate(47.662929, -122.115863);
        private const double DefaultZoomLevel = 16.0;
        private const double MaxZoomLevel = 21.0;
        private const double MinZoomLevel = 10.0;
        private double _zoom;
        private GeoCoordinate _center;
        public event PropertyChangedEventHandler PropertyChanged;
        GeoCoordinateWatcher watcher;
        private bool firstfind = true;
        List<PushpinModel> listModels = new List<PushpinModel>();
        private bool _showPushpins = false;
        private bool _showMe = false;
        private bool _showCar = false;

        // NOTE: This setting is used for testing during development, and should be set to true before compiling a production build
        private bool _limitVoters = false;

        // NOTE: These are rough estimates of the number of degrees covered in one mile
        // in the Redmond, WA area. These should be calculated based on actual latitude
        // of the center of the viewport.
        // private double _mileLat = 0.015;
        // private double _mileLong = 0.027;

        private DataViewport _dataView = new DataViewport(DefaultLocation);
        private List<PushpinModel> _dataViewPins = new List<PushpinModel>();
       
        private bool follow = true;
        Thread backThread;
        ManualResetEvent wait = new ManualResetEvent(false);

        // quadtree tree = new quadtree();
        double zoomlevel = 0.0;

        private Pushpin _me;

        public Pushpin Me
        {
            get { return _me; }
            set
            {
                if (_me != value)
                {
                    _me = value;
                    NotifyPropertyChanged("Me");
                }
            }
        }

        private Pushpin _car;

        public Pushpin Car
        {
            get { return _car; }
            set
            {
                if (_car != value)
                {
                    _car = value;
                    NotifyPropertyChanged("Car");
                }
            }
        }

        private GeoCoordinate _here;
        public GeoCoordinate CurrentLocation
        {
            get { return _here; }
        }

        bool running = true;

        // private static mapapp.data.VoterFileDataContext _voterDB;

        ApplicationBar _mainAppBar = new ApplicationBar();
        
        // Constructor
        public MainPage()
        {
            InitializeComponent();
            DataContext = this;

            // _here = new GeoCoordinate();
            _here = DefaultLocation;

            Me = new Pushpin();
            Me.Content = "Me";

            Car = new Pushpin();
            Car.Content = "Car";
            ShowCar = false;

            // Map.

            Map.MapZoom += new EventHandler<MapZoomEventArgs>(MapZoomed);
            Map.MapPan += new EventHandler<MapDragEventArgs>(MapDragged);
            watcher = new GeoCoordinateWatcher(GeoPositionAccuracy.High); // using high accuracy
            watcher.MovementThreshold = 0.1; // use MovementThreshold to ignore noise in the signal
            
            watcher.StatusChanged += new EventHandler<GeoPositionStatusChangedEventArgs>(watcher_StatusChanged);
            watcher.PositionChanged += new EventHandler<GeoPositionChangedEventArgs<GeoCoordinate>>(watcher_PositionChanged);

            if (_mainAppBar == null)
                _mainAppBar = new ApplicationBar();
            _mainAppBar.Mode = ApplicationBarMode.Default;
            _mainAppBar.Opacity = 1.0;
            _mainAppBar.IsVisible = true;
            _mainAppBar.IsMenuEnabled = true;
            ApplicationBarIconButton buttonLocation = new ApplicationBarIconButton();
            buttonLocation.IconUri = new Uri("/Images/appbar.location.png", UriKind.Relative);
            buttonLocation.Text = "find me";
            buttonLocation.Click += Center_Click;
            _mainAppBar.Buttons.Add(buttonLocation);
            ApplicationBarIconButton buttonSatLayer = new ApplicationBarIconButton();
            buttonSatLayer.IconUri = new Uri("/Images/appbar.satlayer.png", UriKind.Relative);
            buttonSatLayer.Text = "street/sat";
            buttonSatLayer.Click += ChangeMapMode;
            _mainAppBar.Buttons.Add(buttonSatLayer);
            ApplicationBarIconButton buttonShowPushpins = new ApplicationBarIconButton();
            buttonShowPushpins.IconUri = new Uri("/Images/appbar.minus.png", UriKind.Relative);
            buttonShowPushpins.Text = "pushpins";
            buttonShowPushpins.Click += OnTogglePushpinViewMode;
            _mainAppBar.Buttons.Add(buttonShowPushpins);
            ApplicationBarIconButton buttonShowStreetList = new ApplicationBarIconButton();
            buttonShowStreetList.IconUri = new Uri("/Images/appbar.menu.png", UriKind.Relative);
            buttonShowStreetList.Text = "street list";
            buttonShowStreetList.Click += ListStreets;
            _mainAppBar.Buttons.Add(buttonShowStreetList);
            ApplicationBarMenuItem menuitemZoomOut = new ApplicationBarMenuItem("zoom out");
            menuitemZoomOut.Click += ZoomOut;
            _mainAppBar.MenuItems.Add(menuitemZoomOut);
            ApplicationBarMenuItem menuitemPlaceCar = new ApplicationBarMenuItem("set car location");
            menuitemPlaceCar.Click += new EventHandler(menuitemPlaceCar_Click);
            _mainAppBar.MenuItems.Add(menuitemPlaceCar);
            ApplicationBarMenuItem menuitemFindCar = new ApplicationBarMenuItem("show car location");
            menuitemFindCar.Click += new EventHandler(menuitemFindCar_Click);
            _mainAppBar.MenuItems.Add(menuitemFindCar);
            if (_limitVoters == false) // NOTE: This menu item is only useful in test mode when the voter list size is small
            {
                ApplicationBarMenuItem menuitemCenterOnVoters = new ApplicationBarMenuItem("center on voters");
                menuitemCenterOnVoters.Click += new EventHandler(menuitemCenterOnVoters_Click);
                _mainAppBar.MenuItems.Add(menuitemCenterOnVoters);
            }
            ApplicationBarMenuItem menuitemSendChanges = new ApplicationBarMenuItem("upload/download data");
            menuitemSendChanges.Click += new EventHandler(menuitemSendChanges_Click);
            _mainAppBar.MenuItems.Add(menuitemSendChanges);
            ApplicationBar = _mainAppBar;

            watcher.Start();
            
            CenterLocation();
            _dataView.SetSize(0.0);

            Zoom = Map.ZoomLevel;
            zoomlevel = Map.ZoomLevel;

            App.thisApp.PropertyChanged += new PropertyChangedEventHandler(thisApp_PropertyChanged);
            backThread = new Thread(BackgroundThread);
            if (App.thisApp._settings.DbStatus == DbState.Loaded)
            {
                App.Log("DB already loaded. Starting background thread...");
                backThread.Start();
                ShowPushpins = true;
                wait.Set();
            }
            else
            {
                
            }
        }

        void menuitemFindCar_Click(object sender, EventArgs e)
        {
            if (null != Car)
            {
                if (Car.Location.IsUnknown)
                {
                    App.Log("Dude! Where's my car?!");
                    Center = Me.Location;
                }
                else
                    Center = Car.Location;
            }
        }

        void menuitemPlaceCar_Click(object sender, EventArgs e)
        {
            if (null == Car)
            {
                Car = new Pushpin();
                Car.Content = "Car";
            }
            Car.Location = Me.Location;
            ShowCar = true;
        }

        void menuitemCenterOnVoters_Click(object sender, EventArgs e)
        {
            if (listModels.Count > 0)
            {
                GeoCoordinate votersCenter = FindCenterOfVoters(listModels);
                if (!votersCenter.IsUnknown)
                    Center = votersCenter;
            }
        }

        private GeoCoordinate FindCenterOfVoters(List<PushpinModel> list)
        {
            GeoCoordinate locCenter = new GeoCoordinate(Center.Latitude, Center.Longitude);
            // List<PushpinModel> voters = GetLocalVoters(locCenter);

            double west = list.Min<PushpinModel, double>(vMin => vMin.Location.Longitude); // smallest longitude found in list (or largest absolute value)
            double east = list.Max<PushpinModel, double>(vMax => vMax.Location.Longitude); // largest longitude found in list (or smallest absolute value)
            double north = list.Max<PushpinModel, double>(vMax => vMax.Location.Latitude); // largest latitude found in list
            double south = list.Min<PushpinModel, double>(vMin => vMin.Location.Latitude); // smallest latitude found in list

            double diffLat = north - south;
            double diffLong = east - west;
            locCenter.Latitude = south + (diffLat/2);
            locCenter.Longitude = west + (diffLong/2);
            return locCenter;
        }

        void menuitemSendChanges_Click(object sender, EventArgs e)
        {
            // App.thisApp.ReportUpdates();
            // this.NavigationService.Navigate(new Uri("/LiveAccessPage.xaml", UriKind.Relative));
            this.NavigationService.Navigate(new Uri("/DataManagePage.xaml", UriKind.Relative));
        }

        void thisApp_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            App.Log("     Property changed = " + e.PropertyName);
            if (e.PropertyName.Equals("IsDbLoaded") && App.thisApp._settings.DbStatus == DbState.Loaded)
            {
                if (backThread.ThreadState == ThreadState.Unstarted)
                {
                    App.Log("DB is finally loaded, starting background thread...");
                    backThread.Start();
                    ShowPushpins = true;
                }
            }
        }

        public void BackgroundThread()
        {
            // LoadPushPins();
            App.Log("Started background thread...");
            LocationRect _lastRect = null;

            while (wait.WaitOne())
            {
                App.Log("  Wait completed at top of background thread loop.");
                wait.Reset();

                if (running == false)
                {
                    App.Log("  Exiting background thread.");
                    return;
                }

                LocationRect lr = Map.BoundingRectangle;

                if (lr.Equals(_lastRect))
                {
                    App.Log("  We haven't moved, continuing...");
                    continue;
                }

                LocationRect alr = lr;

                double west = lr.West - (lr.East - lr.West) / 3.0;
                double east = lr.East + (lr.East - lr.West) / 3.0;
                double north = lr.North + (lr.North - lr.South) / 3.0;
                double south = lr.South - (lr.North - lr.South) / 3.0;

                bool bfullcontent = false;

                if (zoomlevel < 14.0)
                {
                    App.Log("   Zoomed out too far - continuing");
                    continue;
                }

                if (zoomlevel > 18.0)
                {
                    bfullcontent = true;
                }
                // List<PushpinModel> l = tree.Search(lr.South, lr.North, lr.West, lr.East);
                App.Log("    Made it through prelims - still here.");

                if (!_dataView.IsWithinView(lr))
                {
                    App.Log("  Calling GetLocalVoters...");
                    _dataViewPins = GetLocalVoters(lr.Center);
                    App.Log("  GetLocalVoters call completed.");
                }
                else
                {
                    App.Log("  Still within current dataview, skipping call to GetLocalVoters.");
                    if (lr.Center == _lastRect.Center)
                    {
                        App.Log("   Still in same location. continuing...");
                        continue;
                    }
                }

                _lastRect = lr;
                List<PushpinModel> l = new List<PushpinModel>(_dataViewPins.AsEnumerable());

                int total = 0;

                if (wait.WaitOne(0))
                {
                    App.Log("  Wait was not signaled, continuing.");
                    continue;
                }

                App.Log("  Loading pushpins and calculating distance...");
                foreach (PushpinModel p in l)
                {
                    //calculate distance from center of current viewport of map
                    double lat = lr.Center.Latitude - p.Location.Latitude;
                    double lon = lr.Center.Longitude - p.Location.Longitude;
                    double dist = Math.Sqrt((lat * lat) + (lon * lon));
                    p.dist = dist;

                    if (wait.WaitOne(0))
                    {
                        break;
                    }
                }
                App.Log("  Finished loading Pushpins.");

                if (wait.WaitOne(0))
                {
                    App.Log("  Wait was not signaled, continuing (2).");
                    continue;
                }

                App.Log("  Sorting pushpins...");
                // Sort list of pushpins based on distance from center
                Comparison<PushpinModel> c = new Comparison<PushpinModel>(SortDistance);
                l.Sort(c);
                App.Log("  Done sorting pushpins.");

                if (wait.WaitOne(0))
                {
                    App.Log("  Wait was not signaled, continuing (3).");
                    continue;
                }

                App.Log("  Adding closest pushpins to listModel.");
                listModels.Clear();
                foreach (PushpinModel p in l)
                {
                    total++;

                    if (bfullcontent)
                    {
                        p.Content = p.FullName + "\r" + p.VoterFile.Address + " " + p.VoterFile.Address2;
                    }
                    else 
                    {
                        p.Content = p.VoterFile.LastName;
                    }

                    listModels.Add(p);
                    // Check to see if this PushPin in list is more than preset distance from center, break if so
                    // TODO: While testing, let's not limit pushpins
                    /*
                    if (false == (west <= p.Location.Longitude && east >= p.Location.Longitude &&
                       north >= p.Location.Latitude && south <= p.Location.Latitude))
                    {
                        break;
                    }
                    // If we have already added 200 pushpins to the map, stop adding them
                    if (total >= 200)
                    {
                        break;
                    }
                    */

                    if (wait.WaitOne(0))
                    {
                        break;
                    }
                }
                App.Log("  Done loading closest pushpins.");

                if (App.thisApp._settings.DbStatus == DbState.Loaded)
                {
                    // listModels.Add(me);
                    // ShowMe = true;
                    // ShowPushpins = true;
                }

                if (wait.WaitOne(0))
                {
                    App.Log("  Wait was not signaled, continuing (4).");
                    continue;
                }

                App.Log("  Invoking UpdatePins...");
                Dispatcher.BeginInvoke(UpdatePins);
            }
        }

        public void UpdatePins()
        {
            Pushpins.Clear();
            foreach (PushpinModel p in listModels)
            {
                Pushpins.Add(p);
            }
            NotifyPropertyChanged("Pushpins");

            // Make sure we still have the right ApplicationBar
            if (ApplicationBar != _mainAppBar)
                ApplicationBar = _mainAppBar;
        }

        public void MapDragged(object o, MapDragEventArgs e)
        {
            // TODO: We need to reload the listModels again
            zoomlevel = Map.ZoomLevel;
            follow = false;
            wait.Set();
        }

        public void MapZoomed(object o, MapZoomEventArgs e)
        {
            // we need to reload the listModels again
            zoomlevel = Map.ZoomLevel;
            wait.Set();
        }

        int SortDistance(PushpinModel p1, PushpinModel p2)
        {
            if (p1.dist > p2.dist)
            {
                return 1;
            }
            else if (p1.dist < p2.dist)
            {
                return -1;
            }

            return 0;
        }


        public CredentialsProvider CredentialsProvider
        {
            get { return _credentialsProvider; }
        }

        private List<PushpinModel> GetLocalVoters(GeoCoordinate center)
        {
            VoterFileDataContext _voterDB = new mapapp.data.VoterFileDataContext(string.Format(mapapp.data.VoterFileDataContext.DBConnectionString, App.thisApp._settings.DbFileName));
            System.Diagnostics.Debug.Assert(_voterDB.DatabaseExists());

            List<PushpinModel> _list = new List<PushpinModel>();
            /*
             * where voter.Latitude <= _north && voter.Latitude >= _south && voter.Longitude >= _west && voter.Longitude <= _east
            */
            if (!(App.thisApp._settings.DbStatus == DbState.Loaded))
            {
                App.Log("Database not ready to load voters yet.");
                return _list;
            }

            _dataView.SetSize(2.0);
            _dataView.SetCenterLocation(center);

            double _north = _dataView.North;
            double _south = _dataView.South;
            double _east = _dataView.East;
            double _west = _dataView.West;


            // BEGIN TEST
            // double west = 0.0; // smallest longitude found in list (or largest absolute value)
            // double east = 0.0; // largest longitude found in list (or smallest absolute value)
            // double north = 0.0; // largest latitude found in list
            // double south = 0.0; // smallest latitude found in list

            IEnumerable<string> precincts = (from v in _voterDB.AllVoters select v.Precinct).Distinct();
            foreach (string stPrecinct in precincts)
            {
                // App.Log(" List includes precinct: " + stPrecinct);
                List<VoterFileEntry> pList = (from VoterFileEntry vPrecinct in _voterDB.AllVoters where vPrecinct.Precinct == stPrecinct select vPrecinct).ToList();
                App.Log(String.Format("   There are {0} voters in {1} precinct.", pList.Count, stPrecinct));
                double west = pList.Min<VoterFileEntry, double>(vMin => vMin.Longitude); // smallest longitude found in list (or largest absolute value)
                double east = pList.Max<VoterFileEntry, double>(vMax => vMax.Longitude); // largest longitude found in list (or smallest absolute value)
                double north = pList.Max<VoterFileEntry, double>(vMax => vMax.Latitude); // largest latitude found in list
                double south = pList.Min<VoterFileEntry, double>(vMin => vMin.Latitude); // smallest latitude found in list

                App.Log(String.Format("    North={0}, South={1}, East={2}, West={3}.", north, south, east, west));
            }
            // END TEST

            IEnumerable<VoterFileEntry> data = from VoterFileEntry voter in _voterDB.AllVoters
                                               where voter.Latitude <= _north && voter.Latitude >= _south && voter.Longitude >= _west && voter.Longitude <= _east
                                               select voter;

            if (_limitVoters == false)
            {
                data = from VoterFileEntry voter in _voterDB.AllVoters
                       select voter;
            }

            PushpinModel _pin;
            int _counter = 0;
            foreach (VoterFileEntry _v in data)
            {
                _pin = new PushpinModel(_v);
                if (0.0 == _pin.Location.Latitude || 0.0 == _pin.Location.Longitude)
                {
                    continue;
                }

                if (_limitVoters)
                {
                    // If this record is more than a mile away north or south, ignore it
                    if (_pin.Location.Latitude < _south || _pin.Location.Latitude > _north)
                    {
                        App.Log("Oops! Query returned voters too far north or south.");
                        continue;
                    }

                    // If this record is more than a mile away east or west, ignore it
                    if (_pin.Location.Longitude < _west || _pin.Location.Longitude > _east)
                    {
                        App.Log("Oops! Query returned voters too far east or west.");
                        continue;
                    }
                }

                _counter++;

                if (!_pin.Location.IsUnknown)
                {
                    _list.Add(_pin);
                    _pin.Visibility = Visibility.Visible;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Error: A pin doesn't have a location: " + _pin.Content);
                }
            }

            if (_list.Count == 0 || _counter == 0)
            {
                App.Log("There are no voters around here!");
            }
            else
                System.Diagnostics.Debug.WriteLine("Found {0} voters near here.", _counter);
            // TEST: Does Dispose() kill the data in the VoterData objects in the PushPins
            _voterDB.Dispose();
            return _list;
        }

        private void ChangeMapMode(object o, EventArgs e)
        {
            if (Map.Mode is AerialMode)
            {
                Map.Mode = new RoadMode();
            }
            else
            {
                Map.Mode = new AerialMode(true);
            }
        }
        
        private void Follow(object o, EventArgs e)
        {
            follow = true;
        }

        public double Zoom
        {
            get { return _zoom; }
            set
            {
                var coercedZoom = Math.Max(MinZoomLevel, Math.Min(MaxZoomLevel, value));
                if (_zoom != coercedZoom)
                {
                    _zoom = value;
                    NotifyPropertyChanged("Zoom");
                }
            }
        }

        public GeoCoordinate Center
        {
            get { return _center; }
            set
            {
                if (_center != value)
                {
                    _center = value;

                    NotifyPropertyChanged("Center");
                }
            }
        }

        private void CenterLocation()
        {
            if (null != Me)
            {
                if (Me.Location.IsUnknown)
                {
                    App.Log("I don't know where I am!");
                    Center = DefaultLocation;
                }
                else
                    Center = Me.Location;
            }
        }

        private void CreateNewPushpin(GeoCoordinate location)
        {
            PushpinModel p = new PushpinModel();
            p.Location = location;
            Pushpins.Add(p);
        }

        

        private readonly ObservableCollection<PushpinModel> _pushpins = new ObservableCollection<PushpinModel>
        {

        };

        public ObservableCollection<PushpinModel> Pushpins
        {
            get { return _pushpins; }
        }

        public bool ShowPushpins
        {
            get { return _showPushpins; }
            set
            {
                if (_showPushpins != value)
                {
                    _showPushpins = value;
                    NotifyPropertyChanged("ShowPushpins");
                }
            }
        }

        public bool ShowMe
        {
            get { return _showMe; }
            set
            {
                _showMe = value;
                NotifyPropertyChanged("ShowMe");
            }
        }

        public bool ShowCar
        {
            get { return _showCar; }
            set
            {
                if (_showCar != value)
                {
                    _showCar = value;
                    NotifyPropertyChanged("ShowCar");
                }
            }
        }

        public void ZoomOut(object sender, EventArgs e)
        {
            if (Zoom > MinZoomLevel + 1)
                Zoom = Zoom - 1;
            else
                Zoom = MinZoomLevel;
            MapZoomed(sender, e as MapZoomEventArgs);
        }

        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                if (CheckAccess())
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                }
                else
                {
                    Dispatcher.BeginInvoke(new Action<string>(NotifyPropertyChanged), propertyName);
                }
            }
        }

        private void Center_Click(object sender, EventArgs e)
        {
            CenterLocation();
            follow = true;
            NotifyPropertyChanged("Pushpins");
        }

        // Event handler for the GeoCoordinateWatcher.StatusChanged event.
        void watcher_StatusChanged(object sender, GeoPositionStatusChangedEventArgs e)
        {
            switch (e.Status)
            {
                case GeoPositionStatus.Disabled:
                    // The Location Service is disabled or unsupported.
                    // Check to see whether the user has disabled the Location Service.
                    if (watcher.Permission == GeoPositionPermission.Denied)
                    {
                        watcher.Stop();
                    }
                    else
                    {
                        watcher.Stop();
                    }
                    break;

                case GeoPositionStatus.Initializing:
                    break;

                case GeoPositionStatus.NoData:
                    watcher.Stop();
                    break;

                case GeoPositionStatus.Ready:
                    {
                        ShowMe = true;
                    }
                    break;
            }
        }

        void watcher_PositionChanged(object sender, GeoPositionChangedEventArgs<GeoCoordinate> e)
        {
            if (null == Me)
            {
                App.Log("I don't exist.");
                return;
            }

            if (Me.Location.IsUnknown)
                App.Log("The map moved, but I don't know where I am!");

            if (System.Diagnostics.Debugger.IsAttached)
            {
                if (CurrentLocation != null && CurrentLocation.IsUnknown)
                {
                    _here = DefaultLocation;
                    Me.Location = DefaultLocation;
                }
                else
                {
                    _here = e.Position.Location;
                    Me.Location = e.Position.Location;
                }
            }
            else
            {
                _here = e.Position.Location;
                Me.Location = e.Position.Location;
            }
            if (!Me.Location.IsUnknown)
            {
                ShowMe = true;
            }

            if (firstfind || follow)
            {
                App.Log("   Centering for first time, or following location");
                CenterLocation();
            }

            if (firstfind)
            {
                Zoom = DefaultZoomLevel;
                zoomlevel = Zoom;
                firstfind = false;
            }
            App.Log(string.Format("  Position changed: Zoom={0}, zoomLevel={1}, Map.TargetZoom={2}", Zoom, zoomlevel, Map.TargetZoomLevel));

            NotifyPropertyChanged("Pushpins");
            NotifyPropertyChanged("Me");
            NotifyPropertyChanged("CurrentLocation");
            App.Log("  Setting thread wait from watcher_PositionChanged");
            wait.Set();
        }

        private void ListStreets(object sender, EventArgs e)
        {
            App.VotersViewModel.VoterList.Clear();
            foreach (PushpinModel p in listModels)
            {
                if (p.Content.Equals("Me"))
                    continue;
                if ((p.Street != null) && (!App.VotersViewModel.StreetList.Contains(p.Street)))
                {
                    App.VotersViewModel.StreetList.Add(p.Street);
                }
                App.VotersViewModel.VoterList.Add(p);
            }
            this.NavigationService.Navigate(new Uri("/HouseListPage.xaml", UriKind.Relative));
        }

        private void PhoneApplicationPage_BackKeyPress(object sender, CancelEventArgs e)
        {
            if (this.NavigationService.CanGoBack)
                this.NavigationService.GoBack();
        }

        private void OnTogglePushpinViewMode(object sender, EventArgs e)
        {
            ShowPushpins = !ShowPushpins;
            UpdatePins();
        }

        private void Pushpin_Hold(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (sender is Pushpin)
            {
                Pushpin pinHeld = sender as Pushpin;
                if (pinHeld.DataContext is PushpinModel)
                {
                    PushpinModel pinModel = pinHeld.DataContext as PushpinModel;
                    App.thisApp.SelectedHouse = pinModel.VoterFile;
                    this.NavigationService.Navigate(new Uri("/VoterDetailsPage.xaml", UriKind.Relative));
                }
            }
        }

        private void LayoutRoot_Loaded(object sender, RoutedEventArgs e)
        {
            // Make sure we still have the right ApplicationBar
            if (ApplicationBar != _mainAppBar)
                ApplicationBar = _mainAppBar;
        }
    }

    public class DataViewport
    {
        LocationRect dataView;
        private double _mileLat = 0.015;
        private double _mileLong = 0.027;

        private double _size = 0.0;

        public double North { get { return dataView.North; } }
        public double South { get { return dataView.South; } }
        public double East { get { return dataView.East; } }
        public double West { get { return dataView.West; } }

        public DataViewport(GeoCoordinate center)
        {
            dataView = new LocationRect(center, (_size * _mileLong), (_size * _mileLat));
        }

        public void SetSize(double size)
        {
            _size = size;
            dataView.East = dataView.Center.Longitude + (size * _mileLong);
            dataView.West = dataView.Center.Longitude - (size * _mileLong);
            dataView.North = dataView.Center.Latitude + (size * _mileLat);
            dataView.South = dataView.Center.Latitude - (size * _mileLat);
        }

        public void SetCenterLocation(GeoCoordinate center)
        {
            dataView = new LocationRect(center, 2 * _mileLong, 2 * _mileLat);
        }

        public bool IsWithinView(LocationRect screenView)
        {
            if (dataView.North >= screenView.North && dataView.South <= screenView.South &&
                dataView.East >= screenView.East && dataView.West <= screenView.West)
                return true;
            else
                return false;
        }
    }
}
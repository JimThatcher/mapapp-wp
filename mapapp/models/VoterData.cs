﻿using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System;

namespace mapapp.data
{
    [Table(Name="VoterFile")]
    public class VoterFileEntry : INotifyPropertyChanged, INotifyPropertyChanging
    {
        // Version column aids update performance.
        [Column(IsVersion = true)]
        private Binary _version;


        /// <summary>
        /// Unique Voter ID field - used as primary key. May be the state voter ID or a voter file database unique redcord ID,
        /// but it must be an integer. State IDs that are numeric with a state prefix, such as WA001234567 must have the text
        /// prefix removed prior to being set in the VoterFileEntry object
        /// </summary>
        private long _id;
        [Column(IsPrimaryKey = true, IsDbGenerated = true, DbType = "INT NOT NULL Identity", CanBeNull = false)]
        public long ID
        {
            get { return _id; }
            set
            {
                if (_id != value)
                {
                    NotifyPropertyChanging("ID");
                    _id = value;
                    NotifyPropertyChanged("ID");
                }
            }
        }

        /// <summary>
        /// Unique Voter ID field - used as primary key. May be the state voter ID or a voter file database unique redcord ID,
        /// but it must be an integer. State IDs that are numeric with a state prefix, such as WA001234567 must have the text
        /// prefix removed prior to being set in the VoterFileEntry object
        /// </summary>
        private long _voterID;
        [Column]
        public long VoterID
        {
            get { return _voterID; }
            set
            {
                if (_voterID != value)
                {
                    NotifyPropertyChanging("VoterID");
                    _voterID = value;
                    NotifyPropertyChanged("VoterID");
                }
            }
        }

        public string VoterIdString
        {
            get { return VoterID.ToString(); }
            set
            {
                string _id = value;
                if (_id.StartsWith("WA"))
                {
                    _id = _id.Substring(2);
                }
                long _idLong = 0;
                if (long.TryParse(_id, out _idLong))
                    VoterID = _idLong;
                else
                {
                    string _IdNumbersOnly = "";
                    foreach (char c in _id)
                    {
                        if (c >= '0' && c <= '9')
                            _IdNumbersOnly += c;
                    }
                    if (long.TryParse(_IdNumbersOnly, out _idLong))
                        VoterID = _idLong;
                }
            }
        }

        /// <summary>
        /// First name of voter
        /// </summary>
        private string _firstName;
        [Column]
        public string FirstName
        {
            get
            {
                return _firstName;
            }
            set
            {
                if (_firstName != value)
                {
                    NotifyPropertyChanging("FirstName");
                    _firstName = value;
                    NotifyPropertyChanged("FirstName");
                }
            }
        }

        /// <summary>
        /// Voter's last name
        /// </summary>
        private string _lastName;
        [Column]
        public string LastName
        {
            get { return _lastName; }
            set
            {
                if (_lastName != value)
                {
                    NotifyPropertyChanging("LastName");
                    _lastName = value;
                    NotifyPropertyChanged("LastName");
                }
            }
        }

        /// <summary>
        /// Voter's registered address (first line)
        /// </summary>
        private string _address;
        [Column]
        public string Address
        {
            get { return _address; }
            set
            {
                if (_address != value)
                {
                    NotifyPropertyChanging("Address");
                    _address = value;
                    NotifyPropertyChanged("Address");
                }
            }
        }

        public string FullName
        {
            get
            {
                return string.Format("{0}, {1}", LastName, FirstName);
            }
        }

        /// <summary>
        /// Line 2 of voter's registered address (Apartment, etc.)
        /// </summary>
        private string _address2;
        [Column]
        public string Address2
        {
            get { return _address2; }
            set
            {
                if (_address2 != value)
                {
                    NotifyPropertyChanging("Address2");
                    _address2 = value;
                    NotifyPropertyChanged("Address2");
                }
            }
        }

        /// <summary>
        /// City of voter's registered address
        /// </summary>
        private string _city;
        [Column]
        public string City
        {
            get { return _city; }
            set
            {
                if (_city != value)
                {
                    NotifyPropertyChanging("City");
                    _city = value;
                    NotifyPropertyChanged("City");
                }
            }
        }

        /// <summary>
        /// State of voter's registered address
        /// </summary>
        private string _state;
        [Column]
        public string State
        {
            get { return _state; }
            set
            {
                if (_state != value)
                {
                    NotifyPropertyChanging("State");
                    _state = value;
                    NotifyPropertyChanged("State");
                }
            }
        }

        /// <summary>
        /// Zip code of voter's registered address
        /// </summary>
        private string _zip;
        [Column]
        public string Zip
        {
            get { return _zip; }
            set
            {
                if (_zip != value)
                {
                    NotifyPropertyChanging("Zip");
                    _zip = value;
                    NotifyPropertyChanged("Zip");
                }
            }
        }


        /// <summary>
        /// Phone number
        /// </summary>
        private string _phone;
        [Column]
        public string Phone
        {
            get { return _phone; }
            set
            {
                if (_phone != value)
                {
                    NotifyPropertyChanging("Phone");
                    _phone = value;
                    NotifyPropertyChanged("Phone");
                }
            }
        }

        /// <summary>
        /// Returns the phone number formatted for display
        /// Phone is stored with digits only
        /// </summary>
        public string PhoneFormatted
        {
            get
            {
                string o = "";
                int digits = 0;
                string p = _phone.Trim();

                foreach (char c in p)
                {
                    if (char.IsDigit(c))
                    {
                        digits++;
                        o = o + c;
                        if (((p.Length > 10) && (digits == (p.Length - 10))) || 
                            ((p.Length > 7) && (digits == (p.Length - 7))) || 
                            ((p.Length > 4) && (digits == (p.Length - 4))))
                        {
                            o = o + "-";
                        }
                    }
                }

                return o;
            }
        }

        /// <summary>
        /// Indicates the voter's political party preference on a scale of 0-6.
        /// 0 = Unidentified/undeclared
        /// 1 = Strong Republican
        /// 2 = Leaning Republican
        /// 3 = Declared Independent or undecided
        /// 4 = Leaning Democrat
        /// 5 = Strong Democrat
        /// 6 = Refused to answer
        /// </summary>
        private int _party;
        [Column]
        public int Party
        {
            get { return _party; }
            set
            {
                if (_party != value)
                {
                    NotifyPropertyChanging("Party");
                    _party = value;
                    NotifyPropertyChanged("Party");
                }
            }
        }

        public string PartyString
        {
            get { return _party.ToString(); }
            set
            {
                int nParsed = 0;
                if (value.Length > 0)
                {
                    if (int.TryParse(value, out nParsed))
                        Party = nParsed;
                }
                Party = nParsed;
            }
        }

        /// <summary>
        /// Political precinct in which voter lives.
        /// </summary>
        private string _precinct;
        [Column]
        public string Precinct
        {
            get { return _precinct; }
            set
            {
                if (_precinct != value)
                {
                    NotifyPropertyChanging("Precinct");
                    _precinct = value;
                    NotifyPropertyChanged("Precinct");
                }
            }
        }

         /// <summary>
        /// Perfect Voter score for Primary Elections.
        /// Indicates the number of times the voter has voted in the previous 4 primary elections.
        /// A value of 5 indicates that this is a new voter.
        /// </summary>
        private int _PvPri;
        [Column]
        public int PerfectVoterPrimary
        {
            get { return _PvPri; }
            set
            {
                if (_PvPri != value)
                {
                    NotifyPropertyChanging("PerfectVoterPrimary");
                    _PvPri = value;
                    NotifyPropertyChanged("PerfectVoterPrimary");
                }
            }
        }

        public string PvpString
        {
            get { return _PvPri.ToString(); }
            set
            {
                int nParsed = 0;
                if (value.Length > 0)
                {
                    if (int.TryParse(value, out nParsed))
                        PerfectVoterPrimary = nParsed;
                }
                else
                    nParsed = 5;
                PerfectVoterPrimary = nParsed;
            }
        }


       /// <summary>
        /// Perfect Voter score for General Elections.
        /// Indicates the number of times the voter has voted in the previous 4 general elections.
        /// A value of 5 indicates that this is a new voter.
        /// </summary>
        private int _PvGen;
        [Column]
        public int PerfectVoterGeneral
        {
            get { return _PvGen; }
            set
            {
                if (_PvGen != value)
                {
                    NotifyPropertyChanging("PerfectVoterGeneral");
                    _PvGen = value;
                    NotifyPropertyChanged("PerfectVoterGeneral");
                }
            }
        }

        public string PvgString
        {
            get { return _PvGen.ToString(); }
            set
            {
                int nParsed = 0;
                if (value.Length > 0)
                {
                    if (int.TryParse(value, out nParsed))
                        PerfectVoterGeneral = nParsed;
                }
                else
                    nParsed = 5;
                PerfectVoterGeneral = nParsed;
            }
        }


        /// <summary>
        /// Latitude of voter's registered address
        /// </summary>
        private float _lat;
        [Column]
        public float Latitude
        {
            get { return _lat; }
            set
            {
                if (_lat != value)
                {
                    NotifyPropertyChanging("Latitude");
                    _lat = value;
                    NotifyPropertyChanged("Latitude");
                }
            }
        }

        /// <summary>
        /// Longitude of voter's registered address
        /// </summary>
        private float _long;
        [Column]
        public float Longitude
        {
            get { return _long; }
            set
            {
                if (_long != value)
                {
                    NotifyPropertyChanging("Longitude");
                    _long = value;
                    NotifyPropertyChanged("Longitude");
                }
            }
        }

        /// <summary>
        /// This is used set the Latitude and Longitude from a string in the XML loading process
        /// </summary>
        public string Coordinates
        {
            get
            {
                string _coords = string.Format("{0},{1}", Latitude, Longitude);
                return _coords;
            }
            set
            {
                string[] latlong = value.Split(',');
                double lat = 0.0;
                double lon = 0.0;

                // System.Diagnostics.Debug.Assert(latlong.Length == 2);
                if (latlong.Length == 2)
                {
                    double.TryParse(latlong[0], out lat);
                    double.TryParse(latlong[1], out lon);
                }
                Latitude = (float)lat;
                Longitude = (float)lon;
            }
        }

        /*
        private EntityRef<VoterUpdateInfo> _voterUpdate;
        [Association(Storage = "_voterUpdate", ThisKey = "VoterID", OtherKey = "VoterID")]
        public VoterUpdateInfo VoterUpdates
        {
            get { return this._voterUpdate.Entity; }
            set 
            {
                NotifyPropertyChanging("VoterUpdates");
                this._voterUpdate.Entity = value;
                // TODO: Set any information we already have into the VoterUpdate if this is a clean VoterUpdate 
                NotifyPropertyChanged("VoterUpdates");
            }
        }
         */
        /// <summary>
        /// Opt-in email address for voter
        /// </summary>
        private string _email;
        [Column]
        public string Email
        {
            get { return _email; }
            set
            {
                if (_email != value)
                {
                    NotifyPropertyChanging("Email");
                    _email = value;
                    NotifyPropertyChanged("Email");
                }
            }
        }

        /// <summary>
        /// Opt-in Cell phone number for voter
        /// </summary>
        private string _cellPhone;
        [Column]
        public string CellPhone
        {
            get { return _cellPhone; }
            set
            {
                if (_cellPhone != value)
                {
                    NotifyPropertyChanging("CellPhone");
                    _cellPhone = value;
                    NotifyPropertyChanged("CellPhone");
                }
            }
        }

        /// <summary>
        /// Voter indicated support for candidate/issue
        /// </summary>
        private bool _isSupporter;
        [Column]
        public bool IsSupporter
        {
            get { return _isSupporter; }
            set
            {
                if (_isSupporter != value)
                {
                    NotifyPropertyChanging("IsSupporter");
                    _isSupporter = value;
                    NotifyPropertyChanged("IsSupporter");
                }
            }
        }

        /// <summary>
        /// Indicates whether the voter is willing to volunteer for the campaign
        /// </summary>
        private bool _isVolunteer;
        [Column]
        public bool IsVolunteer
        {
            get { return _isVolunteer; }
            set
            {
                if (_isVolunteer != value)
                {
                    NotifyPropertyChanging("IsVolunteer");
                    _isVolunteer = value;
                    NotifyPropertyChanged("IsVolunteer");
                }
            }
        }

        /// <summary>
        /// Indicates the result of the doorbell contact from 0 to x.
        /// 0 = No contact
        /// 1 = No answer                               (Not Home)
        /// 2 = non-voter answered, no voter present    (Left Message)
        /// 3 = left literature                         (Left Message)
        /// 4 = Confirmed voter at address              (Answered)
        /// 5 = Voter has moved                         (Wrong Address)
        /// 6 = Address is vacant                       (Wrong Address)
        /// 7 = Couldn't find address                   (Wrong Address)
        /// 8 = Deceased                                (Deceased)
        /// 9 = Refused to identify                     (Initial Refused)
        /// TODO: Provide these options:
        /// Answered
        /// Deceased
        /// Left Message
        /// Not Home
        /// Initial Refused
        /// Wrong Address

        /// </summary>
        private int _result;
        [Column]
        public int ResultOfContact
        {
            get { return _result; }
            set
            {
                if (_result != value)
                {
                    NotifyPropertyChanging("ResultOfContact");
                    _result = value;
                    NotifyPropertyChanged("ResultOfContact");
                }
            }
        }

        /// <summary>
        /// Notes about contact with voter
        /// This may contain predefined tags set by UI checkboxes, plus free-form text.
        /// </summary>
        private string _comments;
        [Column]
        public string Comments
        {
            get { return _comments; }
            set
            {
                if (_comments != value)
                {
                    NotifyPropertyChanging("Comments");
                    _comments = value;
                    NotifyPropertyChanged("Comments");
                }
            }
        }

        /// <summary>
        /// Indicates whether the voter record has been updated since initial load
        /// </summary>
        private bool _isUpdated;
        [Column]
        public bool IsUpdated
        {
            get { return _isUpdated; }
            set
            {
                if (_isUpdated != value)
                {
                    NotifyPropertyChanging("IsUpdated");
                    _isUpdated = value;
                    NotifyPropertyChanged("IsUpdated");
                }
                // ModifiedTime = DateTime.Now;
            }
        }

        private DateTime _modifiedTime;
        [Column]
        public DateTime ModifiedTime
        {
            get { return _modifiedTime; }
            set
            {
                NotifyPropertyChanging("ModifiedTime");
                _modifiedTime = value;
                NotifyPropertyChanged("ModifiedTime");
            }
        }

        #region INotifyPropertyChanged Members
        public event PropertyChangedEventHandler PropertyChanged;

        // Used to notify the page that a data context property changed
        private void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
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

/*
    [Table(Name = "VoterUpdate")]
    public class VoterUpdateInfo : INotifyPropertyChanged, INotifyPropertyChanging
    {
        // Version column aids update performance.
        [Column(IsVersion = true)]
        private Binary _version;

        /// <summary>
        /// Unique Voter ID field - used as primary key. This is used as the linking key field to associate
        /// records in the VoterUpdateInfo table with records in the VoterFileEntry table.
        /// </summary>
        private int _voterID;
        [Column(IsPrimaryKey = true, IsDbGenerated = false, DbType = "INT NOT NULL Identity", CanBeNull = false, AutoSync = AutoSync.OnInsert)]
        public int VoterID
        {
            get { return _voterID; }
            set
            {
                if (_voterID != value)
                {
                    NotifyPropertyChanging("VoterID");
                    _voterID = value;
                    NotifyPropertyChanged("VoterID");
                }
            }
        }

        /// <summary>
        /// Indicates the voter's political party preference as indicated by voter during contact.
        /// This will override value currently in back-end database
        /// 0 = Unidentified/undeclared
        /// 1 = String Republican
        /// 2 = Leaning Republican
        /// 3 = Declared Independent or undecided
        /// 4 = Leaning Democrat
        /// 5 = Strong Democrat
        /// 6 = Refused to answer
        /// </summary>
        private int _party;
        [Column]
        public int Party
        {
            get { return _party; }
            set
            {
                if (_party != value)
                {
                    NotifyPropertyChanging("Party");
                    _party = value;
                    NotifyPropertyChanged("Party");
                }
            }
        }


        /// <summary>
        /// Opt-in email address for voter
        /// </summary>
        private string _email;
        [Column]
        public string Email
        {
            get { return _email; }
            set
            {
                if (_email != value)
                {
                    NotifyPropertyChanging("Email");
                    _email = value;
                    NotifyPropertyChanged("Email");
                }
            }
        }

        /// <summary>
        /// Opt-in Cell phone number for voter
        /// </summary>
        private string _cellPhone;
        [Column]
        public string CellPhone
        {
            get { return _cellPhone; }
            set
            {
                if (_cellPhone != value)
                {
                    NotifyPropertyChanging("CellPhone");
                    _cellPhone = value;
                    NotifyPropertyChanged("CellPhone");
                }
            }
        }

        /// <summary>
        /// Voter indicated support for candidate/issue
        /// </summary>
        private bool _isSupporter;
        [Column]
        public bool IsSupporter
        {
            get { return _isSupporter; }
            set
            {
                if (_isSupporter != value)
                {
                    NotifyPropertyChanging("IsSupporter");
                    _isSupporter = value;
                    NotifyPropertyChanged("IsSupporter");
                }
            }
        }

        /// <summary>
        /// Indicates whether the voter is willing to volunteer for the campaign
        /// </summary>
        private bool _isVolunteer;
        [Column]
        public bool IsVolunteer
        {
            get { return _isVolunteer; }
            set
            {
                if (_isVolunteer != value)
                {
                    NotifyPropertyChanging("IsVolunteer");
                    _isVolunteer = value;
                    NotifyPropertyChanged("IsVolunteer");
                }
            }
        }

        /// <summary>
        /// Indicates the result of the doorbell contact from 0 to x.
        /// 0 = No answer
        /// 1 = non-voter answered, no voter present
        /// 2 = left literature
        /// 3 = Confirmed voter at address
        /// 4 = Voter has moved
        /// 5 = Address is vacant
        /// 6 = Couldn't find address
        /// </summary>
        private int _result;
        [Column]
        public int ResultOfContact
        {
            get { return _result; }
            set
            {
                if (_result != value)
                {
                    NotifyPropertyChanging("ResultOfContact");
                    _result = value;
                    NotifyPropertyChanged("ResultOfContact");
                }
            }
        }

        /// <summary>
        /// Notes about contact with voter
        /// This may contain predefined tags set by UI checkboxes, plus free-form text.
        /// </summary>
        private string _comments;
        [Column]
        public string Comments
        {
            get { return _comments; }
            set
            {
                if (_comments != value)
                {
                    NotifyPropertyChanging("Comments");
                    _comments = value;
                    NotifyPropertyChanged("Comments");
                }
            }
        }

        #region INotifyPropertyChanged Members
        public event PropertyChangedEventHandler PropertyChanged;

        // Used to notify the page that a data context property changed
        private void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
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
*/


    public class VoterFileDataContext : DataContext
    {
        public static string DBConnectionString = "Data Source=isostore:/{0}";

        public VoterFileDataContext(string connString)
            : base(connString)
        {
        }

        public Table<VoterFileEntry> AllVoters;
        // public Table<VoterUpdateInfo> VoterUpdates;
    }

}
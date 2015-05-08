using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Device.Location;
using Microsoft.Phone.Controls.Maps;
using System.Collections.ObjectModel;

namespace mapapp
{
    public class PushpinModel
    {
        public PushpinModel(mapapp.data.VoterFileEntry _voter)
        {
            VoterFile = _voter;
        }

        public PushpinModel()
        {
        }

        private GeoCoordinate _location;
        public GeoCoordinate Location
        {
            get
            {
                if (_location != null)
                    return _location;
                else
                {
                    _location = new GeoCoordinate();
                    if (VoterFile != null && VoterFile.Latitude != 0.0)
                    {
                        _location.Latitude = VoterFile.Latitude;
                        _location.Longitude = VoterFile.Longitude;

                    }
                    return _location;
                }
            }
            set { _location = value; }
        }

        public bool Valid { get; set; }

        public Visibility Visibility { get; set; }
        public string Content { get; set; }
        // public string FullContent { get; set; }
        public mapapp.data.VoterFileEntry VoterFile { get; set; }

        public long VoterID
        {
            get { return VoterFile.VoterID; }
        }
        public string FirstName { get { return VoterFile.FirstName; } }
        public string LastName { get { return VoterFile.LastName; } }
        public string Address { get { return VoterFile.Address; } }
        public string Address2 { get { return VoterFile.Address2; } }
        public string City { get { return VoterFile.City; } }
        public string State { get { return VoterFile.State; } }
        public string Zip { get { return VoterFile.Zip; } }
        public string Phone 
        { 
            get { return VoterFile.Phone; }
            set
            {
                VoterFile.Phone = value;
            }
        }
        // public string RecordID { get; set; }
        public int party { get { return VoterFile.Party; } set { VoterFile.Party = value; } }
        public string precinct { get { return VoterFile.Precinct; } }
        public int PrimaryVoteHistory { get { return VoterFile.PerfectVoterPrimary; } }
        public int GeneralVoteHistory { get { return VoterFile.PerfectVoterGeneral; } }
        public bool IsSupporter { get { return VoterFile.IsSupporter; } set { VoterFile.IsSupporter = value; } }
        public bool IsVolunteer { get { return VoterFile.IsVolunteer; } set { VoterFile.IsVolunteer = value; } }
        public string Email { get { return VoterFile.Email; } set { VoterFile.Email = value; } }
        public string CellPhone { get { return VoterFile.CellPhone; } set { VoterFile.CellPhone = value; } }
        public string Comments { get { return VoterFile.Comments; } set { VoterFile.Comments = value; } }
        public int ResultOfContact { get { return VoterFile.ResultOfContact; } set { VoterFile.ResultOfContact = value; } }

        private string _street = null;
        public string Street 
        { 
            get
            {
                if ((this.VoterFile != null) && (this.VoterFile.Address != null) && (_street == null || _street.Length <= 0))
                {
                    _street = this.VoterFile.Address.Substring(this.VoterFile.Address.IndexOf(' ') + 1);
                }
                return _street;
            }
        }

        private int _housenum = 0;
        public int HouseNum 
        {
            get
            {
                if ((this.VoterFile != null) && (this.VoterFile.Address != null) && (_housenum == 0))
                {
                    int nHouseNum = 0;
                    string strHouseNum = this.VoterFile.Address.Substring(0, this.VoterFile.Address.IndexOf(' '));
                    if (Int32.TryParse(strHouseNum, out nHouseNum))
                        _housenum = nHouseNum;
                }
                return _housenum;
            }
        }

        public bool IsEven 
        {
            get
            {
                bool bEven = false;
                if (this.HouseNum != 0)
                    bEven = (_housenum % 2) == 0;
                return bEven;
            }
        }

        public string FullName 
        {
            get
            {
                return (VoterFile != null) ? VoterFile.LastName + ", " + VoterFile.FirstName : Content;
            }
        }

        public Brush Background 
        {
            get
            {
                Brush _bg = new SolidColorBrush(Colors.White);
                if (VoterFile != null)
                {
                    switch (VoterFile.Party)
                    {
                        case 1:
                            _bg = new SolidColorBrush(Colors.Red);
                            break;
                        case 2:
                            _bg = new SolidColorBrush(Color.FromArgb(0xff, 0xff, 0x88, 0x88));
                            break;
                        case 3:
                            _bg = new SolidColorBrush(Colors.Purple);
                            break;
                        case 4:
                            _bg = new SolidColorBrush(Color.FromArgb(0xff, 0x88, 0x88, 0xff));
                            break;
                        case 5:
                            _bg = new SolidColorBrush(Colors.Blue);
                            break;
                        case 6:
                            _bg = new SolidColorBrush(Colors.Black);
                            break;
                        default:
                            break;
                    }
                }
                return _bg;
            }
        }

        public Brush Foreground
        {
            get
            {
                Brush _fg = new SolidColorBrush(Colors.Black);
                if (VoterFile != null)
                {
                    switch (VoterFile.Party)
                    {
                        case 1:
                        case 2:
                        case 3:
                        case 4:
                        case 5:
                        case 6:
                            _fg = new SolidColorBrush(Colors.White);
                            break;
                        default:
                            break;
                    }
                }
                return _fg;
            }
        }

        public double dist = 0.0;

        public string LatLong 
        { 
            set
            {
                string [] latlong = value.Split(',');
                double lat = 0.0;
                double lon = 0.0;

                if (latlong.Length != 2)
                {
                    Valid = false;
                    return;
                }

                double.TryParse(latlong[0], out lat);
                double.TryParse(latlong[1], out lon);
                if (lat != 0.0 && this.VoterFile != null)
                {
                    float _lat = (float)lat;
                    this.VoterFile.Latitude = _lat;
                }
                if (lon != 0.0 && this.VoterFile != null)
                {
                    float _lon = (float)lon;
                    this.VoterFile.Longitude = _lon;
                }

                Location = new GeoCoordinate(lat, lon);
                Valid = true;
            }
        }

        public PushpinModel Clone(GeoCoordinate location)
        {
            return new PushpinModel(VoterFile)
            {
                Location = location,
                //TypeName = TypeName,
                //Icon = Icon
            };
        }



    }

}

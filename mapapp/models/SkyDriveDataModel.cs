using System;
using System.Net;
using System.ComponentModel;
using System.Collections.ObjectModel;


namespace mapapp.data
{
    public class SkyDriveDataModel : INotifyPropertyChanged, INotifyPropertyChanging
    {
        private string _id;

        /// <summary>
        /// String representation of the SkyDrive item identifier
        /// This value is passed to SkyDrive APIs as the name of the file or folder to work with.
        /// </summary>
        public string ID
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

        private string _name;

        /// <summary>
        /// This value is the name of the object for user presentation
        /// </summary>
        public string Name
        {
            get { return _name; }
            set
            {
                if (_name != value)
                {
                    NotifyPropertyChanging("Name");
                    _name = value;
                    NotifyPropertyChanged("Name");
                }
            }
        }

        private string _type;

        /// <summary>
        /// Provides the type of entry this item is.
        /// Possible values are:
        ///     file
        ///     folder
        /// </summary>
        public string Type
        {
            get { return _type; }
            set
            {
                if (_type != value)
                {
                    NotifyPropertyChanging("Type");
                    _type = value;
                    NotifyPropertyChanged("Type");
                }
            }
        }

        private int _size;

        /// <summary>
        /// Indicates the size of the file
        /// </summary>
        public int Size
        {
            get { return _size; }
            set
            {
                if (_size != value)
                {
                    NotifyPropertyChanging("From");
                    _size = value;
                    NotifyPropertyChanged("From");
                }
            }
        }

        private string _parent;

        /// <summary>
        /// SkyDrive object ID of the parent folder of this entry
        /// </summary>
        public string Parent
        {
            get { return _parent; }
            set
            {
                if (_parent != value)
                {
                    NotifyPropertyChanging("Parent");
                    _parent = value;
                    NotifyPropertyChanged("Parent");
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
}

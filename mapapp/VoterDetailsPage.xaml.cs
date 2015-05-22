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
using System.Globalization;
using System.Windows.Data;
using System.ComponentModel;
using Microsoft.Phone.Shell;
using mapapp.data;


namespace mapapp
{
    public partial class VoterDetailsPage : PhoneApplicationPage
    {
        ApplicationBar _detailsAppBar = new ApplicationBar();

        VoterFileDataContext _voterDB = null;

        public VoterDetailsPage()
        {
            InitializeComponent();
            DataContext = App.thisApp.SelectedHouse;
            _voterDB = new VoterFileDataContext(string.Format(VoterFileDataContext.DBConnectionString, App.thisApp._settings.DbFileName));
            if (_voterDB.DatabaseExists())
            {
                try
                {
                    IQueryable<VoterFileEntry> voterQuery = from voter in _voterDB.AllVoters where voter.VoterID == App.thisApp.SelectedHouse.VoterID select voter;
                    VoterFileEntry voterToUpdate = voterQuery.FirstOrDefault();
                    if (voterToUpdate.VoterID == App.thisApp.SelectedHouse.VoterID)
                    {
                        DataContext = voterToUpdate;
                        App.Log(String.Format("Setting Voter Detail page DataContext to voter {0}", voterToUpdate.FullName));
                    }
                    else
                    {
                        App.Log(String.Format("ERROR: Setting Voter Detail page DataContext to voter {0} failed.", voterToUpdate.FullName));
                    }
                }
                catch (Exception ex)
                {
                    App.Log("Exception setting voter as DataContext" + ex.ToString());
                }
            }
            _detailsAppBar.Mode = ApplicationBarMode.Default;
            _detailsAppBar.Opacity = 1.0;
            _detailsAppBar.IsVisible = true;
            _detailsAppBar.IsMenuEnabled = false;
            ApplicationBarIconButton buttonSave = new ApplicationBarIconButton();
            buttonSave.IconUri = new Uri("/Images/appbar.save.rest.png", UriKind.Relative);
            buttonSave.Text = "save";
            buttonSave.Click += saveButton_Click;
            _detailsAppBar.Buttons.Add(buttonSave);
            ApplicationBarIconButton buttonCancel = new ApplicationBarIconButton();
            buttonCancel.IconUri = new Uri("/Images/appbar.cancel.rest.png", UriKind.Relative);
            buttonCancel.Text = "cancel";
            buttonCancel.Click += cancelButton_Click;
            _detailsAppBar.Buttons.Add(buttonCancel);
            ApplicationBar = _detailsAppBar;
        }

        private void PhoneApplicationPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (ApplicationBar != _detailsAppBar)
                ApplicationBar = _detailsAppBar;
        }

        private void ListPicker_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            try
            {
            }
            catch (Exception ex)
            {
                App.Log(" Problem in ListPicker_Tap: " + ex);
            }
        }

        private void partyList_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (pnlParty != null)
            {
                pnlParty.Height = e.NewSize.Height+20;
            }
            if (ApplicationBar != _detailsAppBar)
                ApplicationBar = _detailsAppBar;
        }

        private void resultList_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (pnlContact != null)
            {
                pnlContact.Height = e.NewSize.Height + 20;
                if (resultList.IsExpanded)
                {
                    pnlEmail.Visibility = System.Windows.Visibility.Collapsed;
                    pnlParty.Visibility = System.Windows.Visibility.Collapsed;
                }
                else 
                {
                    pnlEmail.Visibility = System.Windows.Visibility.Visible;
                    pnlParty.Visibility = System.Windows.Visibility.Visible;
                }
            }
        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            // If either the email or cell textbox controls had focus when save button was tapped those changes were 
            // not updated ot the view model (because focus was not lost), so we will do that now.
            if (txtEmail.Text != ((VoterFileEntry)DataContext).Email)
            {
                BindingExpression expression = txtEmail.GetBindingExpression(TextBox.TextProperty);
                expression.UpdateSource();
            }
            if (txtCell.Text != ((VoterFileEntry)DataContext).CellPhone)
            {
                BindingExpression expression = txtCell.GetBindingExpression(TextBox.TextProperty);
                expression.UpdateSource();
            }
            if (_voterDB.DatabaseExists())
            {
                ((VoterFileEntry)DataContext).ModifiedTime = System.DateTime.Now;
                ((VoterFileEntry)DataContext).IsUpdated = true;
                _voterDB.SubmitChanges();
                App.Log("Submitted changes to database");
            }
            else
                App.Log("ERROR: we don't have a database!");

            _voterDB.Dispose();

            if (this.NavigationService.CanGoBack)
                this.NavigationService.GoBack();
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            _voterDB.Dispose();
            if (this.NavigationService.CanGoBack)
                this.NavigationService.GoBack();
        }
    }

    public class ResultListConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int limitedValue = 0; // set initially to 
            if (value is int?)
            {
                limitedValue = (int)value;
                // restrict value range to 0 to 6, with anything outside that range getting set to "unknown"
                if (limitedValue > 9 || limitedValue < 0)
                    limitedValue = 0;
            }
            return limitedValue;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }


    public class PartyListConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int limitedValue = 0; // set initially to 
            if (value is int?)
            {
                limitedValue = (int)value;
                // restrict value range to 0 to 6, with anything outside that range getting set to "unknown"
                if (limitedValue > 6 || limitedValue < 0)
                    limitedValue = 0;
            }
            return limitedValue;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }

    public class PartyBackgroundBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Brush _bg = new SolidColorBrush(Colors.White);
            if ((value is int?) && (targetType == typeof(Brush)))
            {
                int partyNum = (int)value;
                switch (partyNum)
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
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return 0;
        }
    }

    public class PartyForegroundBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Brush _fg = new SolidColorBrush(Colors.Black);
            if ((value is int?) && (targetType == typeof(Brush)))
            {
                int partyNum = (int)value;
                switch (partyNum)
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
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return 0;
        }
    }

}
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
using Microsoft.Live;
using Microsoft.Live.Controls;
using System.IO;
using System.IO.IsolatedStorage;
using mapapp.data;
using System.Threading;
using System.Text;

namespace mapapp
{
    public partial class DataManagementPage : PhoneApplicationPage
    {
        private Mutex _voterFileMutex = new Mutex(false, "VoterFileMutex");
        private bool _dataLoaded = false;
        private bool _dataRequested = false;

        public DataManagementPage()
        {
            InitializeComponent();
            IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication();
            if (isf != null)
            {
                string voterFile = "";
                bool voterFileExists = false;
                if (isf.FileExists("voters.xml"))
                {
                    voterFile = "voters.xml";
                    voterFileExists = true;
                }
                else if (isf.FileExists("voters.csv"))
                {
                    voterFile = "voters.csv";
                    voterFileExists = true;
                }
                else if (isf.FileExists("voters.zip"))
                {
                    voterFile = "voters.zip";
                    voterFileExists = true;
                }
                if (voterFileExists)
                {
                    MessageBoxResult result = MessageBox.Show("Voter file exists. Do you want to load it?", "Use existing data", MessageBoxButton.OKCancel);
                    if (result == MessageBoxResult.OK)
                    {
                        App.thisApp.LoadDatabaseFromFile(voterFile);
                    }
                }
            }
        }

        private void txt_CampaignID_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (txt_CampaignID.Text.Length > 0)
            {
                // txtStatus.Text = "Signed in.";
                txtMessage.Text = "Click \'get voter file\' to download.";
                btnDownload.IsEnabled = true;
                // TODO: track upload status in app settings
                btnUpload.IsEnabled = true;
                txtMessage.FontWeight = FontWeights.Normal;
            }
            else
            {
                // txtStatus.Text = "Not signed in.";
                txtMessage.Text = "Enter your Campaign ID to download voter file.";
                btnDownload.IsEnabled = false;
                btnUpload.IsEnabled = false;
            }
        }

        private void btnDownloadFile_Begin(object sender, RoutedEventArgs e)
        {
            if (txt_CampaignID.Text.Length == 0)
            {
                txtMessage.Text = "Enter your Campaign ID to download voter file.";
                txtMessage.FontWeight = FontWeights.Bold;
            }
            else
            {
                if (!_dataRequested)
                {
                    try
                    {
                        Uri voterListUri = new Uri("https://supershare1.azurewebsites.net/sheets/" + txt_CampaignID.Text);
                        WebClient webClient = new WebClient();
                        webClient.Headers["If-modified-since"] = App.thisApp._settings.GetSetting<DateTime>("listdate").ToString("r");
                        webClient.DownloadStringCompleted += new DownloadStringCompletedEventHandler(webClient_DownloadStringCompleted);
                        webClient.DownloadProgressChanged += webClient_DownloadProgressChanged;
                        webClient.DownloadStringAsync(voterListUri);
                        _dataRequested = true;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(ex.ToString());
                    }
                }
                // LiveConnectClient client = new LiveConnectClient(session);
                // client.GetCompleted += new EventHandler<LiveOperationCompletedEventArgs>(client_GetCompleted);
                // now track down the right name for the voters.xml file.
                // client.GetAsync("me/skydrive/files");
                progBar.IsEnabled = true;
                progBar.IsIndeterminate = true;
                progBar.Visibility = System.Windows.Visibility.Visible;
            }
        }

        private void webClient_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            progBar.IsIndeterminate = false;
            progBar.Value = e.ProgressPercentage;
            System.Diagnostics.Debug.WriteLine("Download in progress ... " + e.ProgressPercentage.ToString() + " % complete, " + e.BytesReceived.ToString() + " bytes received.");
        }

        /// <summary>
        /// If this set of results are later than the set stored in isolated storage
        /// replace the file in isolated storage and parse this set of results
        /// </summary>
        /// <param name="sender">The WebClient object on which this request was made and the response received</param>
        /// <param name="e">The DownloadStringCompletedEventArgs object with the results</param>
        void webClient_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                System.Diagnostics.Debug.WriteLine("Download Error: " + e.Error.Message);
            }
            else if (e.Result.Length > 90) // There is enough data that we have more than one line
            {
                progBar.IsIndeterminate = false;
                progBar.Value = progBar.Maximum - progBar.LargeChange;
                // Write results string to isolated storage
                IsolatedStorageFile isfStore = IsolatedStorageFile.GetUserStoreForApplication();
                IsolatedStorageFileStream streamVoterFile = null;
                bool _haveMutex = false;
                try
                {
                    byte[] byteArray = Encoding.UTF8.GetBytes(e.Result);
                    _voterFileMutex.WaitOne();
                    _haveMutex = true;
                    string fileName = "voters.csv";

                    if (byteArray[0] == 'P')
                    {
                        // this may be a zip file, check next byte for 'K'
                        if (byteArray[1] == 'K')
                        {
                            fileName = "voters.zip";
                        }
                    }
                    else if (byteArray[0] == '<')
                    {
                        // this may be a raw XML file, see if the first element is <VoterFile>
                        string firstElement = System.Text.Encoding.UTF8.GetString(byteArray, 0, 11);
                        if (firstElement.StartsWith("<VoterFile>"))
                            fileName = "voters.xml";
                    }
                    // TODO: This will need to be adjusted to allow for loading of data with arbitrary field order
                    else if (byteArray[0] == 'a')
                    {
                        // this may be a raw csv file, see if the first label is "timestamp"
                        string firstField = System.Text.Encoding.UTF8.GetString(byteArray, 0, 11);
                        if (firstField.StartsWith("address"))
                            fileName = "voters.csv";
                    }
                    else
                        fileName = "unknown.txt";

                    if (isfStore.FileExists(fileName))
                    {
                        // streamVoterFile = isfStore.OpenFile(fileName, FileMode.Truncate, FileAccess.ReadWrite, FileShare.ReadWrite);
                        isfStore.DeleteFile(fileName);
                    }
                        /*
                    else
                    {
                        streamVoterFile = isfStore.OpenFile(fileName, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
                    }
                         * */
                    streamVoterFile = isfStore.OpenFile(fileName, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
                    streamVoterFile.Write(byteArray, 0, byteArray.Length);
                    streamVoterFile.Close();

                    // Load voter data from newly downloaded voters.xml
                    App.thisApp.LoadDatabaseFromFile(fileName);
                    progBar.Value = progBar.Maximum;

                    WebClient client = sender as WebClient;
                    // TODO: Do some work to validate that this is a valid voter data file

                    // TODO: Update UploadUrl setting
                    App.thisApp._settings.UploadUrl= client.BaseAddress;
                    // TODO: The back-end service needs to provide a meaningful "Last-modified" response.
                    App.thisApp._settings.LastUpdated = client.ResponseHeaders["Date"];
                    // ResponseHeaders["If-modified-since"];
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Can't get stream for voters.csv - exception " + ex.ToString());
                }
                finally
                {
                    if (_haveMutex)
                    {
                        try
                        {
                            _voterFileMutex.ReleaseMutex();
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine("Hmmm, I guess we don't have the mutex" + ex.ToString());
                        }
                        _haveMutex = false;
                    }
                }

                progBar.Value = progBar.Maximum;
                App.thisApp._settings.LastChecked = DateTime.Now;
                _dataLoaded = true;
                _dataRequested = false;
                App.thisApp._settings.UpdateSetting("downloaded", true);
                // NotifyPropertyChanged("IsDataLoaded");
                MessageBoxResult result = MessageBox.Show("Voter data download complete.", "Done", MessageBoxButton.OK);
            }
            else // Response was probably a 304 Not Modified response
            {
                App.thisApp._settings.LastChecked = DateTime.Now;
                _dataLoaded = true;
                _dataRequested = false;
                MessageBoxResult result = MessageBox.Show("No updates to your voter data are available", "No update", MessageBoxButton.OK);
            }
            this.NavigationService.GoBack();
        }

        private void btnUpload_Click(object sender, RoutedEventArgs e)
        {
            if (txt_CampaignID.Text.Length == 0 || App.thisApp._settings.GetSetting<bool>("downloaded") != true)
            {
                txtMessage.Text = "Enter your Campaign ID to download voter file.";
                txtMessage.FontWeight = FontWeights.Bold;
            }
            else
            {
                // BOOKMARK
                // LiveConnectClient client = new LiveConnectClient(session);
                string updateFileName = App.thisApp.ReportUpdates();
                // System.IO.Stream xmlStream = System.IO.File.OpenRead(updateFileName);
                IsolatedStorageFile _iso = IsolatedStorageFile.GetUserStoreForApplication();

                IsolatedStorageFileStream xmlStream = _iso.OpenFile(updateFileName, System.IO.FileMode.Open);
                // client.UploadCompleted += new EventHandler<LiveOperationCompletedEventArgs>(client_UploadCompleted);
                // client.UploadAsync(App.thisApp._settings.UploadUrl, updateFileName, xmlStream, OverwriteOption.Overwrite);
                progBar.IsEnabled = true;
                progBar.IsIndeterminate = true;
                progBar.Visibility = System.Windows.Visibility.Visible;
            }
        }

        void client_UploadCompleted(object sender, LiveOperationCompletedEventArgs e)
        {
            progBar.Minimum = 0;
            progBar.Maximum = 100;
            progBar.Value = 100;
            progBar.IsIndeterminate = false;
            progBar.IsEnabled = false;
            progBar.Visibility = System.Windows.Visibility.Collapsed;
            // TODO: Track upload status in app settings
            Object fileName = "";
            e.Result.TryGetValue("name", out fileName);
            App.Log("Voter updates uploaded to SkyDrive: " + fileName);
            btnUpload.IsEnabled = false;
        }
    }
}
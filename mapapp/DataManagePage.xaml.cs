﻿using System;
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
using System.IO;
using System.IO.IsolatedStorage;
using mapapp.data;
using System.Threading;
using System.Text;
using Newtonsoft.Json;
namespace mapapp
{
    public class AuthResult
    {
        public string AuthToken { get; set; }
        public string Server { get; set; }
        public AuthResult() { }
        public AuthResult(string authKey, string server)
        {
            AuthToken = authKey;
            Server = server;
        }
    }

    public partial class DataManagementPage : PhoneApplicationPage
    {
        private Mutex _voterFileMutex = new Mutex(false, "VoterFileMutex");
        private bool _dataLoaded = false;
        private bool _dataRequested = false;
        // TODO: Make this into a setting that can be changed by a user
        private string _baseDataUrl = "http://supershare1.azurewebsites.net/sheets/0";
        private string _baseLoginUrl = "http://supershare1.azurewebsites.net/login/code?code=";

        public DataManagementPage()
        {
            InitializeComponent();
            IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication();
            if (isf != null)
            {
                string voterFile = "";
                bool voterFileExists = false;
                if (App.thisApp._settings.DbStatus == DbState.Loaded)
                {
                    // TODO: Add setting (or check of database) indicating whether changes have been made to local database that have not been uploaded
                    if (App.thisApp.DbHasUpdates())
                        btnUpload.IsEnabled = true;
                }
                else
                {
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
        }

        private void txt_CampaignID_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (txt_CampaignID.Text.Length > 0)
            {
                // txtStatus.Text = "Signed in.";
                txtMessage.Text = "Click \'get voter file\' to download.";
                btnDownload.IsEnabled = true;
                // TODO: track upload status in app settings
                // btnUpload.IsEnabled = true;
                txtMessage.FontWeight = FontWeights.Normal;
            }
            else
            {
                // txtStatus.Text = "Not signed in.";
                txtMessage.Text = "Enter your Campaign ID to download voter file.";
                btnDownload.IsEnabled = false;
                // btnUpload.IsEnabled = false;
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
                // We don't have an auth key, so we need to see if we can get one
                if (App.thisApp._settings.GetSetting<string>("authkey") == "")
                {
                    try
                    {
                        Uri authUri = new Uri(_baseLoginUrl + txt_CampaignID.Text);
                        WebClient webClient = new WebClient();
                        webClient.UploadStringCompleted += new UploadStringCompletedEventHandler(webClient_UploadAuthCompleted);
                        webClient.UploadProgressChanged += new UploadProgressChangedEventHandler(webClient_UploadProgressChanged);
                        webClient.UploadStringAsync(authUri, "POST", "");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(ex.ToString());
                    }
                }
                else if (!_dataRequested)
                {
                    try
                    {
                        Uri voterListUri = new Uri(_baseDataUrl);
                        WebClient webClient = new WebClient();
                        webClient.Headers[HttpRequestHeader.Authorization] = "bearer " + App.thisApp._settings.GetSetting<string>("authkey");
                        // webClient.Headers["If-modified-since"] = App.thisApp._settings.GetSetting<DateTime>("listdate").ToString("r");
                        webClient.DownloadStringCompleted += new DownloadStringCompletedEventHandler(webClient_DownloadVoterListCompleted);
                        webClient.DownloadProgressChanged += webClient_DownloadProgressChanged;
                        webClient.DownloadStringAsync(voterListUri);
                        _dataRequested = true;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(ex.ToString());
                    }
                }
                if (_dataRequested)
                {
                    progBar.IsEnabled = true;
                    progBar.IsIndeterminate = true;
                    progBar.Visibility = System.Windows.Visibility.Visible;
                }
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
        void webClient_UploadAuthCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            string authKey = "";
            try
            {
                if (sender is WebClient)
                {
                    WebClient wc = sender as WebClient;
                    if (true)
                    {
                        AuthResult authResult = JsonConvert.DeserializeObject<AuthResult>(e.Result);
                        if (authResult != null && authResult.AuthToken.Length > 10) // The length check is arbitrary
                        {
                            authKey = authResult.AuthToken;
                            App.thisApp._settings.UpdateSetting("authkey", authResult.AuthToken);
                            App.thisApp._settings.UpdateSetting("userid", txt_CampaignID.Text);
                        }
                    }
                }
                App.Log(string.Format("Login completed. Response is: {0}", e.Result));

                if (!_dataRequested && authKey != "")
                {
                    try
                    {
                        Uri voterListUri = new Uri(_baseDataUrl);
                        WebClient webClient = new WebClient();
                        webClient.Headers[HttpRequestHeader.Authorization] = "bearer " + App.thisApp._settings.GetSetting<string>("authkey");
                        webClient.DownloadStringCompleted += new DownloadStringCompletedEventHandler(webClient_DownloadVoterListCompleted);
                        webClient.DownloadProgressChanged += webClient_DownloadProgressChanged;
                        webClient.DownloadStringAsync(voterListUri);
                        _dataRequested = true;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(ex.ToString());
                    }
                }
                if (_dataRequested)
                {
                    progBar.IsEnabled = true;
                    progBar.IsIndeterminate = true;
                    progBar.Visibility = System.Windows.Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                App.Log("Problem logging results of upload: " + ex.ToString());
            }
        }


        /// <summary>
        /// If this set of results are later than the set stored in isolated storage
        /// replace the file in isolated storage and parse this set of results
        /// </summary>
        /// <param name="sender">The WebClient object on which this request was made and the response received</param>
        /// <param name="e">The DownloadStringCompletedEventArgs object with the results</param>
        void webClient_DownloadVoterListCompleted(object sender, DownloadStringCompletedEventArgs e)
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
                bool _bGotVoters = false;
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
                    else if (e.Result.StartsWith("<VoterFile>"))
                    {
                        fileName = "voters.xml";
                    }
                    // TODO: This will need to be adjusted to allow for loading of data with arbitrary field order
                    else if (e.Result.StartsWith("recid"))
                    {
                        fileName = "voters.csv";
                    }
                    else if (e.Result.StartsWith("{")) // This is the beginning brace for a json message, indicating some kind of error
                    {
                        fileName = "";
                        App.Log("ERROR: Request for voters failed: " + e.Result);
                        MessageBoxResult result = MessageBox.Show("Unable to download voter list. Your account may have expired.", "Authentication failure", MessageBoxButton.OK);
                    }
                    else
                        fileName = "unknown.txt";

                    if (isfStore.FileExists(fileName))
                    {
                        isfStore.DeleteFile(fileName);
                    }
                    if (fileName.StartsWith("voters"))
                    {
                        streamVoterFile = isfStore.OpenFile(fileName, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
                        streamVoterFile.Write(byteArray, 0, byteArray.Length);
                        streamVoterFile.Close();
                        _bGotVoters = true;

                        // Load voter data from newly downloaded voters.xml
                        App.thisApp.LoadDatabaseFromFile(fileName);
                        WebClient client = sender as WebClient;
                        // TODO: Do some work to validate that this is a valid voter data file

                        App.thisApp._settings.UploadUrl = _baseDataUrl;
                        // TODO: The back-end service needs to provide a meaningful "Last-modified" response.
                        App.thisApp._settings.LastUpdated = client.ResponseHeaders["Date"];
                        // ResponseHeaders["If-modified-since"];
                    }
                    progBar.Value = progBar.Maximum;
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
                if (_bGotVoters)
                {
                    App.thisApp._settings.LastChecked = DateTime.Now;
                    _dataLoaded = true;
                    _dataRequested = false;
                    App.thisApp._settings.UpdateSetting("downloaded", true);
                    // NotifyPropertyChanged("IsDataLoaded");
                    MessageBoxResult result = MessageBox.Show("Voter data download complete.", "Done", MessageBoxButton.OK);
                    this.NavigationService.Navigate(new Uri("/MainPage.xaml", UriKind.Relative));
                }
                else
                {
                    App.Log("Unable to download voter list.");
                }
            }
            else // Response was probably a 304 Not Modified response
            {
                App.thisApp._settings.LastChecked = DateTime.Now;
                _dataLoaded = true;
                _dataRequested = false;
                MessageBoxResult result = MessageBox.Show("No updates to your voter data are available", "No update", MessageBoxButton.OK);
            }
            if (this.NavigationService.CanGoBack)
                this.NavigationService.GoBack();
        }

        private void btnUpload_Click(object sender, RoutedEventArgs e)
        {
            if (App.thisApp._settings.GetSetting<string>("authkey").Length == 0 || App.thisApp._settings.GetSetting<bool>("downloaded") != true)
            {
                txtMessage.Text = "Enter your Campaign ID to upload voter file.";
                txtMessage.FontWeight = FontWeights.Bold;
            }
            else
            {
                string updateFileName = App.thisApp.ReportUpdates();
                IsolatedStorageFile _iso = IsolatedStorageFile.GetUserStoreForApplication();

                IsolatedStorageFileStream updateStream = _iso.OpenFile(updateFileName, System.IO.FileMode.Open);

                try
                {
                    Uri voterListUri = new Uri(App.thisApp._settings.UploadUrl);

                    string stReportText = "";
                    StreamReader srUpdate = new StreamReader(updateStream);
                    stReportText = srUpdate.ReadToEnd();
                    WebClient webClient = new WebClient();
                    webClient.Headers[HttpRequestHeader.ContentType] = "application/text";
                    webClient.Headers[HttpRequestHeader.ContentLength] = stReportText.Length.ToString();
                    webClient.Headers[HttpRequestHeader.Authorization] = "bearer " + App.thisApp._settings.GetSetting<string>("authkey");
                    webClient.UploadStringCompleted += new UploadStringCompletedEventHandler(webClient_UploadStringCompleted);
                    webClient.UploadProgressChanged += new UploadProgressChangedEventHandler(webClient_UploadProgressChanged);
                    webClient.UploadStringAsync(voterListUri, "POST", stReportText);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.ToString());
                }

                
                progBar.IsEnabled = true;
                progBar.IsIndeterminate = true;
                progBar.Visibility = System.Windows.Visibility.Visible;
            }
        }

        void webClient_UploadStringCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            string size = "0";
            try
            {
                if (sender is WebClient)
                {
                    WebClient wc = sender as WebClient;
                    if (wc.ResponseHeaders != null && wc.ResponseHeaders[HttpRequestHeader.ContentLength] != null)
                        size = wc.ResponseHeaders[HttpRequestHeader.ContentLength];
                    // TODO: Figure out how to test for succeeded - parse from response JSON if needed
                    App.thisApp._settings.UpdateSetting("lastsync", DateTime.Now);
                }
                App.Log(string.Format("Uploaded {0} bytes. Response is: {1}", size, e.Result));
            }
            catch (Exception ex)
            {
                App.Log("Problem logging results of upload: " + ex.ToString());
            }
            if (this.NavigationService.CanGoBack)
                this.NavigationService.GoBack();
        }

        private void webClient_UploadProgressChanged(object sender, UploadProgressChangedEventArgs e)
        {
            progBar.IsIndeterminate = false;
            progBar.Value = e.ProgressPercentage;
            System.Diagnostics.Debug.WriteLine("Upload in progress ... " + e.ProgressPercentage.ToString() + " % complete, " + e.BytesReceived.ToString() + " bytes received.");
        }
    }
}
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
using System.IO.IsolatedStorage;
using mapapp.data;

namespace mapapp
{
    public partial class LiveAccessPage : PhoneApplicationPage
    {
        private LiveAuthClient auth;
        private LiveConnectClient client;
        private LiveConnectSession session;

        public LiveAccessPage()
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

        private void signInButton1_SessionChanged(object sender, LiveConnectSessionChangedEventArgs e)
        {
            if (e.Status == LiveConnectSessionStatus.Connected)
            {
                session = e.Session;
                client = new LiveConnectClient(session);
                txtStatus.Text = "Signed in.";
                txtMessage.Text = "Click button to download voter file.";
                btnDownload.IsEnabled = true;
                // TODO: track upload status in app settings
                btnUpload.IsEnabled = true;
                txtMessage.FontWeight = FontWeights.Normal;
            }
            else
            {
                txtStatus.Text = "Not signed in.";
                txtMessage.Text = "Sign in to SkyDrive to download voter file.";
                btnDownload.IsEnabled = false;
                btnUpload.IsEnabled = false;
                client = null;
            }
        }

        private void btnDownloadFile_Begin(object sender, RoutedEventArgs e)
        {
            if (session == null)
            {
                txtMessage.Text = "Sign in to SkyDrive to download voter file.";
                txtMessage.FontWeight = FontWeights.Bold;
            }
            else
            {
                LiveConnectClient client = new LiveConnectClient(session);
                client.GetCompleted += new EventHandler<LiveOperationCompletedEventArgs>(client_GetCompleted);
                // now track down the right name for the voters.xml file.
                client.GetAsync("me/skydrive/files");
                progBar.IsEnabled = true;
                progBar.IsIndeterminate = true;
                progBar.Visibility = System.Windows.Visibility.Visible;
            }
        }

        void client_GetCompleted(object sender, LiveOperationCompletedEventArgs e)
        {
            progBar.Minimum = 0;
            progBar.Maximum = 100;
            progBar.Value = 100;
            progBar.IsIndeterminate = false;
            progBar.IsEnabled = false;
            progBar.Visibility = System.Windows.Visibility.Collapsed;
            if (e.Error == null)
            {
                IDictionary<string, object> folder = e.Result;
                Object fileListData = folder["data"];
                if (fileListData is List<Object>)
                {
                    List<Object> filesKvpList = fileListData as List<Object>;
                    foreach (Object fileEntry in filesKvpList)
                    {
                        if (fileEntry is Dictionary<String, Object>)
                        {
                            SkyDriveDataModel item = new SkyDriveDataModel();
                            try
                            {
                                Dictionary<String, Object> entryDict = (Dictionary<String, Object>)fileEntry;
                                // This is a file or folder dictionary
                                if (entryDict.ContainsKey("name"))
                                    item.Name = (string)entryDict["name"];
                                if (entryDict.ContainsKey("id"))
                                    item.ID = (string)entryDict["id"];
                                if (entryDict.ContainsKey("parent_id"))
                                    item.Parent = (string)entryDict["parent_id"];
                                if (entryDict.ContainsKey("type"))
                                    item.Type = (string)entryDict["type"];
                                if (item.Type.Equals("file") && entryDict.ContainsKey("size"))
                                {
                                    item.Size = (int)entryDict["size"];
                                }
                                if (item.Name != null && (item.Type.Equals("folder") || item.Type.Equals("file")))
                                    fileList.Items.Add(item);
                            }
                            catch (Exception ex)
                            {
                                App.Log("Problem reading an entry from SkyDrive " + item.Name);
                            }
                        }
                    }
                }
            }
            else
            {
                txtMessage.Text = "Error calling API: " + e.Error.Message;
            }            
        }

        void OnDownloadCompleted(object sender, LiveDownloadCompletedEventArgs e)
        {
            progBar.Minimum = 0;
            progBar.Maximum = 100;
            progBar.Value = 100;
            progBar.IsIndeterminate = false;
            progBar.IsEnabled = false;
            progBar.Visibility = System.Windows.Visibility.Collapsed;
            if (e.Result != null)
            {
                string fileName = "voters.xml";
                try
                {
                    // Peek inside stream to see if it is a ZIP file or an XML file
                    if (e.Result.CanSeek)
                    {
                        byte[] fileFront = new byte[64];
                        int bytesRead = e.Result.Read(fileFront, 0, 64);
                        if (fileFront[0] == 'P')
                        {
                            // this may be a zip file, check next byte for 'K'
                            if (fileFront[1] == 'K')
                            {
                                fileName = "voters.zip";
                            }
                        }
                        else if (fileFront[0] == '<')
                        {
                            // this may be a raw XML file, see if the first element is <VoterFile>
                            fileFront[11] = 0;
                            string firstElement = System.Text.Encoding.UTF8.GetString(fileFront, 0, 11);
                            if (firstElement.StartsWith("<VoterFile>"))
                                fileName = "voters.xml";
                        }
                            // TODO: This will need to be adjusted to allow for loading of data with arbitrary field order
                        else if (fileFront[0] == 'a')
                        {
                            // this may be a raw csv file, see if the first label is "timestamp"
                            fileFront[11] = 0;
                            string firstField = System.Text.Encoding.UTF8.GetString(fileFront, 0, 11);
                            if (firstField.StartsWith("address"))
                                fileName = "voters.csv";
                        }
                        else
                            fileName = "unknown.txt";
                        e.Result.Seek(0, System.IO.SeekOrigin.Begin);
                    }
                    IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication();
                    // IsolatedStorageFileStream voterFile = isf.CreateFile("voters.xml");
                    IsolatedStorageFileStream voterFile = new IsolatedStorageFileStream(fileName, System.IO.FileMode.Create, isf);
                    // byte[] _buffer = new byte[4096];
                    e.Result.CopyTo(voterFile);
                    e.Result.Close();
                    voterFile.Close();
                    // TODO: Do some work to validate that this is a valid voter data file
                }
                catch (Exception ex)
                {
                    App.Log("Failed to write downloaded stream to isolated storage: " + ex.ToString());
                    return;
                }

                // Load voter data from newly downloaded voters.xml
                App.thisApp.LoadDatabaseFromFile(fileName);
            }
            else
            {
                txtMessage.Text = "Error downloading image: " + e.Error.ToString();
            }
        }

        private void btnUpload_Click(object sender, RoutedEventArgs e)
        {
            if (session == null)
            {
                txtMessage.Text = "Sign in to SkyDrive to upload voter file.";
                txtMessage.FontWeight = FontWeights.Bold;
            }
            else
            {
                LiveConnectClient client = new LiveConnectClient(session);
                string updateFileName = App.thisApp.ReportUpdates();
                // System.IO.Stream xmlStream = System.IO.File.OpenRead(updateFileName);
                IsolatedStorageFile _iso = IsolatedStorageFile.GetUserStoreForApplication();

                IsolatedStorageFileStream xmlStream = _iso.OpenFile(updateFileName, System.IO.FileMode.Open);
                client.UploadCompleted += new EventHandler<LiveOperationCompletedEventArgs>(client_UploadCompleted);
                client.UploadAsync(App.thisApp._settings.UploadUrl, updateFileName, xmlStream, OverwriteOption.Overwrite);
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

        private void fileList_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            App.Log("List Item Tap" + sender.ToString());
            if (sender is ListBox)
            {
                Object selObj = this.fileList.SelectedItem;
                if (selObj != null && selObj is SkyDriveDataModel)
                {
                    SkyDriveDataModel selItem = selObj as SkyDriveDataModel;
                    string itemID = selItem.ID;
                    if (selItem.Type == "folder")
                    {
                        this.fileList.Items.Clear();
                        client.GetCompleted += new EventHandler<LiveOperationCompletedEventArgs>(client_GetCompleted);
                        client.GetAsync(itemID + "/files");
                        progBar.IsEnabled = true;
                        progBar.IsIndeterminate = true;
                        progBar.Visibility = System.Windows.Visibility.Visible;
                    }
                    else if (selItem.Type == "file")
                    {
                        App.thisApp._settings.UploadUrl = selItem.Parent;
                        client.DownloadProgressChanged += new EventHandler<LiveDownloadProgressChangedEventArgs>(client_DownloadProgressChanged);
                        client.DownloadCompleted += new EventHandler<LiveDownloadCompletedEventArgs>(OnDownloadCompleted);
                        client.DownloadAsync(itemID + "/content");
                        progBar.IsEnabled = true;
                        // progBar.IsIndeterminate = false;
                        // progBar.Minimum = 0;
                        // progBar.Maximum = 100;
                        // progBar.Value = 0;
                        progBar.IsIndeterminate = true;
                        progBar.Visibility = System.Windows.Visibility.Visible;
                    }
                }
            }
        }

        void client_DownloadProgressChanged(object sender, LiveDownloadProgressChangedEventArgs e)
        {
            progBar.Value = e.ProgressPercentage;
        }
    }
}
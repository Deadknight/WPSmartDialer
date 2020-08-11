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
using System.Text;
using Microsoft.Phone.Tasks;
using Microsoft.Phone.Shell;
using System.IO.IsolatedStorage;
using Microsoft.Phone.UserData;
using System.Windows.Media.Imaging;
using System.IO;
using System.Threading;
using System.Windows.Navigation;
using Microsoft.Phone.Net.NetworkInformation;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Coding4Fun.Phone.Controls;
using System.Runtime.Serialization.Json;
using System.Runtime.Serialization;
using System.Windows.Data;
using NavigationListControl;

namespace SmartDialer
{
    public partial class MainPage : PhoneApplicationPage
    {
        System.Windows.Threading.DispatcherTimer dt;
        String theme;
        private Int32 tickDuration;
        private UInt64 lastResourceUpdate;
        private bool allowedToSendHapticFeedback;
        private bool allowedToSendSound;
        private bool useDefaultSIP;
        private bool textBoxFocused = false;

        WebBrowser wb;
        Dictionary<String, int> dlCounter = new Dictionary<String, int>();
        ResourceDataList updateList;
        int totalCounter = 0;
        String[] themeArr = { "0.png", "1.png",  "2.png",  "3.png",  "4.png",  "5.png",  "6.png",  "7.png",  "8.png",  "9.png",  "0.png",  "call.png", "del.png",  "people.png", 
                                    "callhistory.png", "number.png", "people.png", "star.png" };

        // Constructor
        public MainPage()
        {
            InitializeComponent();

            ApplySettings(true);

            // Set the data context of the listbox control to the sample data
            DataContext = App.ViewModel;
            App.ViewModel.parentView = this;
            this.Loaded += new RoutedEventHandler(MainPage_Loaded);
            dt = new System.Windows.Threading.DispatcherTimer();
            dt.Interval = new TimeSpan(0, 0, 0, 0, tickDuration); // 500 Milliseconds
            dt.Tick += new EventHandler(dt_Tick);

            Visibility darkBackgroundVisibility = (Visibility)Application.Current.Resources["PhoneDarkThemeVisibility"];
            if (darkBackgroundVisibility == Visibility.Visible)
            {
                CallNumberBackground.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 0, 0, 0));
                CallNumberBackgroundBuiltIn.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 0, 0, 0));
            }
            else
            {
                CallNumberBackground.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 255, 255, 255));
                CallNumberBackgroundBuiltIn.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 255, 255, 255));
            }
        }

        [DataContract]
        public class ResourceDataList
        {
            public ResourceDataList() { }

            [DataMember]
            public String M;

            [DataMember]
            public UInt64 T;

            [DataMember]
            public String B;

            [DataMember]
            public String[] L;
        }

        void wb_Navigated(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            updateList = new ResourceDataList();

            try
            {
                String json = (String)wb.InvokeScript("getupdate");
                if (json == "")
                    return;
                MemoryStream ms = new MemoryStream(Encoding.Unicode.GetBytes(json));
                System.Runtime.Serialization.Json.DataContractJsonSerializer serializer = new System.Runtime.Serialization.Json.DataContractJsonSerializer(updateList.GetType());
                updateList = serializer.ReadObject(ms) as ResourceDataList;
                ms.Close();
            }
            catch (Exception ex)
            {
                return;
            }

            if (updateList.M != null && updateList.M != "")
            {
                var toast = new ToastPrompt
                {
                    Title = "Announcement",
                    TextOrientation = System.Windows.Controls.Orientation.Vertical,
                    Message = updateList.M,
                    ImageSource = new BitmapImage(new Uri("ApplicationIcon.png", UriKind.RelativeOrAbsolute))
                };
                toast.Show();

                lastResourceUpdate = updateList.T;
                IsolatedStorageSettings userSettings = IsolatedStorageSettings.ApplicationSettings;
                userSettings["LastResourceUpdate"] = lastResourceUpdate;
                userSettings.Save();

                return;
            }

            MessageBoxResult mbr = MessageBox.Show("There are theme update would you like to download?", "Theme Updates", MessageBoxButton.OKCancel);

            if (mbr == MessageBoxResult.OK)
            {
                lastResourceUpdate = updateList.T;
                pBarNetLoading.Visibility = Visibility.Visible;
                pBarNetLoading.Maximum = updateList.L.Length * themeArr.Length;

                foreach (String s in updateList.L)
                {
                    foreach (String toDl in themeArr)
                    {
                        WebClient wbImage = new WebClient();
                        wbImage.BaseAddress = updateList.B + s + "\\" + toDl;
                        wbImage.OpenReadCompleted += new OpenReadCompletedEventHandler(wb_OpenReadCompleted);
                        wbImage.OpenReadAsync(new Uri(updateList.B + s + "\\" + toDl));
                    }
                }
            }
            else
            {
                MessageBoxResult mbrN = MessageBox.Show("Would you like to download it on next check?", "Really?", MessageBoxButton.OKCancel);
                if (mbrN != MessageBoxResult.OK)
                {
                    lastResourceUpdate = updateList.T;
                    IsolatedStorageSettings userSettings = IsolatedStorageSettings.ApplicationSettings;
                    userSettings["LastResourceUpdate"] = lastResourceUpdate;
                    userSettings.Save();
                }
            }
        }

        void wb_OpenReadCompleted(object sender, OpenReadCompletedEventArgs e)
        {
            WebClient wc = (WebClient)sender;

            String[] arr = wc.BaseAddress.Split('/');
            //String filename = wc.BaseAddress.Substring(wc.BaseAddress.LastIndexOf("/") + 1);
            String place = arr[arr.Length - 2];
            String filename = arr[arr.Length - 1];
            String filePath = place + "\\" + filename;
            using (IsolatedStorageFile myIsolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (!myIsolatedStorage.DirectoryExists(place))
                    myIsolatedStorage.CreateDirectory(place);

                if (myIsolatedStorage.FileExists(filePath))
                    myIsolatedStorage.DeleteFile(filePath);

                IsolatedStorageFileStream fileStream = myIsolatedStorage.CreateFile(filePath);
                byte[] data = new byte[204800];
                int offset = 0;
                int writeOffset = 0;
                while ((offset = e.Result.Read(data, offset, 204800)) != 0)
                {
                    fileStream.Write(data, writeOffset, 204800);
                    writeOffset = offset;
                }
                fileStream.Flush();
                fileStream.Close();
            }

            int counter = 0;
            dlCounter.TryGetValue(place, out counter);
            counter++;
            totalCounter++;
            pBarNetLoading.Value = totalCounter;
            dlCounter[place] = counter;
            if(counter == themeArr.Length)
            {
                IsolatedStorageSettings userSettings = IsolatedStorageSettings.ApplicationSettings;
                String downloadedThemes = "";
                if (userSettings.Contains("DownloadedThemes"))
                    downloadedThemes = Convert.ToString(userSettings["DownloadedThemes"]);
                else
                {
                    userSettings["DownloadedThemes"] = "";
                    userSettings.Save();
                }
                if (downloadedThemes == "")
                    downloadedThemes = place;
                else
                    downloadedThemes += "," + place;
                userSettings["DownloadedThemes"] = downloadedThemes;
                userSettings.Save();
            }
            if (totalCounter == dlCounter.Count * themeArr.Length)
            {
                IsolatedStorageSettings userSettings = IsolatedStorageSettings.ApplicationSettings;
                userSettings["LastResourceUpdate"] = lastResourceUpdate;
                userSettings.Save();
                pBarNetLoading.Visibility = Visibility.Collapsed;
            }
        }

        void toast_Completed(object sender, PopUpEventArgs<string, PopUpResult> e)
        {

        }

        // Handle selection changed on ListBox
        private void MainListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // If selected index is -1 (no selection) do nothing
#if DEFINE_LISTBOX
            if (MainListBox.SelectedIndex == -1)
                return;

            if (useDefaultSIP)
            {
                CallNumberButton.Focus();
                BuiltinNumbers.Visibility = Visibility.Collapsed;
            }

            PhoneCallTask phoneCallTask = new PhoneCallTask();

            ItemViewModel ivm = App.ViewModel.Items[MainListBox.SelectedIndex];
            phoneCallTask.PhoneNumber = ivm.LineTwo;
            phoneCallTask.DisplayName = ivm.LineOne;

            phoneCallTask.Show();

            // Reset selected index to -1 (no selection)
            MainListBox.SelectedIndex = -1;
#endif
        }

        ItemViewModel lastSelection = null;
        private void NavigationList_Navigation(object sender, NavigationListControl.NavigationEventArgs e)
        {
            /*if (MainListBox.SelectedIndex == -1)
                return;*/

            if (useDefaultSIP)
            {
                CallNumberButton.Focus();
                BuiltinNumbers.Visibility = Visibility.Collapsed;
            }

            lastSelection = (ItemViewModel)e.Item;

            // Reset selected index to -1 (no selection)
            //MainListBox.SelectedIndex = -1;
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            CallNumberTextBox.Text = "";
            string save = "";
            if (NavigationContext.QueryString.TryGetValue("save", out save))
            {
                if(save == "1")
                {
                    String saveTheme = "0";
                    if(NavigationContext.QueryString.TryGetValue("saveTheme", out saveTheme))
                    {
                        if(saveTheme == "0")
                            ApplySettings(false);
                        else
                            ApplySettings(true);
                    }
                    ApplySettings(false);
                }
            }

            NavigationService.RemoveBackEntry();
            NavigationService.RemoveBackEntry();
            if (dt.IsEnabled)
            {
                dt.Stop();
                dt.Start();
            }
            else
                dt.Start();
        }

        StringBuilder sb = new StringBuilder();

        // Load data for the ViewModel Items
        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            CallNumberTextBox.IsTabStop = true;
            if (useDefaultSIP)
            {
                CallNumberTextBox.Focus();
                BuiltinNumbers.Visibility = Visibility.Visible;
            }
            else
                Numbers.Visibility = Visibility.Visible;

            PhoneApplicationService.Current.ApplicationIdleDetectionMode = IdleDetectionMode.Disabled;
            ContactDataSaver<ContactDataStorage> cds = new ContactDataSaver<ContactDataStorage>();
            List<ContactDataStorage> lstTest = cds.LoadMyData("contacts");
            List<ContactDataStorage> lstLastCall = cds.LoadMyData("contactsLastCall");
            App.ViewModel.Items.Clear();
            App.ViewModel.Items.Clear();
            if (lstLastCall.Count > 0)
            {
                App.ViewModel.lstLastCalls.Clear();
                foreach (ContactDataStorage cdsD in lstLastCall)
                {
                    App.ViewModel.lstLastCalls.Add(cdsD.DisplayName, cdsD);
                    if (App.ViewModel.callHistory)
                    {
                        foreach (String phone in cdsD.PhoneNumbers)
                        {
                            ItemViewModel ivm = new ItemViewModel() { LineOne = cdsD.DisplayName, LineTwo = phone, Photo = null, Color1 = App.ViewModel.color1, Color2 = App.ViewModel.color2, Theme = App.ViewModel.Theme, CallHistory = Visibility.Visible };
                            if (cdsD.Picture != null)
                            {
                                try
                                {
                                    BitmapImage bmp = new BitmapImage();
                                    MemoryStream ms = new MemoryStream(cdsD.Picture);
                                    bmp.SetSource(ms);
                                    ivm.Photo = bmp;
                                }
                                catch
                                {
                                }
                            }
                            App.ViewModel.Items.Add(ivm);
                        }
                    }
                }
            }
            IsolatedStorageSettings userSettings = IsolatedStorageSettings.ApplicationSettings;
            if (lstTest.Count > 0)
            {
                if (userSettings.Contains("LastCheck"))
                {
                    App.ViewModel.loaded = true;
                    App.ViewModel.lastCheck = new DateTime(Convert.ToInt64((String)userSettings["LastCheck"]));
                }
                //App.ViewModel.lst = lstTest;
                App.ViewModel.ContactSetSynchronized(lstTest);
                foreach (ContactDataStorage cdsD in lstTest)
                {
                    foreach (String phone in cdsD.PhoneNumbers)
                    {
                        ItemViewModel ivm = new ItemViewModel() { LineOne = cdsD.DisplayName, LineTwo = phone, Photo = null, Color1 = App.ViewModel.color1, Color2 = App.ViewModel.color2, Theme = App.ViewModel.Theme, CallHistory = Visibility.Collapsed };
                        if (cdsD.Picture != null)
                        {
                            try
                            {
                                BitmapImage bmp = new BitmapImage();
                                MemoryStream ms = new MemoryStream(cdsD.Picture);
                                bmp.SetSource(ms);
                                ivm.Photo = bmp;
                            }
                            catch
                            {
                            }
                        }
                        if (App.ViewModel.ViewLimit == 0 || App.ViewModel.Items.Count < App.ViewModel.ViewLimit)
                            App.ViewModel.Items.Add(ivm);
                        App.ViewModel.ItemsCache.Add(ivm);
                    }
                }
            }

            // Ensure that application state is restored appropriately
            if (!App.ViewModel.IsDataLoaded)
            {
                ThreadPool.QueueUserWorkItem(o =>
                {
                    App.ViewModel.LoadData("");
                });
            }
            if (userSettings.Contains("Theme"))
                theme = (String)userSettings["Theme"];
            else
            {
                userSettings["Theme"] = "Retro";
                theme = "Retro";
                userSettings.Save();
            }

            bool checkUpdates = true;

            if (userSettings.Contains("CheckUpdates"))
                checkUpdates = (bool)userSettings["CheckUpdates"];
            else
            {
                userSettings["CheckUpdates"] = true;
                userSettings.Save();
            }
            DateTime updateLastCheck = new DateTime(0);
            if (userSettings.Contains("CheckUpdatesLastCheck"))
            {
                updateLastCheck = new DateTime(Convert.ToInt64((String)userSettings["CheckUpdatesLastCheck"]));
            }
            else
            {
                userSettings["CheckUpdatesLastCheck"] = updateLastCheck.Ticks.ToString();
                userSettings.Save();
            }

            bool checkThemeUpdates = true;

            if (userSettings.Contains("CheckThemeUpdates"))
                checkUpdates = (bool)userSettings["CheckThemeUpdates"];
            else
            {
                userSettings["CheckThemeUpdates"] = true;
                userSettings.Save();
            }
            DateTime updateThemeLastCheck = new DateTime(0);
            if (userSettings.Contains("CheckThemeUpdatesLastCheck"))
            {
                updateThemeLastCheck = new DateTime(Convert.ToInt64((String)userSettings["CheckThemeUpdatesLastCheck"]));
            }
            else
            {
                userSettings["CheckThemeUpdatesLastCheck"] = updateThemeLastCheck.Ticks.ToString();
                userSettings.Save();
            }

            if((DateTime.Now - updateLastCheck) > TimeSpan.FromDays(1))
            {
#if !NO_MAIN_ADS
                AdRotatorControl.IsEnabled = true;
#endif
                if (checkUpdates)
                {
                    try
                    {
                        UpdateChecker uc = new UpdateChecker();
                        uc.GotResult += new EventHandler<Service4u2.Common.EventArgs<UpdateCheckerResponse>>(uc_GotResult);
                        uc.FetchData();
                    }
                    catch (Exception ex)
                    {

                    }
                }
                updateLastCheck = DateTime.Now;
                userSettings["CheckUpdatesLastCheck"] = updateLastCheck.Ticks.ToString();
                userSettings.Save();
            }

            if (checkThemeUpdates)
            {
                try
                {
                    updateThemeLastCheck = DateTime.Now;
                    userSettings["CheckThemeUpdatesLastCheck"] = updateThemeLastCheck.Ticks.ToString();
                    userSettings.Save();
                    wb = new WebBrowser();
                    wb.IsScriptEnabled = true;
                    Uri uri = new Uri("http://www.sombrenuit.org/smartdialer/resourceupdate.html?t=" + lastResourceUpdate + "&cache=" + DateTime.Now.Ticks);
                    wb.Navigate(uri);
                    wb.Navigated += new EventHandler<System.Windows.Navigation.NavigationEventArgs>(wb_Navigated);
                }
                catch (Exception ex)
                {
                }
            }

            String oneTimeLastChangeLog = "";
            String version = System.Reflection.Assembly.GetExecutingAssembly().FullName.Split('=')[1].Split(',')[0];
            if (userSettings.Contains("OneTimeLastChangeLog"))
                oneTimeLastChangeLog = Convert.ToString(userSettings["OneTimeLastChangeLog"]);
            if (oneTimeLastChangeLog != version)
            {
                MessageBoxResult mbr = MessageBox.Show(ChangeLog.GetLastChangeLog(), "Last Changes", MessageBoxButton.OK);
                userSettings["OneTimeLastChangeLog"] = version;
                userSettings.Save();

                userSettings["UseThreading"] = true;
                userSettings["DelayDuration"] = "200";
                userSettings.Save();
            }

            Boolean oneTimeMessage = true;
            if (userSettings.Contains("OneTimeMessage"))
                oneTimeMessage = Convert.ToBoolean(userSettings["OneTimeMessage"]);
            if (oneTimeMessage)
            {
                var toast = new ToastPrompt
                {
                    Title = "One Time Message",
                    TextOrientation = System.Windows.Controls.Orientation.Vertical,
                    Message = "Please support my project by clicking the ads \nyou interested, Thanks",
                    ImageSource = new BitmapImage(new Uri("ApplicationIcon.png", UriKind.RelativeOrAbsolute))
                };
                toast.Show();
                userSettings["OneTimeMessage"] = false;
                userSettings.Save();
            }

            try
            {
                ApplicationTitle.Text = "SmartDialer (" + DeviceNetworkInformation.CellularMobileOperator.ToString() + ")";
            }
            catch (Exception ex)
            {
                ApplicationTitle.Text = "SmartDialer (Airplane - Sombrenuit)";
            }
            //ApplySettings(true);
        }

        void uc_GotResult(object sender, Service4u2.Common.EventArgs<UpdateCheckerResponse> e)
        {
            try
            {
                if (e.Argument == null)
                    return;
                String[] arr = e.Argument.V.Split('.');
                String version = System.Reflection.Assembly.GetExecutingAssembly().FullName.Split('=')[1].Split(',')[0];
                String[] arr2 = version.Split('.');
                if (Convert.ToInt32(arr[0]) > Convert.ToInt32(arr2[0]))
                {
                    var toast = new ToastPrompt
                    {
                        Title = "Update",
                        TextOrientation = System.Windows.Controls.Orientation.Vertical,
                        Message = ((e.Argument.M == "") ? "New update available check the marketplace and developer site." : e.Argument.M),
                        ImageSource = new BitmapImage(new Uri("ApplicationIcon.png", UriKind.RelativeOrAbsolute))
                    };
                    toast.Show();
                }
                else if (Convert.ToInt32(arr[0]) == Convert.ToInt32(arr2[0]))
                {
                    if (Convert.ToInt32(arr[1]) > Convert.ToInt32(arr2[1]))
                    {
                        var toast = new ToastPrompt
                        {
                            Title = "Update",
                            TextOrientation = System.Windows.Controls.Orientation.Vertical,
                            Message = ((e.Argument.M == "") ? "New update available check the marketplace and developer site." : e.Argument.M),
                            ImageSource = new BitmapImage(new Uri("ApplicationIcon.png", UriKind.RelativeOrAbsolute))
                        };
                        toast.Show();
                    }
                    else if (Convert.ToInt32(arr[1]) == Convert.ToInt32(arr2[1]))
                    {
                        if (Convert.ToInt32(arr[2]) > Convert.ToInt32(arr2[2]))
                        {
                            var toast = new ToastPrompt
                            {
                                Title = "Update",
                                TextOrientation = System.Windows.Controls.Orientation.Vertical,
                                Message = ((e.Argument.M == "") ? "New update available check the marketplace and developer site." : e.Argument.M),
                                ImageSource = new BitmapImage(new Uri("ApplicationIcon.png", UriKind.RelativeOrAbsolute))
                            };
                            toast.Show();
                        }
                        else if (Convert.ToInt32(arr[2]) == Convert.ToInt32(arr2[2]))
                        {
                            if (Convert.ToInt32(arr[3]) > Convert.ToInt32(arr2[3]))
                            {
                                var toast = new ToastPrompt
                                {
                                    Title = "Update",
                                    TextOrientation = System.Windows.Controls.Orientation.Vertical,
                                    Message = ((e.Argument.M == "") ? "New update available check the marketplace and developer site." : e.Argument.M),
                                    ImageSource = new BitmapImage(new Uri("ApplicationIcon.png", UriKind.RelativeOrAbsolute))
                                };
                                toast.Show();
                            }
                        }
                    }
                }
            }
            catch
            {
            }
        }

        double ConvertToUnixTimestamp(DateTime date)
        {
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            TimeSpan diff = date - origin;
            return Math.Floor(diff.TotalSeconds);
        }

        public void ApplySettings(bool refreshTheme)
        {
            IsolatedStorageSettings userSettings = IsolatedStorageSettings.ApplicationSettings;
            if (userSettings.Contains("LastResourceUpdate"))
                lastResourceUpdate = Convert.ToUInt64(userSettings["LastResourceUpdate"]);
            else
                lastResourceUpdate = Convert.ToUInt64(ConvertToUnixTimestamp(new DateTime(2011, 9, 25)));

            if (userSettings.Contains("DelayDuration"))
                tickDuration = Convert.ToInt32(userSettings["DelayDuration"]);
            else
                tickDuration = 500;

            if (userSettings.Contains("HapticFeedback"))
                allowedToSendHapticFeedback = Convert.ToBoolean(userSettings["HapticFeedback"]);
            else
                allowedToSendHapticFeedback = false;

            if (userSettings.Contains("SoundFeedback"))
                allowedToSendSound = Convert.ToBoolean(userSettings["SoundFeedback"]);
            else
                allowedToSendSound = false;

            if (userSettings.Contains("DefaultSIP"))
                useDefaultSIP = Convert.ToBoolean(userSettings["DefaultSIP"]);
            else
                useDefaultSIP = false;

            if (useDefaultSIP)
            {
                CallNumberTextBox.Visibility = Visibility.Visible;
                Numbers.Visibility = Visibility.Collapsed;
                BuiltinNumbers.Visibility = Visibility.Visible;
                CallNumberTextBox.Focus();
            }
            else
            {
                CallNumberButton.Focus();
                Numbers.Visibility = Visibility.Visible;
                BuiltinNumbers.Visibility = Visibility.Collapsed;
                CallNumberTextBox.Visibility = Visibility.Collapsed;
            }

            if (refreshTheme)
                ApplyTheme();

            try
            {
                using (IsolatedStorageFile myIsolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    using (IsolatedStorageFileStream fileStream = myIsolatedStorage.OpenFile("ThemeBackground.png", FileMode.Open, FileAccess.Read))
                    {
                        App.ThemeBackground = new MemoryStream();
                        fileStream.CopyTo(App.ThemeBackground);
                    }
                }
            }
            catch (Exception ex)
            {
                App.ThemeBackground = null;
            }
            if (App.ThemeBackground != null)
            {
                try
                {
                    ImageBrush imgbrushThemeBackground = new ImageBrush();
                    BitmapImage b = new BitmapImage();
                    b.SetSource(App.ThemeBackground);
                    imgbrushThemeBackground.ImageSource = b;
                    ContentPanel.Background = imgbrushThemeBackground;
                }
                catch (Exception e)
                {
                    App.ThemeBackground = null;
                }
            }
            else
            {
                Visibility darkBackgroundVisibility = (Visibility)Application.Current.Resources["PhoneDarkThemeVisibility"];
                if (darkBackgroundVisibility == Visibility.Visible)
                {
                    SolidColorBrush scbThemeBackground = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 0, 0, 0));
                    ContentPanel.Background = scbThemeBackground;
                }
                else
                {
                    SolidColorBrush scbThemeBackground = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 255, 255, 255));
                    ContentPanel.Background = scbThemeBackground;
                }
            }

            App.ViewModel.ApplySettings();
            LanguageHelper.ApplySettings();
        }

        public void ApplyTheme()
        {
            Visibility darkBackgroundVisibility = (Visibility)Application.Current.Resources["PhoneDarkThemeVisibility"];
            IsolatedStorageSettings userSettings = IsolatedStorageSettings.ApplicationSettings;
            if (userSettings.Contains("Theme"))
                theme = (String)userSettings["Theme"];
            else
            {
                if (darkBackgroundVisibility == Visibility.Visible)
                    theme = "adiliyo";
                else
                    theme = "weeezel-whiteone";

                userSettings["Theme"] = theme;
                userSettings.Save();
            }

            int buttonMargin = 15;
            if (userSettings.Contains("ThemeButtonHeightMargin"))
                buttonMargin = (int)userSettings["ThemeButtonHeightMargin"];
            else
            {
                userSettings["ThemeButtonHeightMargin"] = buttonMargin;
                userSettings.Save();
            }

            ImageBrush imgbrushNum0 = new ImageBrush();
            imgbrushNum0.ImageSource = ResourceHelper.GetBitmap(theme + "/0.png");
            Num0.Background = imgbrushNum0;
            Num0.Margin = new Thickness(Num0.Margin.Left, -1 * buttonMargin, Num0.Margin.Right, Num0.Margin.Bottom);
            ImageBrush imgbrushNum1 = new ImageBrush();
            imgbrushNum1.ImageSource = ResourceHelper.GetBitmap(theme + "/1.png");
            Num1.Background = imgbrushNum1;
            Num1.Margin = new Thickness(Num1.Margin.Left, -1 * buttonMargin, Num1.Margin.Right, Num1.Margin.Bottom);
            ImageBrush imgbrushNum2 = new ImageBrush();
            imgbrushNum2.ImageSource = ResourceHelper.GetBitmap(theme + "/2.png");
            Num2.Background = imgbrushNum2;
            Num2.Margin = new Thickness(Num2.Margin.Left, -1 * buttonMargin, Num2.Margin.Right, Num2.Margin.Bottom);
            ImageBrush imgbrushNum3 = new ImageBrush();
            imgbrushNum3.ImageSource = ResourceHelper.GetBitmap(theme + "/3.png");
            Num3.Background = imgbrushNum3;
            Num3.Margin = new Thickness(Num3.Margin.Left, -1 * buttonMargin, Num3.Margin.Right, Num3.Margin.Bottom);
            ImageBrush imgbrushNum4 = new ImageBrush();
            imgbrushNum4.ImageSource = ResourceHelper.GetBitmap(theme + "/4.png");
            Num4.Background = imgbrushNum4;
            Num4.Margin = new Thickness(Num4.Margin.Left, -1 * buttonMargin, Num4.Margin.Right, Num4.Margin.Bottom);
            ImageBrush imgbrushNum5 = new ImageBrush();
            imgbrushNum5.ImageSource = ResourceHelper.GetBitmap(theme + "/5.png");
            Num5.Background = imgbrushNum5;
            Num5.Margin = new Thickness(Num5.Margin.Left, -1 * buttonMargin, Num5.Margin.Right, Num5.Margin.Bottom);
            ImageBrush imgbrushNum6 = new ImageBrush();
            imgbrushNum6.ImageSource = ResourceHelper.GetBitmap(theme + "/6.png");
            Num6.Background = imgbrushNum6;
            Num6.Margin = new Thickness(Num6.Margin.Left, -1 * buttonMargin, Num6.Margin.Right, Num6.Margin.Bottom);
            ImageBrush imgbrushNum7 = new ImageBrush();
            imgbrushNum7.ImageSource = ResourceHelper.GetBitmap(theme + "/7.png");
            Num7.Background = imgbrushNum7;
            Num7.Margin = new Thickness(Num7.Margin.Left, -1 * buttonMargin, Num7.Margin.Right, Num7.Margin.Bottom);
            ImageBrush imgbrushNum8 = new ImageBrush();
            imgbrushNum8.ImageSource = ResourceHelper.GetBitmap(theme + "/8.png");
            Num8.Background = imgbrushNum8;
            Num8.Margin = new Thickness(Num8.Margin.Left, -1 * buttonMargin, Num8.Margin.Right, Num8.Margin.Bottom);
            ImageBrush imgbrushNum9 = new ImageBrush();
            imgbrushNum9.ImageSource = ResourceHelper.GetBitmap(theme + "/9.png");
            Num9.Background = imgbrushNum9;
            Num9.Margin = new Thickness(Num9.Margin.Left, -1 * buttonMargin, Num9.Margin.Right, Num9.Margin.Bottom);
            ImageBrush imgbrushCall = new ImageBrush();
            imgbrushCall.ImageSource = ResourceHelper.GetBitmap(theme + "/call.png");
            Call.Background = imgbrushCall;
            Call.Margin = new Thickness(Call.Margin.Left, -1 * buttonMargin, Call.Margin.Right, Call.Margin.Bottom);
            ImageBrush imgbrushStar = new ImageBrush();
            imgbrushStar.ImageSource = ResourceHelper.GetBitmap(theme + "/star.png");
            NumStar.Background = imgbrushStar;
            NumStar.Margin = new Thickness(NumStar.Margin.Left, -1 * buttonMargin, NumStar.Margin.Right, NumStar.Margin.Bottom);
            ImageBrush imgbrushNumber = new ImageBrush();
            imgbrushNumber.ImageSource = ResourceHelper.GetBitmap(theme + "/number.png");
            NumNumber.Background = imgbrushNumber;
            NumNumber.Margin = new Thickness(NumNumber.Margin.Left, -1 * buttonMargin, NumNumber.Margin.Right, NumNumber.Margin.Bottom);
            ImageBrush imgbrushPeople = new ImageBrush();
            imgbrushPeople.ImageSource = ResourceHelper.GetBitmap(theme + "/callhistory.png");
            People.Background = imgbrushPeople;
            People.Margin = new Thickness(People.Margin.Left, -1 * buttonMargin, People.Margin.Right, People.Margin.Bottom);
            ImageBrush imgbrushDel = new ImageBrush();
            imgbrushDel.ImageSource = ResourceHelper.GetBitmap(theme + "/del.png");
            Del.Background = imgbrushDel;
            Del.Margin = new Thickness(Del.Margin.Left, -1 * buttonMargin, Del.Margin.Right, Del.Margin.Bottom);
            if (useDefaultSIP)
            {
                if (darkBackgroundVisibility == Visibility.Visible)
                    App.ViewModel.Theme = ResourceHelper.GetBitmap("Button/appbar.feature.email.rest.png");
                else
                    App.ViewModel.Theme = ResourceHelper.GetBitmap("Button/appbar.feature.email.rest.light.png");
            }
            else
                App.ViewModel.Theme = ResourceHelper.GetBitmap(theme + "/people.png");

            if (darkBackgroundVisibility == Visibility.Visible)
                App.ViewModel.AddTheme = ResourceHelper.GetBitmap("Button/appbar.add.rest.png");
            else
                App.ViewModel.AddTheme = ResourceHelper.GetBitmap("Button/appbar.add.rest.light.png");
        }
        
        static void TimerTun(object state)
        {
            MainPage mp = (MainPage)state;
            ThreadPool.QueueUserWorkItem(o =>
            {
                App.ViewModel.LoadData(mp.sb.ToString());
            });
        }

        void dt_Tick(object sender, EventArgs e)
        {
            ThreadPool.QueueUserWorkItem(o =>
            {
                App.ViewModel.LoadData(sb.ToString());
            });
            dt.Stop();
        }

        private void Del_Hold(object sender, System.Windows.Input.GestureEventArgs e)
        {
            sb.Clear();
            if (allowedToSendHapticFeedback)
            {
                Microsoft.Devices.VibrateController.Default.Start(TimeSpan.FromMilliseconds(50));
                System.Windows.Threading.DispatcherTimer timer = new System.Windows.Threading.DispatcherTimer();
                timer.Interval = new TimeSpan(0, 0, 0, 0, 200);
                timer.Tick += (tsender, tevt) =>
                {
                    var t = tsender as System.Windows.Threading.DispatcherTimer;
                    t.Stop();
                    Microsoft.Devices.VibrateController.Default.Stop();
                };
                timer.Start();

            }

            CallNumber.Text = sb.ToString();
            App.ViewModel.accentBrush = (SolidColorBrush)Resources["PhoneAccentBrush"];
            if (dt.IsEnabled)
            {
                dt.Stop();
                dt.Start();
            }
            else
                dt.Start();
        }

        private void MainListBox_ManipulationStarted(object sender, ManipulationStartedEventArgs e)
        {
            if (useDefaultSIP)
            {
                CallNumberButton.Focus();
                BuiltinNumbers.Visibility = Visibility.Collapsed;
            }
            else
            {
                CallNumberButton.Focus();
                Numbers.Visibility = System.Windows.Visibility.Collapsed;
            }
        }

        private void ApplicationBarIconButton_Click(object sender, EventArgs e)
        {
            if (useDefaultSIP)
            {
                if (textBoxFocused)
                {
                    CallNumberButton.Focus();
                    BuiltinNumbers.Visibility = Visibility.Collapsed;
                }
                else
                {
                    CallNumberTextBox.Focus();
                    BuiltinNumbers.Visibility = Visibility.Visible;
                }
            }
            else
                Numbers.Visibility = ((Numbers.Visibility == Visibility.Visible) ? Visibility.Collapsed : Visibility.Visible);
        }

        private void ApplicationBarClearHistoryIconButton_Click(object sender, EventArgs e)
        {
            App.ViewModel.lstLastCalls.Clear();
            ContactDataSaver<ContactDataStorage> cds = new ContactDataSaver<ContactDataStorage>();
            cds.SaveMyData(new List<ContactDataStorage>(), "contactsLastCall");
            CallNumber.Text = "";
            if (dt.IsEnabled)
            {
                dt.Stop();
                dt.Start();
            }
            else
                dt.Start();
        }

        private void ApplicationBarIconCallButton_Click(object sender, EventArgs e)
        {
            if (useDefaultSIP)
            {
                CallNumberButton.Focus();
                BuiltinNumbers.Visibility = Visibility.Collapsed;
            }

            PhoneCallTask phoneCallTask = new PhoneCallTask();

            phoneCallTask.PhoneNumber = sb.ToString();
            phoneCallTask.DisplayName = "Unknown-" + sb.ToString();

            phoneCallTask.Show();

            ContactDataStorage cds = App.ViewModel.GetContactDataStorageByName(phoneCallTask.DisplayName);
            if (cds == null)
            {
                cds = new ContactDataStorage();
                cds.DisplayName = phoneCallTask.DisplayName;
                cds.PhoneNumbers = new String[1];
                cds.PhoneNumbers[0] = phoneCallTask.PhoneNumber;
            }
            cds.LastCallTime = App.ConvertToUnixTimestamp(DateTime.Now);
            if (!App.ViewModel.lstLastCalls.ContainsKey(cds.DisplayName))
                App.ViewModel.lstLastCalls.Add(cds.DisplayName + cds.PhoneNumbers, cds);

            App.ViewModel.lstLastCalls = (Dictionary<String, ContactDataStorage>)App.ViewModel.lstLastCalls.OrderByDescending(value => value.Value.LastCallTime).ToDictionary(x => x.Key, x => x.Value);
        }

        String saveContactTaskParameter = "";
        private void Sms_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Image im = (Image)sender;
            ItemViewModel ivm = (ItemViewModel)im.Tag;
            if (ivm.LineOne.StartsWith("Unknown"))
            {
                try
                {
                    SaveContactTask saveContactTask = new SaveContactTask();

                    saveContactTask.FirstName = "";
                    saveContactTask.LastName = "";
                    saveContactTask.MobilePhone = ivm.LineTwo;
                    saveContactTask.Completed += new EventHandler<SaveContactResult>(saveContactTask_Completed);
                    saveContactTaskParameter = ivm.LineOne;
                    saveContactTask.Show();
                }
                catch (Exception ex)
                {

                }
            }
            else
            {
                SmsComposeTask smsComposeTask = new SmsComposeTask();
                smsComposeTask.To = ivm.LineTwo;
                smsComposeTask.Body = "";

                smsComposeTask.Show();
            }
            e.Handled = true;
        }

        void saveContactTask_Completed(object sender, SaveContactResult e)
        {
            if (e.TaskResult == TaskResult.OK)
            {
                /*ContactDataStorage value = new ContactDataStorage();
                if (App.ViewModel.lstLastCalls.TryGetValue(saveContactTaskParameter, out value))
                    value.DisplayName = saveContactTaskParameter;*/
                App.ViewModel.lstLastCalls.Remove(saveContactTaskParameter);
                //App.ViewModel.lstLastCalls.Add(value.DisplayName
            }
        }

        private void StackPanel_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
#if DEFINE_LISTBOX
            // If selected index is -1 (no selection) do nothing
            if (MainListBox.SelectedIndex == -1)
                return;

            if (useDefaultSIP)
            {
                CallNumberButton.Focus();
                BuiltinNumbers.Visibility = Visibility.Collapsed;
            }

            PhoneCallTask phoneCallTask = new PhoneCallTask();

            ItemViewModel ivm = App.ViewModel.Items[MainListBox.SelectedIndex];
            phoneCallTask.PhoneNumber = ivm.LineTwo;
            phoneCallTask.DisplayName = ivm.LineOne;
            phoneCallTask.Show();

            ContactDataStorage cds = App.ViewModel.GetContactDataStorageByName(phoneCallTask.DisplayName);
            if (cds == null)
            {
                cds = new ContactDataStorage();
                cds.DisplayName = phoneCallTask.DisplayName;
                cds.PhoneNumbers = new String[1];
                cds.PhoneNumbers[0] = phoneCallTask.PhoneNumber;
            }
            cds.LastCallTime = App.ConvertToUnixTimestamp(DateTime.Now);
            if(!App.ViewModel.lstLastCalls.ContainsKey(cds.DisplayName))
                App.ViewModel.lstLastCalls.Add(cds.DisplayName, cds);

            App.ViewModel.lstLastCalls = (Dictionary<String, ContactDataStorage>)App.ViewModel.lstLastCalls.OrderByDescending(value => value.Value.LastCallTime).ToDictionary(x => x.Key, x => x.Value);
                    
            // Reset selected index to -1 (no selection)
            MainListBox.SelectedIndex = -1;

            if (dt.IsEnabled)
            {
                dt.Stop();
                dt.Start();
            }
            else
                dt.Start();
#endif
            // If selected index is -1 (no selection) do nothing
            if (lastSelection == null)
                return;

            if (useDefaultSIP)
            {
                CallNumberButton.Focus();
                BuiltinNumbers.Visibility = Visibility.Collapsed;
            }

            PhoneCallTask phoneCallTask = new PhoneCallTask();

            ItemViewModel ivm = lastSelection;
            phoneCallTask.PhoneNumber = ivm.LineTwo;
            phoneCallTask.DisplayName = ivm.LineOne;
            phoneCallTask.Show();

            ContactDataStorage cds = App.ViewModel.GetContactDataStorageByName(phoneCallTask.DisplayName);
            if (cds == null)
            {
                cds = new ContactDataStorage();
                cds.DisplayName = phoneCallTask.DisplayName;
                cds.PhoneNumbers = new String[1];
                cds.PhoneNumbers[0] = phoneCallTask.PhoneNumber;
            }
            cds.LastCallTime = App.ConvertToUnixTimestamp(DateTime.Now);
            if (!App.ViewModel.lstLastCalls.ContainsKey(cds.DisplayName))
                App.ViewModel.lstLastCalls.Add(cds.DisplayName, cds);

            App.ViewModel.lstLastCalls = (Dictionary<String, ContactDataStorage>)App.ViewModel.lstLastCalls.OrderByDescending(value => value.Value.LastCallTime).ToDictionary(x => x.Key, x => x.Value);

            // Reset selected index to -1 (no selection)
            //MainListBox.SelectedIndex = -1;

            if (dt.IsEnabled)
            {
                dt.Stop();
                dt.Start();
            }
            else
                dt.Start();
        }

        private void ApplicationBarIconButton_Click_1(object sender, EventArgs e)
        {
            Random r = new Random(Convert.ToInt32(DateTime.Now.Ticks % Int32.MaxValue));

            try
            {
                SaveContactTask saveContactTask = new SaveContactTask();

                saveContactTask.FirstName = r.Next().ToString();
                saveContactTask.LastName = r.Next().ToString();
                saveContactTask.MobilePhone = r.Next().ToString();
                saveContactTask.Show();
            }
            catch (Exception ex)
            {

            }
            
        }

        private void ApplicationBarMenuItem_Click(object sender, EventArgs e)
        {
            // Navigate to the new page
            NavigationService.Navigate(new Uri("/SettingsPage.xaml", UriKind.Relative));
        }

        private void me_MediaOpened(object sender, RoutedEventArgs e)
        {
            ((MediaElement)sender).Play();
        }

        private void Numbers_Tap(object sender, RoutedEventArgs e)
        {
            bool canSendFeedback = true;
            String sound = "";
            Button btn = (Button)sender;
            switch (btn.Name)
            {
                case "Num0":
                    {
                        sb.Append('0');
                        sound = "0";
                    } break;
                case "Num1":
                    {
                        sb.Append('1');
                        sound = "1";
                    } break;
                case "Num2":
                    {
                        sb.Append('2');
                        sound = "2";
                    } break;
                case "Num3":
                    {
                        sb.Append('3');
                        sound = "3";
                    } break;
                case "Num4":
                    {
                        sb.Append('4');
                        sound = "4";
                    } break;
                case "Num5":
                    {
                        sb.Append('5');
                        sound = "5";
                    } break;
                case "Num6":
                    {
                        sb.Append('6');
                        sound = "6";
                    } break;
                case "Num7":
                    {
                        sb.Append('7');
                        sound = "7";
                    } break;
                case "Num8":
                    {
                        sb.Append('8');
                        sound = "8";
                    } break;
                case "Num9":
                    {
                        sb.Append('9');
                        sound = "9";
                    } break;
                case "NumStar":
                    {
                        sb.Append('*');
                        sound = "star";
                    } break;
                case "NumNumber":
                    {
                        sb.Append('#');
                        sound = "hash";
                    } break;
                case "People":
                    {
                        canSendFeedback = false;
                    } break;
                case "Call":
                    {
                        canSendFeedback = false;

                        if (useDefaultSIP)
                        {
                            CallNumberButton.Focus();
                            BuiltinNumbers.Visibility = Visibility.Collapsed;
                        }

                        PhoneCallTask phoneCallTask = new PhoneCallTask();

                        phoneCallTask.PhoneNumber = sb.ToString();
                        phoneCallTask.DisplayName = "Unknown-" + sb.ToString();

                        phoneCallTask.Show();

                        ContactDataStorage cds = App.ViewModel.GetContactDataStorageByName(phoneCallTask.DisplayName);
                        if (cds == null)
                        {
                            cds = new ContactDataStorage();
                            cds.DisplayName = phoneCallTask.DisplayName;
                            cds.PhoneNumbers = new String[1];
                            cds.PhoneNumbers[0] = phoneCallTask.PhoneNumber;
                        }
                        cds.LastCallTime = App.ConvertToUnixTimestamp(DateTime.Now);
                        if (!App.ViewModel.lstLastCalls.ContainsKey(cds.DisplayName))
                            App.ViewModel.lstLastCalls.Add(cds.DisplayName + cds.PhoneNumbers, cds);

                        App.ViewModel.lstLastCalls = (Dictionary<String, ContactDataStorage>)App.ViewModel.lstLastCalls.OrderByDescending(value => value.Value.LastCallTime).ToDictionary(x => x.Key, x => x.Value);
                    } break;
                case "Del":
                    {
                        if (sb.Length == 0)
                            break;
                        if (sb.Length == 1)
                            sb.Clear();
                        else
                            sb.Remove(sb.Length - 1, 1);
                    } break;
            };
            if (canSendFeedback && allowedToSendHapticFeedback)
            {
                Microsoft.Devices.VibrateController.Default.Start(TimeSpan.FromMilliseconds(50));
                System.Windows.Threading.DispatcherTimer timer = new System.Windows.Threading.DispatcherTimer();
                timer.Interval = new TimeSpan(0, 0, 0, 0, 200);
                timer.Tick += (tsender, tevt) =>
                {
                    var t = tsender as System.Windows.Threading.DispatcherTimer;
                    t.Stop();
                    Microsoft.Devices.VibrateController.Default.Stop();
                };
                timer.Start();

            }
            if (canSendFeedback && allowedToSendSound && sound != "")
            {
                Stream stream = TitleContainer.OpenStream("sounds/dtmf-" + sound + ".mp3");
                me.Stop();
                me.Source = new Uri("sounds/dtmf-" + sound + ".mp3", UriKind.Relative);
                /*SoundEffect effect = SoundEffect.FromStream(stream);
                FrameworkDispatcher.Update();
                effect.Play();*/
                me.Play();
                /*System.Windows.Threading.DispatcherTimer timer = new System.Windows.Threading.DispatcherTimer();
                timer.Interval = new TimeSpan(0, 0, 0, 0, 1000);
                timer.Tick += (tsender, tevt) =>
                {
                    var t = tsender as System.Windows.Threading.DispatcherTimer;
                    t.Stop();
                    me.Stop();
                };
                timer.Start();*/
            }
            
            if (sb.Length != 0)
                CallNumber.Text = sb.ToString();
            else
                CallNumber.Text = "Enter name or number";
            App.ViewModel.accentBrush = (SolidColorBrush)Resources["PhoneAccentBrush"];

            if (dt.IsEnabled)
            {
                dt.Stop();
                dt.Start();
            }
            else
                dt.Start();
        }

        private void CallNumberTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            bool canSendFeedback = true;
            String sound = "";
            switch (e.Key)
            {
                case Key.Space:
                case Key.Unknown:
                    {
                    } break;
                case Key.NumPad0:
                    {
                        sb.Append('0');
                        sound = "0";
                    } break;
                case Key.NumPad1:
                    {
                        sb.Append('1');
                        sound = "1";
                    } break;
                case Key.NumPad2:
                    {
                        sb.Append('2');
                        sound = "2";
                    } break;
                case Key.NumPad3:
                    {
                        sb.Append('3');
                        sound = "3";
                    } break;
                case Key.NumPad4:
                    {
                        sb.Append('4');
                        sound = "4";
                    } break;
                case Key.NumPad5:
                    {
                        sb.Append('5');
                        sound = "5";
                    } break;
                case Key.NumPad6:
                    {
                        sb.Append('6');
                        sound = "6";
                    } break;
                case Key.NumPad7:
                    {
                        sb.Append('7');
                        sound = "7";
                    } break;
                case Key.NumPad8:
                    {
                        sb.Append('8');
                        sound = "8";
                    } break;
                case Key.NumPad9:
                    {
                        sb.Append('9');
                        sound = "9";
                    } break;
                case Key.D8:
                    {
                        sb.Append('*');
                        sound = "star";
                    } break;
                case Key.D3:
                    {
                        sb.Append('#');
                        sound = "hash";
                    } break;
                /*case "People":
                    {
                        canSendFeedback = false;
                    } break;
                case "Call":
                    {
                        canSendFeedback = false;
                        PhoneCallTask phoneCallTask = new PhoneCallTask();

                        phoneCallTask.PhoneNumber = sb.ToString();
                        phoneCallTask.DisplayName = "Unknown-" + sb.ToString();

                        phoneCallTask.Show();

                        ContactDataStorage cds = App.ViewModel.GetContactDataStorageByName(phoneCallTask.DisplayName);
                        if (cds == null)
                        {
                            cds = new ContactDataStorage();
                            cds.DisplayName = phoneCallTask.DisplayName;
                            cds.PhoneNumbers = new String[1];
                            cds.PhoneNumbers[0] = phoneCallTask.PhoneNumber;
                        }
                        cds.LastCallTime = App.ConvertToUnixTimestamp(DateTime.Now);
                        if (!App.ViewModel.lstLastCalls.ContainsKey(cds.DisplayName))
                            App.ViewModel.lstLastCalls.Add(cds.DisplayName + cds.PhoneNumbers, cds);

                        App.ViewModel.lstLastCalls = (Dictionary<String, ContactDataStorage>)App.ViewModel.lstLastCalls.OrderByDescending(value => value.Value.LastCallTime).ToDictionary(x => x.Key, x => x.Value);
                    } break;*/
                case Key.Back:
                    {
                        if (sb.Length == 0)
                            break;
                        if (sb.Length == 1)
                            sb.Clear();
                        else
                            sb.Remove(sb.Length - 1, 1);
                    } break;
            };
            if (canSendFeedback && allowedToSendHapticFeedback)
            {
                Microsoft.Devices.VibrateController.Default.Start(TimeSpan.FromMilliseconds(50));
                System.Windows.Threading.DispatcherTimer timer = new System.Windows.Threading.DispatcherTimer();
                timer.Interval = new TimeSpan(0, 0, 0, 0, 200);
                timer.Tick += (tsender, tevt) =>
                {
                    var t = tsender as System.Windows.Threading.DispatcherTimer;
                    t.Stop();
                    Microsoft.Devices.VibrateController.Default.Stop();
                };
                timer.Start();

            }
            if (canSendFeedback && allowedToSendSound && sound != "")
            {
                Stream stream = TitleContainer.OpenStream("sounds/dtmf-" + sound + ".mp3");
                me.Stop();
                me.Source = new Uri("sounds/dtmf-" + sound + ".mp3", UriKind.Relative);
                /*SoundEffect effect = SoundEffect.FromStream(stream);
                FrameworkDispatcher.Update();
                effect.Play();*/
                System.Windows.Threading.DispatcherTimer timer = new System.Windows.Threading.DispatcherTimer();
                timer.Interval = new TimeSpan(0, 0, 0, 0, 1000);
                timer.Tick += (tsender, tevt) =>
                {
                    var t = tsender as System.Windows.Threading.DispatcherTimer;
                    t.Stop();
                    me.Stop();
                };
                timer.Start();
            }

            if (useDefaultSIP)
            {
                if (sb.Length != 0)
                    CallNumberBuiltIn.Text = sb.ToString();
                else
                    CallNumberBuiltIn.Text = "Enter name or number";
            }
            else
            {
                if (sb.Length != 0)
                    CallNumber.Text = sb.ToString();
                else
                    CallNumber.Text = "Enter name or number";
            }
            App.ViewModel.accentBrush = (SolidColorBrush)Resources["PhoneAccentBrush"];

            if (dt.IsEnabled)
            {
                dt.Stop();
                dt.Start();
            }
            else
                dt.Start();
            e.Handled = true;
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem mi = (MenuItem)sender;
            String DisplayName = (String)mi.Tag;

            if (App.ViewModel.lstLastCalls.ContainsKey(DisplayName))
                App.ViewModel.lstLastCalls.Remove(DisplayName);

            if (dt.IsEnabled)
            {
                dt.Stop();
                dt.Start();
            }
            else
                dt.Start();
        }

        private void MenuItem_Loaded(object sender, RoutedEventArgs e)
        {
            MenuItem mi = (MenuItem)sender;
            String DisplayName = (String)mi.Tag;

            if (App.ViewModel.lstLastCalls.ContainsKey(DisplayName))
                mi.IsEnabled = true;
            else
                mi.IsEnabled = false;           
        }

        private void CallNumberTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            textBoxFocused = true;
        }

        private void CallNumberTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            textBoxFocused = false;
        }
    }
}
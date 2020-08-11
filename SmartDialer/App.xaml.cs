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
using Microsoft.Phone.UserData;
using System.IO.IsolatedStorage;
using System.Windows.Media.Imaging;
using System.IO;
using Microsoft.Phone.Tasks;
using Service4u2.Json;
using System.Text;
using WindowsPhonePostClient;
using System.Threading;
using System.Runtime.Serialization.Json;
using System.Runtime.Serialization;

namespace SmartDialer
{
    public partial class App : Application
    {
        private static MainViewModel viewModel = null;

        public static Stream ThemeBackground = null;

        public static UInt64 ConvertToUnixTimestamp(DateTime date)
        {
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            TimeSpan diff = date - origin;
            return Convert.ToUInt64(Math.Floor(diff.TotalSeconds));
        }

        /// <summary>
        /// A static ViewModel used by the views to bind against.
        /// </summary>
        /// <returns>The MainViewModel object.</returns>
        public static MainViewModel ViewModel
        {
            get
            {
                // Delay creation of the view model until necessary
                if (viewModel == null)
                    viewModel = new MainViewModel();

                return viewModel;
            }
        }

        /// <summary>
        /// Provides easy access to the root frame of the Phone Application.
        /// </summary>
        /// <returns>The root frame of the Phone Application.</returns>
        public PhoneApplicationFrame RootFrame { get; private set; }

        /// <summary>
        /// Constructor for the Application object.
        /// </summary>
        public App()
        {
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
                // which shows areas of a page that are handed off GPU with a colored overlay.
                //Application.Current.Host.Settings.EnableCacheVisualization = true;

                // Disable the application idle detection by setting the UserIdleDetectionMode property of the
                // application's PhoneApplicationService object to Disabled.
                // Caution:- Use this under debug mode only. Application that disables user idle detection will continue to run
                // and consume battery power when the user is not using the phone.
                PhoneApplicationService.Current.UserIdleDetectionMode = IdleDetectionMode.Disabled;
            }

        }

        // Code to execute when the application is launching (eg, from Start)
        // This code will not execute when the application is reactivated
        private void Application_Launching(object sender, LaunchingEventArgs e)
        {
        }

        // Code to execute when the application is activated (brought to foreground)
        // This code will not execute when the application is first launched
        private void Application_Activated(object sender, ActivatedEventArgs e)
        {
            // Ensure that application state is restored appropriately
           /* if (!App.ViewModel.IsDataLoaded)
            {
                //App.ViewModel.LoadData("");
                ThreadPool.QueueUserWorkItem(o =>
                {
                    App.ViewModel.LoadData("");
                });
            }*/
            ContactDataSaver<ContactDataStorage> cds = new ContactDataSaver<ContactDataStorage>();
            List<ContactDataStorage> lstTest = cds.LoadMyData("contacts");
            List<ContactDataStorage> lstLastCall = cds.LoadMyData("contactsLastCall");
            App.ViewModel.Items.Clear();
            App.ViewModel.ItemsCache.Clear();
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
            if (lstTest.Count > 0)
            {
                IsolatedStorageSettings userSettings = IsolatedStorageSettings.ApplicationSettings;
                if (userSettings.Contains("LastCheck"))
                {
                    App.ViewModel.loaded = true;
                    App.ViewModel.lastCheck = new DateTime(Convert.ToInt64((String)userSettings["LastCheck"]));
                }
                viewModel.lst = lstTest;

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

                        if(App.ViewModel.ViewLimit == 0 || App.ViewModel.Items.Count < App.ViewModel.ViewLimit)
                            App.ViewModel.Items.Add(ivm);
                        App.ViewModel.ItemsCache.Add(ivm);
                    }
                }
            }
        }

        // Code to execute when the application is deactivated (sent to background)
        // This code will not execute when the application is closing
        private void Application_Deactivated(object sender, DeactivatedEventArgs e)
        {
            // Ensure that required application state is persisted here.
            ContactDataSaver<ContactDataStorage> cds = new ContactDataSaver<ContactDataStorage>();
            cds.SaveMyData(viewModel.lst, "contacts");
            List<ContactDataStorage> cdsList = new List<ContactDataStorage>();
            foreach(KeyValuePair<String, ContactDataStorage> kvp in viewModel.lstLastCalls)
                cdsList.Add(kvp.Value);

            cds.SaveMyData(cdsList, "contactsLastCall");

            IsolatedStorageSettings userSettings = IsolatedStorageSettings.ApplicationSettings;
            userSettings["LastCheck"] = App.viewModel.lastCheck.Ticks.ToString();
            userSettings.Save();
        }

        // Code to execute when the application is closing (eg, user hit Back)
        // This code will not execute when the application is deactivated
        private void Application_Closing(object sender, ClosingEventArgs e)
        {
            ContactDataSaver<ContactDataStorage> cds = new ContactDataSaver<ContactDataStorage>();
            cds.SaveMyData(viewModel.lst, "contacts");
            List<ContactDataStorage> cdsList = new List<ContactDataStorage>();
            foreach (KeyValuePair<String, ContactDataStorage> kvp in viewModel.lstLastCalls)
                cdsList.Add(kvp.Value);

            cds.SaveMyData(cdsList, "contactsLastCall");

            IsolatedStorageSettings userSettings = IsolatedStorageSettings.ApplicationSettings;
            userSettings["LastCheck"] = App.viewModel.lastCheck.Ticks.ToString();
            userSettings.Save();
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

        [DataContract]
        public class ErrorData
        {
            [DataMember]
            public String M { get; set; }
            [DataMember]
            public String S { get; set; }
        }
        // Code to execute on Unhandled Exceptions
        private void Application_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            /*if (e.ExceptionObject.Message == "Quit")
                return;*/
            if (e.ExceptionObject.Message.StartsWith("WebClient does not support"))
                e.Handled = true;
            //this is the ADMOB exception
            else if (e.ExceptionObject.Message == "You cannot call WebBrowser methods until it is in the visual tree.")
                e.Handled = true;
            else if (e.ExceptionObject.Message.StartsWith("An unknown error has occurred. Error: 80020101"))
                e.Handled = true;

            if (e.Handled)
                return;

            MessageBoxResult mbr = MessageBox.Show("If you send error log, it can be fixed on other update...", "Send error log?", MessageBoxButton.OKCancel);
            if (mbr == MessageBoxResult.OK)
            {
                try
                {
                    ThreadPool.QueueUserWorkItem(o =>
                    {
                        ErrorData err = new ErrorData();
                        err.M = e.ExceptionObject.Message;
                        if (e.ExceptionObject.StackTrace != null)
                            err.S = e.ExceptionObject.StackTrace;
                        else
                            err.S = "";
                        DataContractJsonSerializer dcjs = new DataContractJsonSerializer(typeof(ErrorData));
                        MemoryStream ms = new MemoryStream();
                        dcjs.WriteObject(ms, err);
                        var myStr = Encoding.UTF8.GetString(ms.GetBuffer(), 0, (int)ms.Length);

                        Dictionary<String, Object> par = new Dictionary<String, Object>();
                        par.Add("V", Uri.EscapeDataString(myStr));
                        PostClient pc = new PostClient(par);
                        pc.DownloadStringCompleted += new PostClient.DownloadStringCompletedHandler(pc_DownloadStringCompleted);
                        pc.DownloadStringAsync(new Uri("http://www.alangoya.com/edo/smartdialer/errorhandler.aspx"));
                    });
                    e.Handled = true;
                }
                catch
                {
                }
            }
            else
                e.Handled = true;
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // An unhandled exception has occurred; break into the debugger
                System.Diagnostics.Debugger.Break();
            }
        }

        void pc_DownloadStringCompleted(object sender, WindowsPhonePostClient.DownloadStringCompletedEventArgs e)
        {
            //throw new Exception("Quit");
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
    }
}
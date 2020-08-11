using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.IO.IsolatedStorage;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using Microsoft.Phone.Tasks;
using System.Device.Location;
using SmartDialer.Ad;

namespace SmartDialer
{
    /// <summary>
    /// User control for embedding into Windows Phone 7 Silverlight applications.
    /// Here is how it works:
    ///     1.  Request an ad from adMob
    ///     2.  Receive a response, which is in JSON format
    ///     3.  Deconstruct the JSON message into a class
    ///     4.  Build a webpage on the fly using the received information and a template
    ///     5.  Get the Browser control to display the webpage
    ///     6.  If the user clicks on the ad, spin up IE to go to the URL provided by adMob
    ///     
    /// TODO:
    ///     1.  Add property to control how often the ad changes
    ///     2.  If UseGPS property is on, delay the initial ad fetch for a bit so that we have GPS
    ///     3.  Add property to turn the Test mode on and off
    ///     4.  Add property to let the user specify keywords for ads
    ///     5.  Better error handling
    ///     6.  Better handle case when adMob has no ad inventory
    ///     7.  ???
    ///     8.  Profit!!!
    /// </summary>
    public partial class adWhirlRenderer : UserControl
    {
        #region Private Variables
        private DispatcherTimer timerReloadAd = new DispatcherTimer();
        private WebClient wcAdMob = new WebClient();
        private WebClient wcAdWhirl = new WebClient();
        private WebClient wcDeviceInfo = new WebClient();
        private GeoCoordinateWatcher geoWatcher;
        private const string baseURL = "http://mob.adwhirl.com/getInfo.php";
        private const string deviceInfoURL = "http://www.angryhacker.com/detect/browserinfo.ashx";
        internal event EventHandler<ErrorEventArgs> ErrorOccured = delegate { };
        private string keywords = "sports";
        private string cookieContent = string.Empty;
        private bool useGps;
        private bool requestPending;
        private string browserUserAgent = null;
        private string externalIpAddress = null;
        #endregion

        #region Constructors
        public adWhirlRenderer()
        {
            InitializeComponent();
            if (!System.ComponentModel.DesignerProperties.IsInDesignTool)
            {
                SetEvents();
                SetDefaults();
            }
        }

        #endregion

        #region Events
        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            BeginAdFetch();
        }

        private void timerReloadAd_Tick(object sender, EventArgs e)
        {
            BeginAdFetch();
        }

        void wcAdMob_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            if (e.Error != null)
                ErrorOccured.RaiseEvent(this, new ErrorEventArgs(e.Error));
            else
            {
                var json = new DataContractJsonSerializer(typeof(AdWhirlPayload));
                var ms = new MemoryStream(Encoding.UTF8.GetBytes(e.Result));
                AdWhirlPayload adWhirlPayload = json.ReadObject(ms) as AdWhirlPayload;

                try
                {
                    string formFields = string.Format("appid={0}&nid={1}&uuid={2}&country_code={3}&location={4}&location_timestamp={5}&appver={6}&client={7}",
                        this.PublisherID, adWhirlPayload.RationList[0].Nid, 0, this.CountryCode, GetLatLong(), DateTime.Now.Ticks, this.AppVer, this.ClientId);

                    Uri address = new Uri("http://cus.adwhirl.com/custom.php?" + formFields);
                    wcAdWhirl.DownloadStringAsync(address);
                }
                catch (Exception ex)
                {
                    ErrorOccured.RaiseEvent(this, new ErrorEventArgs { Error = ex });
                }
            }
        }

        void wcAdWhirl_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            if (e.Error != null)
                ErrorOccured.RaiseEvent(this, new ErrorEventArgs(e.Error));
            else
            {
                adFetched = true;
                var json = new DataContractJsonSerializer(typeof(AdWhirlCustom));
                var ms = new MemoryStream(Encoding.UTF8.GetBytes(e.Result));
                AdWhirlCustom adWhirlCustom = json.ReadObject(ms) as AdWhirlCustom;

                adPage = BuildAdPage(adWhirlCustom);

                if (browserLoaded)
                {
                    try
                    {
                        brsAds.NavigateToString(adPage);
                        adRendered = true;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                }
            }
        }

        private bool browserLoaded;
        private bool adFetched;
        private bool adRendered;
        private string adPage = string.Empty;

        private void brsAds_Loaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine(browserLoaded);
            // this should fire only once
            browserLoaded = true;

            if (adFetched && !adRendered)
                brsAds.NavigateToString(adPage);
        }

        private void wcDeviceInfo_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            if (e.Error != null)
                ErrorOccured.RaiseEvent(this, new ErrorEventArgs(e.Error));
            else
            {
                var json = new DataContractJsonSerializer(typeof(DeviceInfo));
                var ms = new MemoryStream(Encoding.UTF8.GetBytes(e.Result));
                var deviceInfo = json.ReadObject(ms) as DeviceInfo;

                if (deviceInfo == null)
                    return;

                externalIpAddress = deviceInfo.IpAddress;

                // we can now fetch the ad
                if (requestPending)
                {
                    requestPending = false;
                    BeginAdFetch();
                }
            }
        }

        private void brsAds_Navigating(object sender, Microsoft.Phone.Controls.NavigatingEventArgs e)
        {
            // start the browser to serve the ad
            WebBrowserTask task = new WebBrowserTask();
            task.URL = e.Uri.ToString();
            task.Show();

            // cancel navigation since we want to stay on the Ad
            e.Cancel = true;
        }

        private void brsAds_ScriptNotify(object sender, Microsoft.Phone.Controls.NotifyEventArgs e)
        {
            browserUserAgent = e.Value;

            // we can now fetch the ad
            if (requestPending)
            {
                requestPending = false;
                BeginAdFetch();
            }
        }

        #endregion

        #region Private Methods
        private void SetEvents()
        {
            this.Loaded += new RoutedEventHandler(MainPage_Loaded);
            wcAdMob.DownloadStringCompleted += new DownloadStringCompletedEventHandler(wcAdMob_DownloadStringCompleted);
            wcAdWhirl.DownloadStringCompleted += new DownloadStringCompletedEventHandler(wcAdWhirl_DownloadStringCompleted);
            wcDeviceInfo.DownloadStringCompleted += new DownloadStringCompletedEventHandler(wcDeviceInfo_DownloadStringCompleted);
            brsAds.Loaded += new RoutedEventHandler(brsAds_Loaded);
        }

        private void SetDefaults()
        {
            timerReloadAd.Interval = new TimeSpan(0, 0, 1, 0, 0);  // one minute for now
            timerReloadAd.Tick += new EventHandler(timerReloadAd_Tick);
            this.TestMode = true;   // for now this is the default
        }

        private string BuildAdPage(AdWhirlCustom adWhirlCustom)
        {
            if (adWhirlCustom == null) return string.Empty;

            string html = UserResources.AdPageTemplate.Replace("{text}", adWhirlCustom.AdText)
                                                        .Replace("{url}", adWhirlCustom.RedirectUrl)
                                                        .Replace("{width}", this.ActualWidth.ToString());
            return html;
        }

        private void BeginAdFetch()
        {
            if (browserUserAgent == null)
            {
                requestPending = true;
                ObtainUserAgent();
                return;
            }

            if (externalIpAddress == null)
            {
                // indicate that the request is pending
                requestPending = true;
                ObtainDeviceInformation();
                return;
            }

            try
            {
                string formFields = string.Format("?appid={0}&appver={1}&client={2}",
                    this.PublisherID, this.AppVer, this.ClientId);

                // add optional parameters
                /*if (this.TestMode)
                    formFields += "&m=test";
                
                if (useGps)
                    formFields += "&d[coord]=" + GetLatLong();*/

                Uri address = new Uri(baseURL + formFields);
                wcAdMob.DownloadStringAsync(address);
            }
            catch (Exception ex)
            {
                ErrorOccured.RaiseEvent(this, new ErrorEventArgs { Error = ex });
            }
        }

        private void ObtainUserAgent()
        {
            string html = UserResources.GetUserAgentScript;
            brsAds.NavigateToString(html);
        }

        private void ObtainDeviceInformation()
        {
            Uri address = new Uri(deviceInfoURL);
            wcDeviceInfo.DownloadStringAsync(address);
        }

        private string GetCookie()
        {
            // come up with a 32 character unique string and store it in the IsolatedStorage
            // then retrieve on the next access.
            const string COOKIE = "cookie.txt";

            if (!string.IsNullOrEmpty(cookieContent)) return cookieContent;

            // we have not yet created or retrieved the cookie
            try
            {
                using (var store = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    try
                    {
                        if (store.FileExists(COOKIE))
                        {
                            using (var reader = new StreamReader(store.OpenFile(COOKIE, FileMode.Open, FileAccess.Read)))
                            {
                                cookieContent = reader.ReadToEnd();
                            }
                        }
                    }
                    catch (IsolatedStorageException ex)
                    {
                        // log it somewhere? 
                    }


                    if (cookieContent.Length == 0)
                    {
                        // cookie does not exist, or could not be read - thus we shall create a new cookie
                        try
                        {
                            using (var sw = new StreamWriter(store.CreateFile(COOKIE)))
                            {
                                Guid g = Guid.NewGuid();
                                cookieContent = g.ToString();

                                // remove dashes to come up with a 32 char value
                                cookieContent = cookieContent.Replace("-", "");

                                sw.Write(cookieContent);
                            }
                        }
                        catch (IsolatedStorageException ex)
                        {
                            // log somewhere?
                        }
                    }
                }
            }
            catch
            {
                // log ?
                // this means we won't be able to save it.  Will just return an empty string
            }

            return cookieContent;
        }

        private string UrlEncode(string s)
        {
            return HttpUtility.UrlEncode(s);
        }

        private string GetLatLong()
        {
            if (geoWatcher != null)
                return string.Format("{0}, {1}", geoWatcher.Position.Location.Latitude, geoWatcher.Position.Location.Longitude);

            return string.Empty;
        }

        private void InitializeGps()
        {
            if (geoWatcher != null)
                geoWatcher.Dispose();

            geoWatcher = new GeoCoordinateWatcher(GeoPositionAccuracy.Default);
            geoWatcher.Start();
        }

        private void DisableGps()
        {
            if (geoWatcher != null)
            {
                geoWatcher.Stop();
                geoWatcher.Dispose();
                geoWatcher = null;
            }
        } 
        #endregion

        #region Properties
        public bool UseGps
        {
            get
            {
                return useGps;
            }
            set
            {
                useGps = value;

                if (useGps)
                    InitializeGps();
                else
                    DisableGps();
            }
        }

        public string PublisherID { get; set; }

        public string CountryCode { get; set; }

        public string AppVer { get; set; }

        public string ClientId { get; set; }

        public bool TestMode { get; set; }
        #endregion
    }

    [DataContractAttribute]
    public class AdWhirlPayload
    {
        [DataMember(Name = "extra")]
        public Extra ExtraData { get; set; }
        [DataMember(Name = "rations")]
        public List<Rations> RationList { get; set; }
    }

    [DataContractAttribute]
    public class AdColor
    {
        [DataMember(Name = "red")]
        public int Red { get; set; }
        [DataMember(Name = "green")]
        public int Green { get; set; }
        [DataMember(Name = "blue")]
        public int Blue { get; set; }
        [DataMember(Name = "alpha")]
        public int Alpha { get; set; }
    }

    /*
     {
      "extra":{"location_on":0,"background_color_rgb":{"red":255,"green":255,"blue":255,"alpha":1},"text_color_rgb":{"red":0,"green":0,"blue":0,"alpha":1},"cycle_time":30000,"transition":8},
      "rations":[{"nid":"f90c2559167b4225aa5fff0e9a013cbb","nname":"admob","type":1,"weight":80,"priority":1,"key":"a213789152"},
                 {"nid":"e636a224704947b6b34cd17299f0047e","nname":"custom","type":9,"weight":20,"priority":2,"key":"__CUSTOM__"}]
    }*/
    [DataContractAttribute]
    public class Extra
    {
        [DataMember(Name = "location_on")]
        public string LocationOn { get; set; }
        [DataMember(Name = "background_color_rgb")]
        public AdColor BackgroundColor { get; set; }
        [DataMember(Name = "text_color_rgb")]
        public AdColor TextColor { get; set; }
        [DataMember(Name = "cycle_time")]
        public int CycleTime { get; set; }
        [DataMember(Name = "transition")]
        public int Transition { get; set; }
    }

    [DataContractAttribute]
    public class Rations
    {
        [DataMember(Name = "nid")]
        public string Nid { get; set; }
        [DataMember(Name = "nname")]
        public string NName { get; set; }
        [DataMember(Name = "type")]
        public int Type { get; set; }
        [DataMember(Name = "weight")]
        public int Weight { get; set; }
        [DataMember(Name = "priority")]
        public int Priority { get; set; }
        [DataMember(Name = "key")]
        public string Key { get; set; }
    }

    [DataContractAttribute]
    public class AdWhirlCustom
    {
        [DataMember(Name = "image_url")]
        public string ImageUrl { get; set; }
        [DataMember(Name = "redirect_url")]
        public string RedirectUrl { get; set; }
        [DataMember(Name = "metrics_url")]
        public string MetricsUrl { get; set; }
        [DataMember(Name = "ad_type")]
        public String AdType { get; set; }
        [DataMember(Name = "ad_text")]
        public string AdText { get; set; }
        [DataMember(Name = "launch_type")]
        public int LaunchType { get; set; }
        [DataMember(Name = "webview_animation_type")]
        public string AnimationType { get; set; }
    }
}

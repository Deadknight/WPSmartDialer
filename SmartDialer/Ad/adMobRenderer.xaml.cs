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
    public partial class adMobRenderer : UserControl
    {
        #region Private Variables
        private DispatcherTimer timerReloadAd = new DispatcherTimer();
        private WebClient wcAdMob = new WebClient();
        private WebClient wcDeviceInfo = new WebClient();
        private GeoCoordinateWatcher geoWatcher;
        private const string baseURL = "http://r.admob.com/ad_source.php";
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
        public adMobRenderer()
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

        private void wcAdMob_UploadStringCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            if (e.Error != null)
                ErrorOccured.RaiseEvent(this, new ErrorEventArgs(e.Error));
            else
            {
                adFetched = true;
                var json = new DataContractJsonSerializer(typeof(AdMobPayload));
                var ms = new MemoryStream(Encoding.UTF8.GetBytes(e.Result));
                AdMobPayload adMobPayload = json.ReadObject(ms) as AdMobPayload;

                adPage = BuildAdPage(adMobPayload);

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

                /*
                {
                    "text":"AdMob Test  Web Ad",
                    "url":"http://c.admob.com/c1/3/EkGAjzm-4xEkCJK1HIemS4C4144F00733C0005bfe89ebfd98afb72/t106",
                    "image_url":"http://c.admob.com/img/c.gif",
                    "jsonp_url":"http://c.admob.com/j1/3/EkGAjzm-4xEkCJK1HIemS4C4144F00733C0005bfe89ebfd98afb72/t106",
                    "204":"/static/iphone_sample/ad_tile.png",
                    "6":"url"
                }
                */
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
            wcAdMob.UploadStringCompleted += new UploadStringCompletedEventHandler(wcAdMob_UploadStringCompleted);            
            wcDeviceInfo.DownloadStringCompleted += new DownloadStringCompletedEventHandler(wcDeviceInfo_DownloadStringCompleted);
            brsAds.Loaded += new RoutedEventHandler(brsAds_Loaded);
        }


        private void SetDefaults()
        {
            timerReloadAd.Interval = new TimeSpan(0, 0, 1, 0, 0);  // one minute for now
            timerReloadAd.Tick += new EventHandler(timerReloadAd_Tick);
            this.TestMode = true;   // for now this is the default
        }

        private string BuildAdPage(AdMobPayload adMobPayload)
        {
            if (adMobPayload == null) return string.Empty;

            string html = UserResources.AdPageTemplate.Replace("{text}", adMobPayload.Text)
                                                        .Replace("{url}", adMobPayload.Url)
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

                //<form action="http://r.admob.com/ad_source.php" method="post" enctype="application/x-www-form-urlencoded"> 
                //    <input type="hidden" name="s" value="a14c3abf3b3f59d" /> 
                //    <input type="hidden" name="u" value="Mozilla/4.0 (compatible; MSIE 7.0; Windows Phone OS 7.0; Trident/3.1; IEMobile/7.0) Asus;Galaxy6" />		
                //    <input type="Hidden" name="ex" value="1" /> 
                //    <input type="Hidden" name="o" value="4e5f585859596f28233f672a674d38514c427c274f4c59633e207c4942" /> 
                //    <input type="Hidden" name="i" value="10.82.60.169" /> 
                //    <input type="Hidden" name="f" value="jsonp" /> 
                //    <input type="Hidden" name="m" value="test" /> 
                //    <input type="submit" value="Go" /> 
                //</form> 

                /// see format here: http://developer.admob.com/wiki/Requests
                /// which is pretty much useless, as it is incomplete
                string formFields = string.Format("s={0}&ex=1&o={1}&i={2}&f=jsonp&u={3}&h[Referer]=http://www.sombrenuit.org",
                    this.PublisherID, GetCookie(), externalIpAddress, browserUserAgent/*"Mozilla/5.0 (iPhone; U; CPU like Mac OS X; en) AppleWebKit/420+ (KHTML, like Gecko) Version/3.0 Mobile/1A543a Safari/419.3"*/);

                // add optional parameters
                if (this.TestMode)
                    formFields += "&m=test";
                
                if (useGps)
                    formFields += "&d[coord]=" + GetLatLong();

                wcAdMob.Headers["Content-Type"] = "application/x-www-form-urlencoded";
                wcAdMob.Headers["User-Agent"] = externalIpAddress;

                Uri address = new Uri(baseURL);
                wcAdMob.UploadStringAsync(address, "POST", formFields);
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

        public bool TestMode { get; set; }
        #endregion
    }
    
    [DataContractAttribute]
    public class AdMobPayload
    {
        [DataMember(Name = "text")]
        public string Text { get; set; }
        [DataMember(Name = "url")]
        public string Url { get; set; }
        [DataMember(Name = "image_url")]
        public string ImageURL { get; set; }
        [DataMember(Name = "jsonp_url")]
        public string JsonpURL { get; set; }
        [DataMember(Name = "204")]
        public string Value204 { get; set; }
        [DataMember(Name = "6")]
        public string Value6 { get; set; }
    }    

    [DataContractAttribute]
    public class DeviceInfo
    {
        [DataMember(Name = "UserAgent")]
        public string UserAgent { get; set; }
        [DataMember(Name = "IpAddress")]
        public string IpAddress { get; set; }
    }
    
}

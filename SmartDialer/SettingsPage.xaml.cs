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
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using System.Collections.ObjectModel;
using System.IO.IsolatedStorage;
using System.ComponentModel;
using System.Reflection;
using System.Text;
using System.Windows.Data;
using Microsoft.Phone.Tasks;
using System.IO;
using Coding4Fun.Phone.Controls;

namespace SmartDialer
{
    public partial class SettingsPage : PhoneApplicationPage
    {
        int saveTheme = 0;
        String selectedTheme = "";
        Int32 selectedLanguages = 0;
        List<LANG> LangArr;
        Brush defaultForegroundBrush = null;
        bool firstInit = true;

        Int32 textColorA, textColorR, textColorG, textColorB = -1;
        Int32 textColorSecondA, textColorSecondR, textColorSecondG, textColorSecondB = -1;

        // Constructor
        public SettingsPage()
        {
            Visibility darkBackgroundVisibility = (Visibility)Application.Current.Resources["PhoneDarkThemeVisibility"];
            if (darkBackgroundVisibility == Visibility.Visible)
                selectedTheme = "adiliyo";
            else
                selectedTheme = "weeezel-whiteone";

            InitializeComponent();
            firstInit = false;
            ListPickerLanguages.SummaryForSelectedItemsDelegate = SummarizeItems;
            LangArr = LanguageHelper.GetLanguageArray();
            //DataContext = this;
        }

        public static Object GetSetting(IsolatedStorageSettings userSettings, String name, object defaultValue)
        {
            if (userSettings.Contains(name))
                return userSettings[name];
            else
            {
                userSettings[name] = defaultValue;
                userSettings.Save();
                return defaultValue;
            }
        }

        public static void UpdateSetting(IsolatedStorageSettings userSettings, String name, object value)
        {
            userSettings[name] = value;
            userSettings.Save();
        }

        // When page is navigated to set data context to selected item in list
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            languageSelectionChanged = false;
            //AdBanner.GpsLocation = GpsLocationProvider.CurrentLocation;
            IsolatedStorageSettings userSettings = IsolatedStorageSettings.ApplicationSettings;
            if (!userSettings.Contains("Theme"))
            {
                Visibility darkBackgroundVisibility = (Visibility)Application.Current.Resources["PhoneDarkThemeVisibility"];
                if (darkBackgroundVisibility == Visibility.Visible)
                {
                    selectedTheme = "adiliyo";
                    ListPickerTheme.SelectedIndex = 3;
                }
                else
                {
                    selectedTheme = "weeezel-whiteone";
                    ListPickerTheme.SelectedIndex = 4;
                }
                userSettings["Theme"] = selectedTheme;
                userSettings.Save();
            }
            else
            {
                selectedTheme = (String)userSettings["Theme"];
                int index = 0;
                foreach (String item in ListPickerTheme.Items)
                {
                    //if (item == selectedTheme)
                    if (item == selectedTheme)
                    {
                        ListPickerTheme.SelectedIndex = index;
                        break;
                    }
                    index++;                    
                }
            }

            txtBoxCacheDuration.Text = (String)GetSetting(userSettings, "CacheDuration", "3600");
            txtBoxDelayDuration.Text = (String)GetSetting(userSettings, "DelayDuration", "200");
            SoundFeedback.IsChecked = Convert.ToBoolean(GetSetting(userSettings, "SoundFeedback", false));
            HapticFeedback.IsChecked = Convert.ToBoolean(GetSetting(userSettings, "HapticFeedback", false));
            DefaultSIP.IsChecked = Convert.ToBoolean(GetSetting(userSettings, "DefaultSIP", false));
            OnlyPhones.IsChecked = Convert.ToBoolean(GetSetting(userSettings, "OnlyPhones", true));
            CallHistory.IsChecked = Convert.ToBoolean(GetSetting(userSettings, "CallHistory", true));
            CallHistoryFilter.IsChecked = Convert.ToBoolean(GetSetting(userSettings, "CallHistoryFilter", true));
            CheckUpdates.IsChecked = Convert.ToBoolean(GetSetting(userSettings, "CheckUpdates", true));
            CheckThemeUpdates.IsChecked = Convert.ToBoolean(GetSetting(userSettings, "CheckThemeUpdates", true));
            selectedLanguages = Convert.ToInt32(GetSetting(userSettings, "SelectedLanguages", 0));
            UseThreading.IsChecked = Convert.ToBoolean(GetSetting(userSettings, "UseThreading", false));
            txtBoxThreadCount.Text = Convert.ToString(GetSetting(userSettings, "ThreadCount", 4));
            txtBoxViewLimit.Text = Convert.ToString(GetSetting(userSettings, "ViewLimit", 0));
            txtBoxThemeButtonHeightMargin.Text = Convert.ToString(GetSetting(userSettings, "ThemeButtonHeightMargin", "15"));
            textColorA = Convert.ToInt32(GetSetting(userSettings, "ThemeTextColorA", -1));
            textColorR = Convert.ToInt32(GetSetting(userSettings, "ThemeTextColorR", -1));
            textColorG = Convert.ToInt32(GetSetting(userSettings, "ThemeTextColorG", -1));
            textColorB = Convert.ToInt32(GetSetting(userSettings, "ThemeTextColorB", -1));
            textColorSecondA = Convert.ToInt32(GetSetting(userSettings, "ThemeTextColorSecondA", -1));
            textColorSecondR = Convert.ToInt32(GetSetting(userSettings, "ThemeTextColorSecondR", -1));
            textColorSecondG = Convert.ToInt32(GetSetting(userSettings, "ThemeTextColorSecondG", -1));
            textColorSecondB = Convert.ToInt32(GetSetting(userSettings, "ThemeTextColorSecondB", -1));
            defaultForegroundBrush = butChangeTextColor.Foreground;
            if(textColorA != -1)
                butChangeTextColor.Foreground = new SolidColorBrush(Color.FromArgb((byte)textColorA, (byte)textColorR, (byte)textColorG, (byte)textColorB));
            if(textColorSecondA != -1)
                butChangeTextColorSecond.Foreground = new SolidColorBrush(Color.FromArgb((byte)textColorSecondA, (byte)textColorSecondR, (byte)textColorSecondG, (byte)textColorSecondB));
        }

        private void ListPickerTheme_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && !firstInit)
            {
                if (((String)e.AddedItems[0]) != selectedTheme)
                {
                    saveTheme = 1;
                    selectedTheme = (String)e.AddedItems[0];
                    IsolatedStorageSettings userSettings = IsolatedStorageSettings.ApplicationSettings;
                    userSettings["Theme"] = selectedTheme;
                    userSettings.Save();
                }
            }
        }

        private void butClearCache_Click(object sender, RoutedEventArgs e)
        {
            IsolatedStorageSettings userSettings = IsolatedStorageSettings.ApplicationSettings;
            userSettings["LastCheck"] = "0";
            userSettings.Save();
        }

        private void ApplicationBarIconButton_Click_1(object sender, EventArgs e)
        {
            IsolatedStorageSettings userSettings = IsolatedStorageSettings.ApplicationSettings;
            userSettings["CacheDuration"] = txtBoxCacheDuration.Text;
            userSettings["DelayDuration"] = txtBoxDelayDuration.Text;
            userSettings["SoundFeedback"] = Convert.ToInt32(SoundFeedback.IsChecked);
            userSettings["HapticFeedback"] = Convert.ToInt32(HapticFeedback.IsChecked);
            userSettings["DefaultSIP"] = Convert.ToInt32(DefaultSIP.IsChecked);
            userSettings["OnlyPhones"] = Convert.ToInt32(OnlyPhones.IsChecked);
            userSettings["CallHistory"] = Convert.ToInt32(CallHistory.IsChecked);
            userSettings["CallHistoryFilter"] = Convert.ToInt32(CallHistoryFilter.IsChecked);
            userSettings["CheckUpdates"] = Convert.ToBoolean(CheckUpdates.IsChecked);
            userSettings["CheckThemeUpdates"] = Convert.ToBoolean(CheckThemeUpdates.IsChecked);
            userSettings["UseThreading"] = Convert.ToBoolean(UseThreading.IsChecked);
            userSettings["ThemeTextColorA"] = textColorA;
            userSettings["ThemeTextColorR"] = textColorR;
            userSettings["ThemeTextColorG"] = textColorG;
            userSettings["ThemeTextColorB"] = textColorB;
            userSettings["ThemeTextColorSecondA"] = textColorSecondA;
            userSettings["ThemeTextColorSecondR"] = textColorSecondR;
            userSettings["ThemeTextColorSecondG"] = textColorSecondG;
            userSettings["ThemeTextColorSecondB"] = textColorSecondB;
            int threadCount = 4;
            try
            {
                threadCount = Convert.ToInt32(txtBoxThreadCount.Text);
                if (threadCount > 20)
                    threadCount = 20;
                if (threadCount < 1)
                    threadCount = 1;
            }
            catch
            {
            }
            userSettings["ThreadCount"] = threadCount;

            int viewLimit = 0;
            try
            {
                viewLimit = Convert.ToInt32(txtBoxViewLimit.Text);
                if (viewLimit != 0 && viewLimit < 16)
                    viewLimit = 16;
            }
            catch
            {
            }
            userSettings["ViewLimit"] = viewLimit;

            int buttonHeightMargin = 15;
            try
            {
                buttonHeightMargin = Convert.ToInt32(txtBoxThemeButtonHeightMargin.Text);
            }
            catch
            {
            }
            userSettings["ThemeButtonHeightMargin"] = buttonHeightMargin;

            if (ListPickerLanguages.SelectedItems != null)
            {
                foreach (String languages in ListPickerLanguages.SelectedItems)
                    selectedLanguages |= (int)LanguageHelper.StringToLang(languages);
            }
            else
                selectedLanguages = 0;
            userSettings["SelectedLanguages"] = selectedLanguages;
            if (saveTheme == 1)
                userSettings["Theme"] = selectedTheme;
            userSettings.Save();

            // Navigate to the new page
            NavigationService.Navigate(new Uri("/MainPage.xaml?save=1&saveTheme=" + saveTheme, UriKind.Relative));
        }

        private void ApplicationBarIconButton_Click(object sender, EventArgs e)
        {
            // Navigate to the new page
            NavigationService.Navigate(new Uri("/MainPage.xaml?save=0", UriKind.Relative));
        }

        private readonly Key[] numeric = new Key[] {Key.Back, Key.NumPad0, Key.NumPad1, Key.NumPad2, Key.NumPad3, Key.NumPad4,
        Key.NumPad5, Key.NumPad6, Key.NumPad7, Key.NumPad8, Key.NumPad9 };
        private bool languageSelectionChanged = false;

        private void NumberCheckTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            // handles non numeric
            if (Array.IndexOf(numeric, e.Key) == -1)
            {
                e.Handled = true;
            }
        }

        private void AdMobAdapter_GotAdResponse(object sender, Service4u2.Common.EventArgs<MoAds.AdResponse> e)
        {
            Console.WriteLine();
        }

        private void AdMobAdapter_GotError(object sender, Service4u2.Common.EventArgs<Exception> e)
        {
            Console.WriteLine();
        }

        private string SummarizeItems(System.Collections.IList items)
        {
            if (languageSelectionChanged)
            {
                if (items != null && items.Count > 0)
                {
                    selectedLanguages = 0;
                    string summarizedString = "";
                    for (int i = 0; i < items.Count; i++)
                    {
                        String item = (string)items[i];

                        selectedLanguages |= (int)LanguageHelper.StringToLang(item);
                        summarizedString += item;

                        // If not last item, add a comma to seperate them
                        if (i != items.Count - 1)
                            summarizedString += ",";
                    }

                    return summarizedString;
                }
                else
                    return "No language addon";
            }
            else
            {
                string summarizedString = "";
                foreach (LANG l in LangArr)
                {
                    if ((((int)l) & selectedLanguages) > 0)
                    {
                        summarizedString += LanguageHelper.LANGToString(l) + ",";
                    }
                }
                if (summarizedString != "")
                    return summarizedString.Remove(summarizedString.Length - 1);
                else
                    return "No language addon";
            }
        }

        private void ListPickerLanguages_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            languageSelectionChanged = true;
        }

        private void ChangeLog_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(SmartDialer.ChangeLog.GetChangeLog());
        }

        private void butChooseBackground_Click(object sender, RoutedEventArgs e)
        {
            PhotoChooserTask pct = new PhotoChooserTask();
            pct.Completed += new EventHandler<PhotoResult>(pct_Completed);
            pct.ShowCamera = true;
            pct.Show();
        }

        void pct_Completed(object sender, PhotoResult e)
        {
            if (e.TaskResult == TaskResult.OK)
            {
                String FileName = "ThemeBackground.png";
                using (IsolatedStorageFile myIsolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    if (myIsolatedStorage.FileExists(FileName))
                    {
                        myIsolatedStorage.DeleteFile(FileName);
                    }
                    using (IsolatedStorageFileStream fileStream = new IsolatedStorageFileStream(FileName, FileMode.Create, myIsolatedStorage))
                    {
                        using (BinaryWriter writer = new BinaryWriter(fileStream))
                        {
                            Stream resourceStream = e.ChosenPhoto;
                            long length = resourceStream.Length;
                            byte[] buffer = new byte[32];
                            int readCount = 0;
                            using (BinaryReader reader = new BinaryReader(e.ChosenPhoto))
                            {
                                // read file in chunks in order to reduce memory consumption and increase performance
                                while (readCount < length)
                                {
                                    int actual = reader.Read(buffer, 0, buffer.Length);
                                    readCount += actual;
                                    writer.Write(buffer, 0, actual);
                                }
                            }
                        }
                    }
                }
                App.ThemeBackground = new MemoryStream();
            }
        }

        private void butClearBackground_Click(object sender, RoutedEventArgs e)
        {
            if(App.ThemeBackground != null)
                App.ThemeBackground.Dispose();

            App.ThemeBackground = null;

            String FileName = "ThemeBackground.png";
            using (IsolatedStorageFile myIsolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (myIsolatedStorage.FileExists(FileName))
                {
                    myIsolatedStorage.DeleteFile(FileName);
                }
            }
        }

        private void butChangeTextColor_Click(object sender, RoutedEventArgs e)
        {
            textColorA = colorPicker.Color.A;
            textColorR = colorPicker.Color.R;
            textColorG = colorPicker.Color.G;
            textColorB = colorPicker.Color.B;
            butChangeTextColor.Foreground = new SolidColorBrush(Color.FromArgb((byte)textColorA, (byte)textColorR, (byte)textColorG, (byte)textColorB));
        }

        private void butClearTextColor_Click(object sender, RoutedEventArgs e)
        {
            textColorA = -1;
            butChangeTextColor.Foreground = defaultForegroundBrush;
        }

        private void butChangeTextColorSecond_Click(object sender, RoutedEventArgs e)
        {
            textColorSecondA = colorPicker.Color.A;
            textColorSecondR = colorPicker.Color.R;
            textColorSecondG = colorPicker.Color.G;
            textColorSecondB = colorPicker.Color.B;
            butChangeTextColorSecond.Foreground = new SolidColorBrush(Color.FromArgb((byte)textColorSecondA, (byte)textColorSecondR, (byte)textColorSecondG, (byte)textColorSecondB));
        }

        private void butClearTextColorSecond_Click(object sender, RoutedEventArgs e)
        {
            textColorSecondA = -1;
            butChangeTextColorSecond.Foreground = defaultForegroundBrush;
        }
    }

    public class Model : INotifyPropertyChanged
    {
        public ObservableCollection<String> Themes { get; private set; }
        public ObservableCollection<String> Languages { get; private set; }
        public String Version { get; private set; }

        public Model()
        {
            List<String> themeList = new List<String>(new String[] { "Retro", "HtcRest", "Metro", "adiliyo", "weeezel-whiteone", "RU-Metro", "RU-Htc-Variant"});
            if (!DesignerProperties.IsInDesignTool)
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
                themeList.AddRange(downloadedThemes.Split(','));
            }
            Themes = new ObservableCollection<String>();
            foreach (String t in themeList)
                Themes.Add(t);
            Languages = LanguageHelper.GetLanguageObservableCollection();

            try
            {
                Version = System.Reflection.Assembly.GetExecutingAssembly().FullName.Split('=')[1].Split(',')[0];
            }
            catch
            {
                Version = "";
            }
        }

        private void InvokePropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (null != handler)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
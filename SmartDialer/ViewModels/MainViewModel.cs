using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Collections.ObjectModel;

using Microsoft.Phone.UserData;
using System.Globalization;
using System.Threading;
using System.IO;
using Microsoft.Phone.Controls;
using System.IO.IsolatedStorage;
using Microsoft.Practices.Prism.Commands;

namespace SmartDialer
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public SolidColorBrush color1;
        public SolidColorBrush color2;
        public SolidColorBrush customColor;
        public SolidColorBrush customColorSecond;

        Int32 textColorA, textColorR, textColorG, textColorB = -1;
        Int32 textColorSecondA, textColorSecondR, textColorSecondG, textColorSecondB = -1;

        public MainViewModel()
        {
            //this.Items = new ObservableCollection<ItemViewModel>();
            this.Items = new CustomViewModel();
            this.ItemsCache = new CustomViewModel();
            //ApplySettings();
            Visibility darkBackgroundVisibility = (Visibility)Application.Current.Resources["PhoneDarkThemeVisibility"];
            if (darkBackgroundVisibility == Visibility.Visible)
            {
                color1 = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
                color2 = new SolidColorBrush(Color.FromArgb(153, 255, 255, 255));
            }
            else
            {
                color1 = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0));
                color2 = new SolidColorBrush(Color.FromArgb(153, 0, 0, 0));
            }

            fetchMoreDataCommand = new DelegateCommand(
            () =>
            {
                if (ItemsCache.Count != 0)
                {
                    int start = Items.Count;
                    int toAdd = start + ViewLimit;
                    for (int i = start; i < toAdd && i < ItemsCache.Count; i++)
                    {
                        Items.Add(ItemsCache[i]);
                    }
                }
            });
        }

        readonly DelegateCommand fetchMoreDataCommand;

        public ICommand FetchMoreDataCommand
        {
            get
            {
                return fetchMoreDataCommand;
            }
        }

        /// <summary>
        /// A collection for ItemViewModel objects.
        /// </summary>
        //public ObservableCollection<ItemViewModel> Items { get; private set; }
        public CustomViewModel Items { get; private set; }
        public CustomViewModel ItemsCache { get; private set; }

        private ImageSource _addTheme = ResourceHelper.GetBitmap("Button/appbar.add.rest.png");
        public ImageSource AddTheme
        {
            get
            {
                return _addTheme;
            }
            set
            {
                if (value != _addTheme)
                {
                    _addTheme = value;
                    NotifyPropertyChanged("AddTheme");
                }
            }
        }

        private ImageSource _theme = ResourceHelper.GetBitmap("Themes/Retro/people.png");
        public ImageSource Theme 
        { 
            get
            {
                return _theme;
            }
            set
            {
                if (value != _theme)
                {
                    _theme = value;
                    NotifyPropertyChanged("Theme");
                }
            }
        }

        private string _sampleProperty = "Sample Runtime Property Value";
        /// <summary>
        /// Sample ViewModel property; this property is used in the view to display its value using a Binding
        /// </summary>
        /// <returns></returns>
        public string SampleProperty
        {
            get
            {
                return _sampleProperty;
            }
            set
            {
                if (value != _sampleProperty)
                {
                    _sampleProperty = value;
                    NotifyPropertyChanged("SampleProperty");
                }
            }
        }

        public bool IsDataLoaded
        {
            get;
            private set;
        }

        String input;
        public DateTime lastCheck = new DateTime(2000, 1, 1);
        public bool loaded = false;
        bool isLoading = false;
        public List<ContactDataStorage> lst = new List<ContactDataStorage>();
        public Dictionary<String, ContactDataStorage> lstLastCalls = new Dictionary<String, ContactDataStorage>();
        public Int32 cacheDuration;
        bool onlyPhones;
        public bool callHistory;
        public bool callHistoryFilter;
        public bool useThreading;
        public MainPage parentView;
        public SolidColorBrush accentBrush;
        public Int32 ViewLimit = 0;

        public void ApplySettings()
        {
            IsolatedStorageSettings userSettings = IsolatedStorageSettings.ApplicationSettings;
            if (userSettings.Contains("CacheDuration"))
                cacheDuration = Convert.ToInt32(userSettings["CacheDuration"]) * 1000;
            else
                cacheDuration = 3600;

            if (userSettings.Contains("LastCheck"))
            {
                String lastCheck = (String)userSettings["LastCheck"];
                if (lastCheck == "0")
                {
                    loaded = false;
                    LoadData("");
                }
            }

            if (userSettings.Contains("OnlyPhones"))
                onlyPhones = Convert.ToBoolean(userSettings["OnlyPhones"]);
            else
                onlyPhones = false;

            if (userSettings.Contains("CallHistory"))
                callHistory = Convert.ToBoolean(userSettings["CallHistory"]);
            else
                callHistory = true;

            if (userSettings.Contains("CallHistoryFilter"))
                callHistoryFilter = Convert.ToBoolean(userSettings["CallHistoryFilter"]);
            else
                callHistoryFilter = true;

            if (userSettings.Contains("ViewLimit"))
                ViewLimit = Convert.ToInt32(userSettings["ViewLimit"]);
            else
                ViewLimit = 0;

            if (userSettings.Contains("UseThreading"))
                useThreading = Convert.ToBoolean(userSettings["UseThreading"]);
            else
                useThreading = false;

            if (useThreading)
            {
                if (userSettings.Contains("ThreadCount"))
                {
                    threadCount = Convert.ToInt32(userSettings["ThreadCount"]);
                    if (threadCount == 0)
                        threadCount = 4;
                }
                else
                {
                    threadCount = 4;
                }

                if (tList != null)
                {
                    foreach (BackgroundWorker bwD in tList)
                    {
                        try
                        {
                            bwD.CancelAsync();
                        }
                        catch
                        {
                        }
                    }
                }
                tList = new BackgroundWorker[threadCount + 1];
                for (int i = 0; i < threadCount; i++)
                {
                    BackgroundWorker bw = new BackgroundWorker();
                    bw.WorkerSupportsCancellation = true;
                    bw.WorkerReportsProgress = false;
                    bw.DoWork += new DoWorkEventHandler(DoSearchThread);
                    bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(DoSearchThreadCompleted);
                    tList[i] = bw;
                }
                BackgroundWorker bwLastCall = new BackgroundWorker();
                bwLastCall.WorkerSupportsCancellation = true;
                bwLastCall.WorkerReportsProgress = false;
                bwLastCall.DoWork += new DoWorkEventHandler(DoSearchThreadLastCall);
                bwLastCall.RunWorkerCompleted += new RunWorkerCompletedEventHandler(DoSearchThreadCompleted);
                tList[threadCount] = bwLastCall;
            }

            if (userSettings.Contains("ThemeTextColorA"))
                textColorA = Convert.ToInt32(userSettings["ThemeTextColorA"]);
            else
                textColorA = -1;
            if (userSettings.Contains("ThemeTextColorR"))
                textColorR = Convert.ToInt32(userSettings["ThemeTextColorR"]);
            else
                textColorR = -1;
            if (userSettings.Contains("ThemeTextColorG"))
                textColorG = Convert.ToInt32(userSettings["ThemeTextColorG"]);
            else
                textColorG = -1;
            if (userSettings.Contains("ThemeTextColorB"))
                textColorB = Convert.ToInt32(userSettings["ThemeTextColorB"]);
            else
                textColorB = -1;

            if (textColorA != -1)
                customColor = new SolidColorBrush(Color.FromArgb((byte)textColorA, (byte)textColorR, (byte)textColorG, (byte)textColorB));
            else
                customColor = null;

            if (userSettings.Contains("ThemeTextColorSecondA"))
                textColorSecondA = Convert.ToInt32(userSettings["ThemeTextColorSecondA"]);
            else
                textColorSecondA = -1;
            if (userSettings.Contains("ThemeTextColorSecondR"))
                textColorSecondR = Convert.ToInt32(userSettings["ThemeTextColorSecondR"]);
            else
                textColorSecondR = -1;
            if (userSettings.Contains("ThemeTextColorSecondG"))
                textColorSecondG = Convert.ToInt32(userSettings["ThemeTextColorSecondG"]);
            else
                textColorSecondG = -1;
            if (userSettings.Contains("ThemeTextColorSecondB"))
                textColorSecondB = Convert.ToInt32(userSettings["ThemeTextColorSecondB"]);
            else
                textColorSecondB = -1;

            if (textColorSecondA != -1)
                customColorSecond = new SolidColorBrush(Color.FromArgb((byte)textColorSecondA, (byte)textColorSecondR, (byte)textColorSecondG, (byte)textColorSecondB));
            else
                customColorSecond = null;
        }

        /// <summary>
        /// Creates and adds a few ItemViewModel objects into the Items collection.
        /// </summary>
        public void LoadData(String inputA)
        {
            input = inputA;

            if (!isLoading && (!loaded || (loaded && (DateTime.Now - lastCheck).Milliseconds > cacheDuration)))
            {
                try
                {
                    isLoading = true;
                    Contacts cons = new Contacts();

                    //Identify the method that runs after the asynchronous search completes.
                    cons.SearchCompleted += new EventHandler<ContactsSearchEventArgs>(cons_SearchCompleted);

                    //Start the asynchronous search.
                    cons.SearchAsync(String.Empty, FilterKind.None, DateTime.Now.Ticks.ToString());
                    lastCheck = DateTime.Now;
                    IsolatedStorageSettings userSettings = IsolatedStorageSettings.ApplicationSettings;
                    userSettings["LastCheck"] = App.ViewModel.lastCheck.Ticks.ToString();
                    userSettings.Save();
                    System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        MainPage currentClass = (MainPage)((PhoneApplicationFrame)Application.Current.RootVisual).Content;
                        currentClass.pBarLoading.Visibility = Visibility.Visible;
                    });
                }
                catch (Exception e)
                {
                    isLoading = false;
                }
            }
            if(!useThreading)
            {
                ThreadPool.QueueUserWorkItem(o =>
                {
                    DoSearch(lst, input);
                });
            }
            else
            {
                //mMutex.WaitOne(5000);
                are.Reset();
                threadInp = input;
                threadLst = lst;
                threadLstLastCalls = lstLastCalls;
                threadItemsToSet.Clear();
                threadItemsToSetLastCall.Clear();
                for (int i = 0; i < threadCount+1; i++)
                {
                    /*ThreadPool.QueueUserWorkItem(o =>
                    {
                        DoSearchThread(i);
                    });*/
                    try
                    {
                        tList[i].RunWorkerAsync(i);
                    }
                    catch { }
                }
            }

            this.IsDataLoaded = true;
        }

        //Dictionary<String, PatternCache> patternCache = new Dictionary<String, PatternCache>();
        void DoSearch(IEnumerable<ContactDataStorage> lst, String inp)
        {
            if (lst == null || inp == null)
                return;
            /*            String patternA = { filter + "*", "*[ ]" + filter + "*", "*" + filter + "*" };
                        String patternB = { filter + "*", "*[ ]" + filter + "*", filter + "*" };*/

            ObservableCollection<ItemViewModel> itemsToSet = new ObservableCollection<ItemViewModel>();
            Glob globA = null;
            Glob globB = null;
            Glob globNumber = null;
            /*if (patternCache.ContainsKey(inp))
            {
                DateTime start = DateTime.Now;
                Debug.WriteLine("Fetch Pattern Cache");
                PatternCache pc = patternCache[inp];
                globA = pc.globA;
                globB = pc.globB;
                globNumber = pc.globNumber;
                DateTime end = DateTime.Now;
                TimeSpan t = end - start;
                Debug.WriteLine("Finished Fetching " + t.Milliseconds);
            }
            else
            {
                DateTime start = DateTime.Now;
                Debug.WriteLine("Creating Pattern Cache");*/
                char[] currInput = inp.ToCharArray();

                StringBuilder curFilter = new StringBuilder();
                foreach (char ch in currInput)
                {
                    curFilter.Append(LanguageHelper.ButtonToGlobPiece(ch));
                }
                String filter = curFilter.ToString();

                String patternNameA = filter + "*";
                String patternNameB = "*[ ]" + filter + "*";
                String patternNumber = "*" + filter + "*";

                globA = new Glob(patternNameA, false);
                globB = new Glob(patternNameB, false);
                globNumber = new Glob(patternNumber, false);
        /*        PatternCache pc = new PatternCache();
                pc.globA = globA;
                pc.globB = globB;
                pc.globNumber = globNumber;
                patternCache.Add(inp, pc);
                DateTime end = DateTime.Now;
                TimeSpan t = end - start;
                Debug.WriteLine("Finished Creating " + t.Milliseconds);
            }*/
            /*char[] currInput = inp.ToCharArray();

            StringBuilder curFilter = new StringBuilder();
            foreach (char ch in currInput)
            {
                curFilter.Append(LanguageHelper.ButtonToGlobPiece(ch));
            }
            String filter = curFilter.ToString();

            String patternNameA = filter + "*";
            String patternNameB = "*[ ]" + filter + "*";
            String patternNumber = "*" + filter + "*";

            Glob globA = new Glob(patternNameA, false);
            Glob globB = new Glob(patternNameB, false);
            Glob globNumber = new Glob(patternNumber, false);*/
            if (callHistory)
            {
                foreach (KeyValuePair<String, ContactDataStorage> cLast in lstLastCalls)
                {
                    bool hasMatch = false;
                    bool name = false;
                    if (inp != "")
                    {
                        //String dName = cLast.Value.DisplayName.ToUpper(new CultureInfo("en-US"));
                        String dName = cLast.Value.DisplayName.ToUpper(CultureInfo.CurrentCulture);
                        if (globA.IsMatch(dName))
                        {
                            hasMatch = true;
                            name = true;
                        }

                        if (!hasMatch && globB.IsMatch(dName))
                        {
                            hasMatch = true;
                            name = true;
                        }
                    }
                    else
                    {
                        hasMatch = true;
                        name = true;
                    }

                    String number = "";
                    foreach (String phone in cLast.Value.PhoneNumbers)
                    {
                        number = phone;
                        if (hasMatch || globNumber.IsMatch(number))
                        {
                            /*BitmapImage bmp = new BitmapImage();
                                    if (c.GetPicture() != null)
                                        bmp.SetSource(c.GetPicture());*/

                            SolidColorBrush colorToUse1 = customColor != null ? customColor : color1;
                            SolidColorBrush colorToUse2 = customColorSecond != null ? customColorSecond : color2;

                            ItemViewModel ivm;
                            if (inp == "")
                                ivm = new ItemViewModel() { LineOne = cLast.Value.DisplayName, LineTwo = number, Photo = null, Color1 = colorToUse1, Color2 = colorToUse2 };
                            else if (name)
                                ivm = new ItemViewModel() { LineOne = cLast.Value.DisplayName, LineTwo = number, Photo = null, Color1 = accentBrush, Color2 = colorToUse2 };
                            else
                                ivm = new ItemViewModel() { LineOne = cLast.Value.DisplayName, LineTwo = number, Photo = null, Color1 = colorToUse1, Color2 = accentBrush };
                            /*if (c.GetPicture() != null)
                            {
                                ivm.PhotoStream = new byte[c.GetPicture().Length];
                                c.GetPicture().Read(ivm.PhotoStream, 0, ivm.PhotoStream.Length);
                            }*/
                            ivm.PhotoStream = cLast.Value.Picture;
                            ivm.CallHistory = Visibility.Visible;
                            ivm.Theme = Theme;
                            //ivm.Theme = AddTheme;
                            itemsToSet.Add(ivm);
                        }
                    }
                }
            }

            foreach (ContactDataStorage c in lst)
            {
                if (callHistoryFilter && lstLastCalls.ContainsKey(c.DisplayName))
                    continue;

                bool hasMatch = false;
                bool name = false;
                if (inp != "")
                {
                    //String dName = c.DisplayName.ToUpper(new CultureInfo("en-US"));
                    String dName = c.DisplayName.ToUpper(CultureInfo.CurrentCulture);
                    if (globA.IsMatch(dName))
                    {
                        hasMatch = true;
                        name = true;
                    }

                    if (!hasMatch && globB.IsMatch(dName))
                    {
                        hasMatch = true;
                        name = true;
                    }
                }
                else
                {
                    hasMatch = true;
                    name = true;
                }

                String number = "";
                foreach (String phone in c.PhoneNumbers)
                {
                    number = phone;
                    if (hasMatch || globNumber.IsMatch(number))
                    {
                        /*BitmapImage bmp = new BitmapImage();
                                if (c.GetPicture() != null)
                                    bmp.SetSource(c.GetPicture());*/

                        SolidColorBrush colorToUse1 = customColor != null ? customColor : color1;
                        SolidColorBrush colorToUse2 = customColorSecond != null ? customColorSecond : color2;

                        ItemViewModel ivm;
                        if (inp == "")
                            ivm = new ItemViewModel() { LineOne = c.DisplayName, LineTwo = number, Photo = null, Color1 = colorToUse1, Color2 = colorToUse2 };
                        else if (name)
                            ivm = new ItemViewModel() { LineOne = c.DisplayName, LineTwo = number, Photo = null, Color1 = accentBrush, Color2 = colorToUse2 };
                        else
                            ivm = new ItemViewModel() { LineOne = c.DisplayName, LineTwo = number, Photo = null, Color1 = colorToUse1, Color2 = accentBrush };
                        /*if (c.GetPicture() != null)
                        {
                            ivm.PhotoStream = new byte[c.GetPicture().Length];
                            c.GetPicture().Read(ivm.PhotoStream, 0, ivm.PhotoStream.Length);
                        }*/
                        ivm.PhotoStream = c.Picture;
                        ivm.CallHistory = Visibility.Collapsed;
                        ivm.Theme = Theme;
                        itemsToSet.Add(ivm);
                    }
                }
            }
            System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                this.Items.Clear();
                this.ItemsCache.Clear();
                foreach (ItemViewModel ivm in itemsToSet)
                {
                    if (ivm.PhotoStream != null)
                    {
                        try
                        {
                            BitmapImage bmp = new BitmapImage();
                            MemoryStream ms = new MemoryStream(ivm.PhotoStream);
                            bmp.SetSource(ms);
                            ivm.Photo = bmp;
                        }
                        catch
                        {

                        }
                    }
                    if (ViewLimit == 0 || Items.Count < ViewLimit)
                        this.Items.Add(ivm);
                    this.ItemsCache.Add(ivm);
                }
                GC.Collect();
            });
        }

        public String threadInp;
        public IEnumerable<ContactDataStorage> threadLst;
        public Dictionary<String, ContactDataStorage> threadLstLastCalls;
        public int threadCount = 4;
        BackgroundWorker[] tList;
        List<ItemViewModel> threadItemsToSet = new List<ItemViewModel>();
        List<ItemViewModel> threadItemsToSetLastCall = new List<ItemViewModel>();
        //static volatile Mutex mMutex = new Mutex(false, "SearchFinish");
        AutoResetEvent are = new AutoResetEvent(false);
        int finishCount = 0;

        int Compare(ItemViewModel i1, ItemViewModel i2)
        {
            if (i1 == null && i2 == null)
                return 0;
            else if (i1 == null)
                return 1;
            else if (i2 == null)
                return -1;
            else if (i1.LineOne == null && i2.LineOne == null)
                return 0;
            else if (i1.LineOne == null)
                return 1;
            else if (i2.LineOne == null)
                return -1;

            return i1.LineOne.CompareTo(i2.LineOne);
        }

        void DoSearchThreadCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            finishCount++;
            if (finishCount == (threadCount + 1))
            {
                finishCount = 0;
                System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    this.Items.Clear();
                    this.ItemsCache.Clear();
                    try
                    {
                        foreach (ItemViewModel ivm in threadItemsToSetLastCall)
                        {
                            if (ivm.PhotoStream != null)
                            {
                                try
                                {
                                    BitmapImage bmp = new BitmapImage();
                                    MemoryStream ms = new MemoryStream(ivm.PhotoStream);
                                    bmp.SetSource(ms);
                                    ivm.Photo = bmp;
                                }
                                catch
                                {

                                }
                            }

                            ivm.Theme = Theme;
                            this.Items.Add(ivm);
                        }

                        if (threadItemsToSet.Count > 0)
                            threadItemsToSet.Sort(new Comparison<ItemViewModel>(Compare));
                        foreach (ItemViewModel ivm in threadItemsToSet)
                        {
                            if (ivm.PhotoStream != null)
                            {
                                try
                                {
                                    BitmapImage bmp = new BitmapImage();
                                    MemoryStream ms = new MemoryStream(ivm.PhotoStream);
                                    bmp.SetSource(ms);
                                    ivm.Photo = bmp;
                                }
                                catch
                                {

                                }
                            }
                            ivm.Theme = Theme;
                            if (ViewLimit == 0 || Items.Count < ViewLimit)
                                this.Items.Add(ivm);
                            this.ItemsCache.Add(ivm);
                        }
                        GC.Collect();
                        //mMutex.ReleaseMutex();
                        are.Set();
                    }
                    catch { GC.Collect(); are.Set(); }
                });
            }
        }

        void DoSearchThreadLastCall(object sender, DoWorkEventArgs e)
        {
            if (callHistory)
            {
                String inp = threadInp;
                Dictionary<String, ContactDataStorage> lstLastCalls = threadLstLastCalls;
                //int val = (int)threadVal;
                int val = (int)e.Argument;
                if (lst == null || inp == null)
                    return;
                /*            String patternA = { filter + "*", "*[ ]" + filter + "*", "*" + filter + "*" };
                            String patternB = { filter + "*", "*[ ]" + filter + "*", filter + "*" };*/

                ObservableCollection<ItemViewModel> itemsToSet = new ObservableCollection<ItemViewModel>();
                char[] currInput = inp.ToCharArray();

                StringBuilder curFilter = new StringBuilder();
                foreach (char ch in currInput)
                {
                    curFilter.Append(LanguageHelper.ButtonToGlobPiece(ch));
                }
                String filter = curFilter.ToString();

                String patternNameA = filter + "*";
                String patternNameB = "*[ ]" + filter + "*";
                String patternNumber = "*" + filter + "*";

                Glob globA = new Glob(patternNameA, false);
                Glob globB = new Glob(patternNameB, false);
                Glob globNumber = new Glob(patternNumber, false);

                foreach (KeyValuePair<String, ContactDataStorage> cLast in lstLastCalls)
                {
                    bool hasMatch = false;
                    bool name = false;
                    if (inp != "")
                    {
                        //String dName = cLast.Value.DisplayName.ToUpper(new CultureInfo("en-US"));
                        String dName = cLast.Value.DisplayName.ToUpper(CultureInfo.CurrentCulture);
                        if (globA.IsMatch(dName))
                        {
                            hasMatch = true;
                            name = true;
                        }

                        if (!hasMatch && globB.IsMatch(dName))
                        {
                            hasMatch = true;
                            name = true;
                        }
                    }
                    else
                    {
                        hasMatch = true;
                        name = true;
                    }

                    String number = "";
                    foreach (String phone in cLast.Value.PhoneNumbers)
                    {
                        number = phone;
                        if (hasMatch || globNumber.IsMatch(number))
                        {
                            /*BitmapImage bmp = new BitmapImage();
                                    if (c.GetPicture() != null)
                                        bmp.SetSource(c.GetPicture());*/

                            SolidColorBrush colorToUse1 = customColor != null ? customColor : color1;
                            SolidColorBrush colorToUse2 = customColorSecond != null ? customColorSecond : color2;

                            ItemViewModel ivm;
                            if (inp == "")
                                ivm = new ItemViewModel() { LineOne = cLast.Value.DisplayName, LineTwo = number, Photo = null, Color1 = colorToUse1, Color2 = colorToUse2 };
                            else if (name)
                                ivm = new ItemViewModel() { LineOne = cLast.Value.DisplayName, LineTwo = number, Photo = null, Color1 = accentBrush, Color2 = colorToUse2 };
                            else
                                ivm = new ItemViewModel() { LineOne = cLast.Value.DisplayName, LineTwo = number, Photo = null, Color1 = colorToUse1, Color2 = accentBrush };
                            /*if (c.GetPicture() != null)
                            {
                                ivm.PhotoStream = new byte[c.GetPicture().Length];
                                c.GetPicture().Read(ivm.PhotoStream, 0, ivm.PhotoStream.Length);
                            }*/
                            ivm.PhotoStream = cLast.Value.Picture;
                            ivm.CallHistory = Visibility.Visible;
                            itemsToSet.Add(ivm);
                        }
                    }
                }
                foreach (ItemViewModel ivm in itemsToSet)
                {
                    threadItemsToSetLastCall.Add(ivm);
                }  
            }            
        }

        void DoSearchThread(object sender, DoWorkEventArgs e)
        {
            String inp = threadInp;
            IEnumerable<ContactDataStorage> lst = threadLst;
            //int val = (int)threadVal;
            int val = (int)e.Argument;
            if (lst == null || inp == null)
                return;
            /*            String patternA = { filter + "*", "*[ ]" + filter + "*", "*" + filter + "*" };
                        String patternB = { filter + "*", "*[ ]" + filter + "*", filter + "*" };*/

            ObservableCollection<ItemViewModel> itemsToSet = new ObservableCollection<ItemViewModel>();
            char[] currInput = inp.ToCharArray();

            StringBuilder curFilter = new StringBuilder();
            foreach (char ch in currInput)
            {
                curFilter.Append(LanguageHelper.ButtonToGlobPiece(ch));
            }
            String filter = curFilter.ToString();

            String patternNameA = filter + "*";
            String patternNameB = "*[ ]" + filter + "*";
            String patternNumber = "*" + filter + "*";

            Glob globA = new Glob(patternNameA, false);
            Glob globB = new Glob(patternNameB, false);
            Glob globNumber = new Glob(patternNumber, false);

            List<ContactDataStorage> lstA = new List<ContactDataStorage>(lst);
            for(int j = val; j < lstA.Count; j = j+threadCount)
            //foreach (ContactDataStorage c in lst)
            {
                ContactDataStorage c = lstA[j];
                if (callHistoryFilter && threadLstLastCalls.ContainsKey(c.DisplayName))
                    continue;

                bool hasMatch = false;
                bool name = false;
                if (inp != "")
                {
                    //String dName = c.DisplayName.ToUpper(new CultureInfo("en-US"));
                    String dName = c.DisplayName.ToUpper(CultureInfo.CurrentCulture);
                    if (globA.IsMatch(dName))
                    {
                        hasMatch = true;
                        name = true;
                    }

                    if (!hasMatch && globB.IsMatch(dName))
                    {
                        hasMatch = true;
                        name = true;
                    }
                }
                else
                {
                    hasMatch = true;
                    name = true;
                }

                String number = "";
                foreach (String phone in c.PhoneNumbers)
                {
                    number = phone;
                    if (hasMatch || globNumber.IsMatch(number))
                    {
                        /*BitmapImage bmp = new BitmapImage();
                                if (c.GetPicture() != null)
                                    bmp.SetSource(c.GetPicture());*/

                        SolidColorBrush colorToUse1 = customColor != null ? customColor : color1;
                        SolidColorBrush colorToUse2 = customColorSecond != null ? customColorSecond : color2;

                        ItemViewModel ivm;
                        if (inp == "")
                            ivm = new ItemViewModel() { LineOne = c.DisplayName, LineTwo = number, Photo = null, Color1 = colorToUse1, Color2 = colorToUse2 };
                        else if (name)
                            ivm = new ItemViewModel() { LineOne = c.DisplayName, LineTwo = number, Photo = null, Color1 = accentBrush, Color2 = colorToUse2 };
                        else
                            ivm = new ItemViewModel() { LineOne = c.DisplayName, LineTwo = number, Photo = null, Color1 = colorToUse1, Color2 = accentBrush };
                        /*if (c.GetPicture() != null)
                        {
                            ivm.PhotoStream = new byte[c.GetPicture().Length];
                            c.GetPicture().Read(ivm.PhotoStream, 0, ivm.PhotoStream.Length);
                        }*/
                        ivm.PhotoStream = c.Picture;
                        ivm.CallHistory = Visibility.Collapsed;
                        itemsToSet.Add(ivm);
                    }
                }
            }

            foreach (ItemViewModel ivm in itemsToSet)
            {
                threadItemsToSet.Add(ivm);
            }            
        }

        Object contactAddLockObject = new Object();
        public void ContactAddSnychronized(ContactDataStorage cds)
        {
            lock (contactAddLockObject)
            {
                lst.Add(cds);
            }
        }

        public void ContactSetSynchronized(List<ContactDataStorage> cdsList)
        {
            lock (contactAddLockObject)
            {
                lst = cdsList;
            }
        }

        void cons_SearchCompleted(object sender, ContactsSearchEventArgs e)
        {
            loaded = true;
            isLoading = false;
            lst.Clear();
            foreach (Contact c in e.Results)
            {
                ContactDataStorage cds = new ContactDataStorage();
                cds.DisplayName = c.DisplayName;
                List<String> lstP = new List<String>();
                foreach (ContactPhoneNumber cpn in c.PhoneNumbers)
                {
                    /*String phoneNumberTrimmed = cpn.PhoneNumber.Trim().Replace(" ", "").Replace("(", "").Replace(")", "").Replace("+", "");
                    lstP.Add(phoneNumberTrimmed);*/
                    lstP.Add(cpn.PhoneNumber);
                }

                cds.PhoneNumbers = lstP.ToArray();
                if (onlyPhones && cds.PhoneNumbers.Length == 0)
                    continue;

                if (c.GetPicture() != null)
                {
                    try
                    {
                        cds.Picture = new byte[c.GetPicture().Length];
                        c.GetPicture().Read(cds.Picture, 0, cds.Picture.Length);
                    }
                    catch
                    {
                        cds.Picture = null;
                    }
                }
                //lst.Add(cds);
                ContactAddSnychronized(cds);
            }
            System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                MainPage currentClass = (MainPage)((PhoneApplicationFrame)Application.Current.RootVisual).Content;
                currentClass.pBarLoading.Visibility = Visibility.Collapsed;
            });
            if (!useThreading)
                DoSearch(lst, input);
            else
            {
                //mMutex.WaitOne(5000);
                are.Reset();
                threadInp = input;
                threadLst = lst;
                threadLstLastCalls = lstLastCalls;
                threadItemsToSet.Clear();
                threadItemsToSetLastCall.Clear();
                for (int i = 0; i < threadCount + 1; i++)
                {
                    /*ThreadPool.QueueUserWorkItem(o =>
                    {
                        DoSearchThread(i);
                    });*/
                    try
                    {
                        tList[i].RunWorkerAsync(i);
                    }
                    catch { }
                }
            }
        }

        public ContactDataStorage GetContactDataStorageByName(String DisplayName)
        {
            ContactDataStorage ret;
            if (lstLastCalls.TryGetValue(DisplayName, out ret))
                return ret;

            foreach (ContactDataStorage cds in lst)
            {
                if (cds.DisplayName == DisplayName)
                    return cds;
            }
            return null;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (null != handler)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
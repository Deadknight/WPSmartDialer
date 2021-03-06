﻿using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Media.Imaging;
using System.IO;
using System.Windows.Resources;
using System.IO.IsolatedStorage;

namespace SmartDialer
{
    public class ResourceHelper
    {

        public static string ExecutingAssemblyName
        {
            get
            {
                string name = System.Reflection.Assembly.GetExecutingAssembly().FullName;
                return name.Substring(0, name.IndexOf(','));
            }
        }

        public static Stream GetStream(string relativeUri, string assemblyName)
        {
            StreamResourceInfo res = Application.GetResourceStream(new Uri(assemblyName + ";component/" + relativeUri, UriKind.Relative));
            if (res == null)
            {
                res = Application.GetResourceStream(new Uri(relativeUri, UriKind.Relative));
            }
            if (res != null)
            {
                return res.Stream;
            }
            else
            {
                return null;
            }
        }

        public static Stream GetStream(string relativeUri)
        {
            return GetStream(relativeUri, ExecutingAssemblyName);
        }

        public static BitmapImage GetBitmap(string relativeUri, string assemblyName)
        {
            Stream s = GetStream(relativeUri, assemblyName);
            if (s == null) return null;
            using (s)
            {
                BitmapImage bmp = new BitmapImage();
                bmp.SetSource(s);
                return bmp;
            }
        }

        public static BitmapImage GetBitmap(string relativeUri)
        {
            String resourcePath = "Themes/" + relativeUri;
            BitmapImage bi = GetBitmap(resourcePath, ExecutingAssemblyName);
            if (bi == null)
            {
                try
                {
                    using (IsolatedStorageFile myIsolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
                    {
                        using (IsolatedStorageFileStream fileStream = myIsolatedStorage.OpenFile(relativeUri, FileMode.Open, FileAccess.Read))
                        {
                            if (fileStream != null)
                            {
                                bi = new BitmapImage();
                                bi.SetSource(fileStream);
                            }
                        }
                    }
                }
                catch
                {
                    bi = GetBitmap(relativeUri, ExecutingAssemblyName);
                }
            }
            return bi;
        }

        public static string GetString(string relativeUri, string assemblyName)
        {
            Stream s = GetStream(relativeUri, assemblyName);
            if (s == null) return null;
            using (StreamReader reader = new StreamReader(s))
            {
                return reader.ReadToEnd();
            }
        }

        public static string GetString(string relativeUri)
        {
            return GetString(relativeUri, ExecutingAssemblyName);
        }

        public static FontSource GetFontSource(string relativeUri, string assemblyName)
        {
            Stream s = GetStream(relativeUri, assemblyName);
            if (s == null) return null;
            using (s)
            {
                return new FontSource(s);
            }
        }

        public static FontSource GetFontSource(string relativeUri)
        {
            return GetFontSource(relativeUri, ExecutingAssemblyName);
        }

        public static object GetXamlObject(string relativeUri, string assemblyName)
        {
            string str = GetString(relativeUri, assemblyName);
            if (str == null) return null;
            object obj = System.Windows.Markup.XamlReader.Load(str);
            return obj;
        }

        public static object GetXamlObject(string relativeUri)
        {
            return GetXamlObject(relativeUri, ExecutingAssemblyName);
        }

    }
}

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.IO;

namespace SmartDialer
{
    public class ItemViewModel : INotifyPropertyChanged
    {
        private string _lineOne;
        /// <summary>
        /// Sample ViewModel property; this property is used in the view to display its value using a Binding.
        /// </summary>
        /// <returns></returns>
        public string LineOne
        {
            get
            {
                return _lineOne;
            }
            set
            {
                if (value != _lineOne)
                {
                    _lineOne = value;
                    NotifyPropertyChanged("LineOne");
                }
            }
        }

        private string _lineTwo;
        /// <summary>
        /// Sample ViewModel property; this property is used in the view to display its value using a Binding.
        /// </summary>
        /// <returns></returns>
        public string LineTwo
        {
            get
            {
                return _lineTwo;
            }
            set
            {
                if (value != _lineTwo)
                {
                    _lineTwo = value;
                    NotifyPropertyChanged("LineTwo");
                }
            }
        }

        private SolidColorBrush _color1;
        /// <summary>
        /// Sample ViewModel property; this property is used in the view to display its value using a Binding.
        /// </summary>
        /// <returns></returns>
        public SolidColorBrush Color1
        {
            get
            {
                return _color1;
            }
            set
            {
                if (value != _color1)
                {
                    _color1 = value;
                    NotifyPropertyChanged("Color1");
                }
            }
        }

        private SolidColorBrush _color2;
        /// <summary>
        /// Sample ViewModel property; this property is used in the view to display its value using a Binding.
        /// </summary>
        /// <returns></returns>
        public SolidColorBrush Color2
        {
            get
            {
                return _color2;
            }
            set
            {
                if (value != _color2)
                {
                    _color2 = value;
                    NotifyPropertyChanged("Color2");
                }
            }
        }

        private ImageSource _photo;
        /// <summary>
        /// Sample ViewModel property; this property is used in the view to display its value using a Binding.
        /// </summary>
        /// <returns></returns>
        public ImageSource Photo
        {
            get
            {
                return _photo;
            }
            set
            {
                if (value != _photo)
                {
                    _photo = value;
                    NotifyPropertyChanged("Photo");
                }
            }
        }

        private ImageSource _theme;
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

        private byte[] _photoStream;
        public byte[] PhotoStream
        {
            get
            {
                return _photoStream;
            }
            set
            {
                if (value != _photoStream)
                {
                    _photoStream = value;
                    NotifyPropertyChanged("PhotoStream");
                }
            }
        }

        private Visibility _callHistory;
        public Visibility CallHistory
        {
            get
            {
                return _callHistory;
            }
            set
            {
                if (value != _callHistory)
                {
                    _callHistory = value;
                    NotifyPropertyChanged("CallHistory");
                }
            }
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
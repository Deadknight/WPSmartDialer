using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Runtime.Serialization;

namespace SmartDialer
{
    [DataContract]
    public class ContactDataStorage
    {
        [DataMember]
        public String DisplayName { get; set; }
        [DataMember]
        public byte[] Picture { get; set; }
        [DataMember]
        public String[] PhoneNumbers { get; set; }
        [DataMember]
        public UInt64 LastCallTime { get; set; }
    }
}

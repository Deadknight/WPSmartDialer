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
using System.IO.IsolatedStorage;
using System.Collections.Generic;
using System.Xml;
using System.IO;

namespace SmartDialer
{
    public class ContactDataSaver<MyDataType>
    {
        private const string TargetFolderName = "ContactData";
        private DataContractSerializer _mySerializer;
        private IsolatedStorageFile _isoFile;
        IsolatedStorageFile IsoFile
        {
            get
            {
                if (_isoFile == null)
                    _isoFile = System.IO.IsolatedStorage.
                                IsolatedStorageFile.GetUserStoreForApplication();
                return _isoFile;
            }
        }

        public ContactDataSaver()
        {
            _mySerializer = new DataContractSerializer(typeof(List<MyDataType>));
        }

        public void SaveMyData(List<MyDataType> sourceData, String targetFileName)
        {
            string TargetFileName = String.Format("{0}/{1}.dat",
                                           TargetFolderName, targetFileName);
            if (!IsoFile.DirectoryExists(TargetFolderName))
                IsoFile.CreateDirectory(TargetFolderName);
            try
            {
                using (var targetFile = IsoFile.CreateFile(TargetFileName))
                {
                    _mySerializer.WriteObject(targetFile, sourceData);
                }
            }
            catch (Exception e)
            {
                IsoFile.DeleteFile(TargetFileName);
            }


        }

        public List<MyDataType> LoadMyData(string sourceName)
        {
            List<MyDataType> retVal = new List<MyDataType>();
            string TargetFileName = String.Format("{0}/{1}.dat", TargetFolderName, sourceName);
            try
            {
                
                if (IsoFile.FileExists(TargetFileName))
                    using (var sourceStream =
                            IsoFile.OpenFile(TargetFileName, FileMode.Open))
                    {
                        retVal = (List<MyDataType>)_mySerializer.ReadObject(sourceStream);
                    }
            }
            catch (Exception e)
            {
                IsoFile.DeleteFile(TargetFileName);
            }
            return retVal;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System.Text;

namespace WindowsPhonePostClient
{
    /// <summary>
    /// Provides common methods for sending data to and receiving data from an HTTP POST web request.
    /// </summary>
    public class PostClient
    {
        #region Members

        /// <summary>
        /// Post data query string.
        /// </summary>
        StringBuilder _postData = new StringBuilder();

        #endregion

        #region Events

        /// <summary>
        /// Event handler for DownloadStringCompleted event.
        /// </summary>
        /// <param name="sender">Object firing the event.</param>
        /// <param name="e">Argument holding the data downloaded.</param>
        public delegate void DownloadStringCompletedHandler(object sender, DownloadStringCompletedEventArgs e);

        /// <summary>
        /// Occurs when an asynchronous resource-download operation is completed.
        /// </summary>
        public event DownloadStringCompletedHandler DownloadStringCompleted;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new instance of PostClient with the specified parameters.
        /// </summary>
        /// <param name="data">POST parameters in string format. Valid string format is something like "id=120&amp;name=John".</param>
        public PostClient(string parameters)
        {
            _postData.Append(parameters);
        }

        /// <summary>
        /// Creates a new instance of PostClient with the specified parameters.
        /// </summary>
        /// <param name="parameters">POST parameters as a list of string. Valid list elements are "id=120" and "name=John".</param>
        public PostClient(IList<string> parameters)
        {
            foreach (var element in parameters)
            {
                _postData.Append(string.Format("{0}&", element));
            }
        }

        /// <summary>
        /// Creates a new instance of PostClient with the specified parameters.
        /// </summary>
        /// <param name="parameters">POST parameters as a dictionary with string keys and values. Valid elements could have keys "id" and "name" with values "120" and "John" respectively.</param>
        public PostClient(IDictionary<string, object> parameters)
        {
            foreach (var pair in parameters)
            {
                _postData.Append(string.Format("{0}={1}&", pair.Key, pair.Value));
            }
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Downloads the resource at the specified Uri as a string.
        /// </summary>
        /// <param name="address">The location of the resource to be downloaded.</param>
        public void DownloadStringAsync(Uri address)
        {
            HttpWebRequest request;

            try
            {
                request = (HttpWebRequest)WebRequest.Create(address);
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";
                request.BeginGetRequestStream(new AsyncCallback(RequestReady), request);
            }
            catch
            {
                if (DownloadStringCompleted != null)
                {
                    System.Windows.Deployment.Current.Dispatcher.BeginInvoke(delegate()
                    {
                        DownloadStringCompleted(this, new DownloadStringCompletedEventArgs(new Exception("Error creating HTTP web request.")));
                    });
                }
            }
        }

        #endregion

        #region Protected methods

        void RequestReady(IAsyncResult asyncResult)
        {
            HttpWebRequest request = asyncResult.AsyncState as HttpWebRequest;

            /*using (Stream stream = request.EndGetRequestStream(asyncResult))
            {
                using (StreamWriter writer = new StreamWriter(stream))
                {
                    //writer.Write();
                    byte[] encodedBytes = Encoding.UTF8.GetBytes(_postData.ToString())
                    writer.Write(encodedBytes, 0, encodedBytes.Length);
                    writer.Flush();
                }
            }*/

            Stream stream = request.EndGetRequestStream(asyncResult);
            byte[] encodedBytes = Encoding.UTF8.GetBytes(_postData.ToString());
            stream.Write(encodedBytes, 0, encodedBytes.Length);
            stream.Flush();
            stream.Close();
            request.BeginGetResponse(new AsyncCallback(ResponseReady), request);
        }

        void ResponseReady(IAsyncResult asyncResult)
        {
            HttpWebRequest request = asyncResult.AsyncState as HttpWebRequest;
            HttpWebResponse response = null;

            try
            {
                response = (HttpWebResponse)request.EndGetResponse(asyncResult);

                string result = string.Empty;
                using (Stream responseStream = response.GetResponseStream())
                {
                    using (StreamReader reader = new StreamReader(responseStream))
                    {
                        result = reader.ReadToEnd();
                    }
                }

                if (DownloadStringCompleted != null)
                {
                    System.Windows.Deployment.Current.Dispatcher.BeginInvoke(delegate()
                    {
                        DownloadStringCompleted(this, new DownloadStringCompletedEventArgs(result));
                    });
                }
            }
            catch
            {
                if (DownloadStringCompleted != null)
                {
                    System.Windows.Deployment.Current.Dispatcher.BeginInvoke(delegate()
                    {
                        DownloadStringCompleted(this, new DownloadStringCompletedEventArgs(new Exception("Error getting HTTP web response.")));
                    });
                }
            }
        }

        #endregion
    }
}

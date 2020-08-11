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
using System.Device.Location;

namespace SmartDialer
{
    public class GpsLocationProvider
    {
        static GpsLocationProvider()
        {
            try
            {
                CurrentLocation = null;

                GeoCoordinateWatcher Provider = new GeoCoordinateWatcher();
                Provider.PositionChanged += new EventHandler
                    <GeoPositionChangedEventArgs<GeoCoordinate>>
                    (GpsLocationProvider_PositionChanged);
                Provider.Start(true);

                System.Diagnostics.Debug.WriteLine("GpsLocationProvider Position: {0}",
                                                   Provider.Position.Location);
            }
            catch (Exception exception)
            {
                System.Diagnostics.Debug.WriteLine("GpsLocationProvider Exception");
                System.Diagnostics.Debug.WriteLine(exception);
            }
        }

        private static void GpsLocationProvider_PositionChanged(object sender,
            GeoPositionChangedEventArgs<GeoCoordinate> e)
        {
            if (e.Position.Location.IsUnknown)
                CurrentLocation = null;
            else
                CurrentLocation = new GpsLocation
                {
                    Latitude = e.Position.Location.Longitude,
                    Longitude = e.Position.Location.Longitude,
                    Accuracy = e.Position.Location.HorizontalAccuracy
                };
        }

        public static GpsLocation? CurrentLocation
        {
            get;
            private set;
        }
    }

}

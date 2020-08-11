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

namespace SmartDialer
{
    internal static class ExtentionMethods
    {
        static public void RaiseEvent(this EventHandler @event, object sender, EventArgs e)
        {
            if (@event != null)
                @event(sender, e);
        }

        static public void RaiseEvent<T>(this EventHandler<T> @event, object sender, T e)
            where T : EventArgs
        {
            if (@event != null)
                @event(sender, e);
        }
    }

    internal class ErrorEventArgs : EventArgs
    {
        public ErrorEventArgs()
        {

        }
        public ErrorEventArgs(Exception ex)
        {
            Error = ex;
        }
        public Exception Error { get; set; }
    }
}

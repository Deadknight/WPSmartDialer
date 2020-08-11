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
using System.Collections.ObjectModel;
using System.Collections;
using System.Collections.Generic;

namespace SmartDialer
{
    public class CustomViewModel : ObservableCollection<ItemViewModel>/*, IList, IList<ItemViewModel>*/
    {
        /// <summary>
        /// Returns the index of an item; required method for data virtualization
        /// </summary>
        /// <param name="value">The item to find the index of</param>
        /// <returns>The index, or -1 if it doesn't exist</returns>
        public int IndexOf(object value)
        {
            if (value == null)
            {
                return -1;
            }

            int count = 0;
            ItemViewModel current = (ItemViewModel)value;
            foreach (ItemViewModel ivm in this)
            {
                if (ivm.LineOne == current.LineOne
                    && ivm.LineTwo == current.LineTwo)
                    return count;
            }

            return -1;
        }
    }
}

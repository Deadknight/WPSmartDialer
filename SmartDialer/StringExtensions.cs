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
    public static class StringExtensions
    {
        public static bool WildcardMatch(this string str, string compare, bool ignoreCase)
        {
            if (ignoreCase)
                return str.ToLower().WildcardMatch(compare.ToLower());
            else
                return str.WildcardMatch(compare);
        }

        public static bool WildcardMatch(this string str, string compare)
        {
            if (string.IsNullOrEmpty(compare))
                return str.Length == 0;
            int pS = 0;
            int pW = 0;
            int lS = str.Length;
            int lW = compare.Length;

            while (pS < lS && pW < lW && compare[pW] != '*')
            {
                char wild = compare[pW];
                if (wild != '?' && wild != str[pS])
                    return false;
                pW++;
                pS++;
            }

            int pSm = 0;
            int pWm = 0;
            while (pS < lS && pW < lW)
            {
                char wild = compare[pW];
                if (wild == '*')
                {
                    pW++;
                    if (pW == lW)
                        return true;
                    pWm = pW;
                    pSm = pS + 1;
                }
                else if (wild == '?' || wild == str[pS])
                {
                    pW++;
                    pS++;
                }
                else
                {
                    pW = pWm;
                    pS = pSm;
                    pSm++;
                }
            }
            while (pW < lW && compare[pW] == '*')
                pW++;
            return pW == lW && pS == lS;
        }
    }
}

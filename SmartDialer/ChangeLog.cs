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
    public class ChangeLog
    {
        public static String GetChangeLog()
        {
            return
@"Version 1.10.0.0
*Fixed crash on call
*Removed location services
*Added View Limit (which will burst performance try 50-100) scrolling will retrieve remaning data.

Version 1.9.0.0
*Theme for russian language (credits to SergeAv)
*Faster search
*Threaded search is enabled by default now
*Re increased delay duration
*Fixed light heme bug

Version 1.8.0.0
*Fixed resume support and fasten up initialization
*Editable layout margins for theme buttons
*Editable background for phone numbers
*Editable textcolors for phone numbers
*Fixed contact list refresh (and new config value of 3600)
*Fixed search  of 0 and 1 character
*Call history of contact will be deleted if you save
*Fixed default sip popup after call
*Fixed lots of small bugs

Version 1.7.0.0
*Added languages for smart dialer
*Search with special characters of Türkçe,Pусский,Svenska,Deutsch,Magyar,ελληνικά,český,Español,한국의,Polski,Việt
*Speeded up search initialization
*Speed up search
*Automatic theme selection on start based on background
*Fixed error on settings about theme save
*Changed default theme
*Added default sip for telephone, editable via settings.
*Fixed error caused by admob

Version 1.6.0.0
*Added call history for outgoing calls (%100)
*Added new theme from weeelzel (%100)
*Fixed contact list disappearing problem after returning from settings (%100)
*Added automatic theme updating from web.(configurable) (%100)
*Fixed ads to use google library (%100)
*Added show/hide button hide functionality (%100)
*Added clear history button (%100)
*Added save contact to sms button when call history contact is not in contact list (%100)
*Changed place of the number text (%100)
*Add context menu to delete call history (%100)

Version 1.5.0.4(On market from this version)
*Fixed crash on airplane mode
*Fixed light theme character color
*At last it's in marketplace

Version 1.5.0.3
*Fixed obfuscator problem it works now

Version 1.5.0.2
*Better error reporting
*Search result colors name or phone (Color is your theme color)
*Minor performance increase
*Changed position of number and star buttons
*Updated screenshots
*Market link is coming(waiting to be approved)
*BTTF guys, tested on my hd2 no problems

Version 1.5.0.1
*Added more exception control to avatars(i believe this is an memory issue)

Version 1.5.0.0
*Changed application icons
*Increased speed of searching
*Buttons have clearer look now
*Added error reporting (clientside only, not working fully)
*Added automatic update checking (configurable through settings, one per day)
*Fixed some minor bugs about avatar loading
*Added ads to settings page (the won't bother you on main page)
*Settings page divided to groups
*This release uses new theme sizes, please use new sizes.
*Added search on only contacts with phone numbers(this may increase speed on large contact lists, who wants to call a contact without phone number )

Version 1.4.0.1
*Added theme from adiliyo
*Added haptic and sound feedback (Configurable from settings)
*Added cell operator to title (removed my nickname)
*xboxmod please fix bluetooth drain

Version 1.4.0.0
*Added Theme support (you can send me themes, look at second post)
****Added two themes (HtcRest, Metro)
****Fixed some glitches on Retro theme
*Added Settings and About pages
****Cache Refresh Duration: This is the duration of the refresh of the contact list cache
****Button Delay Duration: This is the wait duration to make smart search after pressing button
*Version renaming to standarts
*Fixed some minor issue about searching and cache
-Next:Call history and statistics (if i have time )

Version 1.0.3.1
*Fixed scroll issue (one of the worker threads not stopping). This will also increase speed a little bit

Version 1.0.3.0:
*Fixed avatar loading
*Added contact cache for faster startup (after install first startup still slow)
*Inserted more dispatchers for type delays
*Searching and viewing is faster now.
*Changed application tile and list icons";
        }

        public static String GetLastChangeLog()
        {
            return @"Version 1.10.0.0
*Fixed crash on call
*Removed location services
*Added View Limit (which will burst performance try 50-100) scrolling will retrieve remaning data.";
        }
    }
}

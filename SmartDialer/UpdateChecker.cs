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
using Service4u2.Json;

namespace SmartDialer
{
    public class UpdateCheckerResponse
    {
        public String V { get; set; }
        public String M { get; set; }
    }

    public class UpdateChecker : BaseJsonService<UpdateCheckerResponse>
    {
        public void FetchData()
        {
            StartServiceCall("http://www.sombrenuit.org/smartdialer/update.json");
        }
    }
}

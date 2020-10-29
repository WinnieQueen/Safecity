using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Safecity
{
    public partial class ResultsPage : ContentPage
    {
        public ResultsPage()
        {
            InitializeComponent();
        }

        public void GoToInfo(object sender, System.EventArgs e)
        {
            Application.Current.MainPage.Navigation.PopToRootAsync();
        }

        public void GoToHelp(object sender, System.EventArgs e)
        {
            Application.Current.MainPage.Navigation.PushAsync(new HelpPage());
        }
    }
}

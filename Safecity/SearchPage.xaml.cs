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
    public partial class SearchPage : ContentPage
    {
        public SearchPage()
        {
            InitializeComponent();
            NavigationPage.SetHasNavigationBar(this, false);
        }

        public void GoToInfo(object sender, System.EventArgs e)
        {
            Application.Current.MainPage.Navigation.PopToRootAsync();
        }

        public void GoToResults(object sender, System.EventArgs e)
        {
            string city = GetCity();
            //Application.Current.MainPage.Navigation.PopToRootAsync();
        }
        /*{"zip_code":"08057",
         * "lat":39.979676,
         * "lng":-74.941163,
         * "city":"Moorestown",
         * "state":"NJ",
         * "timezone":  {
         *      "timezone_identifier":"America\/New_York",
         *      "timezone_abbr":"EDT",
         *      "utc_offset_sec":-14400,
         *      "is_dst":"T"
         *},
         *"acceptable_city_names":[
         *      {   "city":"Lenola",
         *          "state":"NJ"
         *      }],
         *"area_codes":[856]}
        */

        private string GetCity()
        {
            try
            {
                var client = new RestClient("https://redline-redline-zipcode.p.rapidapi.com/rest/info.json/08000057/degrees");
                var request = new RestRequest(Method.GET);
                request.AddHeader("x-rapidapi-host", "redline-redline-zipcode.p.rapidapi.com");
                request.AddHeader("x-rapidapi-key", "ebacc40a32mshdcb364465409375p1f8018jsncc41302b2150");

                IRestResponse response = client.Execute(request);
                var jObject = JObject.Parse(response.Content);
                string city = jObject.GetValue("city").ToString();
                return city;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}

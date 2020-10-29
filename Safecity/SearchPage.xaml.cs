using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using System.Net;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

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
        public void GoToHelp(object sender, System.EventArgs e)
        {
            Application.Current.MainPage.Navigation.PushAsync(new HelpPage());
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

        private string GetORI(string state, string county, string city)
        {
            using (WebClient client = new WebClient())
            {
                string htmlCode = client.DownloadString("https://www.icpsr.umich.edu/files/NACJD/ORIs/0  1oris.html");
                if (htmlCode.Contains("ASHVILLE"))
                    Console.Write(htmlCode);
            }
            return "";
        }

        private string[] GetInfo(int zip)
        {
            try
            {
                //getting the state/city from APIs
                string url = $"https://redline-redline-zipcode.p.rapidapi.com/rest/info.json/{zip}/degrees";
                Console.WriteLine(url);
                RestClient client = new RestClient(url);
                //RestRequest request = new RestRequest(Method.GET);
                //Console.WriteLine("so far so good");
                //request.AddHeader("x-rapidapi-host", "redline-redline-zipcode.p.rapidapi.com");
                //request.AddHeader("x-rapidapi-key", "ebacc40a32mshdcb364465409375p1f8018jsncc41302b2150");
                //request.AddHeader("useQueryString", "true");

                

                //IRestResponse response = client.Execute(request);
                //var jObject = JObject.Parse(response.Content);
                //string city = jObject.GetValue("city").ToString();
                //city = city.ToLower();
                //string state = jObject.GetValue("state").ToString();
                //state = state.ToLower();

                //getting the county from database
                string allInfo = Safecity.Properties.Resources.uszips;
                string pattern = "(\"${zip}\".*${state}.*{)";
                Regex r = new Regex(pattern);
                string infoString = r.Match(allInfo.ToLower()).Value;
                string[] cityInfo = infoString.Split(',');
                string county = cityInfo[cityInfo.Length - 2];
                county = county.Replace("\"", "");

                //hardcoded for test
                string state = "utah";
                string city = "huntington";
                return new string[] { state, county, city };
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
                return null;
            }
        }

        private Dictionary<string, string> GetData(int zip, int age, string sex, string race)
        {
            Dictionary<string, string> data = null;
            if (zip >= 00501 && zip <= 99950)
            {
                string[] info = GetInfo(zip);
                if (info != null)
                {
                    data = new Dictionary<string, string>();
                    string state = info[0];
                    string county = info[1];
                    string city = info[2];
                    Console.WriteLine("State: " + state + "County: " + county + "City: " + city);
                    return null;
                }
            }
            return data;
        }

        private void Submit_Clicked(object sender, EventArgs e)
        {
            try
            {
                int zip = Int32.Parse(Zip.Text);
                int age = Age.Text == null ? 0 : Int32.Parse(Age.Text);
                string sex = SexPicker.SelectedItem != null ? SexPicker.SelectedItem.ToString() : "";
                string race = RacePicker.SelectedItem != null ? RacePicker.SelectedItem.ToString() : "";
                Dictionary<string, string> results = GetData(zip, age, sex, race);
                if (results == null)
                {
                    ErrorMsg.IsVisible = true;
                }
                else
                {
                    Application.Current.MainPage.Navigation.PushAsync(new ResultsPage());
                }
            }
            catch (Exception)
            {
                ErrorMsg.IsVisible = true;
            }
        }
    }
}

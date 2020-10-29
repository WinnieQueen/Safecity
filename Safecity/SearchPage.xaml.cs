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
using AngleSharp.Html.Parser;
using AngleSharp.Html.Dom;
using AngleSharp.Dom;

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

        private List<string> GetORI(string state, string county, string city)
        {
            List<string> ORIs = null;
            string allStates = "https://www.icpsr.umich.edu/files/NACJD/ORIs/STATESoris.html";

            using (WebClient client = new WebClient())
            {
                string htmlString = client.DownloadString(allStates);
                HtmlParser parser = new HtmlParser();
                IHtmlDocument document = parser.ParseDocument(htmlString);
                IElement[] links = document.Links.Where(x => x.InnerHtml.ToLower().Contains(state)).ToArray();
                if (links != null && links.Length > 0)
                {
                    string linkPiece = links[0].GetAttribute("href");
                    string stateUrl = $"https://www.icpsr.umich.edu/files/NACJD/ORIs/{linkPiece}";
                    string stateHtml = client.DownloadString(stateUrl);
                    document = parser.ParseDocument(stateHtml);
                    links = document.Links.Where(link => link.InnerHtml.ToLower().Contains(county)).ToArray();
                    if (links != null && links.Length > 0)
                    {
                        foreach (IElement link in links)
                        {
                            string name = link.GetAttribute("href");
                            name = name.Substring(1);
                            name = name.Replace("(", "\\(");
                            name = name.Replace(")", "\\)");

                            string pattern = $"(<h3><a name=\"{name}\">(.*\\s*)*?<pre>(.*\\s*)*?CITY\\/AGENCY(.*\\s*)*?\\s*ORI7\\s*ORI9\\s*)((.*\\s*)*?(?=<\\/pre>))";
                            Regex r = new Regex(pattern);
                            string infoString = r.Match(stateHtml).Groups[5].Value;

                            string[] strings = Regex.Split(infoString, @"\s{2,}");
                            if (strings != null && strings.Length >= 3)
                            {
                                ORIs = new List<string>();
                                for (int i = 2; i < strings.Length; i += 2)
                                {
                                    Console.Write(strings[i] + ", ");
                                    ORIs.Add(strings[i]);
                                }
                                Console.WriteLine();
                            }
                        }
                    }
                }
            }
            return ORIs;
        }

        private string[] GetInfo(int zip)
        {
            //getting the city from APIs
            string url = $"https://redline-redline-zipcode.p.rapidapi.com/rest/info.json/{zip}/degrees";
            RestClient client = new RestClient(url);
            RestRequest request = new RestRequest(Method.GET);
            request.AddHeader("x-rapidapi-host", "redline-redline-zipcode.p.rapidapi.com");
            request.AddHeader("x-rapidapi-key", "ebacc40a32mshdcb364465409375p1f8018jsncc41302b2150");
            request.AddHeader("useQueryString", "true");

            IRestResponse response = client.Execute(request);
            var jObject = JObject.Parse(response.Content);
            string city = jObject.GetValue("city").ToString();
            city = city.ToLower();

            //getting the state/county from database
            string allInfo = Properties.Resources.uszips;
            string pattern = $"(\"{zip}\".*{city}.*{{)";
            Regex r = new Regex(pattern);
            string infoString = r.Match(allInfo.ToLower()).Value;
            string[] cityInfo = infoString.Split(',');
            string county = cityInfo[cityInfo.Length - 2];
            county = county.Replace("\"", "");
            county = county.ToLower();
            string state = cityInfo[cityInfo.Length - 8];
            state = state.Replace("\"", "");
            state = state.ToLower();

            return new string[] { state, county, city };
        }

        private Dictionary<string, string> GetData(int zip, int age, string sex, string race)
        {
            Dictionary<string, string> data = null;
            if (zip >= 00501 && zip <= 99950)
            {
                //hardcoded for testing
                //string[] info = { "utah", "emery", "huntington" };


                string[] info = GetInfo(zip);
                if (info != null)
                {
                    string state = info[0].ToLower();
                    string county = info[1].ToLower();
                    string city = info[2].ToLower();
                    List<string> ORIs = GetORI(state, county, city);
                    if (ORIs != null && ORIs.Count >= 1)
                    {
                        data = new Dictionary<string, string>();

                    }
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
                    //ErrorMsg.IsVisible = true;
                }
                else
                {
                    Application.Current.MainPage.Navigation.PushAsync(new ResultsPage());
                }
            }
            catch (Exception error)
            {
                Console.WriteLine(error.Message);
                Console.WriteLine(error.ToString());
                ErrorMsg.IsVisible = true;
            }
        }
    }
}

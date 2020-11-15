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
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Threading;

namespace Safecity
{
    public partial class SearchPage : ContentPage
    {
        private int counter = 0;
        string[] deathcrimes = { "homicide-offenses", "murder-and-nonnegligent-manslaughter", "negligent-manslaughter" };
        string[] assaultcrimes = { "assault-offenses", "simple-assault", "aggravated-assault" };
        string[] sexcrimes = { "sexual-assult-with-an-object", "human-trafficking-offenses", "rape", "sodomy", "sex-offenses", "sex-offenses-non-forcible" };
        string[] theftcrimes = { "motor-vehicle-theft", "burglary-breaking-and-entering", "pocket-picking", "purse-snatching", "robbery", "stolen-property-offenses", "theft-from-motor-vehicle", "theft-of-motor-vehicle-parts-or-accessories", "theft-from-motor-vehicle" };

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
                                for (int i = 2; i < strings.Length; i += 3)
                                {
                                    ORIs.Add(strings[i]);
                                }
                            }
                        }
                    }
                }
            }
            return ORIs;
        }

        private string[] GetInfo(string zip)
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

        private Dictionary<string, int> GetData(string zip, string ageRange, string sex, string race, int yearRange)
        {
            Dictionary<string, int> data = null;
            //hardcoded for testing
            string[] info = { "utah", "emery", "huntington" };
            //string[] info = GetInfo(zip);

            if (info != null)
            {
                string state = info[0].ToLower();
                string county = info[1].ToLower();
                string city = info[2].ToLower();
                List<string> ORIs = GetORI(state, county, city);
                if (ORIs != null && ORIs.Count >= 1)
                {
                    data = new Dictionary<string, int>
                    {
                        { "Murder", 0 },
                        { "Assault", 0 },
                        { "Sex Crimes", 0 },
                        { "Theft Crimes", 0 }
                    };
                    foreach (string crime in deathcrimes)
                    {
                        int amount = 0;
                        foreach (string ORI in ORIs)
                        {
                            ThreadPool.QueueUserWorkItem(o =>
                            {
                                Interlocked.Increment(ref counter);
                                int result = GetCrimeData(ORI, crime, "count", yearRange);
                                Interlocked.Add(ref amount, result);
                                Console.WriteLine(amount);
                                Interlocked.Decrement(ref counter);
                            });
                        }
                        while (counter != 1)
                        {
                            //do do do doooo :)
                        }
                        data["Murder"] += amount;
                    }
                    foreach (string crime in assaultcrimes)
                    {
                        int amount = 0;
                        foreach (string ORI in ORIs)
                        {
                            ThreadPool.QueueUserWorkItem(o =>
                            {
                                Interlocked.Increment(ref counter);
                                int result = GetCrimeData(ORI, crime, "count", yearRange);
                                Interlocked.Add(ref amount, result);
                                Console.WriteLine(amount);
                                Interlocked.Decrement(ref counter);
                            });
                        }
                        while (counter != 1)
                        {
                            //do do do doooo :)
                        }
                        data["Assault"] += amount;
                    }
                    foreach (string crime in sexcrimes)
                    {
                        int amount = 0;
                        foreach (string ORI in ORIs)
                        {
                            ThreadPool.QueueUserWorkItem(o =>
                            {
                                Interlocked.Increment(ref counter);
                                int result = GetCrimeData(ORI, crime, "count", yearRange);
                                Interlocked.Add(ref amount, result);
                                Console.WriteLine(amount);
                                Interlocked.Decrement(ref counter);
                                Console.WriteLine(counter);
                            });
                        }
                        while (counter != 1)
                        {
                            //do do do doooo :)
                        }
                        data["Sex Crimes"] += amount;
                    }
                    foreach (string crime in theftcrimes)
                    {
                        int amount = 0;
                        foreach (string ORI in ORIs)
                        {
                            ThreadPool.QueueUserWorkItem(o =>
                            {
                                Interlocked.Increment(ref counter);
                                int result = GetCrimeData(ORI, crime, "count", yearRange);
                                Interlocked.Add(ref amount, result);
                                Console.WriteLine(amount);
                                Interlocked.Decrement(ref counter);
                                Console.WriteLine(counter);
                            });
                        }
                        while (counter != 1)
                        {
                            //do do do doooo :)
                        }
                        data["Theft Crimes"] += amount;
                    }
                }
            }
            return data;
        }

        private int GetCrimeData(string ORI, string crime, string variable, int yearRange)
        {
            int count = 0;
            String link = $"https://api.usa.gov/crime/fbi/sapi/api/data/nibrs/{crime}/victim/agencies/{ORI}/{variable}?API_KEY=8i5xgcWXlaTKrcDHIo5mx9l4WfJnEPThPv4dkNmy";
            RestClient client = new RestClient(link);
            RestRequest request = new RestRequest(Method.GET);

            IRestResponse response = client.Execute(request);
            JObject jObject = JObject.Parse(response.Content);
            JToken[] pieces = jObject.GetValue("results").ToArray();
            foreach (JToken piece in pieces)
            {
                JObject obj = JObject.Parse(piece.ToString());
                if (int.Parse(obj.GetValue("data_year").ToString()) >= (DateTime.Now.Year - yearRange))
                {
                    count += int.Parse(obj.GetValue(variable).ToString());
                }
            }
            return count;
        }

        private void Submit_Clicked(object sender, EventArgs e)
        {
            //GetData("84528", 20, "female", "white", 5);




            Dictionary<string, int> results = null;
            loadingBar.Progress = 0;
            Button button = sender as Button;
            button.IsEnabled = false;
            try
            {
                int zip = int.Parse(Zip.Text);
                if (zip >= 00501 && zip <= 99950)
                {
                    int age = Age.Text == null ? -1 : Int32.Parse(Age.Text);
                    string ageRange = null;
                    if (age >= 0 && age <= 9)
                    {
                        ageRange = "range_0_9";
                    }
                    else if (age >= 10 && age <= 99)
                    {
                        char firstNum = Age.Text[0];
                        ageRange = $"range_{firstNum}0_{firstNum}9";
                    }
                    if (ageRange != null)
                    {
                        int yearRange = Year.Text == null ? 100 : Int32.Parse(Year.Text);
                        string sex = SexPicker.SelectedItem != null ? SexPicker.SelectedItem.ToString() : "";
                        string race = RacePicker.SelectedItem != null ? RacePicker.SelectedItem.ToString() : "";
                        Task<bool> barProgress = loadingBar.ProgressTo(1, 10000, Easing.Linear);
                        ThreadPool.QueueUserWorkItem(o =>
                        {
                            Interlocked.Increment(ref counter);
                            results = GetData(Zip.Text, ageRange, sex, race, yearRange);
                            counter = Interlocked.Decrement(ref counter);
                            Console.WriteLine("COUNT COUNT COUNT COUNT " + counter);
                        });
                        barProgress.ContinueWith(o =>
                        {
                            while (counter != 0)
                            {
                                //do do do... waiting waiting waiting for the data to finish.... la la la
                            }
                            if (results == null)
                            {
                                ErrorMsg.IsVisible = true;
                            }
                            else
                            {
                                barProgress.Wait();
                                Device.BeginInvokeOnMainThread(() =>
                                {
                                    loadingBar.Progress = 0;
                                    button.IsEnabled = true;
                                    Console.WriteLine("COUNT COUNT COUNT COUNT " + results.Count);
                                    Application.Current.MainPage.Navigation.PushAsync(new ResultsPage(results));
                                });
                            }
                        });
                    }
                }
            }
            catch (Exception error)
            {
                Console.WriteLine(error.Message);
                Console.WriteLine(error.ToString());
                ErrorMsg.IsVisible = true;
            }
            button.IsEnabled = true;
        }
    }
}

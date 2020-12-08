using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Forms;
using System.Net;
using System.Text.RegularExpressions;
using AngleSharp.Html.Parser;
using AngleSharp.Html.Dom;
using AngleSharp.Dom;
using System.Threading;
using System.Collections.Concurrent;
using System.Diagnostics;
using Xamarin.Essentials;

namespace Safecity
{
    public partial class SearchPage : ContentPage
    {
        private int counter = 0;
        private List<Task> tasks = new List<Task>();
        ConcurrentBag<string> crimes = new ConcurrentBag<string> { "violent-crime", "property-crime", "aggravated-assault", "burglary", "larceny", "motor-vehicle-theft", "homicide", "rape", "robbery", "arson" };
        volatile bool hitYear = false;
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

        private List<string> GetORI(string state, string county)
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

        private string GetCounty(string city, ref string state)
        {
            string county = null;
            city = city.ToLower();
            state = state.ToLower();

            //getting the county from database
            string allInfo = Properties.Resources.uszips;
            string pattern = $"(\"{city}\".*{state}.*{{)";
            Regex r = new Regex(pattern);
            string infoString = r.Match(allInfo.ToLower()).Value;
            Console.WriteLine("STRING STRING STRING: " + infoString);
            if (!infoString.Equals(""))
            {
                string[] cityInfo = infoString.Split(',');
                if (cityInfo.Length > 0)
                {
                    if (state.Length == 2)
                    {
                        state = cityInfo[2];
                        state = state.Replace("\"", "");
                    }
                    county = cityInfo[cityInfo.Length - 2];
                    county = county.Replace("\"", "");
                    county = county.ToLower();
                }
            }
            return county;
        }

        private ConcurrentDictionary<string, int> GetData(string city, string state, string ageRange, string sex, string race, int yearRange)
        {
            ConcurrentDictionary<string, int> data = null;
            //hardcoded for testing
            //string[] info = { "utah", "box elder", "perry" };
            city = city.ToLower();
            state = state.ToLower();
            string county = GetCounty(city, ref state);
            hitYear = false;
            if (county != null)
            {
                List<string> ORIs = GetORI(state, county);
                if (ORIs != null && ORIs.Count >= 1)
                {
                    data = new ConcurrentDictionary<string, int>();
                    foreach (string crime in crimes)
                    {
                        data.TryAdd(crime, 0);
                        data.TryAdd(crime + "_sex", 0);
                        data.TryAdd(crime + "_age", 0);
                        data.TryAdd(crime + "_race", 0);
                        foreach (string ORI in ORIs)
                        {
                            if (ageRange != null)
                            {
                                tasks.Add(Task.Run(() =>
                                {
                                    int ageResult = GetCrimeData(ORI, crime, "age", ageRange, yearRange);
                                    data[crime + "_age"] += ageResult;
                                }));
                            }
                            if (!sex.Equals(""))
                            {
                                tasks.Add(Task.Run(() =>
                                {
                                    int sexResult = GetCrimeData(ORI, crime, "sex", sex, yearRange);
                                    data[crime + "_sex"] += sexResult;
                                }));
                            }
                            if (!race.Equals(""))
                            {
                                tasks.Add(Task.Run(() =>
                                {
                                    int raceResult = GetCrimeData(ORI, crime, "race", race, yearRange);
                                    data[crime + "_race"] += raceResult;
                                }));
                            }
                            tasks.Add(Task.Run(() =>
                            {
                                int result = GetCrimeData(ORI, crime, "count", "Count", yearRange);
                                data[crime] += result;
                            }));
                        }
                    }
                }
            }
            Task.WaitAll(tasks.ToArray());

            return hitYear ? data : null;
        }

        private int GetCrimeData(string ORI, string crime, string variable, string userInfo, int yearRange)
        {
            int count = 0;
            String link = $"https://api.usa.gov/crime/fbi/sapi/api/nibrs/{crime}/victim/agencies/{ORI}/{variable}?API_KEY=8i5xgcWXlaTKrcDHIo5mx9l4WfJnEPThPv4dkNmy";
            RestClient client = new RestClient(link);
            RestRequest request = new RestRequest(Method.GET);

            IRestResponse response = client.Execute(request);
            if (response.StatusCode != HttpStatusCode.NotFound)
            {
                if (response.Content != null)
                {
                    JObject jObject = JObject.Parse(response.Content);
                    if (jObject != null)
                    {
                        JToken result = jObject.GetValue("data");
                        if (result != null)
                        {
                            JToken[] pieces = result.ToArray();
                            if (pieces.Length > 0)
                            {
                                foreach (JToken piece in pieces)
                                {
                                    JObject obj = JObject.Parse(piece.ToString());
                                    if (int.Parse(obj.GetValue("data_year").ToString()) >= (DateTime.Now.Year - yearRange))
                                    {
                                        hitYear = true;
                                        if (obj.GetValue("key").ToString().Equals(userInfo))
                                        {
                                            count += int.Parse(obj.GetValue("value").ToString());
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return count;
        }

        private void Submit_Clicked(object sender, EventArgs e)
        {
            var current = Connectivity.NetworkAccess;

            if (current == NetworkAccess.Internet)
            {
                try
                {
                    int age = Age.Text == null ? -1 : Int32.Parse(Age.Text);
                    string sex = SexPicker.SelectedItem != null ? SexPicker.SelectedItem.ToString() : "";
                    string race = RacePicker.SelectedItem != null ? RacePicker.SelectedItem.ToString() : "";
                    string ageRange = null;
                    if (age >= 0 && age <= 9)
                    {
                        ageRange = "0-9";
                    }
                    else if (age >= 10 && age <= 99)
                    {
                        char firstNum = Age.Text[0];
                        ageRange = $"{firstNum}0-{firstNum}9";
                    }
                    if (ageRange != null || !String.IsNullOrEmpty(sex) || !String.IsNullOrEmpty(race))
                    {
                        Stopwatch stopwatch = new Stopwatch();
                        stopwatch.Start();
                        loadingBar.Progress = 0;
                        Task<bool> barProgress = loadingBar.ProgressTo(1, 25000, Easing.Linear);
                        Indicator.IsRunning = true;
                        ConcurrentDictionary<string, int> results = null;
                        Button button = sender as Button;
                        ErrorMsg.IsVisible = false;
                        button.IsEnabled = false;
                        string city = City.Text;
                        string state = State.Text;
                        int yearRange = Year.Text == null ? 100 : Int32.Parse(Year.Text);

                        ThreadPool.QueueUserWorkItem(o =>
                        {
                            Interlocked.Increment(ref counter);
                            results = GetData(city, state, ageRange, sex, race, yearRange);
                            Interlocked.Decrement(ref counter);
                        });
                        barProgress.ContinueWith(o =>
                        {
                            while (counter != 0)
                            {

                            }

                            foreach (Task task in tasks.ToArray())
                            {
                                tasks.Remove(task);
                                task.Dispose();
                            }

                            if (results == null)
                            {
                                ErrorMsg.IsVisible = true;
                                ErrorMsg.Text = "No results. Please ensure everything is spelled correctly and try expanding your search year range. Otherwise, your area may not be supported.";
                                button.IsEnabled = true;
                                foreach (Task task in tasks)
                                {
                                    tasks.Remove(task);
                                    task.Dispose();
                                }
                                Indicator.IsRunning = false;
                            }
                            else
                            {
                                barProgress.Wait();
                                Device.BeginInvokeOnMainThread(() =>
                                {
                                    loadingBar.Progress = 0;
                                    button.IsEnabled = true;
                                    foreach (Task task in tasks)
                                    {
                                        tasks.Remove(task);
                                        task.Dispose();
                                    }
                                    Indicator.IsRunning = false;
                                    stopwatch.Stop();
                                    Console.WriteLine("TOOK TOOK TOOK TOOK: " + stopwatch.ElapsedMilliseconds);
                                    Application.Current.MainPage.Navigation.PushAsync(new ResultsPage(results, !String.IsNullOrEmpty(sex), !String.IsNullOrEmpty(race), age != -1));
                                });
                            }
                        });
                    }
                    else
                    {
                        ErrorMsg.IsVisible = true;
                        ErrorMsg.Text = "Please fill out at least one demographic";
                    }
                }
                catch (Exception error)
                {
                    Console.WriteLine(error.StackTrace);
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        ErrorMsg.IsVisible = true;
                        ErrorMsg.Text = "An error occurred. Please ensure everything is filled correctly.";
                        loadingBar.Progress = 0;
                        Indicator.IsRunning = false;
                        foreach (Task task in tasks)
                        {
                            tasks.Remove(task);
                            task.Dispose();
                        }
                    });
                }
            }
            else
            {
                DisplayAlert("No Internet", "You must be connected to the internet in order to do searches!", "OK");
            }
        }
    }
}

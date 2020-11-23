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

namespace Safecity
{
    public partial class SearchPage : ContentPage
    {
        private int counter = 0;
        private List<Task> tasks = new List<Task>();
        ConcurrentBag<string> crimes = new ConcurrentBag<string> { "violent-crime", "property-crime", "aggravated-assault", "burglary", "larceny", "motor-vehicle-theft", "homicide", "rape", "robbery", "arson" };
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

        private string[] GetLocationInfo(string zip)
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

            return new string[] { state, county };
        }

        private ConcurrentDictionary<string, int> GetData(string zip, string ageRange, string sex, string race, int yearRange)
        {
            ConcurrentDictionary<string, int> data = null;
            //hardcoded for testing
            //string[] info = { "utah", "box elder", "perry" };
            string[] info = GetLocationInfo(zip);

            if (info != null)
            {
                string state = info[0].ToLower();
                string county = info[1].ToLower();
                List<string> ORIs = GetORI(state, county);
                if (ORIs != null && ORIs.Count >= 1)
                {
                    data = new ConcurrentDictionary<string, int>();
                    foreach (string crime in crimes)
                    {
                        int ageSpecific = 0;
                        int sexSpecific = 0;
                        int raceSpecific = 0;
                        int total = 0;
                        data.TryAdd(crime, total);
                        data.TryAdd(crime + "_age", ageSpecific);
                        data.TryAdd(crime + "_sex", sexSpecific);
                        data.TryAdd(crime + "_race", raceSpecific);
                        foreach (string ORI in ORIs)
                        {
                            Task task = Task.Run(() =>
                            {
                                int result = GetCrimeData(ORI, crime, "count", "Count", yearRange);
                                if (result == -1)
                                {
                                    ORIs.Remove(ORI);
                                }
                                else
                                {
                                    Interlocked.Add(ref total, result);
                                    int ageResult = GetCrimeData(ORI, crime, "age", ageRange, yearRange);
                                    Interlocked.Add(ref ageSpecific, ageResult);
                                    int sexResult = GetCrimeData(ORI, crime, "sex", sex, yearRange);
                                    Interlocked.Add(ref sexSpecific, sexResult);
                                    int raceResult = GetCrimeData(ORI, crime, "race", race, yearRange);
                                    Interlocked.Add(ref raceSpecific, raceResult);
                                    data[crime] += total;
                                    data[crime + "_age"] += ageSpecific;
                                    data[crime + "_sex"] += sexSpecific;
                                    data[crime + "_race"] += raceSpecific;
                                }
                            });
                            tasks.Add(task);
                        }
                    }
                }
            }
            while (tasks.Count != 0)
            {
                Task completedTask = Task.WhenAny(tasks).Result;
                tasks.Remove(completedTask);
                completedTask.Dispose();
            }
            Task.WaitAll(tasks.ToArray());
            return data;
        }

        private int GetCrimeData(string ORI, string crime, string variable, string userInfo, int yearRange)
        {
            int count = -1;
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
                            if (pieces.Length != 0)
                            {
                                count = 0;
                                foreach (JToken piece in pieces)
                                {
                                    JObject obj = JObject.Parse(piece.ToString());
                                    if (int.Parse(obj.GetValue("data_year").ToString()) >= (DateTime.Now.Year - yearRange))
                                    {
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
            Indicator.IsRunning = true;
            ConcurrentDictionary<string, int> results = null;
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
                        Task<bool> barProgress = loadingBar.ProgressTo(.8, 20000, Easing.Linear);
                        ThreadPool.QueueUserWorkItem(o =>
                        {
                            Interlocked.Increment(ref counter);
                            results = GetData(Zip.Text, ageRange, sex, race, yearRange);
                            Interlocked.Decrement(ref counter);
                        });
                        barProgress.ContinueWith(o =>
                        {
                            while (counter != 0)
                            {

                            }

                            if (results == null)
                            {
                                Console.WriteLine("OH NO OH NO OH NO");
                                ErrorMsg.IsVisible = true;
                                button.IsEnabled = true;
                                foreach (Task task in tasks)
                                {
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
                button.IsEnabled = true;
                foreach (Task task in tasks)
                {
                    task.Dispose();
                }
                Indicator.IsRunning = false;
            }
        }
    }
}

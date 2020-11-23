using Xamarin.Forms;

using System;
using OxyPlot;
using OxyPlot.Series;
using System.Collections.Concurrent;

namespace Safecity
{
    public partial class ResultsPage : ContentPage
    {
        public ConcurrentDictionary<String, int> data = null;
        public ResultsPage(ConcurrentDictionary<String, int> data)
        {
            BindingContext = this;
            this.data = data;
            this.PropertyAgeGraph = new PlotModel { Title = "Property Crimes By Age" };
            this.PropertySexGraph = new PlotModel { Title = "Property Crimes By Sex" };
            this.PropertyRaceGraph = new PlotModel { Title = "Property Crimes By Race" };
            this.ViolentAgeGraph = new PlotModel { Title = "Violent Crimes By Age" };
            this.ViolentSexGraph = new PlotModel { Title = "Violent Crimes By Sex" };
            this.ViolentRaceGraph = new PlotModel { Title = "Violent Crimes By Race" };
            this.PersonalGraph = new PlotModel { Title = "Personal Risk" };
            PieSeries PersonalSeries = new PieSeries();
            PieSeries PropertyAgeSeries = new PieSeries();
            PieSeries PropertySexSeries = new PieSeries();
            PieSeries PropertyRaceSeries = new PieSeries();
            PieSeries ViolentAgeSeries = new PieSeries();
            PieSeries ViolentSexSeries = new PieSeries();
            PieSeries ViolentRaceSeries = new PieSeries();
            if (data != null)
            {
                foreach (String key in data.Keys)
                {
                    Console.WriteLine("KEY KEY KEY KEY: " + key);
                    if (key.Equals("property-crime"))
                    {
                        int age = data[key + "_age"];
                        int sex = data[key + "_sex"];
                        int race = data[key + "_race"];
                        PropertyAgeSeries.Slices.Add(new PieSlice("You", age));
                        PropertyAgeSeries.Slices.Add(new PieSlice("Others", data[key] - age));
                        PropertySexSeries.Slices.Add(new PieSlice("You", sex));
                        PropertySexSeries.Slices.Add(new PieSlice("Others", data[key] - sex));
                        PropertyRaceSeries.Slices.Add(new PieSlice("You", race));
                        PropertyRaceSeries.Slices.Add(new PieSlice("Others", data[key] - race));
                    }
                    else if (key.Equals("violent-crime"))
                    {
                        int age = data[key + "_age"];
                        int sex = data[key + "_sex"];
                        int race = data[key + "_race"];
                        ViolentAgeSeries.Slices.Add(new PieSlice("You", age));
                        ViolentAgeSeries.Slices.Add(new PieSlice("Others", data[key] - age));
                        ViolentSexSeries.Slices.Add(new PieSlice("You", sex));
                        ViolentSexSeries.Slices.Add(new PieSlice("Others", data[key] - sex));
                        ViolentRaceSeries.Slices.Add(new PieSlice("You", race));
                        ViolentRaceSeries.Slices.Add(new PieSlice("Others", data[key] - race));
                    }
                    else
                    {
                        //    if (key.Contains("_age"))
                        //    {
                        //        PersonalSeries.Slices.Add(new PieSlice(key, data[key]));
                        //        violentRisk += data[key];
                        //    }
                        //    else if (key.Contains("_sex"))
                        //    {
                        //        PersonalSeries.Slices.Add(new PieSlice(key, data[key]));
                        //        violentRisk += data[key];
                        //    }
                        //    else if (key.Contains("_race"))
                        //    {
                        //        PersonalSeries.Slices.Add(new PieSlice(key, data[key]));
                        //        violentRisk += data[key];
                        //    }
                        //    else
                        //    {
                        //        PersonalSeries.Slices.Add(new PieSlice("OOOOOOOOOOOF", 10));
                        //    }
                    }
                }
                this.PersonalGraph.Series.Add(PersonalSeries);
                this.PropertyAgeGraph.Series.Add(PropertyAgeSeries);
                this.PropertySexGraph.Series.Add(PropertySexSeries);
                this.PropertyRaceGraph.Series.Add(PropertyRaceSeries);
                this.ViolentAgeGraph.Series.Add(ViolentAgeSeries);
                this.ViolentSexGraph.Series.Add(ViolentSexSeries);
                this.ViolentRaceGraph.Series.Add(ViolentRaceSeries);
            }

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
        public PlotModel PersonalGraph { get; private set; }
        public PlotModel ViolentAgeGraph { get; private set; }
        public PlotModel ViolentSexGraph { get; private set; }
        public PlotModel ViolentRaceGraph { get; private set; }
        public PlotModel PropertyAgeGraph { get; private set; }
        public PlotModel PropertySexGraph { get; private set; }
        public PlotModel PropertyRaceGraph { get; private set; }
    }
}

using Xamarin.Forms;

using System;
using OxyPlot;
using OxyPlot.Series;
using System.Collections.Concurrent;
using System.Globalization;

namespace Safecity
{
    public partial class ResultsPage : ContentPage
    {
        public ConcurrentDictionary<String, int> data = null;
        public ResultsPage(ConcurrentDictionary<String, int> data, bool includeSex, bool includeRace, bool includeAge)
        {
            BindingContext = this;
            this.data = data;
            this.PropertyAgeGraph = new PlotModel { Title = "Property Crimes By Age" };
            this.ViolentAgeGraph = new PlotModel { Title = "Violent Crimes By Age" };
            this.PersonalAgeGraph = new PlotModel { Title = "Specific Risks By Age" };
            this.PropertySexGraph = new PlotModel { Title = "Property Crimes By Sex" };
            this.ViolentSexGraph = new PlotModel { Title = "Violent Crimes By Sex" };
            this.PersonalSexGraph = new PlotModel { Title = "Specific Risks By Sex" };
            this.PropertyRaceGraph = new PlotModel { Title = "Property Crimes By Race" };
            this.ViolentRaceGraph = new PlotModel { Title = "Violent Crimes By Race" };
            this.PersonalRaceGraph = new PlotModel { Title = "Specific Risks By Race" };
            PieSeries PersonalAgeSeries = new PieSeries { StrokeThickness = 2.0, InsideLabelPosition = .8, AngleSpan = 360, StartAngle = 0, AreInsideLabelsAngled = true };
            PieSeries PersonalSexSeries = new PieSeries { StrokeThickness = 2.0, InsideLabelPosition = .8, AngleSpan = 360, StartAngle = 0, AreInsideLabelsAngled = true };
            PieSeries PersonalRaceSeries = new PieSeries { StrokeThickness = 2.0, InsideLabelPosition = .8, AngleSpan = 360, StartAngle = 0, AreInsideLabelsAngled = true };
            PieSeries PropertyAgeSeries = new PieSeries { StrokeThickness = 2.0, InsideLabelPosition = 0.8, AngleSpan = 360, StartAngle = 0 };
            PieSeries PropertySexSeries = new PieSeries { StrokeThickness = 2.0, InsideLabelPosition = 0.8, AngleSpan = 360, StartAngle = 0 };
            PieSeries PropertyRaceSeries = new PieSeries { StrokeThickness = 2.0, InsideLabelPosition = 0.8, AngleSpan = 360, StartAngle = 0 };
            PieSeries ViolentAgeSeries = new PieSeries { StrokeThickness = 2.0, InsideLabelPosition = 0.8, AngleSpan = 360, StartAngle = 0 };
            PieSeries ViolentSexSeries = new PieSeries { StrokeThickness = 2.0, InsideLabelPosition = 0.8, AngleSpan = 360, StartAngle = 0 };
            PieSeries ViolentRaceSeries = new PieSeries { StrokeThickness = 2.0, InsideLabelPosition = 0.8, AngleSpan = 360, StartAngle = 0 };
            if (data != null)
            {
                foreach (String key in data.Keys)
                {
                    if (key.Equals("property-crime"))
                    {
                        int age = data[key + "_age"];
                        int sex = data[key + "_sex"];
                        int race = data[key + "_race"];
                        PropertyAgeSeries.Slices.Add(new PieSlice("You", age));
                        PropertyAgeSeries.Slices.Add(new PieSlice("Others", data[key] - age));
                        PropertySexSeries.Slices.Add(new PieSlice("You", sex) { IsExploded = true });
                        PropertySexSeries.Slices.Add(new PieSlice("Others", data[key] - sex));
                        PropertyRaceSeries.Slices.Add(new PieSlice("You", race) { IsExploded = true });
                        PropertyRaceSeries.Slices.Add(new PieSlice("Others", data[key] - race));
                    }
                    else if (key.Equals("violent-crime"))
                    {
                        int age = data[key + "_age"];
                        int sex = data[key + "_sex"];
                        int race = data[key + "_race"];
                        ViolentAgeSeries.Slices.Add(new PieSlice("You", age) { IsExploded = true });
                        ViolentAgeSeries.Slices.Add(new PieSlice("Others", data[key] - age));
                        ViolentSexSeries.Slices.Add(new PieSlice("You", sex) { IsExploded = true });
                        ViolentSexSeries.Slices.Add(new PieSlice("Others", data[key] - sex));
                        ViolentRaceSeries.Slices.Add(new PieSlice("You", race) { IsExploded = true });
                        ViolentRaceSeries.Slices.Add(new PieSlice("Others", data[key] - race));
                    }
                    else
                    {
                        if (!key.Contains("violent-crime") && !key.Contains("property-crime"))
                        {
                            if (data[key] != 0)
                            {
                                if (key.Contains("_age"))
                                {
                                    string label = key.Remove(key.Length - 4);
                                    label = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(label);
                                    PersonalAgeSeries.Slices.Add(new PieSlice(label, data[key]) { IsExploded = true });
                                }
                                else if (key.Contains("_sex"))
                                {
                                    string label = key.Remove(key.Length - 4);
                                    label = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(label);
                                    PersonalSexSeries.Slices.Add(new PieSlice(label, data[key]) { IsExploded = true });
                                }
                                else if (key.Contains("_race"))
                                {
                                    string label = key.Remove(key.Length - 5);
                                    label = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(label);
                                    PersonalRaceSeries.Slices.Add(new PieSlice(label, data[key]) { IsExploded = true });
                                }
                            }
                        }
                    }
                }
                this.PersonalAgeGraph.Series.Add(PersonalAgeSeries);
                this.PersonalSexGraph.Series.Add(PersonalSexSeries);
                this.PersonalRaceGraph.Series.Add(PersonalRaceSeries);
                this.PropertyAgeGraph.Series.Add(PropertyAgeSeries);
                this.PropertySexGraph.Series.Add(PropertySexSeries);
                this.PropertyRaceGraph.Series.Add(PropertyRaceSeries);
                this.ViolentAgeGraph.Series.Add(ViolentAgeSeries);
                this.ViolentSexGraph.Series.Add(ViolentSexSeries);
                this.ViolentRaceGraph.Series.Add(ViolentRaceSeries);
            }
            InitializeComponent();
            if (!includeAge)
            {
                personalAgeGraph.IsVisible = false;
                personalAgeGraph.HeightRequest = 0;
                propertyAgeGraph.IsVisible = false;
                propertyAgeGraph.HeightRequest = 0;
                violentAgeGraph.IsVisible = false;
                violentAgeGraph.HeightRequest = 0;
            }
            if (!includeSex)
            {
                personalSexGraph.IsVisible = false;
                personalSexGraph.HeightRequest = 0;
                propertySexGraph.IsVisible = false;
                propertySexGraph.HeightRequest = 0;
                violentSexGraph.IsVisible = false;
                violentSexGraph.HeightRequest = 0;
            }
            if (!includeRace)
            {
                personalRaceGraph.IsVisible = false;
                personalRaceGraph.HeightRequest = 0;
                propertyRaceGraph.IsVisible = false;
                propertyRaceGraph.HeightRequest = 0;
                violentRaceGraph.IsVisible = false;
                violentRaceGraph.HeightRequest = 0;
            }
        }

        public void GoToInfo(object sender, System.EventArgs e)
        {
            Application.Current.MainPage.Navigation.PopToRootAsync();
        }

        public void GoToHelp(object sender, System.EventArgs e)
        {
            Application.Current.MainPage.Navigation.PushAsync(new HelpPage());
        }
        public PlotModel PersonalAgeGraph { get; private set; }
        public PlotModel PersonalSexGraph { get; private set; }
        public PlotModel PersonalRaceGraph { get; private set; }
        public PlotModel ViolentAgeGraph { get; private set; }
        public PlotModel ViolentSexGraph { get; private set; }
        public PlotModel ViolentRaceGraph { get; private set; }
        public PlotModel PropertyAgeGraph { get; private set; }
        public PlotModel PropertySexGraph { get; private set; }
        public PlotModel PropertyRaceGraph { get; private set; }
    }
}

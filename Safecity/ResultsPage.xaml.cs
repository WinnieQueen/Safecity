using Xamarin.Forms;

using System;
using OxyPlot;
using OxyPlot.Series;
using System.Collections.Generic;

namespace Safecity
{
    public partial class ResultsPage : ContentPage
    {
        public Dictionary<String, int> data = new Dictionary<string, int>();
        public ResultsPage(Dictionary<String, int> data)
        {
            this.data = data;
            this.MyModel = new PlotModel { Title = "Example 1" };
            PieSeries seriesP1 = new PieSeries();
            BindingContext = this;
            InitializeComponent();
            Console.WriteLine(data.Count);
            if (data != null)
            {
                foreach (String key in data.Keys)
                {
                    Console.WriteLine("KEY KEY KEY KEY:" + key);
                    seriesP1.Slices.Add(new PieSlice(key, data[key]));
                }
                this.MyModel.Series.Add(seriesP1);
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
        public PlotModel MyModel { get; private set; }
    }
}

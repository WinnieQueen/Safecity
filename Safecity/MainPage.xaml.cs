using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace Safecity
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
            NavigationPage.SetHasNavigationBar(this, false);
        }

        async public void GoToSearch(object sender, System.EventArgs e)
        {
            var current = Connectivity.NetworkAccess;

            if (current == NetworkAccess.Internet)
            {
                await Application.Current.MainPage.Navigation.PushAsync(new SearchPage());
            }
            else
            {
                await DisplayAlert("No Internet", "You must be connected to the internet in order to do searches!", "OK");
            }
        }
    }
}

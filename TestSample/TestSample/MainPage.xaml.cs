using Plugin.Geolocator;
using Plugin.Geolocator.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace TestSample
{
    public partial class MainPage : ContentPage
    {
        private Position _position;
        public MainPage()
        {
            InitializeComponent();
            StartLocationService();
            DetailButton.Clicked += ButtonClick;
        }

        private async void StartLocationService()
        {

            var locator = CrossGeolocator.Current;
            _position = await locator.GetPositionAsync(TimeSpan.FromSeconds(3));
            SetLocation();

            locator.PositionChanged += (sender, e) =>
            {
                _position = e.Position;
                SetLocation();
            };
        }

        private void SetLocation()
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                LatitudeLabel.Text = _position.Latitude.ToString();
                LongitudeLabel.Text = _position.Longitude.ToString();
            });
        }

        protected async override void OnAppearing()
        {
            base.OnAppearing();
            await CrossGeolocator.Current.StartListeningAsync(TimeSpan.FromSeconds(10), 0);
        }

        protected async override void OnDisappearing()
        {
            base.OnDisappearing();
            await CrossGeolocator.Current.StopListeningAsync();
        }

        private void ButtonClick(object sender, EventArgs e)
        {
            if (_position == null) { 
                DisplayAlert("Alert", "Please enable location services, or wait till location available", "OK");
                return;
            }

            //if (string.IsNullOrEmpty(SearchText.Text))
            //{
            //    DisplayAlert("Alert", "Please enter text for search on intagram", "OK");
            //    return;
            //}

            Navigation.PushAsync(new DetailPage(_position, SearchText.Text));
        }
    }
}

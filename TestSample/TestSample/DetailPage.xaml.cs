using Plugin.Geolocator.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace TestSample
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class DetailPage : ContentPage
    {
        private Position _postion;
        private string _searchText;

        public DetailPage(Position postion, string searchText)
        {
            InitializeComponent();
            _postion = postion;
            _searchText = searchText;
        }

        private async void LoadWeatherInfo()
        {
            var info = await WebApi.GetWeatherInfo(_postion);
            if (info == null)
            {
                DisplayAlert("Alert", "There is some problem with the wethear API", "OK");
                return;
            }

            LatitudeLabel.Text = info.coord?.lat.ToString();
            CityLabel.Text = info.name;
            LongitudeLabel.Text = info.coord?.log.ToString();
            WeatherText.Text = info.weather.FirstOrDefault()?.description.ToString();
            TempText.Text = info.main?.temp.ToString();
            PressureText.Text = info.main?.pressure.ToString();
            HumidityText.Text = info.main?.humidity.ToString();
            MinTempText.Text = info.main?.temp_min.ToString();
            MinTempText.Text = info.main?.temp_max.ToString();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            LoadWeatherInfo();
        }


    }
}
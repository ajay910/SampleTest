using Newtonsoft.Json;
using Plugin.Connectivity;
using Plugin.Geolocator.Abstractions;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using TestSample.Model;

namespace TestSample
{
    public class WebApi
    {
        private const string OpenWeatherMap = "https://api.openweathermap.org/data/2.5/weather?lat={0}&lon={1}&appid=5a720d3031c663348f726a943f161c83";
        public async static Task<WeatherInfo> GetWeatherInfo(Position location)
        {
            try
            {
                if (CrossConnectivity.Current.IsConnected)
                {
                    using (var c = new HttpClient())
                    {
                        var client = new System.Net.Http.HttpClient();
                        client.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/json");

                        var response = await client.GetAsync(new Uri(string.Format(OpenWeatherMap, location.Latitude, location.Longitude)));

                        if (response.IsSuccessStatusCode)
                        {
                            var resultString = await response.Content.ReadAsStringAsync();
                            return JsonConvert.DeserializeObject<WeatherInfo>(resultString);
                        }
                    }
                }
                else
                {
                    return null;
                    //await DisplayAlert("GetEquipmentShiftInfo", "No network is available.", "Ok");
                }

            }
            catch (Exception ex)
            {
                //write a log here
            }

            //return new IObservable<List<Bucket>>();
            return null;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace TestSample.Model
{
    public class WeatherInfo
    {
        public Cord coord { get; set; }
        public List<Weather> weather { get; set; }
        public Main main { get; set; }
        public string name { get; set; }
    }

    public class Cord
    {
        public double log { get; set; }
        public double lat { get; set; }

    }

    public class Weather
    {
        public string main { get; set; }
        public string description { get; set; }


    }

    public class Main
    {
        public decimal  temp { get; set; }
        public decimal pressure{ get; set; }
        public decimal humidity{ get; set; }
        public decimal temp_min{ get; set; }
        public decimal temp_max { get; set; }
    }
}

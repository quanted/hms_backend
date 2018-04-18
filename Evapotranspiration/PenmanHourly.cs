﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Globalization;
using Data;

namespace Evapotranspiration
{
    public class PenmanHourly
    {
        private double latitude;
        private double longitude;
        private double timeZoneCentralLongitude;
        private double elevation;
        private double albedo;
        private double sunAngle;

        private int icounter = 0;
        private double RB2;

        public PenmanHourly()
        {
            latitude = 33.925673;
            longitude = -83.355723;
            elevation = 213.36;
            albedo = 0.23;
            sunAngle = 17.2;
            timeZoneCentralLongitude = 75.0;
        }

        public double Latitude
        {
            get
            {
                return latitude;
            }
            set
            {
                latitude = value;
            }
        }

        public double Longitude
        {
            get
            {
                return longitude;
            }
            set
            {
                longitude = value;
            }
        }

        public double TimeZoneCentralLongitude
        {
            get
            {
                return timeZoneCentralLongitude;
            }
            set
            {
                timeZoneCentralLongitude = value;
            }
        }

        public double Elevation
        {
            get
            {
                return elevation;
            }
            set
            {
                elevation = value;
            }
        }

        public double Albedo
        {
            get
            {
                return albedo;
            }
            set
            {
                albedo = value;
            }
        }

        public double SunAngle
        {
            get
            {
                return sunAngle;
            }
            set
            {
                sunAngle = value;
            }
        }

        public void PenmanHourlyMethod(double thr, int jday, DateTime Time, double shhr, double wind, double solarRad,
                                       out double relHumidityHr, out double petPMH, out string errorMsg)
        {
            double EL = elevation;
            double lat = latitude;
            double LM = Math.Abs(longitude);
            double LZ = Math.Abs(timeZoneCentralLongitude);
            double ALBEDO = albedo;
            double sunangle = sunAngle;

            errorMsg = "";
            relHumidityHr = 0.0;

            double PR = 101.3;  // pressure kilo pascals

            // CONSTANTS
            const double A = 17.27;
            const double B = 237.3;

            // Specific heat at constant pressure[MJ kg-1 deg C-1
            double CP = 1.013 * Math.Pow(10, -3);

            // ratio of mol wt of water vapour to dry air
            const double e = 0.622;

            //Latent heat of Vaporization [MJ kg-1]
            const double LW = 2.45;
            const double pi = Math.PI;

            // extracted from input data
            double TMIN = 0.0;   // celsius
            double TMAX = 0.0;   // celsius
            double jd1 = (double)jday;
            double RHmin = 0.0;
            double RHmax = 0.0;
            double RHmean = 0.0;
            double u = 0.0;

            // EVAPO TRANSPIRATION
            double PET = 0.0;

            // CALCULATED VALUES
            // main calculated parameters used in the final evapo transpiration formulae
            double T = 0.0;
            double RA = 0.0;
            double ra = 0.0;

            // other calculated parameters
            double es1 = 0.0;
            double slope = 0.0;
            double ea = 0.0;
            double deficit = 0.0;
            double P = 0.0; //pressure
            double gama = 0.0; //psychometric const
            double radian = 0.0;
            double dr = 0.0;
            double sigma2 = 0.0;
            double xx = 0.0;
            double ws = 0.0;
            double RS = 0.0;
            double rs = 0.0; //xyang
            double RSO = 0.0;
            double RR = 0.0;
            double RNS = 0.0;
            double tmink = 0.0;
            double EB = 0.0;
            double RB = 0.0;
            double RB1 = 0.6;  // sep 25 2012
            double RNL = 0.0;
            double Rnet = 0.0;
            double ENERG_ET = 0.0;
            double NN = 0.0;
            double denom = 0.0;
            double nenom = 0.0;
            double part1 = 0.0;
            double BB = 0.0;
            double SC = 0.0;
            double w1 = 0.0;
            double w2 = 0.0;
            double G = 0.0;
            double beta = 0.0;  // sep 25 2012
            double u2 = 0.0;

            try
            {
                TMIN = thr;
                TMAX = thr;

                // Convert specific humidity to relative humidity here.  
                RHmin = Utilities.Utility.CalculateRHhourly(shhr, thr);
                RHmax = RHmin;

                // Set relHumidityHr to RHmin or RHmax (it makes no difference in this case).
                relHumidityHr = RHmin;

                T = (TMIN + TMAX) / 2.0;

                RHmean = (RHmin + RHmax) / 2.0;

                u2 = wind;  // Wind is in units of meters/second

                // wind speed correction based on height
                double ht = 10.0;  // The wind data in the NLDAS database is given at a height of 10 meters.
                if (ht != 2.0)
                {
                    u = u2 * (4.87 / (Math.Log(67.8 * ht - 5.42)));
                }
                else
                {
                    u = u2;
                }

                es1 = 0.6108 * Math.Exp((A * T) / (B + T));

                // slope of saturation vapour pressure curve
                slope = es1 * 4098.0 / Math.Pow((T + B), 2.0);

                // Actual water vapor pressure in the atmosphere
                ea = (es1 * (RHmean / 100.0)); //xyang less recommended

                deficit = es1 - ea;

                P = PR * Math.Pow(((293.0 - 0.0065 * EL) / 293.0), 5.26);

                // psychometric constant
                gama = CP * P / (LW * e);

                // ############# ENERGY EQUATIONS ########################################

                /*CALCULATION OF EXTRATERRESTIAL RADIATION FOR DAILY PERIODS
                 ---- THIS PORTION OF THE PROGRAM CALCULATES THE ENERGY COMPONENT  */

                radian = (pi / 180.0) * lat;
                dr = 1.0 + 0.033 * Math.Cos(2 * (pi / 365) * jd1);
                sigma2 = 0.409 * Math.Sin((2.0 * pi * jd1 / 365.0) - 1.39);
                xx = 1.0 - (Math.Pow(Math.Tan(radian), 2.0) * Math.Pow(Math.Tan(sigma2), 2.0));

                if (xx <= 0) xx = 0.00001;

                // extracting hour
                DateTime d = Time;
                int hr = d.Hour;
                double Hour = 0.0;

                if (hr == 0)
                {
                    Hour = 24.0 - 0.5;
                }
                else
                {
                    Hour = hr - 0.5;
                }

                BB = 2 * pi * ((jd1 - 81.0) / 364.0);

                SC = 0.1645 * Math.Sin(2 * BB) - 0.1255 * Math.Cos(BB) - 0.025 * Math.Sin(BB);

                ws = (pi / 12) * ((Hour + 0.06667 * (LZ - LM) + SC) - 12);
                w1 = ws - pi * 1 / 24;
                w2 = ws + pi * 1 / 24;
                RA = (12.0 * 60.0 / pi) * 0.082 * dr * ((w2 - w1) * Math.Sin(radian) * Math.Sin(sigma2) +
                      Math.Cos(radian) * Math.Cos(sigma2) * (Math.Sin(w2) - Math.Sin(w1)));

                if (RA < 0)
                {
                    beta = 0.0;
                }
                else
                {
                    beta = 180 / pi * Math.Asin(Math.Sin(radian) * Math.Sin(sigma2) + Math.Cos(radian) *
                           Math.Cos(sigma2) * Math.Cos(ws));
                }

                // Calculation of SOLAR OR SHORT WAVE RADIATION;
                if (beta == 0.0)
                {
                    RSO = 0.0;
                }
                else
                {
                    RSO = (0.75 + 2.0 * (EL / 100000.0)) * RA;
                }


                RS = solarRad;      // Units are in MJ/(m^2 day) 

                // ##################### sep 25 2012 ########################
                // Light sun angle to be obtained from user currently used as 17.5 degrees or 0.3 radians

                if (beta < sunangle)
                {
                    RR = 0;
                }
                else if (RS / RSO < 0.3)
                {
                    RR = 0.3;
                }
                else if (RS / RSO > 1)
                {
                    RR = 1;
                }
                else
                {
                    RR = RS / RSO;
                }

                // ############################################################
                //                   RB = 1.35 * RR -0.35;
                //                   RB2[i]=RB;

                if (beta < sunangle)
                {
                    if (icounter == 0)
                    {
                        RB = RB1;     // start with an intial value of 0.6 i.e RB1
                        RB2 = RB;
                    }
                    else
                    {
                        RB = RB2;
                        // RB2 = RB;                           
                    }

                }
                else
                {
                    RB = 1.35 * RR - 0.35;
                    RB2 = RB;
                }

                icounter++;

                NN = (24.0 / pi) * ws;
                RNS = (1 - ALBEDO) * RS;
                tmink = (4.903 * Math.Pow(10, -9.0) / 24) * Math.Pow(T + 273.16, 4.0);
                EB = 0.34 - 0.14 * Math.Pow(ea, 0.5);
                RNL = tmink * EB * RB;
                Rnet = RNS - RNL;

                if (Rnet < 0.0)   // sep 25 2012
                {
                    G = 0.5 * Rnet;  // during night hrs
                }
                else
                {
                    G = 0.1 * Rnet;
                }

                // converted into depth of water
                ENERG_ET = 0.408 * (Rnet - G);

                // COMBINATION OF AREODYNAMIC AND ENERGY COMPONENTS

                //add by xyang for rs, and ra
                if (Rnet < 0)
                {
                    rs = 200;
                }
                else
                {
                    rs = 50;
                }

                if (u <= 0.5)
                {
                    ra = 208 / 0.5;
                }
                else
                {
                    ra = 208 / u;
                }

                //denom = ENERG_ET * (slope/(slope + gama * (1+ 0.34 *u)));
                denom = ENERG_ET * (slope / (slope + gama));   //xyang
                part1 = (37.0 / (273 + T) * u);
                nenom = deficit * part1 * (gama / (slope + gama * (1 + rs / ra)));  //xyang
                //end of adding by xyang for rs, and ra  

                PET = denom + nenom;

            }
            catch (Exception ex)
            {
                errorMsg = ex.Message;
            }

            // Convert mm/day to inches/day
            petPMH = (PET / 25.4);

        }

        public ITimeSeriesOutput Compute(double lat, double lon, string startDate, string endDate, int timeZoneOffset, out string errorMsg)
        {
            errorMsg = "";
            NLDAS2 nldas = new NLDAS2(lat, lon, startDate, endDate);
            double petPMH = 0;
            double relHumidityHr = 0.0;
            string strDate;
            CultureInfo CInfoUS = new CultureInfo("en-US");

            bool flagHSPF = false;
            DataTable dt = nldas.getDataHourly(timeZoneOffset, flagHSPF, out errorMsg);
            if (errorMsg != "")
            {
                Utilities.ErrorOutput err = new Utilities.ErrorOutput();
                return err.ReturnError(errorMsg);
            }

            dt.Columns.Add("RH_Hourly");
            dt.Columns.Add("PenmanHourlyPET_in");

            ITimeSeriesOutputFactory oFactory = new TimeSeriesOutputFactory();
            ITimeSeriesOutput output = oFactory.Initialize();
            output.Dataset = "Evapotranspiration";
            output.DataSource = "penmanhourly";
            output.Metadata = new Dictionary<string, string>()
            {
                { "elevation", elevation.ToString() },
                { "latitude", latitude.ToString() },
                { "longitude", longitude.ToString() },
                { "albedo", albedo.ToString() },
                { "sun_angle", sunAngle.ToString() },
                { "central_longitude", timeZoneCentralLongitude.ToString() },
                { "request_time", DateTime.Now.ToString() },
                { "column_1", "Date" },
                { "column_2", "Julian Day" },
                { "column_3", "Hourly Temperature" },
                { "column_4", "Mean Solar Radiation" },
                { "column_5", "Mean Wind Speed" },
                { "column_6", "Hourly Relative Humidity" },
                { "column_7", "Potential Evapotranspiration" }
            };
            output.Data = new Dictionary<string, List<string>>();

            foreach (DataRow dr in dt.Rows)
            {
                double thour = Convert.ToDouble(dr["THourly_C"].ToString());
                strDate = dr["DateHour"].ToString();
                DateTime time = DateTime.ParseExact(strDate, "yyyy-MM-dd HH:00", CInfoUS);
                double shhour = Convert.ToDouble(dr["SH_Hourly"].ToString());
                double wind = Convert.ToDouble(dr["WindSpeed_m/s"].ToString());
                double solarRad = Convert.ToDouble(dr["SolarRad_MJm2day"].ToString());
                int jday = Convert.ToInt32(dr["Julian_Day"].ToString());

                PenmanHourlyMethod(thour, jday, time, shhour, wind, solarRad, out relHumidityHr, out petPMH, out errorMsg);

                dr["RH_Hourly"] = relHumidityHr.ToString("F2", CultureInfo.InvariantCulture);
                dr["PenmanHourlyPET_in"] = petPMH.ToString("F4", CultureInfo.InvariantCulture);
            }

            dt.Columns.Remove("SH_Hourly");

            foreach (DataRow dr in dt.Rows)
            {
                List<string> lv = new List<string>();
                foreach (Object g in dr.ItemArray.Skip(1))
                {
                    lv.Add(g.ToString());
                }
                output.Data.Add(dr[0].ToString(), lv);
            }
            return output;
        }
    }
}
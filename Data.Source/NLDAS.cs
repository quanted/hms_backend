﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Web;
using System.Threading;
using System.IO;

namespace Data.Source
{
    /// <summary>
    /// Base NLDAS class
    /// </summary>
    public class NLDAS
    {
        /// <summary>
        /// Get data function for nldas.
        /// </summary>
        /// <param name="errorMsg"></param>
        /// <param name="dataset">nldas dataset parameter</param>
        /// <param name="componentInput"></param>
        /// <returns></returns>
        public string GetData(out string errorMsg, string dataset, ITimeSeriesInput componentInput)
        {
            errorMsg = "";

            // Adjusts date/times by the timezone offset if timelocalized is set to true.
            if (componentInput.TimeLocalized == true)
            {
                componentInput.DateTimeSpan = AdjustForOffset(out errorMsg, componentInput) as DateTimeSpan;
            }

            // Constructs the url for the NLDAS data request and it's query string.
            string url = ConstructURL(out errorMsg, dataset, componentInput);
            if (errorMsg.Contains("ERROR")) { return null; }

            // Uses the constructed url to download time series data.
            string data = DownloadData(out errorMsg, url);
            if (errorMsg.Contains("ERROR")) { return null; }

            return data;
        }

        /// <summary>
        /// Adjusts the DateTimeSpan object to account for timezone offset. 
        /// NLDAS Date/Times are in GMT.
        /// Adjustment effects range of the requested data but not the date values of the data. Date values are altered with SetDateToLocal()
        /// </summary>
        /// <param name="errorMsg"></param>
        /// <param name="cInput">ITimeSeriesInput</param>
        /// <returns></returns>
        public static IDateTimeSpan AdjustForOffset(out string errorMsg, ITimeSeriesInput cInput)
        {
            errorMsg = "";
            IDateTimeSpan dateTime = cInput.DateTimeSpan;

            if (cInput.Geometry.Timezone.Offset < 0.0) {
                dateTime.StartDate = new DateTime(dateTime.StartDate.Year, dateTime.StartDate.Month, dateTime.StartDate.Day, Convert.ToInt16(System.Math.Abs(cInput.Geometry.Timezone.Offset)), 00, 00);
            }
            else if (cInput.Geometry.Timezone.Offset > 0.0)
            {
                dateTime.StartDate.AddDays(-1.0);
                dateTime.StartDate = new DateTime(dateTime.StartDate.Year, dateTime.StartDate.Month, dateTime.StartDate.Day, 24 - Convert.ToInt16(cInput.Geometry.Timezone.Offset), 00, 00);
            }

            if (cInput.Geometry.Timezone.Offset < 0.0)
            {
                dateTime.EndDate.AddDays(1.0);
                dateTime.EndDate = new DateTime(dateTime.EndDate.Year, dateTime.EndDate.Month, dateTime.EndDate.Day, -1 * Convert.ToInt16(cInput.Geometry.Timezone.Offset) - 1, 00, 00);
            }
            else if (cInput.Geometry.Timezone.Offset > 0.0)
            {
                dateTime.EndDate = new DateTime(dateTime.EndDate.Year, dateTime.EndDate.Month, dateTime.EndDate.Day, 23 - Convert.ToInt16(cInput.Geometry.Timezone.Offset), 00, 00);
            }
            return dateTime;
        }

        /// <summary>
        /// Adjusts the Date/Time value to the local time, using the offset.
        /// </summary>
        /// <param name="errorMsg"></param>
        /// <param name="offset"></param>
        /// <param name="dateHour"></param>
        /// <returns></returns>
        public static string SetDateToLocal(double offset, string dateHour, string dateFormat)
        {
            string[] date = dateHour.Split(' ');
            string hourStr = date[1].Substring(0, 2);
            string dateHourStr = date[0] + " " + hourStr;
            DateTime newDate = new DateTime();
            DateTime.TryParseExact(dateHourStr, new string[] { "yyyy-MM-dd HH" }, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out newDate);
            newDate = (offset != 0.0) ? newDate.AddHours(offset) : newDate;
            string newDateString = newDate.ToString(dateFormat);
            return newDateString;
        }

        /// <summary>
        /// Constructs the url for retrieving nldas data based on the given parameters.
        /// </summary>
        /// <param name="errorMsg"></param>
        /// <param name="componentInput"></param>
        /// <returns></returns>
        private string ConstructURL(out string errorMsg, string dataset, ITimeSeriesInput cInput)
        {
            errorMsg = "";
            StringBuilder sb = new StringBuilder();
            try
            {
                // Reading value from Application variables
                Dictionary<string, string> urls = (Dictionary<string, string>)HttpContext.Current.Application["urlList"];
                Dictionary<string, string> caselessUrls = new Dictionary<string, string>(urls, StringComparer.OrdinalIgnoreCase);
                sb.Append(caselessUrls[cInput.Source + "_" + dataset + "_URL"]);
            }
            catch (Exception ex)
            {
                errorMsg = "ERROR: Unable to load NLDAS url details from configuration file.\n" + ex.Message;
                return null;
            }

            //Add X and Y coordinates
            string[] xy = GetXYCoordinate(out errorMsg, cInput.Geometry.Point); // [0] = x, [1] = y
            if (errorMsg.Contains("ERROR")) { return null; }
            sb.Append("X" + xy[0] + "-" + "Y" + xy[1]);

            //Add Start and End Date
            string[] startDT = cInput.DateTimeSpan.StartDate.ToString("yyyy-MM-dd HH").Split(' ');
            string[] endDT = cInput.DateTimeSpan.EndDate.ToString("yyyy-MM-dd HH").Split(' ');
            sb.Append(@"&startDate=" + startDT[0] + @"T" + startDT[1] + @"&endDate=" + endDT[0] + "T" + endDT[1] + @"&type=asc2");
            
            return sb.ToString();
        }

        /// <summary>
        /// Downloads nldas data from nasa servers. If Http Request fails will retry up to 5 times.
        /// TODO: Add in start date and end date check prior to the download call (Probably add to Validators class)
        /// </summary>
        /// <param name="errorMsg"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        private string DownloadData(out string errorMsg, string url)
        {
            errorMsg = "";
            string data = "";
            try
            {
                // TODO: Read in max retry attempt from config file.
                int retries = 5;

                // Response status message
                string status = "";

                while (retries > 0 && !status.Contains("OK"))
                {
                    Thread.Sleep(100);
                    WebRequest wr = WebRequest.Create(url);
                    HttpWebResponse response = (HttpWebResponse)wr.GetResponse();
                    status = response.StatusCode.ToString();
                    Stream dataStream = response.GetResponseStream();
                    StreamReader reader = new StreamReader(dataStream);
                    data = reader.ReadToEnd();
                    reader.Close();
                    response.Close();
                    retries -= 1;
                }
            }
            catch (Exception ex)
            {
                errorMsg = "ERROR: Unable to download requested nldas data.\n" + ex.Message;
                return null;
            }
            return data;
        }

        /// <summary>
        /// Converts latitude/longitude to X/Y values for NLDAS location.
        /// </summary>
        /// <param name="errorMsg"></param>
        /// <param name="point">ICoordinate</param>
        /// <returns></returns>
        private string[] GetXYCoordinate(out string errorMsg, IPointCoordinate point)
        {
            errorMsg = "";
            double xMax = 463.0;
            double yMax = 223.0;
            double x, y = 0.0;
            string[] results = new string[2];
            x = (point.Longitude + 124.9375) / 0.125;
            y = (point.Latitude - 25.0625) / 0.125;
            if (x > xMax || x < 0)
            {
                errorMsg = "ERROR: Longitude value outside accepted range for NLDAS or is invalid. Provided longitude: " + point.Longitude.ToString() + "\n";
                return null;
            }
            if (y > yMax || y < 0)
            {
                errorMsg = "ERROR: Latitude value outside accepted range for NLDAS or is invalid. Provided latitude: " + point.Latitude.ToString() + "\n";
                return null;
            }
            results[0] = Convert.ToString(Math.Round(x, MidpointRounding.AwayFromZero));
            results[1] = Convert.ToString(Math.Round(y, MidpointRounding.AwayFromZero));
            return results;
        }

        /// <summary>
        /// Determines the coordinate values that corresponds to the closest coordinate point in the NLDAS grid.
        /// </summary>
        /// <param name="errorMsg"></param>
        /// <param name="point">ICoordinate</param>
        /// <returns>[Latitude, Longitude]</returns>
        public double[] DetermineReturnCoordinates(out string errorMsg, IPointCoordinate point)
        {
            errorMsg = "";
            double[] coord = new double[2];
            double step = 0.125;
            double x = (point.Longitude + 124.9375) / step;
            coord[1] = (Math.Round(x, MidpointRounding.AwayFromZero) * step) - 124.9375;
            double y = (point.Latitude - 25.0625) / step;
            coord[0] = (Math.Round(y, MidpointRounding.AwayFromZero) * step) + 25.0625;
            return coord;
        }

        /// <summary>
        /// Takes the data recieved from nldas and sets the ITimeSeries object values.
        /// </summary>
        /// <param name="errorMsg"></param>
        /// <param name="dataset"></param>
        /// <param name="component"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public ITimeSeriesOutput SetDataToOutput(out string errorMsg, string dataset, string data, ITimeSeriesOutput output, ITimeSeriesInput input)
        {
            errorMsg = "";
            string[] splitData = data.Split(new string[] { "Data\n" }, StringSplitOptions.RemoveEmptyEntries);
            output.Dataset = dataset;
            output.DataSource = input.Source;
            output.Metadata = SetMetadata(out errorMsg, splitData[0], output);
            output.Data = SetData(out errorMsg, splitData[1].Substring(0, splitData[1].IndexOf("MEAN")).Trim(), input.TimeLocalized, input.DateTimeSpan.DateTimeFormat, input.DataValueFormat, input.Geometry.Timezone);
            return output;
        }

        /// <summary>
        /// Parses data string from nldas and sets the metadata for the ITimeSeries object.
        /// </summary>
        /// <param name="errorMsg"></param>
        /// <param name="data"></param>
        /// <param name="output"></param>
        /// <returns></returns>
        private Dictionary<string, string> SetMetadata(out string errorMsg, string metaData, ITimeSeriesOutput output)
        {
            errorMsg = "";
            Dictionary<string, string> meta = output.Metadata;
            string[] metaDataLines = metaData.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < metaDataLines.Length - 1; i++)
            {
                if (metaDataLines[i].Contains("="))
                {
                    string[] line = metaDataLines[i].Split('=');
                    if (line[0].Contains("column"))
                    {
                        meta[line[0]] = line[1];
                    }
                    else
                    {
                        meta["nldas_" + line[0]] = line[1];
                    }
                }
            }
            return meta;
        }

        /// <summary>
        /// Parses data string from nldas and sets the data for the ITimeSeries object.
        /// </summary>
        /// <param name="errorMsg"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        private Dictionary<string, List<string>> SetData(out string errorMsg, string data, bool localTime, string dateFormat, string dataFormat, ITimezone tzDetails)
        {
            errorMsg = "";
            Dictionary<string, List<string>> dataDict = new Dictionary<string, List<string>>();
            List<string> timestepData;
            double offset = (localTime == true) ? tzDetails.Offset : 0.0;
            string[] tsLines = data.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < tsLines.Length; i++)
            {
                timestepData = new List<string>();
                string[] lineData = tsLines[i].Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                timestepData.Add(Convert.ToDouble(lineData[2]).ToString(dataFormat));
                dataDict[SetDateToLocal(offset, lineData[0] + " " + lineData[1], dateFormat)] = timestepData;
            }
            return dataDict;
        }
    }
}

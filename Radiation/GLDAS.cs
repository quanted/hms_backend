﻿using Data;
using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Radiation
{
    public class GLDAS
    {

        private Dictionary<string, ITimeSeriesOutput> timeseriesData;


        /// <summary>
        /// Makes the GetData call to the base GLDAS class.
        /// </summary>
        /// <param name="errorMsg"></param>
        /// <param name="output"></param>
        /// <param name="input"></param>
        /// <returns></returns>
        public ITimeSeriesOutput GetData(out string errorMsg, ITimeSeriesOutput output, ITimeSeriesInput input)
        {
            errorMsg = "";
            this.timeseriesData = new Dictionary<string, ITimeSeriesOutput>();
            ITimeSeriesOutputFactory oFactory = new TimeSeriesOutputFactory();
            ITimeSeriesOutput output1 = oFactory.Initialize();
            ITimeSeriesOutput output2 = oFactory.Initialize();
            this.GetLongwaveComponent(out errorMsg, input, output1);
            this.GetShortwaveComponent(out errorMsg, input, output2);
            output = Utilities.Merger.MergeTimeSeries(this.timeseriesData["longwave"], this.timeseriesData["shortwave"]);

            output.Dataset = "DW Radiation";
            output.DataSource = "gldas";

            switch (input.TemporalResolution)
            {
                case "daily":
                    output.Data = NLDAS.DailyAverage(out errorMsg, 7, 1.0, output, input);
                    break;
                case "default":
                default:
                    break;
            }
            output.Metadata["column_1"] = "date";
            output.Metadata["column_2"] = "longwave";
            output.Metadata["column_3"] = "shortwave";
            output.Metadata["column_2_units"] = "W/m^2";
            output.Metadata["column_3_units"] = "W/m^2";

            return output;

        }

        private void GetLongwaveComponent(out string errorMsg, ITimeSeriesInput input, ITimeSeriesOutput output)
        {
            string title = "DW Longwave";
            input.BaseURL = new List<string>() { Data.TimeSeriesInputFactory.GetBaseURL(input.Source, "longwave_radiation") };
            input.Source = title;
            Data.Source.GLDAS gldas = new Data.Source.GLDAS();
            List<string> data = gldas.GetData(out errorMsg, title, input);
            if (errorMsg.Contains("ERROR")) { return; }

            ITimeSeriesOutput gldasOutput = output.Clone();
            gldasOutput = gldas.SetDataToOutput(out errorMsg, title, data, output, input);
            if (errorMsg.Contains("ERROR")) { return; }

            this.timeseriesData.Add("longwave", gldasOutput);
            if (errorMsg.Contains("ERROR")) { return; }
        }

        private void GetShortwaveComponent(out string errorMsg, ITimeSeriesInput input, ITimeSeriesOutput output)
        {
            string title = "DW Shortwave";
            ITimeSeriesInput tempInput = input.Clone(new List<string>() { "radiation" });
            tempInput.BaseURL = new List<string>() { Data.TimeSeriesInputFactory.GetBaseURL("gldas", "shortwave_radiation") };
            tempInput.Source = title;
            Data.Source.GLDAS gldas = new Data.Source.GLDAS();
            List<string> data = gldas.GetData(out errorMsg, title, tempInput);
            if (errorMsg.Contains("ERROR")) { return; }

            ITimeSeriesOutput gldasOutput = output.Clone();
            gldasOutput = gldas.SetDataToOutput(out errorMsg, title, data, output, tempInput);
            if (errorMsg.Contains("ERROR")) { return; }

            this.timeseriesData.Add("shortwave", gldasOutput);
            if (errorMsg.Contains("ERROR")) { return; }
        }

    }
}

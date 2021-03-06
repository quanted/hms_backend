﻿using Data;
using System;
using System.Collections.Generic;
using System.Text;
using Utilities;

namespace Streamflow
{
    public class NWM
    {
        public ITimeSeriesOutput<List<double>> GetData(out string errorMsg, ITimeSeriesOutput<List<double>> output, ITimeSeriesInput input, int retries = 0)
        {
            errorMsg = "";

            string comids = input.Geometry.ComID.ToString();
            if (input.Geometry.GeometryMetadata.ContainsKey("comids"))
            {
                comids = comids + "," + input.Geometry.GeometryMetadata["comids"];
            }
            output.Metadata.Add("comids", comids);

            string dataRequest = "/hms/nwm/data/?dataset=streamflow&comid=" + comids + 
                "&startDate=" + input.DateTimeSpan.StartDate.ToString("yyyy-MM-dd") + 
                "&endDate=" + input.DateTimeSpan.EndDate.ToString("yyyy-MM-dd");

            FlaskData<TimeSeriesOutput<List<double>>> results = Utilities.WebAPI.RequestData<FlaskData<TimeSeriesOutput<List<double>>>>(dataRequest, 1000).Result;
            output = results.data;
            if (input.TemporalResolution.ToLower() == "daily") {
                output.Data = output.ToDaily(input.DateTimeSpan.DateTimeFormat, input, true, false);
                output.Metadata.Add("temporal_timestep", "daily");
            }
            else if (input.TemporalResolution.ToLower() == "monthly")
            {
                output.Data = output.ToMonthly(input.DateTimeSpan.DateTimeFormat, input, true, false);
                output.Metadata.Add("temporal_timestep", "monthly");
            }
            else
            {
                output.Metadata.Add("temporal_timestep", "hourly");
            }
            return output;
        }
    }
}

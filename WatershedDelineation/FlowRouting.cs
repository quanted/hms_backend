﻿using Data;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WatershedDelineation
{
    public class FlowRouting
    {
        public static DataSet calculateStreamFlows(string startDate, string endDate, DataTable dtStreamNetwork, List<string> lst, ITimeSeriesInput input, out string errorMsg)
        {
            //This function returns a dataset containing three tables
            errorMsg = "";
            DateTime startDateTime = Convert.ToDateTime(startDate);
            DateTime endDateTime = Convert.ToDateTime(endDate);

            //Make sure that dtStreamNetwork is sorted by COMID-SEQ column
            dtStreamNetwork.DefaultView.Sort = "[COMID_SEQ] ASC";

            //Create a Dataset with three tables: Surface Runoff, Sub-surfaceRunoff, StreamFlow.
            //Each table has the following columns: DateTime, one column for each COMID with COMID as the columnName
            DataSet ds = new DataSet();
            DataTable dtPrecip = new DataTable();
            DataTable dtSurfaceRunoff = new DataTable();
            DataTable dtSubSurfaceRunoff = new DataTable();
            DataTable dtStreamFlow = new DataTable();

            dtPrecip.Columns.Add("DateTime");
            dtSurfaceRunoff.Columns.Add("DateTime");
            dtSubSurfaceRunoff.Columns.Add("DateTime");
            dtStreamFlow.Columns.Add("DateTime");
            foreach (DataRow dr in dtStreamNetwork.Rows)
            {
                string tocom = dr["TOCOMID"].ToString();
                if (!dtSurfaceRunoff.Columns.Contains(tocom))
                {
                    dtPrecip.Columns.Add(tocom);
                    dtSurfaceRunoff.Columns.Add(tocom);
                    dtSubSurfaceRunoff.Columns.Add(tocom);
                    dtStreamFlow.Columns.Add(tocom);
                }
            }

            //Initialize these tables with 0s
            DataRow drPrecip = null;
            DataRow drSurfaceRunoff = null;
            DataRow drSubSurfaceRunoff = null;
            DataRow drStreamFlow = null;
            int indx = 0;
            for (DateTime date = startDateTime; date <= endDateTime; date = date.AddDays(1))
            {
                drPrecip = dtPrecip.NewRow();
                drSurfaceRunoff = dtSurfaceRunoff.NewRow();
                drSubSurfaceRunoff = dtSubSurfaceRunoff.NewRow();
                drStreamFlow = dtStreamFlow.NewRow();

                foreach (DataColumn dc in dtStreamFlow.Columns)
                {
                    drPrecip[dc.ColumnName] = 0;
                    drSurfaceRunoff[dc.ColumnName] = 0;
                    drSubSurfaceRunoff[dc.ColumnName] = 0;
                    drStreamFlow[dc.ColumnName] = 0;
                }

                drPrecip["DateTime"] = date.ToShortDateString();
                drSurfaceRunoff["DateTime"] = date.ToShortDateString();
                drSubSurfaceRunoff["DateTime"] = date.ToShortDateString();
                drStreamFlow["DateTime"] = date.ToShortDateString();

                dtPrecip.Rows.Add(drPrecip);
                dtSurfaceRunoff.Rows.Add(drSurfaceRunoff);
                dtSubSurfaceRunoff.Rows.Add(drSubSurfaceRunoff);
                dtStreamFlow.Rows.Add(drStreamFlow);
                indx++;
            }
            //Now add the tables to DataSet
            ds.Tables.Add(dtSurfaceRunoff);
            ds.Tables.Add(dtSubSurfaceRunoff);
            ds.Tables.Add(dtStreamFlow);
            ds.Tables.Add(dtPrecip);

            //Iterate through all streams and calculate flows
            string COMID = "";
            string fromCOMID = "";
            Dictionary<string, ITimeSeriesOutput> comSubResults = new Dictionary<string, ITimeSeriesOutput>();
            Dictionary<string, ITimeSeriesOutput> comSurfResults = new Dictionary<string, ITimeSeriesOutput>();
            Dictionary<string, ITimeSeriesOutput> comPrecipResults = new Dictionary<string, ITimeSeriesOutput>();

            Dictionary<string, SurfaceRunoff.SurfaceRunoff> surfaceFlow = new Dictionary<string, SurfaceRunoff.SurfaceRunoff>();
            Dictionary<string, SubSurfaceFlow.SubSurfaceFlow> subsurfaceFlow = new Dictionary<string, SubSurfaceFlow.SubSurfaceFlow>();
            Dictionary<string, Precipitation.Precipitation> precipitation = new Dictionary<string, Precipitation.Precipitation>();

            ITimeSeriesInputFactory inputFactory = new TimeSeriesInputFactory();
            foreach (string com in lst)
            {
                ITimeSeriesInput tsi = new TimeSeriesInput();
                tsi.Geometry = input.Geometry;
                tsi.DateTimeSpan = input.DateTimeSpan;
                tsi.Source = input.Source;
                tsi.TemporalResolution = "daily";
                TimeSeriesGeometry tsGeometry = new TimeSeriesGeometry();
                tsGeometry.Point = GetCatchmentCentroid(out errorMsg, Convert.ToInt32(com));
                tsGeometry.ComID = Convert.ToInt32(com);
                tsGeometry.GeometryMetadata = input.Geometry.GeometryMetadata;

                ITimeSeriesInput subIn = new TimeSeriesInput();
                subIn = tsi;
                subIn.Geometry = tsGeometry;
                subIn.Geometry.Point = GetCatchmentCentroid(out errorMsg, Convert.ToInt32(com));
                subIn.Geometry.ComID = Convert.ToInt32(com);
                SubSurfaceFlow.SubSurfaceFlow sub = new SubSurfaceFlow.SubSurfaceFlow();
                sub.Input = inputFactory.SetTimeSeriesInput(subIn, new List<string>() { "subsurfaceflow" }, out errorMsg);
                subsurfaceFlow.Add(com, sub);


                ITimeSeriesInput surfIn = new TimeSeriesInput();
                surfIn = tsi;
                surfIn.Geometry = tsGeometry;
                surfIn.Geometry.Point = GetCatchmentCentroid(out errorMsg, Convert.ToInt32(com));
                surfIn.Geometry.ComID = Convert.ToInt32(com);
                SurfaceRunoff.SurfaceRunoff runoff = new SurfaceRunoff.SurfaceRunoff();
                runoff.Input = inputFactory.SetTimeSeriesInput(surfIn, new List<string>() { "surfacerunoff" }, out errorMsg);
                surfaceFlow.Add(com, runoff);

                ITimeSeriesInput preIn = new TimeSeriesInput();
                preIn = tsi;
                preIn.Geometry = tsGeometry;
                preIn.Geometry.Point = GetCatchmentCentroid(out errorMsg, Convert.ToInt32(com));
                preIn.Geometry.ComID = Convert.ToInt32(com);
                preIn.Source = preIn.Geometry.GeometryMetadata["precipSource"];
                Precipitation.Precipitation precip = new Precipitation.Precipitation();
                precip.Input = inputFactory.SetTimeSeriesInput(preIn, new List<string>() { "precipitation" }, out errorMsg);
                /*if (precip.Input.Source.Contains("ncdc"))
                {
                    precip.Input.Geometry.GeometryMetadata["token"] = (precip.Input.Geometry.GeometryMetadata.ContainsKey("token")) ? precip.Input.Geometry.GeometryMetadata["token"] : "RUYNSTvfSvtosAoakBSpgxcHASBxazzP";
                }*/
                precipitation.Add(com, precip);
            }

            object outputListLock = new object();
            var options = new ParallelOptions { MaxDegreeOfParallelism = -1 };

            List<string> precipError = new List<string>();
            Parallel.ForEach(precipitation, options, (KeyValuePair<string, Precipitation.Precipitation> preF) =>
            {
                string errorM = "";
                preF.Value.GetData(out errorM);
                lock (outputListLock)
                {
                    precipError.Add(errorM);
                }
            });

            List<string> subsurfaceError = new List<string>();
            Parallel.ForEach(subsurfaceFlow, options, (KeyValuePair<string, SubSurfaceFlow.SubSurfaceFlow> subF) =>
            {
                string errorM = "";
                //subF.Value.GetData(out errorM);
                int retries = 5;
                while (retries > 0 && subF.Value.Output == null)
                {
                    subF.Value.GetData(out errorM);
                    Interlocked.Decrement(ref retries);//retries -= 1;
                }
                lock (outputListLock)
                {
                    subsurfaceError.Add(errorM);
                }
            });

            List<string> surfaceError = new List<string>();
            Parallel.ForEach(surfaceFlow, options, (KeyValuePair<string, SurfaceRunoff.SurfaceRunoff> surF) =>
            {
                string errorM = "";
                //surF.Value.GetData(out errorM);
                int retries = 5;
                while (retries > 0 && surF.Value.Output == null)
                {
                    surF.Value.GetData(out errorM);
                    Interlocked.Decrement(ref retries); //retries -= 1;
                }
                lock (outputListLock)
                {
                    surfaceError.Add(errorM);
                }
            });

            for (int x = 0; x < dtStreamNetwork.Rows.Count; x++)
            {
                COMID = dtStreamNetwork.Rows[x]["TOCOMID"].ToString();
                fromCOMID = dtStreamNetwork.Rows[x]["FROMCOMID"].ToString();
                DataRow[] drsFromCOMIDs = dtStreamNetwork.Select("TOCOMID = " + COMID);

                List<string> fromCOMIDS = new List<string>();
                foreach (DataRow dr2 in drsFromCOMIDs)
                {
                    fromCOMIDS.Add(dr2["FROMCOMID"].ToString());
                }

                for (int i = 0; i < indx; i++)
                {

                    if (subsurfaceFlow[COMID].Output == null || subsurfaceFlow[COMID].Output.Data.Count == 0)
                    {
                        dtSubSurfaceRunoff.Rows[i][COMID] = 0;
                    }
                    else
                    {
                        if (i >= dtSubSurfaceRunoff.Rows.Count)
                        {
                            break;
                        }
                        DateTime datekey = Convert.ToDateTime(dtSubSurfaceRunoff.Rows[i]["DateTime"].ToString());
                        string date = datekey.ToString("yyyy-MM-dd") + " 00";
                        dtSubSurfaceRunoff.Rows[i][COMID] = subsurfaceFlow[COMID].Output.Data[date][0];
                    }

                    if (surfaceFlow[COMID].Output == null || surfaceFlow[COMID].Output.Data.Count == 0)
                    {
                        dtSurfaceRunoff.Rows[i][COMID] = 0;
                    }
                    else
                    {
                        if (i >= dtSurfaceRunoff.Rows.Count)
                        {
                            break;
                        }
                        DateTime datekey = Convert.ToDateTime(dtSurfaceRunoff.Rows[i]["DateTime"].ToString());
                        string date = datekey.ToString("yyyy-MM-dd") + " 00";
                        dtSurfaceRunoff.Rows[i][COMID] = surfaceFlow[COMID].Output.Data[date][0];
                    }

                    if (precipitation[COMID].Output == null || precipitation[COMID].Output.Data.Count == 0)
                    {
                        dtPrecip.Rows[i][COMID] = 0;
                    }
                    else
                    {
                        if (i >= dtPrecip.Rows.Count)
                        {
                            break;
                        }
                        DateTime datekey = Convert.ToDateTime(dtPrecip.Rows[i]["DateTime"].ToString());
                        string date = datekey.ToString("yyyy-MM-dd") + " 00";
                        dtPrecip.Rows[i][COMID] = precipitation[COMID].Output.Data[date][0];
                    }

                    //Fill dtStreamFlow table by adding Surface and SubSurface flow from dtSurfaceRunoff and dtSubSurfaceRunoff tables.  We still need to add boundary condition flows
                    double dsur = Convert.ToDouble(dtSurfaceRunoff.Rows[i][COMID].ToString());
                    double dsub = Convert.ToDouble(dtSubSurfaceRunoff.Rows[i][COMID].ToString());
                    double area = Convert.ToDouble(dtStreamNetwork.Rows[x]["AreaSqKM"]);
                    dtStreamFlow.Rows[i][COMID] = (dsub * area) + (dsur * area);//dsub + dsur;


                    //Get stream flow time series for streams flowing into this COMID i.e. bondary condition.  Skip this step in the following three cases:
                    //  1. dr["FROMCOMID"].ToString()="0" in dtStreanNetwork table for this dr["TOCOMID"].ToString() i.e. it is head water stream
                    //  2. dr["FROMCOMID].ToString() = dr["TOCOMID"].ToString().  This can happen at the pour points
                    //  3. dr["FROMCOMID"].ToString() does not appear in the TOCOMID column of dtStreamNetwork table i.e. FROMCOMID is outside the network
                    //If multiple streams flow into this stream then add up stream flow time series of the inflow streams.
                    //There could be multiple bondary condition flows if multiple upstream streams flow into this COMID.            
                    foreach (string fromCom in fromCOMIDS)
                    {
                        if (fromCom == "0")//No boundary condition flows if the stream is a headwater (fromCOMID=0)
                        {
                            continue;
                        }
                        if (fromCom == COMID)//No boundary condition if fromCOMID=TOCOMID
                        {
                            continue;
                        }
                        DataRow[] drs = dtStreamNetwork.Select("TOCOMID = " + fromCom);
                        //No boundary condition if fromCOMID is not present in the streamNetwork table under TOCOMID column.  THis means that fromCOMID is outside our network.
                        if (drs == null || drs.Length == 0)
                        {
                            continue;
                        }
                        //Now add up all three time series: streams flow of streams inflowing into this stream, surface runoff, and sub-surface runoff
                        dtStreamFlow.Rows[i][COMID] = (Convert.ToDouble(dtStreamFlow.Rows[i][fromCom].ToString()) * area * 1000) + (Convert.ToDouble(dtStreamFlow.Rows[i][COMID].ToString()) * area * 1000);
                    }
                }
            }
            return ds;
        }

        private static PointCoordinate GetCatchmentCentroid(out string errorMsg, int comid)
        {
            errorMsg = "";
            string dbPath = "./App_Data/catchments.sqlite";
            string query = "SELECT CentroidLatitude, CentroidLongitude FROM PlusFlowlineVAA WHERE ComID = " + comid.ToString();
            Dictionary<string, string> centroidDict = Utilities.SQLite.GetData(dbPath, query);
            if (centroidDict.Count == 0)
            {
                errorMsg = "ERROR: Unable to find catchment in database. ComID: " + comid.ToString();
                IPointCoordinate cent = new PointCoordinate()
                {
                    Latitude = 0,
                    Longitude = 0,
                };
                return cent as PointCoordinate;
            }

            IPointCoordinate centroid = new PointCoordinate()
            {
                Latitude = double.Parse(centroidDict["CentroidLatitude"]),
                Longitude = double.Parse(centroidDict["CentroidLongitude"])
            };


            return centroid as PointCoordinate;
        }
    }
}
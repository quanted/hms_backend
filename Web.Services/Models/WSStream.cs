﻿using Data;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WatershedDelineation;

namespace Web.Services.Models
{
    /// <summary>
    /// HMS Web Service Stream Model
    /// </summary>
    public class WSStream
    {

        /// <summary>
        /// Gets stream network data for a provided comid
        /// </summary>
        /// <param name="comid"></param>
        /// <returns></returns>
        public async Task<Dictionary<string, object>> Get(string comid, string endComid=null, string huc=null, double maxDistance=50.0)
        {
            string errorMsg = "";
            
            // Constructs default error output object containing error message.
            Utilities.ErrorOutput err = new Utilities.ErrorOutput();

            // Check comid
            if(comid is null) { return this.Error("ERROR: comid input is not valid."); }

            Dictionary<string, object> result = new Dictionary<string, object>();

            WatershedDelineation.Streams streamN = new WatershedDelineation.Streams(comid, null, null);
            var streamNetwork = streamN.GetNetwork(maxDistance, endComid);
            List<List<object>> networkTable = StreamNetwork.generateTable(streamNetwork, huc);
            result.Add("network", networkTable);
            List<List<int>> segOrder = this.generateOrder(networkTable);
            result.Add("order", segOrder);

            return result;
        }

        public List<List<int>> generateOrder(List<List<object>> networkTable)
        {

            List<List<object>> seq = new List<List<object>>();
            List<int> dag = new List<int>();
            for(int i = networkTable.Count - 1; i > 0; i--)
            {
                int hydroseq = Int32.Parse(networkTable[i][1].ToString());
                seq.Add(new List<object>());
                seq[0].Add(networkTable[i]);
                dag.Add(hydroseq);
            }


            List<List<int>> seqOrder = new List<List<int>>();
            seqOrder.Add(new List<int>()
            {
                Int32.Parse(networkTable[1][1].ToString())
            });
            List<List<int>> comidOrder = new List<List<int>>();
            comidOrder.Add(new List<int>()
            {
                Int32.Parse(networkTable[1][0].ToString())
            });
            for (int i = 2; i <= dag.Count; i++)
            {
                seqOrder.Add(new List<int>());
                comidOrder.Add(new List<int>());
            }
            for (int i = 2; i <= dag.Count; i++)
            {
                int comid = Int32.Parse(networkTable[i][0].ToString());
                int hydroseq = Int32.Parse(networkTable[i][1].ToString());
                int dnhydroseq = Int32.Parse(networkTable[i][3].ToString());
                int uphydroseq = Int32.Parse(networkTable[i][2].ToString());
                int seq_j = 0;
                for (int j = seqOrder.Count - 1; j >= 0; j--)
                {
                    if (seqOrder[j].Contains(dnhydroseq))
                    {
                        seq_j = j + 1;
                        break;
                    }
                    else if (seqOrder[j].Contains(uphydroseq))
                    {
                        seq_j = j - 1;
                        break;
                    }
                }
                comidOrder[seq_j].Add(comid);
                seqOrder[seq_j].Add(hydroseq);
            }

            for (int i = dag.Count - 1; i >= 0; i--)
            {
                if (comidOrder[i].Count == 0)
                {
                    comidOrder.RemoveAt(i);
                }
            }
            comidOrder.Reverse();

            return comidOrder;
        }

        private Dictionary<string, object> Error(string errorMsg)
        {
            Dictionary<string, object> output = new Dictionary<string, object>();
            output.Add("ERROR", errorMsg);
            return output;
        }
    }
}

﻿using HDInsightStormExamples.Bolts;
using HDInsightStormExamples.Spouts;
using Microsoft.SCP;
using Microsoft.SCP.Topology;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HDInsightStormExamples.Topologies
{
    /// <summary>
    /// A topology example of reading data from spout and writing into HBase
    /// Uncomment Active attribute to deploy this topology and comment out Active attribute of any other topologies in this project.
    /// </summary>
    //[Active(true)]
    class HBaseReaderTopology : TopologyDescriptor
    {
        public ITopologyBuilder GetTopologyBuilder()
        {
            var topologyBuilder = new TopologyBuilder(typeof(HBaseReaderTopology).Name + DateTime.Now.ToString("yyyyMMddHHmmss"));

            topologyBuilder.SetSpout(
                typeof(VehicleRecordGeneratorSpout).Name, //Set task name
                VehicleRecordGeneratorSpout.Get, //Set task constructor delegate
                new Dictionary<string, List<string>>() { { Constants.DEFAULT_STREAM_ID, VehicleRecordGeneratorSpout.OutputFields } },
                1, //Set number of tasks
                true //Set enableAck
                );

            topologyBuilder.SetBolt(
                typeof(HBaseLookupBolt).Name, //Set task name
                HBaseLookupBolt.Get, //Set task constructor delegate
                new Dictionary<string, List<string>>() { { Constants.DEFAULT_STREAM_ID, VehicleRecordGeneratorSpout.OutputFields } },
                1, //Set number of tasks
                true //Set enableAck
                ).
                globalGrouping(typeof(VehicleRecordGeneratorSpout).Name);

            topologyBuilder.SetBolt(
                typeof(HBaseLookupBolt).Name + typeof(LoggerBolt).Name, //Set task name
                LoggerBolt.Get, //Set task constructor delegate
                new Dictionary<string, List<string>>(), //Leave empty if the task has no outputSchema defined i.e. no outgoing tuples
                1, //Set number of tasks
                true //Set enableAck
                ).
                globalGrouping(typeof(HBaseLookupBolt).Name);

            //Set the topology config
            var topologyConfig = new StormConfig();
            topologyConfig.setNumWorkers(1); //Set number of worker processes
            topologyConfig.setMaxSpoutPending(512); //Set maximum pending tuples from spout
            topologyConfig.setWorkerChildOps("-Xmx768m"); //Set Java Heap Size

            topologyBuilder.SetTopologyConfig(topologyConfig);

            return topologyBuilder;
        }
    }
}
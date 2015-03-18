using System;
using System.Collections.Generic;
using Microsoft.SCP;
using System.Diagnostics;

namespace EventCountHybridTopology
{
    /// <summary>
    /// Partially count number of messages from EventHubs
    /// </summary>
    public class PartialCountBolt : ISCPBolt
    {
        Context ctx;
        AppConfig appConfig;

        long partialCount = 0L;
        long totalCount = 0L;
        long finalCount = 0L;

        Stopwatch stopwatch;

        public PartialCountBolt(Context ctx)
        {
            this.ctx = ctx;
            this.appConfig = new AppConfig();

            // Declare Input schemas
            Dictionary<string, List<Type>> inputSchema = new Dictionary<string, List<Type>>();
            inputSchema.Add(Constants.DEFAULT_STREAM_ID, new List<Type>() { typeof(string) });
            Dictionary<string, List<Type>> ouputSchema = new Dictionary<string, List<Type>>();
            ouputSchema.Add(Constants.DEFAULT_STREAM_ID, new List<Type>() { typeof(long) });
            this.ctx.DeclareComponentSchema(new ComponentStreamSchema(inputSchema, ouputSchema));
            this.ctx.DeclareCustomizedDeserializer(new CustomizedInteropJSONDeserializer());

            partialCount = 0L;
            totalCount = 0L;
            finalCount = appConfig.EventCountPerPartition;

            Context.Logger.Info("finalCount: " + finalCount);
            stopwatch = Stopwatch.StartNew();
        }

        /// <summary>
        /// The Execute() function will be called, when a new tuple is available.
        /// </summary>
        /// <param name="tuple"></param>
        public void Execute(SCPTuple tuple)
        {
            partialCount++;
            totalCount ++;

            //emit the partialCount every second
            //Specially handle the end of stream using finalCount so that this bolt flushes the count on last expected tuple
            //TODO: In future when SCP.Net will support TickTuple, we should revisit this and use Tick tuple for windowing
            if ((stopwatch.ElapsedMilliseconds >= 1000L) || (totalCount >= finalCount))
            {
                Context.Logger.Info("emitting partialCount: " + partialCount + 
                    ", totalCount: " + totalCount +
                    ", finalCount: " + finalCount);
                this.ctx.Emit(new Values(partialCount));
                partialCount = 0L;
                stopwatch.Restart();
            }
            this.ctx.Ack(tuple);
        }

        /// <summary>
        ///  Implements of delegate "newSCPPlugin", which is used to create a instance of this spout/bolt
        /// </summary>
        /// <param name="ctx">SCP Context instance</param>
        /// <param name="parms">Parameters to initialize this spout/bolt</param>
        /// <returns></returns>
        public static PartialCountBolt Get(Context ctx, Dictionary<string, Object> parms)
        {
            return new PartialCountBolt(ctx);
        }
    }
}
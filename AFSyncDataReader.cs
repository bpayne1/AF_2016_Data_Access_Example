using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using OSIsoft.AF.Asset;
using OSIsoft.AF.Data;
using OSIsoft.AF.Time;
using OSIsoft.AF;
using OSIsoft.AF.PI;

namespace Asynchronous_Data_Access
{


    public class AFSyncDataReader
    {
        public static IList<IDictionary<AFSummaryTypes, AFValues>> GetSummariesSync(AFAttributeList attributeList)
        {
            Console.WriteLine("Calling GetSummariesSync\n");
            PIPagingConfiguration config = new PIPagingConfiguration(PIPageType.TagCount, 100);
            try
            {
                AFSummaryTypes mySummaries = AFSummaryTypes.Minimum | AFSummaryTypes.Maximum | AFSummaryTypes.Average | AFSummaryTypes.Total;
                AFTimeRange timeRange = new AFTimeRange(new AFTime("*-1d"), new AFTime("*"));
                AFTimeSpan span = new AFTimeSpan(days: 1);
                return attributeList.Data.Summaries(timeRange, span,
                    mySummaries,
                    AFCalculationBasis.TimeWeighted,
                    AFTimestampCalculation.Auto,
                    config).ToList();
            }
            catch (AggregateException ae)
            {
                Console.WriteLine("{0}: {1}", attributeList.Count, ae.Flatten().InnerException.Message);
                return null;
            }
        }

    }      
}

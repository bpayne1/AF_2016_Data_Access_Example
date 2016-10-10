using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading.Tasks;
using OSIsoft.AF;
using OSIsoft.AF.Asset;
using OSIsoft.AF.Data;
//using External;

namespace Asynchronous_Data_Access
{
    class Asynchronous_Data_Access_Main
    {

        static void Main(string[] args)
        {
            // Connect to AF Server
            PISystems piSystems = null;
            try
            {
                piSystems = new PISystems();
            }
            catch (Exception e)
            {
                Console.WriteLine(String.Format("An exception occurred when creating PISystems object. Exception: {0}", e.Message));
            }

            if (piSystems == null)
            {
                Console.WriteLine("Cannot create PISystems object.");
            }
            PISystem piSys = piSystems.DefaultPISystem;
            if (piSys == null)
            {
                piSys = piSystems.Add("PISystem"); 
            }

            AFDatabase Afdb = piSys.Databases["AFDB"];
            if (Afdb == null)
            {
                try
                {
                    Afdb = piSys.Databases.Add("AFDB");
                }
                catch (Exception e)
                {
                    Console.WriteLine("Failed creating AFDB on " + piSys.Name + ":" + "{0}", e.Message);
                }
            }


            AFAttributeList attrList = GetAttributes(Afdb);
            Task<IList<IDictionary<AFSummaryTypes, AFValue>>> summariesTask = null;
            try
            {


                while (true)
                {
                    Console.WriteLine(@"Press '1' for Async, '2' for Sync or 'x' key to quit");
                    string sInput = Console.ReadLine();
                    if (sInput == "x" || sInput == "X")
                        break;
                    if (sInput == "1")
                    {
                        // Create new stopwatch.
                        Stopwatch stopwatch = new Stopwatch();

                        // Begin timing.
                        stopwatch.Start();
                        summariesTask = AFAsyncDataReader.GetSummariesAsync(attrList);

                        while (!summariesTask.IsCompleted)
                            Console.WriteLine("Doing work on the Main Thread !!");

                        // Stop timing.
                        stopwatch.Stop();
                        
                        // Write stopwatch result.
                        Console.WriteLine("Time elapsed: {0}", stopwatch.Elapsed);

                        // Wait for the summaries result
                        IList<IDictionary<AFSummaryTypes, AFValue>> summaries = summariesTask.Result;
                        foreach (var summary in summaries)
                         {
                             WriteSummaryItem(summary);
                         }
                     }
                    if (sInput == "2")
                     {

                        // Create new stopwatch.
                        Stopwatch stopwatch = new Stopwatch();

                        // Begin timing.
                        stopwatch.Start();
                        var summaries = AFSyncDataReader.GetSummariesSync(attrList);
                        // Stop timing.
                        stopwatch.Stop();
                        // Write stopwatch result.
                        Console.WriteLine("Time elapsed: {0}", stopwatch.Elapsed);

                        while (summaries.Count == 0)
                         {
                             Console.WriteLine("Doing work on the Main Thread while waiting  !!");
                         }

                         Console.WriteLine("Doing work on the Main Thread AFTER waiting !!");

                         // Holds the results keyed on the associated attribute
                         var resultsMap = new Dictionary<AFAttribute, AFValue>();


                         foreach (IDictionary<AFSummaryTypes, AFValues> summary in summaries)
                         {
                              WriteSummaryItem(summary);
                         }

                     }
                     
                  }                   
               } 
               catch (AggregateException ae)
               {
                   Console.WriteLine("{0}", ae.Flatten().InnerException.Message);
               }
                         
        }

        private static AFAttributeList GetAttributes(AFDatabase database)
        {
            int startIndex = 0;
            int pageSize = 1000;
            int totalCount;

            AFAttributeList attrList = new AFAttributeList();

            do
            {
                AFAttributeList results = AFAttribute.FindElementAttributes(
                     database: database,
                     searchRoot: null,
                     nameFilter: null,
                     elemCategory: null,
                     elemTemplate: database.ElementTemplates["Feeder"],
                     elemType: AFElementType.Any,
                     attrNameFilter: "Power",
                     attrCategory: null,
                     attrType: TypeCode.Empty,
                     searchFullHierarchy: true,
                     sortField: AFSortField.Name,
                     sortOrder: AFSortOrder.Ascending,
                     startIndex: startIndex,
                     maxCount: pageSize,
                     totalCount: out totalCount);

                attrList.AddRange(results);

                startIndex += pageSize;
            } while (startIndex < totalCount);

            return attrList;
        }

        private static void WriteSummaryItem(IDictionary<AFSummaryTypes, AFValue> summary)
        {
            Console.WriteLine();
            Console.WriteLine("Summary for {0}", summary[AFSummaryTypes.Minimum].Attribute.Element);
            Console.WriteLine("  Minimum: {0:N0}", summary[AFSummaryTypes.Minimum].ValueAsDouble());
            Console.WriteLine("  Maximum: {0:N0}", summary[AFSummaryTypes.Maximum].ValueAsDouble());
            Console.WriteLine("  Average: {0:N0}", summary[AFSummaryTypes.Average].ValueAsDouble());
            Console.WriteLine("  Total: {0:N0}", summary[AFSummaryTypes.Total].ValueAsDouble());
            Console.WriteLine();
        }
        public static void WriteSummaryItem(IDictionary<AFSummaryTypes, AFValues> summary)
        {

            Console.WriteLine();
            Console.WriteLine("Summary for {0}", summary[AFSummaryTypes.Minimum].Attribute.Element);
            Console.WriteLine("  Minimum: {0:N0}", summary[AFSummaryTypes.Minimum][0].ValueAsDouble());
            Console.WriteLine("  Maximum: {0:N0}", summary[AFSummaryTypes.Maximum][0].ValueAsDouble());
            Console.WriteLine("  Average: {0:N0}", summary[AFSummaryTypes.Average][0].ValueAsDouble());
            Console.WriteLine("  Total: {0:N0}", summary[AFSummaryTypes.Total][0].ValueAsDouble());
            Console.WriteLine();
        }
        
    }
}

















//            try
//            {

//                AFAttributeList attrList = GetAttributes(Afdb);
//                Task<IList<IDictionary<AFSummaryTypes, AFValue>>> summariesTask = AFAsyncDataReader.GetSummariesAsync(attrList);
               
//                // Wait for the summaries result
//                //IList<IDictionary<AFSummaryTypes, AFValue>> summaries = summariesTask.Result;
//                //foreach (var summary in summaries)
//                //{
//                //    WriteSummaryItem(summary);
//                //}

//                while (true)
//                {
//                    Console.WriteLine("Doing work on the Main Thread !!");
//                }
//            }
//            catch (AggregateException ae)
//            {
//                Console.WriteLine("{0}", ae.Flatten().InnerException.Message);
//            }

//            Console.WriteLine("Press any key to quit");
//            Console.ReadKey();
//        }

//        private static AFAttributeList GetAttributes(AFDatabase database)
//        {
//            int startIndex = 0;
//            int pageSize = 1000;
//            int totalCount;

//            AFAttributeList attrList = new AFAttributeList();

//            do
//            {
//                AFAttributeList results = AFAttribute.FindElementAttributes(
//                     database: database,
//                     searchRoot: null,
//                     nameFilter: null,
//                     elemCategory: null,
//                     elemTemplate: database.ElementTemplates["FeederTemplate"],
//                     elemType: AFElementType.Any,
//                     attrNameFilter: "Power",
//                     attrCategory: null,
//                     attrType: TypeCode.Empty,
//                     searchFullHierarchy: true,
//                     sortField: AFSortField.Name,
//                     sortOrder: AFSortOrder.Ascending,
//                     startIndex: startIndex,
//                     maxCount: pageSize,
//                     totalCount: out totalCount);

//                attrList.AddRange(results);

//                startIndex += pageSize;
//            } while (startIndex < totalCount);

//            return attrList;
//        }

//        public void WriteSummaryItem(IDictionary<AFSummaryTypes, AFValue> summary)
//        {
//            Console.WriteLine("Summary for {0}", summary[AFSummaryTypes.Minimum].Attribute.Element);
//            Console.WriteLine("  Minimum: {0:N0}", summary[AFSummaryTypes.Minimum].ValueAsDouble());
//            Console.WriteLine("  Maximum: {0:N0}", summary[AFSummaryTypes.Maximum].ValueAsDouble());
//            Console.WriteLine("  Average: {0:N0}", summary[AFSummaryTypes.Average].ValueAsDouble());
//            Console.WriteLine("  Total: {0:N0}", summary[AFSummaryTypes.Total].ValueAsDouble());
//            Console.WriteLine();
//        }
//    }

    
////public class AsyncAwaitExample
////{
////    public async Task DoWork()
////    {
////        await Task.Run(() =>
////        {
////            int counter;
////            // Wait for the summaries result
////            IList<IDictionary<AFSummaryTypes, AFValue>> summaries = summariesTask.Result;
////            foreach (var summary in summaries)
////            {
////                WriteSummaryItem(summary);
////            }
////            for (counter = 0; counter < 1000; counter++)
////            {
////                Console.WriteLine(counter);
////            }
////        });
////    }

////}

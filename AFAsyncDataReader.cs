using System;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using OSIsoft.AF.Asset;
using OSIsoft.AF.Data;
using OSIsoft.AF.Time;
using OSIsoft.AF;

namespace Asynchronous_Data_Access
{
    
    
    public class AFAsyncDataReader
    {
        public static async Task<IList<IDictionary<AFSummaryTypes, AFValue>>> GetSummariesAsync(AFAttributeList attributeList)
        {
            Console.WriteLine("Calling GetSummariesAsync\n");

            Task<IDictionary<AFSummaryTypes, AFValue>>[] tasks = attributeList
                // Do not make the call if async is not supported
                .Where(attr => (attr.SupportedDataMethods & AFDataMethods.Asynchronous) == AFDataMethods.Asynchronous)
                .Select(async attr =>
                {
                    try
                    {
                        AFSummaryTypes mySummaries = AFSummaryTypes.Minimum | AFSummaryTypes.Maximum | AFSummaryTypes.Average | AFSummaryTypes.Total;
                        AFTimeRange timeRange = new AFTimeRange(new AFTime("*-1d"), new AFTime("*"));

                        return await attr.Data.SummaryAsync(
                            timeRange: timeRange,
                            summaryType: mySummaries,
                            calculationBasis: AFCalculationBasis.TimeWeighted,
                            timeType: AFTimestampCalculation.Auto);

                        
                    }
                    catch (AggregateException ae)
                    {
                        Console.WriteLine("{0}: {1}", attr.Name, ae.Flatten().InnerException.Message);
                        return null;
                    }

                    
                })
                .ToArray();
           
            return await Task.WhenAll(tasks);
        }

        public static async Task<IList<IDictionary<AFSummaryTypes, AFValue>>> GetSummariesAsyncThrottled(AFAttributeList attributeList, int numConcurrent)
        {
            // Use "asynchronous semaphore" pattern (e.g. SemaphoreSlim.WaitAsync()) to throttle the calls

            Console.WriteLine("Calling GetSummariesAsyncThrottled");

            // Example: Limit to numConcurrent concurrent async I/O operations.
            SemaphoreSlim throttler = new SemaphoreSlim(initialCount: numConcurrent);

            Task<IDictionary<AFSummaryTypes, AFValue>>[] tasks = attributeList
                // Do not make the call if async is not supported
                .Where(attr => (attr.SupportedDataMethods & AFDataMethods.Asynchronous) == AFDataMethods.Asynchronous)
                .Select(async attr =>
                {
                    // asychronously try to acquire the semaphore
                    await throttler.WaitAsync();

                    try
                    {
                        AFSummaryTypes mySummaries = AFSummaryTypes.Minimum | AFSummaryTypes.Maximum | AFSummaryTypes.Average | AFSummaryTypes.Total;
                        AFTimeRange timeRange = new AFTimeRange(new AFTime("*-1d"), new AFTime("*"));

                        return await attr.Data.SummaryAsync(
                            timeRange: timeRange,
                            summaryType: mySummaries,
                            calculationBasis: AFCalculationBasis.TimeWeighted,
                            timeType: AFTimestampCalculation.Auto);
                    }
                    catch (AggregateException ae)
                    {
                        Console.WriteLine("{0}: {1}", attr.Name, ae.Flatten().InnerException.Message);
                        return null;
                    }
                    finally
                    {
                        // release the resource
                        throttler.Release();
                    }
                })
                .ToArray();

            return await Task.WhenAll(tasks);
        }

        public static async Task<IList<IDictionary<AFSummaryTypes, AFValue>>> GetSummariesAsyncWithTimeout(AFAttributeList attributeList, int timeoutInMilliseconds)
        {
            // Use a "competing tasks" pattern to place timeout on multiple async requests

            Console.WriteLine("Calling GetSummariesAsyncWithTimeout");

            CancellationTokenSource cts = new CancellationTokenSource();
            CancellationToken token = cts.Token;
            CancellationTokenSource ctsForTimer = new CancellationTokenSource();
            CancellationToken tokenForTimer = ctsForTimer.Token;

            Task<IDictionary<AFSummaryTypes, AFValue>>[] tasks = attributeList
                // Do not make the call if async is not supported
                .Where(attr => (attr.SupportedDataMethods & AFDataMethods.Asynchronous) == AFDataMethods.Asynchronous)
                .Select(async attr =>
                {
                    try
                    {
                        AFSummaryTypes mySummaries = AFSummaryTypes.Minimum | AFSummaryTypes.Maximum | AFSummaryTypes.Average | AFSummaryTypes.Total;
                        AFTimeRange timeRange = new AFTimeRange(new AFTime("*-1d"), new AFTime("*"));

                        return await attr.Data.SummaryAsync(
                            timeRange: timeRange,
                            summaryType: mySummaries,
                            calculationBasis: AFCalculationBasis.TimeWeighted,
                            timeType: AFTimestampCalculation.Auto,
                            cancellationToken: token);
                    }
                    catch (AggregateException ae)
                    {
                        Console.WriteLine("{0}: {1}", attr.Element.Name, ae.Flatten().InnerException.Message);
                        return null;
                    }
                    catch (OperationCanceledException oe)
                    {
                        Console.WriteLine("{0}: {1}", attr.Element.Name, oe.Message);
                        return null;
                    }
                })
                .ToArray();

            // Define a task that completes when all subtasks are complete
            Task<IDictionary<AFSummaryTypes, AFValue>[]> task = Task.WhenAll(tasks);

            // Asychronously wait for either the summaries or timer task to complete
            if (await Task.WhenAny(task, Task.Delay(timeoutInMilliseconds, tokenForTimer)) == task)
            {
                // Cancel the timer task
                ctsForTimer.Cancel();
                // Return summaries result
                return task.Result;
            }
            else
            {
                // Cancel the summaries task if timeout
                cts.Cancel();
                throw new TimeoutException("The operation has timed out.");
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

       
    }
}


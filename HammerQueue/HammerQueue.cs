using HammerQueue.Multiprocessing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HammerQueue;

public static class Tasks
{
    public static int NumberOfThreads() => (Environment.ProcessorCount < 3) ? 1 : Environment.ProcessorCount - 1;
    //Makes logical sense, get it? :P In reality, we have processors, cores and threads...

    private static readonly ParallelOptions ParallelOptions = new()
    {
        MaxDegreeOfParallelism = NumberOfThreads()
    };

    public static async Task<BatchWork> RunAsync(BatchWork batchWork, bool recreateIfCompleted = false, bool reset = false)
    {
        //This is where the magic happens :)
        switch (batchWork.Tasks.Count)
        {
            case 0:
                break;

            case 1:
                await batchWork.Tasks[0].RunAsync(recreateIfCompleted).ConfigureAwait(false);
                break;

            default:
                var ioTasks = Task.Run(() => Task.WhenAll(batchWork.Tasks.AsReadOnly().AsParallel().WithDegreeOfParallelism(NumberOfThreads()).Where(x => x is
                    {
                        IsIoBound: true
                    })
                    .Select(task => task.RunAsync(recreateIfCompleted))).ConfigureAwait(false));
                
                // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
                await Parallel.ForEachAsync(batchWork.Tasks.AsReadOnly().AsParallel().WithDegreeOfParallelism(NumberOfThreads()).Where(x => x is { IsIoBound: false }),
                    ParallelOptions, async (task, _) => await task.RunAsync(recreateIfCompleted).ConfigureAwait(false)).ConfigureAwait(false);

                using (ioTasks)
                {
                    await ioTasks.ConfigureAwait(false);
                };

                break;
        }

        if (reset)
            batchWork.Reset();

        return batchWork;
    }
}
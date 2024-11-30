﻿using HammerQueue.Multiprocessing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HammerQueue;

public static class Tasks
{
    private static readonly ParallelOptions ParallelOptions = new()
    {
        MaxDegreeOfParallelism = (Environment.ProcessorCount - 2) //Makes logical sense, get it? :P In reality, we have processors, cores and threads...
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
                var ioTasks = Task.Run(() => Task.WhenAll(batchWork.Tasks.AsReadOnly().AsParallel().Where(x => x.IsIoBound).Select(task => task.RunAsync(recreateIfCompleted))).ConfigureAwait(false));
                
                await Parallel.ForEachAsync(batchWork.Tasks.AsReadOnly().AsParallel().Where(x => x.IsIoBound is false), ParallelOptions, async (task, _) =>
                await task.RunAsync(recreateIfCompleted).ConfigureAwait(false)).ConfigureAwait(false);

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
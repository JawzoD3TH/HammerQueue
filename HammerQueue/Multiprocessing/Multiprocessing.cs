﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HammerQueue.Multiprocessing;

public sealed class BatchWork
{
    //public readonly System.Collections.Concurrent.ConcurrentDictionary<int, dynamic?> Results = [];  //Slower, just as unreliable
    public readonly Dictionary<int, dynamic?> Results = [];

    public readonly IList<MultiProcessTask> Tasks = [];

    public void Add(in bool isIoBound, ref int index, Func<dynamic?> function, string? name = null)
    {
        name ??= string.Empty;
        //_ = System.Threading.Interlocked.Increment(ref index); //Sounds clever, but there's nothing sharing the index anyway

        int curIndex = index;
        Tasks.Add(new MultiProcessTask(ref index, in isIoBound, name, () => _ = Results.TryAdd(curIndex, function())));
    }

    public async Task RemoveCompletedAsync()
    {
        //Not ideal, but it's only designed for error cases
        await Parallel.ForEachAsync(Results.Keys.AsParallel().WithDegreeOfParallelism(HammerQueue.Tasks.NumberOfThreads()), async (index, _) =>
        {
            // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
            var task = Tasks.FirstOrDefault(t => t?.Index == index);
            if (task != null)
                Tasks.Remove(task);

            await Task.Yield();
        }).ConfigureAwait(false);
    }

    public void Reset()
    {
        //Allows us to simply reuse the resource
        Tasks.Clear();
        Results.Clear();
    }
}

public sealed class MultiProcessTask : IDisposable
{
    public readonly int Index;
    public readonly bool IsIoBound;
    public readonly string Name;
    private readonly Action _action;
    private Task? _task;

    public MultiProcessTask(ref int index, in bool isIoBound, in string name, Action action)
    {
        Name = name;
        Index = index;
        IsIoBound = isIoBound;
        _action = action;
        _task = new Task(_action);
    }

    public void Dispose()
    {
        try //Ideally shouldn't be necessary to wrap the ?.Dispose() in a try/ catch, but sometimes a task state is not as expected
        {
            _task?.Dispose();
        }
        catch
        {
            // ignored
        }
    }

    public async Task RunAsync(bool recreateIfCompleted = false)
    {
        if (recreateIfCompleted)
            _task = new Task(_action);
        else if (_task is null || _task.IsCompleted)
            return;

        using (this)
        {
            _task.Start();
            await Task.Yield();
            await _task.ConfigureAwait(false);
        }
    }
}
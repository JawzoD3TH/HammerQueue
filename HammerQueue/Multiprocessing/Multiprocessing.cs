﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HammerQueue.Multiprocessing;

public sealed class BatchWork
{
    public readonly System.Collections.Concurrent.ConcurrentDictionary<int, dynamic?> Results = [];
    //public readonly Dictionary<int, dynamic?> Results = []; //Faster, just as unreliable

    public readonly IList<MultiProcessTask> Tasks = [];

    public void Add(in bool isIoBound, Func<dynamic?> function, string? name = null, int index = 1)
    {
        name ??= string.Empty;
        index += Tasks.Count;
        //_ = System.Threading.Interlocked.Increment(ref index); //Sounds clever but there's nothing sharing the index anyway

        Tasks.Add(new MultiProcessTask(ref index, in isIoBound, name, () => _ = Results.TryAdd(index, function())));
    }

    public void RemoveCompleted()
    {
        //Not ideal but it's only designed for error cases
        foreach (var index in Results.Keys)
            Tasks.Remove(Tasks.First(t => t.Index == index));

    }

    public void Reset()
    {
        //Allows use to simply reuse the resource
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

    public void Dispose() => _task?.Dispose();

    public async Task RunAsync(bool recreateIfCompleted = false)
    {
        if (recreateIfCompleted)
            _task = new Task(_action);
        else if (_task is null || _task.IsCompleted)
            return;

        using (this)
        {
            _task.Start();
            await _task.ConfigureAwait(false);
        };
    }
}
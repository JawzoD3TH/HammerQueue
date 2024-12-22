using HammerQueue.Multiprocessing;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using static Testing.Parameters;

namespace Testing;

internal static class Program
{
    private const int Thousand = 1000;
    private static readonly Stopwatch Stopwatch = new();

    internal static async Task Main()
    {
        //Edit test cycles, display settings and test names as appropriate
        await TestAsync(1, 5).ConfigureAwait(false);
        await TestAsync(10, 5).ConfigureAwait(false);
        await TestAsync(100, 5).ConfigureAwait(false);
        await TestAsync(Thousand).ConfigureAwait(false);
        await TestAsync(10_000, 5).ConfigureAwait(false);
        await TestAsync(100_000, 5).ConfigureAwait(false);

        Console.WriteLine("-- DONE --");
        Console.ReadKey();
    }

    private static void Microseconds(in string name) =>
        Console.WriteLine($"{name}: {1000000 * (double)Stopwatch.ElapsedTicks / Stopwatch.Frequency} microseconds"); //Microseconds from Elapsed Ticks

    private static async Task ShowResultsAsync(int roundsPerTest, BatchWork batchWork)
    {
        //Displays the Results and what percentage completed
        if (roundsPerTest <= 101 && batchWork.Tasks.Count >= batchWork.Results.Count)
        {
            if (batchWork.Results.Count > 10)
                await Task.Delay(Thousand).ConfigureAwait(false);

            for (var i = 0; i < batchWork.Results.Count; i++)
            {
                Console.WriteLine($"{batchWork.Results.ElementAt(i).Key}) Result: {batchWork.Results.ElementAt(i).Value?
                    .ToString()} (Is IO Bound: {batchWork.Tasks.First(t => t.Index == batchWork.Results.ElementAt(i).Key).IsIoBound})");
            }
        }

        if (!batchWork.Results.IsEmpty && batchWork.Tasks.Count >= batchWork.Results.Count)
            Console.WriteLine($"Accuracy: {100d * batchWork.Results.Count / batchWork.Tasks.Count}%");
    }

    private static async Task TestAsync(int roundsPerTest, int noOfTests = 10, bool showResultsForA = false, string testNameA = "Test A", string testNameB = "Test B")
    {
        //Do not modify this function, it's the same for all tests
        BatchWork batchWork = new();

        Console.WriteLine($"Test Size: {roundsPerTest}");
        roundsPerTest++;
        for (var x = 0; x < noOfTests; x++)
        {
            Console.WriteLine($"Round: {x + 1}/{noOfTests}");

            for (var i = 1; i < roundsPerTest; i++)
            {
                if (i % 2 == 0)
                    batchWork.Add(true, ref i, () => SimulateIoBoundCall(), nameof(SimulateIoBoundCall));
                else batchWork.Add(false, ref i, () => SimulateCpuBoundCall(), nameof(SimulateCpuBoundCall));
            }

            await HalfCycleAsync(roundsPerTest, TestAAsync(batchWork), testNameA, showResultsForA).ConfigureAwait(false);
            await Task.Yield();
            await HalfCycleAsync(roundsPerTest, TestBAsync(batchWork), testNameB, true).ConfigureAwait(false);
            await Task.Delay(3500).ConfigureAwait(false); //Time to view results
            batchWork.Reset();
        }
    }

    public static async Task HalfCycleAsync(int roundsPerTest, Task<BatchWork> task, string testName, bool showResults)
    {
        await Task.Delay(Thousand).ConfigureAwait(false);
        Stopwatch.Start();

        var batchWork = await task.ConfigureAwait(false);

        Stopwatch.Stop();
        Microseconds(testName);

        if (showResults)
            await ShowResultsAsync(roundsPerTest, batchWork).ConfigureAwait(false);

        batchWork.Results.Clear();
        Stopwatch.Reset();
    }
}
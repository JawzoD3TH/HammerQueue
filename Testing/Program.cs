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
        await TestAsync(100).ConfigureAwait(false);
        await TestAsync(1000).ConfigureAwait(false);
        await TestAsync(10_000).ConfigureAwait(false);

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
                await Task.Delay(Thousand);

            for (var i = 0; i < batchWork.Results.Count; i++)
            {
                Console.WriteLine($"{batchWork.Results.ElementAt(i).Key}) Result: {batchWork.Results.ElementAt(i).Value?
                    .ToString()} (Is IO Bound: {batchWork.Tasks.First(t => t.Index == batchWork.Results.ElementAt(i).Key).IsIoBound})");
            }
        }

        if (batchWork.Results.Count > 0 && batchWork.Tasks.Count >= batchWork.Results.Count)
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

            await Task.Delay(Thousand);
            Stopwatch.Start();

            batchWork = await TestAAsync(batchWork).ConfigureAwait(false);

            Stopwatch.Stop();
            Microseconds(testNameA);

            if (showResultsForA)
                await ShowResultsAsync(roundsPerTest, batchWork).ConfigureAwait(false);

            batchWork.Results.Clear();
            await Task.Delay(Thousand);

            Stopwatch.Restart();

            batchWork = await TestBAsync(batchWork).ConfigureAwait(false);

            Stopwatch.Stop();
            Microseconds(testNameB);

            await ShowResultsAsync(roundsPerTest, batchWork).ConfigureAwait(false);

            await Task.Delay(3000);

            batchWork.Reset();
        }
    }
}
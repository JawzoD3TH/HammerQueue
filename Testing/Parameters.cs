using HammerQueue.Multiprocessing;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Testing
{
    internal static class Parameters
    {
        private static readonly string FilePath = Path.Combine(Directory.GetCurrentDirectory(), "LOREM.TXT");
        private static readonly Random Random = new();

        public static int SimulateCpuBoundCall() => Random.Next(int.MinValue, int.MaxValue); //Sample CPU-bound task

        public static string SimulateIoBoundCall()
        {
            //Sample IO-bound task
            using FileStream fileStream = new(FilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using StreamReader reader = new(fileStream);
            return reader.ReadToEnd().Substring(Random.Next(0, 10), Random.Next(11, 100));
        }

        public static async Task<BatchWork> TestAAsync(BatchWork batchWork)
        {
            //Default Comparison Code
            switch (batchWork.Tasks.Count)
            {
                case 0:
                    break;

                case 1:
                    await batchWork.Tasks[0].RunAsync().ConfigureAwait(false);
                    break;

                default:
                    foreach (var task in batchWork.Tasks)
                        await task.RunAsync().ConfigureAwait(false);

                    break;
            }

            return batchWork;
        }

        public static async Task<BatchWork> TestBAsync(BatchWork batchWork)
        {
            #region Delete This Region To Use Your Own Code (Even Without HammerQueue.Tasks)
            await HammerQueue.Tasks.RunAsync(batchWork, true).ConfigureAwait(false);
#if DEBUG
            while (batchWork.Tasks.Count > batchWork.Results.Count)
                batchWork = await TestBFailuresAsync(batchWork).ConfigureAwait(false);
#endif
            return batchWork;
            #endregion
        }

        private static async Task<BatchWork> TestBFailuresAsync(BatchWork batchWork)
        {
            Console.WriteLine(string.Concat($"{batchWork.Tasks.Count - batchWork.Results.Count} Tasks Failed! Running remaining...",
                    Environment.NewLine, $"Accuracy: {100d * batchWork.Results.Count / batchWork.Tasks.Count}%"));
            await batchWork.RemoveCompletedAsync().ConfigureAwait(false);
            return await HammerQueue.Tasks.RunAsync(batchWork, true).ConfigureAwait(false);
        }
    }
}
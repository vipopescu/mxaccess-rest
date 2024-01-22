using System.Diagnostics;
using ArchestrA.GRAccess;
using Microsoft.Extensions.Options;
using MXAccesRestAPI.Classes;
using MXAccesRestAPI.MXDataHolder;
using MXAccesRestAPI.Settings;
using MXAccess_RestAPI.DBContext;

namespace MXAccesRestAPI.GRAccess
{
    public class GRAccessReadingService : BackgroundService
    {
        public bool IsFetchComplete;

        private readonly GalaxySettings _mySettings;

        public GRAccessApp grAccess = new GRAccessAppClass();

        private readonly IServiceScopeFactory _scopeFactory;

        private readonly IMXDataHolderServiceFactory _imxDataHolderFactory;

        private readonly int _numberOfThreads;


        public GRAccessReadingService(IOptions<MxDataDataServiceSettings> mxDataServiceSettings, IOptions<GalaxySettings> settings, IServiceScopeFactory scopeFactory, IMXDataHolderServiceFactory imxDataHolderFactory)
        {
            _scopeFactory = scopeFactory;
            _mySettings = settings.Value;
            _imxDataHolderFactory = imxDataHolderFactory;
            IsFetchComplete = false;
            _numberOfThreads = mxDataServiceSettings.Value.MxDataServiceThreads;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Stopwatch stopwatch = new();
            stopwatch.Start();

            Console.WriteLine("Getting runtime objects...");

            List<string> instancesTagNames = [];
            using (var scope = _scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<GRDBContext>();
                instancesTagNames = dbContext.GetRuntimeObjectInstances();
            }

            // Register to _Attributed
            Console.WriteLine("Found " + instancesTagNames.Count + " variables to register...");

            RegisterMxTags([.. instancesTagNames]);

            Console.WriteLine("Finished loading data...");

            stopwatch.Stop();
            Console.WriteLine($"Time elapsed: {stopwatch.Elapsed.Minutes} minutes and {stopwatch.Elapsed.Seconds} seconds");

            return Task.CompletedTask;
        }

        /// <summary>
        /// Registers UDAs and other related MX tags
        /// </summary>
        /// <param name="galaxy"></param>
        /// <param name="tag_names"></param>
        public void RegisterMxTags(string[] tag_names)
        {

            int numberOfThreads = _numberOfThreads;
            if (numberOfThreads > tag_names.Length) numberOfThreads = 1;
            int segmentSize = tag_names.Length / numberOfThreads;

            for (int i = 0; i < numberOfThreads; i++)
            {
                // init MxDataHolderService per thread
                int locali = i;

                // add segment of tags to mxdataholder (AddItem)
                int segmentStart = i * segmentSize;
                int segmentEnd = (i == numberOfThreads - 1) ? tag_names.Length : segmentStart + segmentSize;
                ArraySegment<string> segment = new(tag_names, segmentStart, segmentEnd - segmentStart);

                // TPL (Task Parallel Library)
                // These longrunning threads will be continously running in background
                Task.Factory.StartNew(() =>
                 {
                     int threadIndex = locali + 1; // Fix for closure issue
                     MXDataProcessorService mxDataHolderService;
                     try
                     {
                         mxDataHolderService = _imxDataHolderFactory.Create(threadIndex);


                         foreach (string tag_name in segment)
                         {
                             string fullRefName = tag_name + "._Attributes";
                             mxDataHolderService.AddItem(fullRefName);
                         }

                         mxDataHolderService.AdviseAll();

                         _imxDataHolderFactory.MonitorAlarmsOnThread(threadIndex);
                     }
                     catch (Exception e)
                     {

                         Console.WriteLine($"[Thrd: {threadIndex}] Couldn't be created so it's not being used");

                         //if (!e.Message.Contains("E_ACCESSDENIED"))
                         //{
                         //    throw;
                         //}
                         //Console.WriteLine($"Creating Service Attemp #2 [Thrd: {threadIndex}]");
                         //// Attemp 2
                         //mxDataHolderService = _imxDataHolderFactory.Create(threadIndex);

                     }

                 }, TaskCreationOptions.LongRunning);

                //Thread.Sleep(2000);
            }

            _imxDataHolderFactory.RegisterOnInitializationComplete();
        }



        /// <summary>
        /// Retrives GrAccess Galaxy
        /// </summary>
        /// <returns></returns>
        IGalaxy? RetrieveGalaxy()
        {
            // Query the Galaxies, check if we can reach the server and if the desired
            // galaxy exists
            Console.WriteLine("Querying galaxies from " + _mySettings.GalaxyHost);

            var galaxies = grAccess.QueryGalaxies(_mySettings.GalaxyHost);
            if (!grAccess.CommandResult.Successful)
            {
                Console.WriteLine("Failed to fetch galaxies, is this a GR node?");
                return null;
            }

            // Attempt to login to the galaxy
            Console.WriteLine("Logging in to " + _mySettings.GalaxyName + " as " + _mySettings.GalaxyUserName);
            IGalaxy galaxy = galaxies[_mySettings.GalaxyName];
            galaxy.Login(_mySettings.GalaxyUserName, _mySettings.GalaxyPassword);
            if (!galaxy.CommandResult.Successful)
            {
                Console.WriteLine("Login to galaxy " + _mySettings.GalaxyName + " Failed :" +
                galaxy.CommandResult.Text + " : " +
                galaxy.CommandResult.CustomMessage);
                return null;
            }
            return galaxy;
        }
    }
}


using System.Diagnostics;
using System.Xml.Serialization;
using ArchestrA.GRAccess;
using Microsoft.Extensions.Options;
using MXAccesRestAPI.Classes;
using MXAccesRestAPI.MXDataHolder;
using MXAccesRestAPI.Settings;
using MXAccesRestAPI.XML;
using MXAccess_RestAPI.DBContext;

namespace MXAccesRestAPI.GRAccess
{
    public class GRAccessReadingService : BackgroundService
    {
        public bool IsFetchComplete;

        private readonly GalaxySettings _mySettings;

        public GRAccessApp grAccess = new GRAccessAppClass();

        private readonly IServiceScopeFactory _scopeFactory;

        private IMXDataHolderService _mxDataHolder;

        private int attrCount;

        public GRAccessReadingService(IOptions<GalaxySettings> settings, IServiceScopeFactory scopeFactory, IMXDataHolderService mxDataHolder)
        {
            _scopeFactory = scopeFactory;
            _mySettings = settings.Value;
            IsFetchComplete = false;
            _mxDataHolder = mxDataHolder;
            attrCount = 0;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();


            Console.WriteLine("Starting GR Reading Service...");
            IGalaxy? galaxy = RetrieveGalaxy();
            if (galaxy == null)
            {
                Console.WriteLine("Cannot retrieve the galaxy.");
                return Task.CompletedTask;
            }

            Console.WriteLine("Getting runtime objects...");

            List<string> instancesTagNames = new List<string>();
            using (var scope = _scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<GRDBContext>();
                instancesTagNames = dbContext.GetRuntimeObjectInstances();
            }

            Console.WriteLine("Found " + instancesTagNames.Count + " variables to register...");
            GetUDAInfo(galaxy, instancesTagNames.ToArray());

            Console.WriteLine("Finished loading data...");

            stopwatch.Stop();
            Console.WriteLine($"Time elapsed: {stopwatch.Elapsed.Minutes} minutes and {stopwatch.Elapsed.Seconds} seconds");

            return Task.CompletedTask;
        }

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

        private void GetUDAInfo(IGalaxy galaxy, ArraySegment<string> tag_names, EgObjectIsTemplateOrInstance objectType, int thread)
        {
            try
            {
                UDAInfo? resultUDA = null;
                UDAInfo? resultInheritedUDA = null;

                IgObjects instances = galaxy.QueryObjectsByName(EgObjectIsTemplateOrInstance.gObjectIsInstance, tag_names.ToArray());
                foreach (IgObject item in instances)
                {
                    IAttribute udaInfo = item.Attributes["UDAs"];
                    IAttribute inheritedUdaInfo = item.Attributes["_InheritedUDAs"];
                    string fullRefName;

                    if (!string.IsNullOrEmpty(udaInfo.value.GetString()))
                    {
                        XmlSerializer serializerUDA = new XmlSerializer(typeof(UDAInfo));
                        StringReader readerUDA = new StringReader(udaInfo.value.GetString());
                        resultUDA = serializerUDA.Deserialize(readerUDA) as UDAInfo;
                        foreach (var attr in resultUDA.Attributes)
                        {
                            fullRefName = item.Tagname + "." + attr.Name;
                            _mxDataHolder.AddItem(new MXAttribute { TagName = fullRefName });
                            attrCount++;
                        }
                    }

                    if (!string.IsNullOrEmpty(inheritedUdaInfo.value.GetString()))
                    {
                        XmlSerializer serializerInheritedUDA = new XmlSerializer(typeof(UDAInfo));
                        StringReader readerInheritedUDA = new StringReader(inheritedUdaInfo.value.GetString());
                        resultInheritedUDA = (UDAInfo)serializerInheritedUDA.Deserialize(readerInheritedUDA);
                        foreach (UDAAttribute attr in resultInheritedUDA.Attributes)
                        {
                            fullRefName = item.Tagname + "." + attr.Name;
                            _mxDataHolder.AddItem(new MXAttribute { TagName = fullRefName });
                            attrCount++;
                        }
                    }
                    item.Unload();
                }
            }
            catch (System.Exception ex)
            {
                Console.WriteLine("EXCEPTION - " + ex.ToString());
            }
        }

        /// <summary>
        /// Creates n number of threads and makes as many api calls to GR access to all instances to get the attributes configuration.
        /// I do this rather than one single query because it reduces the execution time by 25% approx. 
        /// </summary>
        /// <param name="galaxy"></param>
        /// <param name="tag_name"></param>
        public void GetUDAInfo(IGalaxy galaxy, string[] tag_name)
        {
            int numberOfThreads = 10;

            if (numberOfThreads < tag_name.Length) numberOfThreads = 1;
            int segmentSize = tag_name.Length / numberOfThreads;
            Task[] tasks = new Task[numberOfThreads];

            for (int i = 0; i < numberOfThreads; i++)
            {
                int thid = i;
                int segmentStart = i * segmentSize;
                int segmentEnd = (i == numberOfThreads - 1) ? tag_name.Length : segmentStart + segmentSize;
                ArraySegment<string> segment = new ArraySegment<string>(tag_name, segmentStart, segmentEnd - segmentStart);
                tasks[i] = Task.Run(() => GetUDAInfo(galaxy, segment, EgObjectIsTemplateOrInstance.gObjectIsInstance, thid));
            }

            // Wait for all tasks to complete
            Task.WaitAll(tasks);

            _mxDataHolder.AdviseAll();
        }
    }
}
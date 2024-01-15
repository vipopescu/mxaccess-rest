
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


            Console.WriteLine("Getting runtime objects...");

            List<string> instancesTagNames = new List<string>();
            using (var scope = _scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<GRDBContext>();
                instancesTagNames = dbContext.GetRuntimeObjectInstances();
            }

            // Register to _Attributed
            Console.WriteLine("Found " + instancesTagNames.Count + " variables to register...");
            GetUDAInfo(instancesTagNames.ToArray());

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

        // private void GetUDAInfo(IGalaxy galaxy, ArraySegment<string> tag_names, EgObjectIsTemplateOrInstance objectType, int thread)
        // {
        //     Console.WriteLine("Wait what? ");
        //     try
        //     {
        //         UDAInfo? resultUDA = null;
        //         UDAInfo? resultInheritedUDA = null;

        //         IgObjects instances = galaxy.QueryObjectsByName(EgObjectIsTemplateOrInstance.gObjectIsInstance, tag_names.ToArray());
        //         foreach (IgObject item in instances)
        //         {


        //             string fullRefName;

        //             string attr_list_str = "";

        //             // foreach (IAttribute attr in item.Attributes)
        //             // {
        //             //     if (attr.Name == "_Attributes") attr_list_str = attr.value.GetString();
        //             // }
        //             IAttribute attr = item.Attributes["_Attributes"];
        //             Console.WriteLine(attr.value.GetString());

        //             string[] attr_list = attr_list_str.Split(',');

        //             foreach (string attr_str in attr_list)
        //             {
        //                 fullRefName = item.Tagname + "." + attr;
        //                 Console.WriteLine("Registering " + fullRefName);

        //                 _mxDataHolder.AddItem(new MXAttribute { TagName = fullRefName });
        //                 attrCount++;
        //             }

        //             // IAttribute udaInfo = item.Attributes["UDAs"];
        //             // IAttribute inheritedUdaInfo = item.Attributes["_InheritedUDAs"];
        //             // string fullRefName;

        //             // if (!string.IsNullOrEmpty(udaInfo.value.GetString()))
        //             // {
        //             //     XmlSerializer serializerUDA = new XmlSerializer(typeof(UDAInfo));
        //             //     StringReader readerUDA = new StringReader(udaInfo.value.GetString());
        //             //     resultUDA = serializerUDA.Deserialize(readerUDA) as UDAInfo;
        //             //     foreach (var attr in resultUDA.Attributes)
        //             //     {
        //             //         fullRefName = item.Tagname + "." + attr.Name;
        //             //         _mxDataHolder.AddItem(new MXAttribute { TagName = fullRefName });
        //             //         attrCount++;
        //             //     }
        //             // }

        //             // if (!string.IsNullOrEmpty(inheritedUdaInfo.value.GetString()))
        //             // {
        //             //     XmlSerializer serializerInheritedUDA = new XmlSerializer(typeof(UDAInfo));
        //             //     StringReader readerInheritedUDA = new StringReader(inheritedUdaInfo.value.GetString());
        //             //     resultInheritedUDA = (UDAInfo)serializerInheritedUDA.Deserialize(readerInheritedUDA);
        //             //     foreach (UDAAttribute attr in resultInheritedUDA.Attributes)
        //             //     {
        //             //         fullRefName = item.Tagname + "." + attr.Name;
        //             //         _mxDataHolder.AddItem(new MXAttribute { TagName = fullRefName });
        //             //         attrCount++;
        //             //     }
        //             // }
        //             item.Unload();
        //         }
        //     }
        //     catch (System.Exception ex)
        //     {
        //         Console.WriteLine("EXCEPTION - " + ex.ToString());
        //     }
        // }

        /// <summary>
        /// </summary>
        /// <param name="galaxy"></param>
        /// <param name="tag_names"></param>
        public void GetUDAInfo(string[] tag_names)
        {

            foreach (string tag_name in tag_names)
            {
                string fullRefName = tag_name + "._Attributes";
                _mxDataHolder.AddItem(new MXAttribute { TagName = fullRefName });
                attrCount++;
            }
            _mxDataHolder.AdviseAll();
        }
    }
}
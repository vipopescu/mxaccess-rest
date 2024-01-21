using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using MXAccesRestAPI.Classes;
using MXAccesRestAPI.Settings;

namespace MXAccesRestAPI.MXDataHolder
{
    public class MXDataHolderServiceFactory : IMXDataHolderServiceFactory
    {
        private readonly ConcurrentDictionary<int, MXAttribute> _datastore;
        private readonly MxDataDataServiceSettings _settings;
        private readonly AttributeConfigSettings _attributeConfig;



        public MXDataHolderServiceFactory(ConcurrentDictionary<int, MXAttribute> datastore, IOptions<MxDataDataServiceSettings> settings, AttributeConfigSettings attributeConfig)
        {
            _datastore = datastore;
            _settings = settings.Value;
            _attributeConfig = attributeConfig;
        }

        public MXDataHolderService Create(int threadNumber)
        {
            return new MXDataHolderService(threadNumber, _settings.ServerName,_settings.LmxVerifyUser, _attributeConfig.AllowedTagAttributes, _datastore);
        }
    }


}
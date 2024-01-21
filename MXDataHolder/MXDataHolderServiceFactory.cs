using System.Collections.Concurrent;
using MXAccesRestAPI.Classes;

namespace MXAccesRestAPI.MXDataHolder
{
    public class MXDataHolderServiceFactory : IMXDataHolderServiceFactory
    {
        private readonly ConcurrentDictionary<int, MXAttribute> _datastore;

        public MXDataHolderServiceFactory(ConcurrentDictionary<int, MXAttribute> datastore)
        {
            _datastore = datastore;
        }

        public MXDataHolderService Create(int threadNumber, string serverName, List<string> allowedAttributes)
        {
            return new MXDataHolderService(threadNumber, serverName, allowedAttributes, _datastore);
        }
    }


}
using System.Collections.Concurrent;
using MXAccesRestAPI.Classes;
using MXAccesRestAPI.Global;

namespace MXAccesRestAPI.MXDataHolder
{

    /// <summary>
    /// Data provider service that provides access to a thread safe mx data store
    /// </summary>
    public class MxDataProviderService : IDataProviderService
    {

        private readonly ConcurrentDictionary<int, MXAttribute> _mxDataStore = [];

        public bool AddItem(MXAttribute item)
        {
            return _mxDataStore.TryAdd(item.Key, item);
        }

        public List<MXAttribute> GetAllData()
        {
            return [.. _mxDataStore.Values];
        }

        public List<MXAttribute> GetBadAndUncertainData()
        {
            return _mxDataStore
                  .Where(kvp => !GlobalConstants.IsGood(kvp.Value.Quality))
                  .Select(kvp => kvp.Value)
                  .ToList();
        }

        public List<MXAttribute> GetBadAndUncertainData(string instance)
        {
            return _mxDataStore
                  .Where(kvp => !GlobalConstants.IsGood(kvp.Value.Quality) && kvp.Value.TagName.StartsWith(instance))
                  .Select(kvp => kvp.Value)
                  .ToList();
        }

        public MXAttribute? GetData(int key)
        {
            if (!_mxDataStore.TryGetValue(key, out MXAttribute? value))
            {
                return null;
            }
            return value.Value as MXAttribute;
        }

        public MXAttribute? GetData(string fullattrName)
        {
            var item = _mxDataStore.FirstOrDefault(a => a.Value.TagName == fullattrName);
            if (item.Value == null)
            {
                return null;
            }
            return item.Value;
        }

        public List<MXAttribute> GetInstanceData(string instance)
        {
            return _mxDataStore
                       .Where(kvp => kvp.Value.TagName.StartsWith(instance))
                       .Select(kvp => kvp.Value)
                       .ToList();
        }

        public bool RemoveData(string tagName)
        {
            var item = _mxDataStore.FirstOrDefault(a => a.Value.TagName == tagName);
            if (item.Value == null)
                return false;
            return _mxDataStore.TryRemove(item);

        }

        public bool RemoveData(int id)
        {
            return _mxDataStore.TryRemove(id, out var valueRemoved);
        }
    }
}
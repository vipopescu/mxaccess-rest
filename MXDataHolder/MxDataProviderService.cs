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

        public event IDataProviderService.OnDataWriteChangeEventHandler? OnDataWrite;

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
            return value;
        }

        public MXAttribute? GetData(string tagName)
        {
            var item = _mxDataStore.FirstOrDefault(a => a.Value.TagName == tagName);
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
            var item = GetData(tagName);
            if(item == null)
            {
                return true;
            }
            return RemoveData(item.Key);

        }

        public bool RemoveData(int id)
        {
            return _mxDataStore.TryRemove(id, out var valueRemoved);
        }

        public void WriteData(string tagName, object value, DateTime? timeStamp)
        {
            // notify about the write, but don't update value (let LMX do that)
            OnDataWrite?.Invoke(tagName, value, timeStamp);
        }
    }
}
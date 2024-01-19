using System.Collections.Concurrent;
using System.Globalization;
using ArchestrA.GRAccess;
using ArchestrA.MxAccess;
using MXAccesRestAPI.Classes;
using MXAccesRestAPI.Global;
using static MXAccesRestAPI.MXDataHolder.IMXDataHolderService;

namespace MXAccesRestAPI.MXDataHolder
{

    public class MXDataHolderService : IMXDataHolderService
    {


        // Event for data store changes
        public event DataStoreChangeEventHandler? OnDataStoreChanged;
        private readonly int _threadNumber;

        private static readonly List<string> _allowedAttributes = [];
        private readonly ConcurrentDictionary<int, MXAttribute> _dataStore;


        private static LMXProxyServerClass _LMX_Server = new();



        public int hLMX;
        public int userLMX;
        public string ServerName;

        public MXDataHolderService(int threadNumber, string serverName, List<string> allowedAttributes, ConcurrentDictionary<int, MXAttribute> datastore)
        {

            _threadNumber = threadNumber;
            Console.WriteLine($"START Registered MXDataHolderService [thread {_threadNumber}]...");
            // _dataStore = new ConcurrentDictionary<int, MXAttribute>();
            _dataStore = datastore;

            _allowedAttributes.AddRange(allowedAttributes);
            hLMX = 0;
            ServerName = serverName;
            userLMX = 0;
            Register();
            //RegisterUser();
            Console.WriteLine($"END Registered MXDataHolderService [thread {threadNumber}]...");
        }
        ~MXDataHolderService()
        {
            Console.WriteLine($"Destroying [thread {_threadNumber}]...");
            Unregister();
        }

        /// <summary>
        /// Subscribes to updates for a specific tag.
        /// </summary>
        /// <param name="tagName"></param>
        public void Advise(string tagName)
        {
            var item = _dataStore.FirstOrDefault(a => a.Value.TagName == tagName && a.Value.CurrentThread == _threadNumber);
            if (!item.Value.OnAdvise)
            {
                _LMX_Server.Advise(hLMX, GetLmxTagKey(item.Key));
                item.Value.OnAdvise = true;
            }
        }

        /// <summary>
        /// Subscribes to updates for all tags of a specific device
        /// </summary>
        /// <param name="tagName"></param>
        public void AdviseDevice(string device_name)
        {

            var items = _dataStore.Where(a => a.Value.TagName.StartsWith(device_name) && a.Value.CurrentThread == _threadNumber).Select(a => a);
            foreach (var item in items)
            {
                if (!item.Value.OnAdvise)
                {
                    _LMX_Server.Advise(hLMX, GetLmxTagKey(item.Key));
                    item.Value.OnAdvise = true;
                }
            }

        }

        /// <summary>
        /// Subscribes to updates for all tags
        /// </summary>
        public void AdviseAll()
        {

            if (_dataStore.IsEmpty)
            {
                return;
            }
            foreach (var item in _dataStore)
            {
                if(item.Value.CurrentThread != _threadNumber){
                    // obj instance of different thread
                    continue;
                }
                if (!item.Value.OnAdvise)
                {
                    _LMX_Server.Advise(hLMX, GetLmxTagKey(item.Key));
                    item.Value.OnAdvise = true;
                }
            }
        }





        private int GetLmxTagKey(int threadKey)
        {
            return threadKey - _threadNumber * 100000;
        }


        private int GetThreadFormattedKey(int lmxKey)
        {
            return _threadNumber * 100000 + lmxKey;
        }

        /// <summary>
        /// Add Tag to tag store
        /// </summary>
        /// <param name="item"></param>
        public void AddItem(MXAttribute item)
        {
            item.CurrentThread ??= _threadNumber;
            if (LXMRegistered())
            {
                try {

                    int key = GetThreadFormattedKey(_LMX_Server.AddItem(hLMX, item.TagName));

                    bool succcess = _dataStore.TryAdd(key, item);
                    if (!succcess)
                    {

                        Console.WriteLine($"[thread {_threadNumber}] > fail to add item [{key}] [{item.TagName}]");
                    }
                    else
                    {
                        Console.WriteLine($"[Thr: {_threadNumber}] [ADDED] [ {item.TagName} ] KEY -> {key}");
                        NotifyDataStoreChange(key, item, DataStoreChangeType.ADDED);
                    }
                }
                catch(Exception e)
                {
                    Console.WriteLine($"");
                }
                


            }
        }



        /// <summary>
        /// Adds a group of MXAttribute items to the data store
        /// </summary>
        /// <param name="items"></param>
        public void AddGroupItem(IEnumerable<MXAttribute> items)
        {

            if (LXMRegistered())
            {
                foreach (var item in items)
                {
                     item.CurrentThread ??= _threadNumber;
                    if (item.TagName != null)
                    {
                        int key = GetThreadFormattedKey(_LMX_Server.AddItem(hLMX, item.TagName));
                        _dataStore.TryAdd(key, item);
                        NotifyDataStoreChange(key, item, DataStoreChangeType.ADDED);
                    }
                }
            }
        }

        /// <summary>
        /// Unsubscribes from updates for all tags
        /// </summary>
        public void UnAdviseAll()
        {
            foreach (var item in _dataStore)
                Unadvise(item.Value.TagName);
        }

        /// <summary>
        /// Unsubscribes from updates for a specific tag
        /// </summary>
        /// <param name="value"></param>
        public void Unadvise(string value)
        {
            var item = _dataStore.FirstOrDefault(a => a.Value.TagName == value && a.Value.CurrentThread == _threadNumber);
            if (item.Value != null && item.Value.OnAdvise)
            {
                _LMX_Server.UnAdvise(hLMX, GetLmxTagKey(item.Key));
                item.Value.OnAdvise = false;
            }
        }

        /// <summary>
        /// Unsubscribes from updates for a specific tag by index
        /// </summary>
        /// <param name="index">Datastore key</param>
        public void Unadvise(int index)
        {
            
            if (_dataStore[index].OnAdvise && _dataStore[index].CurrentThread == _threadNumber)
            {
                _LMX_Server.UnAdvise(hLMX, index);
                _dataStore[index].OnAdvise = false;
            }
        }

        /// <summary>
        /// Removes all data from the data store and unsubscribes from updates.
        /// </summary>
        public void RemoveAll()
        {
            UnAdviseAll();

            if (_dataStore.Count != 0)
            {
                foreach (var item in _dataStore)
                    RemoveData(item.Key);
            }
        }

        /// <summary>
        /// Removes a specific tag's data from the data store by tagname
        /// </summary>
        /// <param name="tagName"></param>
        /// <returns></returns>
        public bool RemoveData(string tagName)
        {
            var item = _dataStore.FirstOrDefault(a => a.Value.TagName == tagName);
            if (item.Value == null)
                return false;
            if (item.Value.OnAdvise)
            {
                Unadvise(tagName);
            }

            if (!_dataStore.IsEmpty)
            {
                _LMX_Server.RemoveItem(hLMX, GetLmxTagKey(item.Key));
                _dataStore.TryRemove(item);
                NotifyDataStoreChange(item.Key, item.Value, DataStoreChangeType.REMOVED);

            }
            return true;
        }

        /// <summary>
        /// Removes a specific tag's data from the data store by id
        /// </summary>
        /// <param name="id">Datastore key</param>
        /// <returns></returns> <summary>
        /// 
        /// </summary>
        /// <param name="id">Datastore key</param>
        /// <returns></returns>
        public bool RemoveData(int id)
        {
            if (_dataStore[id].OnAdvise)
            {
                Unadvise(_dataStore[id].TagName);
            }
            _LMX_Server.RemoveItem(hLMX, GetLmxTagKey(id));
            _dataStore.TryRemove(id, out var valueRemoved);
            if (valueRemoved != null)
            {
                NotifyDataStoreChange(id, valueRemoved, DataStoreChangeType.REMOVED);
            }

            return true;
        }


        /// <summary>
        /// Registers the service
        /// </summary>
        public void Register()
        {
            try
            {

                if ((_LMX_Server != null) && (hLMX == 0))
                {
                    hLMX = _LMX_Server.Register(ServerName);
                    _LMX_Server.OnDataChange += new _ILMXProxyServerEvents_OnDataChangeEventHandler(LMX_OnDataChange);
                    //_dataStore = new ConcurrentDictionary<int, MXAttribute>();
                     Console.WriteLine($"[Thr: {_threadNumber}] hLMX [{hLMX}]");

                }
            }
            catch (System.Exception ex)
            {
                Console.WriteLine($"Register: Exception occurred. [thread {_threadNumber}]");
                Console.WriteLine(ex.Message);
                throw;
            }
        }

        public void RegisterUser()
        {
            try
            {
                if (LXMRegistered())
                {
                    Console.WriteLine($"hLMX [{hLMX}] [thread {_threadNumber}]");
                    userLMX = _LMX_Server.AuthenticateUser(hLMX, "vipopescu", "");
                }
            }
            catch (System.Exception ex)
            {
                Console.WriteLine($"RegisterUser: Exception occurred. [thread {_threadNumber}]");
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Checks if the service is registered with the LMX server
        /// </summary>
        /// <returns></returns>
        public bool LXMRegistered()
        {
            return (_LMX_Server != null) && (hLMX != 0);
        }


        /// <summary>
        /// Registers Tag's attributes with 
        /// </summary>
        /// <param name="tag_name">Parent Tag name</param>
        /// <param name="all_attributes">Tag's attributes</param>
        private void RegisterAttributes(string tag_name, string[] all_attributes)
        {
            if (string.IsNullOrEmpty(tag_name)) return;
            string full_tag_name;

            var attributes = all_attributes
              .Where(attribute => (!attribute.Contains('.') || _allowedAttributes.Where(allowedAttr => attribute.Contains(allowedAttr)).ToArray().Length > 0) && !attribute.StartsWith('_'))
              .ToArray();


            foreach (string attribute in attributes)
            {

                full_tag_name = tag_name + "." + attribute;
                AddItem(new MXAttribute { TagName = full_tag_name });
            }
            AdviseDevice(tag_name);
        }

        /// <summary>
        /// Event handler for data changes from the LMX server
        /// </summary>
        /// <param name="hLMXServerHandle"></param>
        /// <param name="phItemHandle"></param>
        /// <param name="pvItemValue"></param>
        /// <param name="pwItemQuality"></param>
        /// <param name="pftItemTimeStamp"></param>
        /// <param name="ItemStatus"></param>
        private void LMX_OnDataChange(int hLMXServerHandle, int phItemHandle, object pvItemValue, int pwItemQuality, object pftItemTimeStamp, ref ArchestrA.MxAccess.MXSTATUS_PROXY[] ItemStatus)
        {

            int threadTagKey = GetThreadFormattedKey(phItemHandle);
            MXAttribute? mxAttr = _dataStore[phItemHandle];
            Console.WriteLine($"LMX_OnDataChange [thread {_threadNumber}] [{threadTagKey}]");


            if (mxAttr != null)
            {
                if (ItemStatus[0].success != 0)
                {
                    try
                    {

                        // Tag's available attributes
                        if (mxAttr.TagName.EndsWith("._Attributes"))
                        {

                            string[] tag_name = mxAttr.TagName.Split('.');
                            string[] attr_list = (string[])pvItemValue;
                            RegisterAttributes(tag_name[0], attr_list);
                            RemoveData(mxAttr.TagName);
                        }
                        else
                        {
                            mxAttr.Quality = pwItemQuality;

                            DateTime dateValue;
                            if (DateTime.TryParse(pftItemTimeStamp.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out dateValue))
                            {
                                mxAttr.TimeStamp = dateValue;
                            }
                            mxAttr.Value = pvItemValue;

                            NotifyDataStoreChange(threadTagKey, mxAttr, DataStoreChangeType.MODIFIED);

                        }
                    }
                    catch (System.Exception ex)
                    {
                        Console.WriteLine("Something wrong parsing " + ex.Message);
                    }
                }
            }
            else
            {
                Console.WriteLine("BIG CRY....");
            }
        }


        /// <summary>
        /// Writes data to a tag
        /// </summary>
        /// <param name="tagName"></param>
        /// <param name="value"></param>
        /// <param name="timeStamp"></param>
        /// <exception cref="Exception"></exception>
        public void WriteData(string tagName, object value, DateTime? timeStamp = null)
        {
            var item = _dataStore.FirstOrDefault(a => a.Value.TagName == tagName);
            if (item.Value == null)
            {
                throw new Exception("Item was not found");
            }
            if (item.Value.OnAdvise)
            {
                if (timeStamp != null)
                {
                    _LMX_Server.Write2(hLMX, GetLmxTagKey(item.Key), value.ToString(), timeStamp, userLMX);
                }
                else
                {
                    _LMX_Server.Write(hLMX, GetLmxTagKey(item.Key), value.ToString(), userLMX);
                }
            }
        }

        /// <summary>
        /// Unregisters the service from the LMX server
        /// </summary>
        public void Unregister()
        {
            if ((_LMX_Server != null) && (hLMX != 0))
            {
                UnAdviseAll();
                RemoveAll();

                _LMX_Server.Unregister(hLMX);
                _LMX_Server = new LMXProxyServerClass();
                hLMX = 0;
            }
        }

        /// <summary>
        /// Raise the event when data is updated
        /// </summary>
        /// <param name="key">datastore key</param>
        /// <param name="data"></param>
        /// <param name="changeType"></param>
        private void NotifyDataStoreChange(int key, MXAttribute data, DataStoreChangeType changeType)
        {
            OnDataStoreChanged?.Invoke(key, data, changeType);
        }

        /// <summary>
        /// Retrieves data for a specific key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public MXAttribute? GetData(int key)
        {
            if (!_dataStore.TryGetValue(key, out MXAttribute? value))
            {
                return null;
            }
            return value.Value as MXAttribute;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tagname"></param>
        /// <returns></returns>
        public MXAttribute? GetData(string tagname)
        {
            var item = _dataStore.FirstOrDefault(a => a.Value.TagName == tagname);
            if (item.Value == null)
            {
                return null;
            }
            return item.Value;
        }

        public int GetCount()
        {
            return _dataStore.Count;
        }

        public List<MXAttribute> GetInstanceData(string tag_name)
        {
            return _dataStore
                    .Where(kvp => kvp.Value.TagName.StartsWith(tag_name))
                    .Select(kvp => kvp.Value)
                    .ToList();
        }

        public List<MXAttribute> GetBadAndUncertainData()
        {
            return _dataStore
                    .Where(kvp => !GlobalConstants.IsGood(kvp.Value.Quality))
                    .Select(kvp => kvp.Value)
                    .ToList();
        }

        public List<MXAttribute> GetBadAndUncertainData(string instance)
        {
            return _dataStore
                    .Where(kvp => !GlobalConstants.IsGood(kvp.Value.Quality) && kvp.Value.TagName.StartsWith(instance))
                    .Select(kvp => kvp.Value)
                    .ToList();
        }

        public List<MXAttribute> GetAllData()
        {
            return _dataStore
                    .Select(kvp => kvp.Value)
                    .ToList();
        }
    }
}
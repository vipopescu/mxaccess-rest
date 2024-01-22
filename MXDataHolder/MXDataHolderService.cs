using System.Globalization;
using System.Timers;
using ArchestrA.MxAccess;
using MXAccesRestAPI.Classes;
using static MXAccesRestAPI.MXDataHolder.IMXDataHolderService;
using Timer = System.Timers.Timer;

namespace MXAccesRestAPI.MXDataHolder
{

    public class MXDataHolderService : IMXDataHolderService
    {

        private System.Timers.Timer timer;

        // Event for data store changes
        public event DataStoreChangeEventHandler? OnDataStoreChanged;
        public readonly int threadNumber;

        private readonly List<string> _allowedAttributes = [];
        private readonly IDataProviderService _dataProvider;


        // LMX Server Config
        private LMXProxyServerClass _LmxServer = new();

        private readonly string _serverName;
        private readonly string _lmxVerifyUser;

        private int _hLmxServerId = 0;
        private int _userLmxId;


        public MXDataHolderService(int threadNumber, string serverName, string lmxVerifyUser, List<string> allowedAttributes, IDataProviderService dataProvider)
        {

            this.threadNumber = threadNumber;
            _dataProvider = dataProvider;

            _allowedAttributes = allowedAttributes;
            _serverName = serverName;
            _lmxVerifyUser = lmxVerifyUser;
            _userLmxId = 0;
            Register();
            RegisterOnDataWrite();
            // RegisterUser(); // TODO: disabled for now, but will need when writing values
        }
        ~MXDataHolderService()
        {
            Console.WriteLine($"Destroying [thread {threadNumber}]...");
            Unregister();
        }

        private void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            List<MXAttribute> items = _dataProvider.GetAllData();
            var itemsNotInitialized = items.Where(a => a.CurrentThread == threadNumber && !a.initialized).Select(a => a).Count();
            if (itemsNotInitialized != 0)
            {
                Console.WriteLine($"{DateTime.Now} -> Thread [{threadNumber}] have {itemsNotInitialized} items not initalizaed...");
            }
            else
            {
                Console.WriteLine($"{DateTime.Now} -> Thread [{threadNumber}] all initialised");
                timer.Enabled = false;
            }

            // DEBUG
            //Console.WriteLine($"{DateTime.Now.ToString()} -> Thread [{_threadNumber}] have {itemsNotInitialized.Count()} items not initalizaed...");
            //if (itemsNotInitialized.Count() > 0)
            //{
            //    foreach (var item in itemsNotInitialized)
            //    {
            //        Console.Write(item.Value.TagName + ", ");
            //    }
            //}
        }

        /// <summary>
        /// Subscribes to updates for a specific tag.
        /// </summary>
        /// <param name="tagName"></param>
        public void Advise(string tagName)
        {

            List<MXAttribute> items = _dataProvider.GetAllData();
            // exclude other thread objects
            MXAttribute? item = items.FirstOrDefault(a => a.TagName == tagName && a.CurrentThread == threadNumber);
            if (item == null)
            {
                return;
            }

            if (!item.OnAdvise)
            {
                _LmxServer.Advise(_hLmxServerId, GetLmxTagKey(item.Key));
                item.OnAdvise = true;
            }
        }

        /// <summary>
        /// Subscribes to updates for all tags of a specific device
        /// </summary>
        /// <param name="tagName"></param>
        public void AdviseDevice(string device_name)
        {

            List<MXAttribute> items = _dataProvider.GetInstanceData(device_name);
            // exclude other thread objects
            items = items.Where(a => a.CurrentThread == threadNumber).Select(a => a).ToList();
            foreach (var item in items)
            {
                if (!item.OnAdvise)
                {
                    _LmxServer.Advise(_hLmxServerId, GetLmxTagKey(item.Key));
                    item.OnAdvise = true;
                }
            }

        }

        /// <summary>
        /// Subscribes to updates for all tags
        /// </summary>
        public void AdviseAll()
        {
            List<MXAttribute> mxTags = _dataProvider.GetAllData();
            // exclude other thread objects
            mxTags = mxTags.Where(a => a.CurrentThread == threadNumber).Select(a => a).ToList();
            foreach (MXAttribute item in mxTags)
            {
                if (!item.OnAdvise)
                {
                    _LmxServer.Advise(_hLmxServerId, GetLmxTagKey(item.Key));
                    item.OnAdvise = true;
                }
            }
        }




        private int GetLmxTagKey(int threadKey)
        {
            return threadKey - threadNumber * 100000;
        }


        private int GetThreadFormattedKey(int lmxKey)
        {
            return threadNumber * 100000 + lmxKey;
        }

        /// <summary>
        /// Add Tag to tag store
        /// </summary>
        /// <param name="item"></param>
        public void AddItem(string tagname)
        {

            if (LXMRegistered())
            {
                try
                {
                    int itemId = _LmxServer.AddItem(_hLmxServerId, tagname);
                    int key = GetThreadFormattedKey(itemId);

                    MXAttribute item = new() { TagName = tagname, Key = key, CurrentThread = threadNumber };

                    bool succcess = _dataProvider.AddItem(item);
                    if (!succcess)
                    {
                        Console.WriteLine($"[thread {threadNumber}] > fail to add item [{key}] [{item.TagName}]");
                    }
                    else
                    {
                        NotifyDataStoreChange(key, item, DataStoreChangeType.ADDED);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error adding item -> {e.Message}");
                }

            }
        }


        /// <summary>
        /// Unsubscribes from updates for all tags
        /// </summary>
        public void UnAdviseAll()
        {
            foreach (var item in _dataProvider.GetAllData())
                Unadvise(item.Key);
        }


        /// <summary>
        /// Unsubscribes from updates for a specific tag by index
        /// </summary>
        /// <param name="index">Datastore key</param>
        public void Unadvise(int index)
        {
            MXAttribute? mxTag = _dataProvider.GetData(index);
            if (mxTag == null)
            {
                return;
            }


            if (mxTag.OnAdvise && mxTag.CurrentThread == threadNumber)
            {
                _LmxServer.UnAdvise(_hLmxServerId, index);
                mxTag.OnAdvise = false;
            }
        }

        /// <summary>
        /// Removes all data from the data store and unsubscribes from updates.
        /// </summary>
        public void RemoveAll()
        {
            UnAdviseAll();

            foreach (var item in _dataProvider.GetAllData())
                RemoveData(item.Key);
        }

        /// <summary>
        /// Removes a specific tag's data from the data store by tagname
        /// </summary>
        /// <param name="tagName"></param>
        /// <returns></returns>
        public bool RemoveData(string tagName)
        {
            MXAttribute? tag = _dataProvider.GetData(tagName);

            if (tag?.Value == null)
            {
                return true;
            }
            else
            {
                Unadvise(tag.Key);
            }

            _LmxServer.RemoveItem(_hLmxServerId, GetLmxTagKey(tag.Key));
            NotifyDataStoreChange(tag.Key, tag, DataStoreChangeType.REMOVED);
            bool isSuccess = _dataProvider.RemoveData(tag.Key);
            if (isSuccess)
            {
                return true;
            }

            return false;
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

            MXAttribute? tag = _dataProvider.GetData(id);

            if (tag?.Value == null)
            {
                return true;
            }
            else
            {
                Unadvise(tag.Key);
            }

            _LmxServer.RemoveItem(_hLmxServerId, GetLmxTagKey(tag.Key));
            bool isSuccess = _dataProvider.RemoveData(id);
            if (isSuccess)
            {
                NotifyDataStoreChange(tag.Key, tag, DataStoreChangeType.REMOVED);
                return true;
            }

            return false;
        }


        /// <summary>
        /// Registers the service
        /// </summary>
        public void Register()
        {
            try
            {
                if ((_LmxServer != null) && (_hLmxServerId == 0))
                {

                    _hLmxServerId = _LmxServer.Register(_serverName + "_" + threadNumber);
                    _LmxServer.OnDataChange += new _ILMXProxyServerEvents_OnDataChangeEventHandler(LMX_OnDataChange);
                    Console.WriteLine($"[Thr: {threadNumber}] hLMX [{_hLmxServerId}] -> Registered");

                }
            }
            catch (System.Exception ex)
            {
                Console.WriteLine($"Register: Exception occurred. [thread {threadNumber}]");
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
                    Console.WriteLine($"hLMX [{_hLmxServerId}] [thread {threadNumber}] UserAuth [{_lmxVerifyUser}]");
                    _userLmxId = _LmxServer.AuthenticateUser(_hLmxServerId, _lmxVerifyUser, "");
                }
            }
            catch (System.Exception ex)
            {
                Console.WriteLine($"RegisterUser: Exception occurred. [thread {threadNumber}]");
                Console.WriteLine(ex.Message);
            }
        }
        

        /// <summary>
        /// Checks if the service is registered with the LMX server
        /// </summary>
        /// <returns></returns>
        public bool LXMRegistered()
        {
            return (_LmxServer != null) && (_hLmxServerId != 0);
        }


        private void RegisterOnDataWrite(){
            _dataProvider.OnDataWrite +=  WriteData;
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

                AddItem(full_tag_name);
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

            MXAttribute? mxAttr = _dataProvider.GetData(threadTagKey);

            // Console.WriteLine($"LMX_OnDataChange [thread {threadNumber}] [{threadTagKey}] [{mxAttr?.TagName}]");

            if (mxAttr != null)
            {
                if (ItemStatus[0].success != 0)
                {
                    try
                    {

                        // Tag's available attributes
                        if (mxAttr.TagName.EndsWith("._Attributes"))
                        {
                            mxAttr.initialized = true;

                            //Console.WriteLine($"LMX_OnDataChange [thread {_threadNumber}] [{threadTagKey}] [{_dataStore[threadTagKey].TagName}]");

                            string[] tag_name = mxAttr.TagName.Split('.');
                            string[] attr_list = (string[])pvItemValue;

                            RegisterAttributes(tag_name[0], attr_list);
                            RemoveData(threadTagKey);


                            timer ??= new Timer(5000);

                            if (!timer.Enabled)
                            {
                                timer.Elapsed += OnTimedEvent;
                                timer.Enabled = true;
                            }
                        }
                        else
                        {
                            mxAttr.Quality = pwItemQuality;
                            mxAttr.initialized = true;

                            DateTime dateValue;
                            if (DateTime.TryParse(pftItemTimeStamp.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out dateValue))
                            {
                                mxAttr.TimeStamp = dateValue;
                            }
                            mxAttr.Value = pvItemValue;

                            //Console.WriteLine($"LMX_OnDataChange [thread {_threadNumber}] [{threadTagKey}] [{_dataStore[threadTagKey].TagName}]");

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
            MXAttribute? item = _dataProvider.GetData(tagName);
            if (item?.Value == null)
            {
                throw new Exception("Item was not found");
            }
            if (item.OnAdvise)
            {
                if (timeStamp != null)
                {
                    _LmxServer.Write2(_hLmxServerId, GetLmxTagKey(item.Key), value.ToString(), timeStamp, _userLmxId);
                }
                else
                {
                    _LmxServer.Write(_hLmxServerId, GetLmxTagKey(item.Key), value.ToString(), _userLmxId);
                }
            }
        }

        /// <summary>
        /// Unregisters the service from the LMX server
        /// </summary>
        public void Unregister()
        {
            if ((_LmxServer != null) && (_hLmxServerId != 0))
            {
                UnAdviseAll();
                RemoveAll();

                _LmxServer.Unregister(_hLmxServerId);
                _LmxServer = new LMXProxyServerClass();
                _hLmxServerId = 0;
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
            return _dataProvider.GetData(key);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tagname"></param>
        /// <returns></returns>
        public MXAttribute? GetData(string tagname)
        {
            return _dataProvider.GetData(tagname);
        }

        public int GetCount()
        {
            return GetAllData().Count;
        }

        public List<MXAttribute> GetInstanceData(string tagname)
        {
            return _dataProvider.GetInstanceData(tagname);
        }

        public List<MXAttribute> GetBadAndUncertainData()
        {
            return _dataProvider.GetBadAndUncertainData();
        }

        public List<MXAttribute> GetBadAndUncertainData(string instance)
        {
            return _dataProvider.GetBadAndUncertainData(instance);

        }

        public List<MXAttribute> GetAllData()
        {
            return _dataProvider.GetAllData();
        }
    }
}
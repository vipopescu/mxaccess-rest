using System;
using System.Collections.Concurrent;
using System.Globalization;
using ArchestrA.GRAccess;
using ArchestrA.MxAccess;
using Microsoft.AspNetCore.Components.Web;
using MXAccesRestAPI.Classes;
using MXAccesRestAPI.Global;
using static MXAccesRestAPI.MXDataHolder.IMXDataHolderService;

namespace MXAccesRestAPI.MXDataHolder
{

    public class MXDataHolderService : IMXDataHolderService
    {


        // Event for data store changes
        public event DataStoreChangeEventHandler? OnDataStoreChanged;

        private static readonly List<string> _allowedAttributes = [];
        private ConcurrentDictionary<int, MXAttribute> _dataStore;
        private static LMXProxyServerClass _LMX_Server = new();



        public int hLMX;
        public int userLMX;
        public string ServerName;

        public MXDataHolderService(string serverName, List<string> allowedAttributes)
        {
            _dataStore = new ConcurrentDictionary<int, MXAttribute>();
            _allowedAttributes.AddRange(allowedAttributes);
            hLMX = 0;
            ServerName = serverName;
            userLMX = 0;
            Register();
            RegisterUser();
        }
        ~MXDataHolderService()
        {
            Console.WriteLine("Destroying...");
            Unregister();
        }

        /// <summary>
        /// Subscribes to updates for a specific tag.
        /// </summary>
        /// <param name="tagName"></param>
        public void Advise(string tagName)
        {
            var item = _dataStore.FirstOrDefault(a => a.Value.TagName == tagName);
            if (!item.Value.OnAdvise)
            {
                _LMX_Server.Advise(hLMX, item.Key);
                item.Value.OnAdvise = true;
            }
        }

        /// <summary>
        /// Subscribes to updates for all tags of a specific device
        /// </summary>
        /// <param name="tagName"></param>
        public void AdviseDevice(string device_name)
        {

            var items = _dataStore.Where(a => a.Value.TagName.StartsWith(device_name)).Select(a => a);
            foreach (var item in items)
            {
                if (!item.Value.OnAdvise)
                {
                    _LMX_Server.Advise(hLMX, item.Key);
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
                if (!item.Value.OnAdvise)
                {
                    _LMX_Server.Advise(hLMX, item.Key);
                    item.Value.OnAdvise = true;
                }
            }
        }

        /// <summary>
        /// Add Tag to tag store
        /// </summary>
        /// <param name="item"></param>
        public void AddItem(MXAttribute item)
        {

            if (LXMRegistered())
            {
                int key = _LMX_Server.AddItem(hLMX, item.TagName);
                _dataStore.TryAdd(key, item);
                NotifyDataStoreChange(key, item, DataStoreChangeType.ADDED);

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
                    if (item.TagName != null)
                    {
                        int key = _LMX_Server.AddItem(hLMX, item.TagName);
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
            var item = _dataStore.FirstOrDefault(a => a.Value.TagName == value);
            if (item.Value != null && item.Value.OnAdvise)
            {
                _LMX_Server.UnAdvise(hLMX, item.Key);
                item.Value.OnAdvise = false;
            }
        }

        /// <summary>
        /// Unsubscribes from updates for a specific tag by index
        /// </summary>
        /// <param name="index"></param>
        public void Unadvise(int index)
        {
            if (_dataStore[index].OnAdvise)
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
                _LMX_Server.RemoveItem(hLMX, item.Key);
                _dataStore.TryRemove(item);
                NotifyDataStoreChange(item.Key, item.Value, DataStoreChangeType.REMOVED);

            }
            return true;
        }

        /// <summary>
        /// Removes a specific tag's data from the data store by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns> <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool RemoveData(int id)
        {
            if (_dataStore[id].OnAdvise)
            {
                Unadvise(_dataStore[id].TagName);
            }
            _LMX_Server.RemoveItem(hLMX, id);
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
                    _dataStore = new ConcurrentDictionary<int, MXAttribute>();

                }
            }
            catch (System.Exception ex)
            {
                Console.WriteLine("Register: Exception occurred.");
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
                    userLMX = _LMX_Server.AuthenticateUser(hLMX, "vipopescu", "");
                }
            }
            catch (System.Exception ex)
            {
                Console.WriteLine("Register: Exception occurred.");
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
            if (String.IsNullOrEmpty(tag_name)) return;
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

            if (_dataStore[phItemHandle] != null)
            {
                if (ItemStatus[0].success != 0)
                {
                    try
                    {



                        // UDAs
                        if (_dataStore[phItemHandle].TagName.EndsWith("._InheritedUDAs"))
                        {
                            NotifyDataStoreChange(phItemHandle, _dataStore[phItemHandle], DataStoreChangeType.MODIFIED);
                        }


                        // Tag's available attributes
                        // if (_dataStore[phItemHandle].TagName.EndsWith("._Attributes"))
                        // {

                        //     string[] tag_name = _dataStore[phItemHandle].TagName.Split('.');
                        //     string[] attr_list = (string[])pvItemValue;
                        //     RegisterAttributes(tag_name[0], attr_list);
                        //     RemoveData(_dataStore[phItemHandle].TagName);
                        // }
                        // else
                        // {
                        //     _dataStore[phItemHandle].Quality = pwItemQuality;

                        //     DateTime dateValue;
                        //     if (DateTime.TryParse(pftItemTimeStamp.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out dateValue))
                        //     {
                        //         _dataStore[phItemHandle].TimeStamp = dateValue;
                        //     }
                        //     _dataStore[phItemHandle].Value = pvItemValue;

                        //     NotifyDataStoreChange(phItemHandle, _dataStore[phItemHandle], DataStoreChangeType.MODIFIED);

                        // }
                    }
                    catch (System.Exception ex)
                    {
                        Console.WriteLine("Something wrong parsing " + ex.Message);
                    }
                }
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
                    _LMX_Server.Write2(hLMX, item.Key, value.ToString(), timeStamp, userLMX);
                }
                else
                {
                    _LMX_Server.Write(hLMX, item.Key, value.ToString(), userLMX);
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
        /// <param name="key"></param>
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
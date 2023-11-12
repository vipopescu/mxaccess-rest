using System.Collections.Concurrent;
using System.Globalization;
using ArchestrA.MxAccess;
using MXAccesRestAPI.Classes;
using MXAccesRestAPI.Global;

namespace MXAccesRestAPI.MXDataHolder
{
    public class MXDataHolderService : IMXDataHolderService
    {

        private ConcurrentDictionary<int, MXAttribute> _dataStore;
        LMXProxyServerClass? LMX_Server;
        public int hLMX;

        public int userLMX;
        public string ServerName;

        public MXDataHolderService(string serverName)
        {
            _dataStore = new ConcurrentDictionary<int, MXAttribute>();
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
        /// 
        /// </summary>
        /// <param name="tagName"></param>
        public void Advise(string tagName)
        {

            var item = _dataStore.FirstOrDefault(a => a.Value.TagName == tagName);
            if (!item.Value.OnAdvise)
            {
                LMX_Server.Advise(hLMX, item.Key);
                item.Value.OnAdvise = true;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void AdviseAll()
        {

            if (_dataStore.Count == 0)
            {
                return;
            }
            foreach (var item in _dataStore)
            {
                if (!item.Value.OnAdvise)
                {
                    LMX_Server.Advise(hLMX, item.Key);
                    item.Value.OnAdvise = true;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        public void AddItem(MXAttribute item)
        {

            if (LXMRegistered())
            {
                int key = LMX_Server.AddItem(hLMX, item.TagName);
                _dataStore.TryAdd(key, item);
            }
        }

        /// <summary>
        /// 
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
                        int key = LMX_Server.AddItem(hLMX, item.TagName);
                        _dataStore.TryAdd(key, item);
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void UnAdviseAll()
        {
            foreach (var item in _dataStore)
                Unadvise(item.Value.TagName);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public void Unadvise(string value)
        {
            var item = _dataStore.FirstOrDefault(a => a.Value.TagName == value);
            if (item.Value != null && item.Value.OnAdvise)
            {
                LMX_Server.UnAdvise(hLMX, item.Key);
                item.Value.OnAdvise = false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        public void Unadvise(int index)
        {
            if (_dataStore[index].OnAdvise)
            {
                LMX_Server.UnAdvise(hLMX, index);
                _dataStore[index].OnAdvise = false;
            }
        }

        /// <summary>
        /// 
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
        /// 
        /// </summary>
        /// <param name="tagName"></param>
        /// <returns></returns> <summary>
        /// 
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

            if (_dataStore.Count != 0)
            {
                LMX_Server.RemoveItem(hLMX, item.Key);
                _dataStore.TryRemove(item);
            }
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns> <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public bool RemoveData(int index)
        {
            if (_dataStore[index].OnAdvise)
            {
                Unadvise(_dataStore[index].TagName);
            }
            LMX_Server.RemoveItem(hLMX, index);
            _dataStore.TryRemove(index, out var valueRemoved);
            return true;
        }


        /// <summary>
        /// 
        /// </summary>
        public void Register()
        {
            try
            {
                if (LMX_Server == null)
                {
                    LMX_Server = new ArchestrA.MxAccess.LMXProxyServerClass();
                }

                if ((LMX_Server != null) && (hLMX == 0))
                {
                    hLMX = LMX_Server.Register(ServerName);
                    LMX_Server.OnDataChange += new _ILMXProxyServerEvents_OnDataChangeEventHandler(LMX_OnDataChange);
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
                    userLMX = LMX_Server.AuthenticateUser(hLMX, "vipopescu", "");
                }
            }
            catch (System.Exception ex)
            {
                Console.WriteLine("Register: Exception occurred.");
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool LXMRegistered()
        {
            return (LMX_Server != null) && (hLMX != 0);
        }

        /// <summary>
        /// 
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
                        _dataStore[phItemHandle].Quality = pwItemQuality;

                        DateTime dateValue;
                        if (DateTime.TryParse(pftItemTimeStamp.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out dateValue))
                        {
                            _dataStore[phItemHandle].TimeStamp = dateValue;
                        }

                        _dataStore[phItemHandle].Value = pvItemValue;
                    }
                    catch (System.Exception ex)
                    {
                        Console.WriteLine("Something wrong parsing " + ex.Message);
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tagName"></param>
        /// <param name="value"></param>
        /// <param name="timeStamp"></param>
        /// <exception cref="Exception"></exception> <summary>
        /// 
        /// </summary>
        /// <param name="tagName"></param>
        /// <param name="value"></param>
        /// <param name="timeStamp"></param>
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
                    LMX_Server.Write2(hLMX, item.Key, value.ToString(), timeStamp, userLMX);
                }
                else
                {
                    LMX_Server.Write(hLMX, item.Key, value.ToString(), userLMX);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void Unregister()
        {
            if ((LMX_Server != null) && (hLMX != 0))
            {
                UnAdviseAll();
                RemoveAll();

                LMX_Server.Unregister(hLMX);
                LMX_Server = null;
                hLMX = 0;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public MXAttribute GetData(int key)
        {
            if (!_dataStore.ContainsKey(key))
            {
                return null;
            }
            return (MXAttribute)_dataStore[key].Value;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tagname"></param>
        /// <returns></returns>
        public MXAttribute GetData(string tagname)
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

        public List<MXAttribute> GetAllData()
        {
            return _dataStore
                    .Select(kvp => kvp.Value)
                    .ToList();
        }
    }
}
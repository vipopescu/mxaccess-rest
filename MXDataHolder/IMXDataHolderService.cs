using MXAccesRestAPI.Classes;

namespace MXAccesRestAPI.MXDataHolder
{
    // Enum for data store change types
    public enum DataStoreChangeType { ADDED, REMOVED, MODIFIED };

    public interface IMXDataHolderService
    {
        // Delegate for data store changes
        delegate void DataStoreChangeEventHandler(int key, MXAttribute data, DataStoreChangeType changeType);

        // Event for data store changes
        event DataStoreChangeEventHandler OnDataStoreChanged;

        void AddItem(string tagName);
        void Advise(string tagName);
        void AdviseAll();
        MXAttribute? GetData(int key);
        MXAttribute? GetData(string fullattrName);
        List<MXAttribute> GetInstanceData(string instance);
        List<MXAttribute> GetBadAndUncertainData();
        List<MXAttribute> GetBadAndUncertainData(string instance);
        List<MXAttribute> GetAllData();
        bool RemoveData(int key);
        bool RemoveData(string fullattrName);
        void WriteData(string fullattrName, object value, DateTime? timeStamp);
        int GetCount();
    }
}
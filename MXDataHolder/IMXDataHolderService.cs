using MXAccesRestAPI.Classes;

namespace MXAccesRestAPI.MXDataHolder
{
    public interface IMXDataHolderService
    {
        void AddItem(MXAttribute item);
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
        public void WriteData(string fullattrName, object value, DateTime? timeStamp);
        public int GetCount();
    }
}
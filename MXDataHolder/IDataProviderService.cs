using MXAccesRestAPI.Classes;
using static MXAccesRestAPI.MXDataHolder.IMXDataHolderService;

namespace MXAccesRestAPI.MXDataHolder
{
    public interface IDataProviderService
    {

        // Delegate for on data write
        delegate void OnDataWriteChangeEventHandler(string tagName, object value, DateTime? timeStamp = null);
        // Event for on data write
        event OnDataWriteChangeEventHandler? OnDataWrite;
        void WriteData(string tagName, object value, DateTime? timeStamp);

        bool AddTag(string tag);
        List<string> GetAllTags();

        MXAttribute? GetData(int key);
        MXAttribute? GetData(string tagName);
        List<MXAttribute> GetInstanceData(string instance);
        List<MXAttribute> GetBadAndUncertainData();
        List<MXAttribute> GetBadAndUncertainData(string instance);
        List<MXAttribute> GetAllData();

        bool AddItem(MXAttribute item);

        bool RemoveData(string tagName);

        bool RemoveData(int id);
    }

}

using MXAccesRestAPI.Classes;

namespace MXAccesRestAPI.MXDataHolder
{
    public interface IDataProviderService
    {
        MXAttribute? GetData(int key);
        MXAttribute? GetData(string fullattrName);
        List<MXAttribute> GetInstanceData(string instance);
        List<MXAttribute> GetBadAndUncertainData();
        List<MXAttribute> GetBadAndUncertainData(string instance);
        List<MXAttribute> GetAllData();
    }

}

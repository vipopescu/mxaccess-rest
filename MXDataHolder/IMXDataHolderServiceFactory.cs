namespace MXAccesRestAPI.MXDataHolder
{
    public interface IMXDataHolderServiceFactory
    {

        MXDataHolderService Create(int threadNumber);
    }

}
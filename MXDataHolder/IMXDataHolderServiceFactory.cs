namespace MXAccesRestAPI.MXDataHolder
{
    public interface IMXDataHolderServiceFactory
    {
        MXDataHolderService Create(int threadNumber, string serverName, List<string> allowedAttributes);
    }

}
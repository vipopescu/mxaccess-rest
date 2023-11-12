using MXAccesRestAPI.Classes;
using MXAccesRestAPI.Global;

namespace MXAccesRestAPI
{
    public static class Extensions
    {
        public static MXAttributeDto AsDto(this MXAttribute mxattr)
        {
            return new MXAttributeDto(mxattr.TagName, mxattr.TimeStamp, mxattr.Value, mxattr.Quality, GlobalConstants.GetQualityDescription(mxattr.Quality), mxattr.OnAdvise);
        }
    }
}
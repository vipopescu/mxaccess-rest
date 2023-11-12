using System;
using System.ComponentModel.DataAnnotations;

namespace MXAccesRestAPI
{
    public record MXAttributeDto(string tagName, DateTime timeStamp, object value, int quality, string qualityDescr, bool onAdvise);

    public record UpdadeAttributeDto(string new_value);
}

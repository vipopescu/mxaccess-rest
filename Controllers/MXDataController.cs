using Microsoft.AspNetCore.Mvc;
using MXAccesRestAPI.Classes;
using MXAccesRestAPI.MXDataHolder;

namespace MXAccesRestAPI.Controllers
{
    [ApiController]
    [Route("mxdata")]
    public class MXDataController : ControllerBase
    {
        private IMXDataHolderService _dataHolderService;

        public MXDataController(IMXDataHolderService dataHolderService)
        {
            _dataHolderService = dataHolderService;
        }

        [HttpGet("reference/{fullAttrName}")]
        public IActionResult GetData(string fullAttrName)
        {
            MXAttribute mxattr = _dataHolderService.GetData(fullAttrName);
            if (mxattr == null)
                return NotFound("Data not found.");
            return Ok(mxattr.AsDto());
        }

        [HttpPut("reference/{fullAttrName}")]
        public IActionResult UpdateData(string fullAttrName, [FromBody] UpdadeAttributeDto updatedValue)
        {
            MXAttribute mxattr = _dataHolderService.GetData(fullAttrName);
            if (mxattr == null)
                return NotFound("Data not found.");

            _dataHolderService.WriteData(fullAttrName, updatedValue.new_value, DateTime.Now);

            return NoContent();
        }

        [HttpGet("instance/{instanceName}")]
        public IActionResult GetDataByInstance(string instanceName)
        {
            if (string.IsNullOrEmpty(instanceName)) return NotFound("Data not found.");
            var data = _dataHolderService.GetInstanceData(instanceName)
                                    .Select(x => x.AsDto());
            if (data == null)
                return NotFound("Data not found.");
            return Ok(data);
        }

        [HttpGet("baddata")]
        public IActionResult GetBadAndUncertainTags()
        {
            var data = _dataHolderService.GetBadAndUncertainData()
                                    .Select(x => x.AsDto());
            if (data == null)
                return NotFound("Data not found.");
            return Ok(data);
        }

        [HttpGet("alldata")]
        public IActionResult GetAllData()
        {
            var data = _dataHolderService.GetAllData()
                                    .Select(x => x.AsDto());
            if (data == null)
                return NotFound("Data not found.");
            return Ok(data);
        }
    }
}
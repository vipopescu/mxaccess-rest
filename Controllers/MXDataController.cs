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

        [HttpGet("instances/{instanceName}/tags")]
        public IActionResult GetInstanceData(string instanceName)
        {
            if (string.IsNullOrEmpty(instanceName)) return NotFound("Data not found.");
            var data = _dataHolderService.GetInstanceData(instanceName)
                                    .Select(x => x.AsDto());
            if (data == null)
                return NotFound("Data not found.");
            return Ok(data);
        }

        [HttpGet("instances/{instanceName}/tags/{tagId}")]
        public IActionResult GetTagData(string instanceName, string tagId)
        {
            string fullAttrName = instanceName + "." + tagId;
            MXAttribute mxattr = _dataHolderService.GetData(fullAttrName);
            if (mxattr == null)
                return NotFound("Data not found.");
            return Ok(mxattr.AsDto());
        }

        [HttpPut("instances/{instanceName}/tags/{tagId}")]
        public IActionResult UpdateData(string instanceName, string tagId, [FromBody] UpdadeAttributeDto updatedValue)
        {
            string fullAttrName = instanceName + "." + tagId;
            MXAttribute mxattr = _dataHolderService.GetData(fullAttrName);
            if (mxattr == null)
                return NotFound("Data not found.");

            _dataHolderService.WriteData(fullAttrName, updatedValue.new_value, DateTime.Now);

            return NoContent();
        }

        [HttpGet("instances/{instanceName}/bad-tags")]
        public IActionResult GetBadAndUncertainTagsForInstance(string instanceName)
        {
            var data = _dataHolderService.GetBadAndUncertainData(instanceName)
                                    .Select(x => x.AsDto());
            if (data == null)
                return NotFound("Data not found.");
            return Ok(data);
        }

        [HttpGet("instances/bad-tags")]
        public IActionResult GetBadAndUncertainTags()
        {
            var data = _dataHolderService.GetBadAndUncertainData()
                                    .Select(x => x.AsDto());
            if (data == null)
                return NotFound("Data not found.");
            return Ok(data);
        }

        [HttpGet("instances/all")]
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
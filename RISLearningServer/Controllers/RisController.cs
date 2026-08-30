using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RisLearning.Server;

namespace RISLearningServer.Controllers
{
    [Route("ris")]
    [ApiController]
    public class RisController : ControllerBase
    {
            private readonly RadiologyStore _store;
        public RisController(RadiologyStore store) { 
            _store = store;        
        }
        [HttpPost("studies/{orderId}/ready")]
        public IActionResult MarkReady(string orderId)
        {
            var study = Program.Store
                .GetAll()
                .FirstOrDefault(x =>
                    x.OpenEmrOrderId == orderId);

            if (study is null)
            {
                return NotFound();
            }

            if (study.Status != "Scheduled")
            {
                return BadRequest(
                    $"Study is already '{study.Status}'.");
            }

            study.Status = "Active";

            return Ok(study);
        }
    }
}

using Microsoft.AspNetCore.Mvc;

namespace RisLearning.Server;

[ApiController]
[Route("fake-openemr")]
public class FakeOpenEmrController : ControllerBase
{
    [HttpGet("orders")]
    public IActionResult GetOrders()
    {
        return Ok(new[]
        {
            new
            {
                id = "1001",
                patient = new
                {
                    mrn = "P001",
                    name = "DOE^JOHN",
                    birthDate = "19950520",
                    sex = "M"
                },
                procedure = "CT Chest",
                orderedAt = new DateTime(2026, 8, 30, 10, 0, 0)
            },

            new
            {
                id = "1002",
                patient = new
                {
                    mrn = "P002",
                    name = "DOE^JANE",
                    birthDate = "19900115",
                    sex = "F"
                },
                procedure = "MRI Brain",
                orderedAt = new DateTime(2026, 8, 30, 11, 0, 0)
            }
        });
    }
}
using Bl;
using Bl.Api;
using Bl.Models;
using Microsoft.AspNetCore.Mvc;

namespace Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GownsController : ControllerBase
    {
        IBLGowns bl;

        public GownsController(IBl bl)
        {
            this.bl = bl.BLGowns;
        }

        [HttpGet]
        public ActionResult<List<ModelFullDetaills>> GetAllGowns()
        {
            return Ok(bl.getFullGowns());
        }


        [Route("available")]
        [HttpGet]
        public ActionResult<List<ModelFullDetaills>> getAvailableGowns()
        {
            return Ok(bl.getAvailableGowns());
        }

        [Route("groups")]
        [HttpGet]
        public ActionResult<List<ModelGrouped>> GetGroupedGowns()
        {
            return Ok(bl.GetModelGroupeds());
        }

        [HttpGet("{id}")]
        public ActionResult<ModelFullDetaills> GetGownById(int id)
        {
            var res = bl.getGownById(id);
            if (res == null) return NotFound();
            return Ok(res);
        }

        [HttpPost]
        public IActionResult Post([FromBody] ModelFullDetaills newGown)
        {
            if (newGown == null) return BadRequest("Body is empty.");
            try
            {
                bl.createGown(newGown);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal server error: " + ex.Message);
            }
            return Ok(" new gown added ");
        }

        [HttpPut]
        public IActionResult Put([FromBody] ModelFullDetaills updatedGown)
        {
            if (updatedGown == null) return BadRequest();
            try
            {
                bl.updateGown(updatedGown);
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            try
            {
                bl.deleteGown(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}
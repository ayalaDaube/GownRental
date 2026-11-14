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


        [HttpGet("byDate")]
        public ActionResult<List<ModelGrouped>> getByDate([FromQuery] DateOnly date)
        {
            return Ok(bl.markingTheOccupieByDate(date));
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

        /// <summary>Adds a new gown model with its first size, or a new size to the model given by modelId.</summary>
        [HttpPost]
        public ActionResult<ModelFullDetaills> Post([FromBody] NewGownRequest request)
        {
            var created = bl.createGown(request);
            return CreatedAtAction(nameof(GetGownById), new { id = created.Id }, created);
        }

        /// <summary>Changes the size number, price or availability of one size.</summary>
        [HttpPut("{id:int}")]
        public ActionResult<ModelFullDetaills> PutSize(int id, [FromBody] UpdateSizeRequest request)
        {
            return Ok(bl.updateGownSize(id, request));
        }

        /// <summary>Changes the name, color, description or image of a model (shared by all its sizes).</summary>
        [HttpPut("models/{modelId:int}")]
        public ActionResult<ModelFullDetaills> PutModel(int modelId, [FromBody] UpdateModelRequest request)
        {
            return Ok(bl.updateGownModel(modelId, request));
        }

        /// <summary>Deletes one size of a gown (and the model when it was the last size).</summary>
        [HttpDelete("{id:int}")]
        public IActionResult Delete(int id)
        {
            bl.deleteGown(id);
            return NoContent();
        }

        /// <summary>Deletes a gown model together with all of its sizes.</summary>
        [HttpDelete("models/{modelId:int}")]
        public IActionResult DeleteModel(int modelId)
        {
            bl.deleteGownModel(modelId);
            return NoContent();
        }
    }
}
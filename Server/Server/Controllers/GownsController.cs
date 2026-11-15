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
        public ActionResult<List<ModelFullDetails>> GetAllGowns()
        {
            return Ok(bl.GetFullGowns());
        }


        [HttpGet("byDate")]
        public ActionResult<List<ModelGrouped>> GetByDate([FromQuery] DateOnly date)
        {
            return Ok(bl.GetGroupedGownsForDate(date));
        }

        [Route("groups")]
        [HttpGet]
        public ActionResult<List<ModelGrouped>> GetGroupedGowns()
        {
            return Ok(bl.GetGroupedGowns());
        }

        [HttpGet("{id}")]
        public ActionResult<ModelFullDetails> GetGownById(int id)
        {
            var res = bl.GetGownById(id);
            if (res == null) return NotFound();
            return Ok(res);
        }

        /// <summary>Adds a new gown model with its first size, or a new size to the model given by modelId.</summary>
        [HttpPost]
        public ActionResult<ModelFullDetails> Post([FromBody] NewGownRequest request)
        {
            var created = bl.CreateGown(request);
            return CreatedAtAction(nameof(GetGownById), new { id = created.Id }, created);
        }

        /// <summary>Changes the size number, price or availability of one size.</summary>
        [HttpPut("{id:int}")]
        public ActionResult<ModelFullDetails> PutSize(int id, [FromBody] UpdateSizeRequest request)
        {
            return Ok(bl.UpdateGownSize(id, request));
        }

        /// <summary>Changes the name, color, description or image of a model (shared by all its sizes).</summary>
        [HttpPut("models/{modelId:int}")]
        public ActionResult<ModelFullDetails> PutModel(int modelId, [FromBody] UpdateModelRequest request)
        {
            return Ok(bl.UpdateGownModel(modelId, request));
        }

        /// <summary>Deletes one size of a gown (and the model when it was the last size).</summary>
        [HttpDelete("{id:int}")]
        public IActionResult Delete(int id)
        {
            bl.DeleteGown(id);
            return NoContent();
        }

        /// <summary>Deletes a gown model together with all of its sizes.</summary>
        [HttpDelete("models/{modelId:int}")]
        public IActionResult DeleteModel(int modelId)
        {
            bl.DeleteGownModel(modelId);
            return NoContent();
        }
    }
}
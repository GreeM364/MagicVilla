using MagicVilla.Data;
using MagicVilla.Models;
using MagicVilla.Models.Dto;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using System;

namespace MagicVilla.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VillaAPIController : ControllerBase
    {
        private readonly ILogger<VillaAPIController> _logger;

        public VillaAPIController(ILogger<VillaAPIController> logger)
        {
            _logger = logger;
        }


        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<IEnumerable<VillaDTO>> GetVillas() 
        {
            _logger.LogInformation("Getting all villas");
            return Ok(VillaStore.villaList);            
        }

        [HttpGet("{id}", Name = "GetVilla")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<VillaDTO> GetVilla(int id)
        {
            var villa = VillaStore.villaList.FirstOrDefault(u => u.Id == id);

            if(villa == null)
            {
                _logger.LogError("Get Villa NotFound with Id: " + id);
                return NotFound();
            }

            _logger.LogInformation("Getting Villa with Id: " + id);
            return Ok(villa);
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public ActionResult<VillaDTO> CreateVilla([FromBody]VillaDTO villaDTO)
        {
            if(VillaStore.villaList.FirstOrDefault(i => i.Name.ToLower() == villaDTO.Name.ToLower()) != null)
            {
                _logger.LogError("Villa alredy exsists");
                ModelState.AddModelError("CustomerError", "Villa alredy exsists!");
                return BadRequest(ModelState);
            }
            if(villaDTO == null)
            {
                _logger.LogError("The resulting model of villa is null");
                return BadRequest(villaDTO);
            }
            if(villaDTO.Id > 0)
            {
                _logger.LogError("Id was specified when creating a new villa");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }

            villaDTO.Id = VillaStore.villaList.OrderByDescending(i => i.Id).FirstOrDefault().Id + 1;
            VillaStore.villaList.Add(villaDTO);

            _logger.LogInformation("A new villa has been created");
            return CreatedAtRoute("GetVilla", new {id = villaDTO.Id}, villaDTO);
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult DeleteVilla(int id)
        {
            if(id == 0)
            {
                _logger.LogError("Invalid villa deletion id specified: " + id);
                return BadRequest();
            }

            var villa = VillaStore.villaList.FirstOrDefault(v => v.Id == id);

            if(villa == null)
            {
                _logger.LogError($"Villa with similar id {id} not found for deletion");
                return NotFound();
            }

            VillaStore.villaList.Remove(villa);

            _logger.LogInformation("Villa was removed with id: " + id);
            return NoContent();
        }

        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IActionResult UpdateVilla(int id, [FromBody]VillaDTO villaDTO)
        {
            if(villaDTO == null)
            {
                _logger.LogError("The resulting model of villa is null");
                return BadRequest();
            }
            if(id != villaDTO.Id)
            {
                _logger.LogError($"The specified id {id} does not match the model id: {villaDTO.Id}");
                return BadRequest();
            }

            var villa = VillaStore.villaList.FirstOrDefault(i => i.Id == id);
            villa.Name = villaDTO.Name;
            villa.Occupancy = villaDTO.Occupancy;
            villa.Sqft = villaDTO.Sqft;

            _logger.LogInformation("Villa was updated with id: " + id);
            return NoContent();
        }

        [HttpPatch("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IActionResult UpdatePartialVilla(int id, JsonPatchDocument<VillaDTO> patchDTO)
        {
            if (id == 0)
            {
                _logger.LogError("Invalid villa partial update id specified: " + id);
                return BadRequest();
            }
            if (patchDTO == null)
            {
                _logger.LogError("The resulting patch model of villa is null");
                return BadRequest();
            }
            
            var villa = VillaStore.villaList.FirstOrDefault(i => i.Id == id);

            if(villa == null)
            {
                _logger.LogError($"Villa with similar id {id} not found for partial update");
                return BadRequest();
            }

            patchDTO.ApplyTo(villa, ModelState);
            
            if(!ModelState.IsValid)
            {
                _logger.LogError($"Villa model is not valid for partial update");
                return BadRequest();
            }

            _logger.LogInformation("Villa was partial updated with id: " + id);
            return NoContent();
        }
    }
}

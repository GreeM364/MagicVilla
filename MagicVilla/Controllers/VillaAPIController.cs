using MagicVilla.Data;
using MagicVilla.Models;
using MagicVilla.Models.Dto;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MagicVilla.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VillaAPIController : ControllerBase
    {
        private readonly ILogger<VillaAPIController> _logger;
        private readonly ApplicationDbContext _db;

        public VillaAPIController(ILogger<VillaAPIController> logger, ApplicationDbContext db)
        {
            _logger = logger;
            _db = db;
        }


        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<IEnumerable<VillaDTO>> GetVillas()
        {
            _logger.LogInformation("Getting all villas");
            return Ok(_db.Villas.ToList());
        }

        [HttpGet("{id}", Name = "GetVilla")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<VillaDTO> GetVilla(int id)
        {
            var villa = _db.Villas.FirstOrDefault(u => u.Id == id);

            if (villa == null)
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
        public ActionResult<VillaDTO> CreateVilla([FromBody] VillaCreateDTO villaDTO)
        {
            if (_db.Villas.FirstOrDefault(i => i.Name.ToLower() == villaDTO.Name.ToLower()) != null)
            {
                _logger.LogError("Villa alredy exsists");
                ModelState.AddModelError("CustomerError", "Villa alredy exsists!");
                return BadRequest(ModelState);
            }
            if (villaDTO == null)
            {
                _logger.LogError("The resulting model of villa is null");
                return BadRequest(villaDTO);
            }

            Villa model = new()
            {
                Amenity = villaDTO.Amenity,
                Details = villaDTO.Details,
                ImageUrl = villaDTO.ImageUrl,
                Name = villaDTO.Name,
                Occupancy = villaDTO.Occupancy,
                Rate = villaDTO.Rate,
                Sqft = villaDTO.Sqft
            };
            _db.Villas.Add(model);
            _db.SaveChanges();

            _logger.LogInformation("A new villa has been created");
            return CreatedAtRoute("GetVilla", new { id = model.Id }, model);
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult DeleteVilla(int id)
        {
            if (id == 0)
            {
                _logger.LogError("Invalid villa deletion id specified: " + id);
                return BadRequest();
            }

            var villa = _db.Villas.FirstOrDefault(v => v.Id == id);

            if (villa == null)
            {
                _logger.LogError($"Villa with similar id {id} not found for deletion");
                return NotFound();
            }

            _db.Villas.Remove(villa);
            _db.SaveChanges();

            _logger.LogInformation("Villa was removed with id: " + id);
            return NoContent();
        }

        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IActionResult UpdateVilla(int id, [FromBody] VillaUpdateDTO villaDTO)
        {
            if (villaDTO == null)
            {
                _logger.LogError("The resulting model of villa is null");
                return BadRequest();
            }
            if (id != villaDTO.Id)
            {
                _logger.LogError($"The specified id {id} does not match the model id: {villaDTO.Id}");
                return BadRequest();
            }

            Villa model = new()
            {
                Amenity = villaDTO.Amenity,
                Details = villaDTO.Details,
                Id = villaDTO.Id,
                ImageUrl = villaDTO.ImageUrl,
                Name = villaDTO.Name,
                Occupancy = villaDTO.Occupancy,
                Rate = villaDTO.Rate,
                Sqft = villaDTO.Sqft
            };
            _db.Villas.Update(model);
            _db.SaveChanges();

            _logger.LogInformation("Villa was updated with id: " + id);
            return NoContent();
        }

        [HttpPatch("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IActionResult UpdatePartialVilla(int id, JsonPatchDocument<VillaUpdateDTO> patchDTO)
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

            var villa = _db.Villas.AsNoTracking().FirstOrDefault(i => i.Id == id);

            VillaUpdateDTO villaDTO = new()
            {
                Amenity = villa.Amenity,
                Details = villa.Details,
                Id = villa.Id,
                ImageUrl = villa.ImageUrl,
                Name = villa.Name,
                Occupancy = villa.Occupancy,
                Rate = villa.Rate,
                Sqft = villa.Sqft
            };

            if (villa == null)
            {
                _logger.LogError($"Villa with similar id {id} not found for partial update");
                return BadRequest();
            }

            patchDTO.ApplyTo(villaDTO, ModelState);

            Villa model = new Villa()
            {
                Amenity = villaDTO.Amenity,
                Details = villaDTO.Details,
                Id = villaDTO.Id,
                ImageUrl = villaDTO.ImageUrl,
                Name = villaDTO.Name,
                Occupancy = villaDTO.Occupancy,
                Rate = villaDTO.Rate,
                Sqft = villaDTO.Sqft
            };

            _db.Villas.Update(model);
            _db.SaveChanges();

            if (!ModelState.IsValid)
            {
                _logger.LogError($"Villa model is not valid for partial update");
                return BadRequest();
            }

            _logger.LogInformation("Villa was partial updated with id: " + id);
            return NoContent();
        }
    }
}

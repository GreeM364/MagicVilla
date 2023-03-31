using AutoMapper;
using MagicVilla.Models;
using MagicVilla.Models.Dto;
using MagicVilla.Repository.IRepository;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;

namespace MagicVilla.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VillaAPIController : ControllerBase
    {
        private readonly ILogger<VillaAPIController> _logger;
        private readonly IMapper _mapper;
        private readonly IVillaRepository _villaRepository;

        public VillaAPIController(ILogger<VillaAPIController> logger, IMapper mapper, IVillaRepository villaRepository)
        {
            _logger = logger;
            _mapper = mapper;
            _villaRepository = villaRepository;
        }


        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<VillaDTO>>> GetVillas()
        {
            IEnumerable<Villa> villas = await _villaRepository.GetAllAsync();

            _logger.LogInformation("Getting all villas");
            return Ok(_mapper.Map<IEnumerable<Villa>>(villas));
        }

        [HttpGet("{id}", Name = "GetVilla")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<VillaDTO>> GetVilla(int id)
        {
            var villa = await _villaRepository.GetAsync(u => u.Id == id);

            if (villa == null)
            {
                _logger.LogError("Get Villa NotFound with Id: " + id);
                return NotFound();
            }

            _logger.LogInformation("Getting Villa with Id: " + id);
            return Ok(_mapper.Map<VillaDTO>(villa));
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<VillaDTO>> CreateVilla([FromBody] VillaCreateDTO createDTO)
        {
            if (await _villaRepository.GetAsync(i => i.Name.ToLower() == createDTO.Name.ToLower()) != null)
            {
                _logger.LogError("Villa alredy exsists");
                ModelState.AddModelError("CustomerError", "Villa alredy exsists!");
                return BadRequest(ModelState);
            }
            if (createDTO == null)
            {
                _logger.LogError("The resulting model of villa is null");
                return BadRequest(createDTO);
            }

            Villa model = _mapper.Map<Villa>(createDTO);

            await _villaRepository.CreateAsync(model);

            _logger.LogInformation("A new villa has been created");
            return CreatedAtRoute("GetVilla", new { id = model.Id }, model);
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteVilla(int id)
        {
            if (id == 0)
            {
                _logger.LogError("Invalid villa deletion id specified: " + id);
                return BadRequest();
            }

            var villa = await _villaRepository.GetAsync(v => v.Id == id);

            if (villa == null)
            {
                _logger.LogError($"Villa with similar id {id} not found for deletion");
                return NotFound();
            }

            await _villaRepository.RemoveAsync(villa);

            _logger.LogInformation("Villa was removed with id: " + id);
            return NoContent();
        }

        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateVilla(int id, [FromBody] VillaUpdateDTO updateDTO)
        {
            if (updateDTO == null)
            {
                _logger.LogError("The resulting model of villa is null");
                return BadRequest();
            }
            if (id != updateDTO.Id)
            {
                _logger.LogError($"The specified id {id} does not match the model id: {updateDTO.Id}");
                return BadRequest();
            }

            Villa model = _mapper.Map<Villa>(updateDTO);

            await _villaRepository.UpdateAsync(model);

            _logger.LogInformation("Villa was updated with id: " + id);
            return NoContent();
        }

        [HttpPatch("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdatePartialVilla(int id, JsonPatchDocument<VillaUpdateDTO> patchDTO)
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

            var villa = await _villaRepository.GetAsync(i => i.Id == id, false);

            VillaUpdateDTO villaDTO = _mapper.Map<VillaUpdateDTO>(villa);
            

            if (villa == null)
            {
                _logger.LogError($"Villa with similar id {id} not found for partial update");
                return BadRequest();
            }

            patchDTO.ApplyTo(villaDTO, ModelState);

            Villa model = _mapper.Map<Villa>(villaDTO);

            await _villaRepository.UpdateAsync(model);

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

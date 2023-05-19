using AutoMapper;
using MagicVilla.Models;
using MagicVilla.Models.Dto;
using MagicVilla.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Net;

namespace MagicVilla.Controllers.v1
{
    [Route("api/v{version:apiVersion}/VillaAPI")]
    [ApiController]
    [ApiVersion("1.0")]
    public class VillaAPIController : ControllerBase
    {
        private readonly ILogger<VillaAPIController> _logger;
        private readonly IMapper _mapper;
        private readonly IVillaRepository _villaRepository;
        protected APIResponse _response;

        public VillaAPIController(ILogger<VillaAPIController> logger, IMapper mapper, IVillaRepository villaRepository)
        {
            _logger = logger;
            _mapper = mapper;
            _villaRepository = villaRepository;
            _response = new APIResponse();
        }


        [HttpGet]
        [ResponseCache(CacheProfileName = "Default30")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<APIResponse>> GetVillas([FromQuery(Name = "filterOccupancy")] int? occupancy,
                                                     [FromQuery] string? search, int pageSize = 0, int pageNumber = 1)
        {
            try
            {
                IEnumerable<Villa> villas;

                if (occupancy > 0)
                {
                    villas = await _villaRepository.GetAllAsync(u => u.Occupancy == occupancy, pageSize: pageSize,
                                                                pageNumber: pageNumber);
                }
                else
                {
                    villas = await _villaRepository.GetAllAsync(pageSize: pageSize, pageNumber: pageNumber);
                }
                if (!string.IsNullOrEmpty(search))
                {
                    villas = villas.Where(u => u.Name.ToLower().Contains(search) || u.Amenity.ToLower().Contains(search));
                }

                Pagination pagination = new() { PageNumber = pageNumber, PageSize = pageSize };
                Response.Headers.Add("X-Pagination", JsonSerializer.Serialize(pagination));

                _response.Result = _mapper.Map<List<VillaDTO>>(villas);
                _response.StatusCode = HttpStatusCode.OK;

                _logger.LogInformation("Getting all Villas");
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string> { ex.ToString() };
                _logger.LogInformation(ex.ToString());
            }
            return _response;
        }

        [HttpGet("{id}", Name = "GetVilla")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<APIResponse>> GetVilla(int id)
        {
            try
            {
                var villa = await _villaRepository.GetAsync(u => u.Id == id);

                if (villa == null)
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;

                    _logger.LogError("Get Villa NotFound with Id: " + id);
                    return NotFound(_response);
                }

                _response.Result = _mapper.Map<VillaDTO>(villa);
                _response.StatusCode = HttpStatusCode.OK;

                _logger.LogInformation("Getting Villa with Id: " + id);
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string> { ex.ToString() };
                _logger.LogInformation(ex.ToString());
            }
            return _response;
        }

        [HttpPost]
        [Authorize(Roles = "admin")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<APIResponse>> CreateVilla([FromBody] VillaCreateDTO createDTO)
        {
            try
            {
                if (await _villaRepository.GetAsync(i => i.Name.ToLower() == createDTO.Name.ToLower()) != null)
                {
                    _logger.LogError("Villa alredy exsists");
                    ModelState.AddModelError("ErrorMessages", "Villa alredy exsists!");
                    return BadRequest(ModelState);
                }
                if (createDTO == null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;

                    _logger.LogError("The resulting model of Villa is null");
                    return BadRequest(_response);
                }

                Villa villa = _mapper.Map<Villa>(createDTO);

                await _villaRepository.CreateAsync(villa);

                _response.Result = _mapper.Map<VillaDTO>(villa);
                _response.StatusCode = HttpStatusCode.Created;

                _logger.LogInformation("A new Villa has been created");
                return CreatedAtRoute("GetVilla", new { id = villa.Id }, _response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string> { ex.ToString() };
                _logger.LogInformation(ex.ToString());
            }
            return _response;
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<APIResponse>> DeleteVilla(int id)
        {
            try
            {
                var villa = await _villaRepository.GetAsync(v => v.Id == id);

                if (villa == null)
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;

                    _logger.LogError($"Villa with similar id {id} not found for deletion");
                    return NotFound(_response);
                }

                await _villaRepository.RemoveAsync(villa);

                _response.StatusCode = HttpStatusCode.NoContent;

                _logger.LogInformation("Villa was removed with id: " + id);
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string> { ex.ToString() };
                _logger.LogInformation(ex.ToString());
            }
            return _response;
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<APIResponse>> UpdateVilla(int id, [FromBody] VillaUpdateDTO updateDTO)
        {
            try
            {
                if (updateDTO == null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;

                    _logger.LogError("The received model of Villa is null");
                    return BadRequest(_response);
                }
                if (id != updateDTO.Id)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;

                    _logger.LogError($"The specified id {id} does not match the model id: {updateDTO.Id}");
                    return BadRequest(_response);
                }

                Villa model = _mapper.Map<Villa>(updateDTO);

                await _villaRepository.UpdateAsync(model);

                _response.StatusCode = HttpStatusCode.NoContent;

                _logger.LogInformation("Villa was updated with id: " + id);
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string> { ex.ToString() };
                _logger.LogInformation(ex.ToString());
            }
            return _response;
        }

        [HttpPatch("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<APIResponse>> UpdatePartialVilla(int id, JsonPatchDocument<VillaUpdateDTO> patchDTO)
        {
            try
            {
                if (patchDTO == null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;

                    _logger.LogError("The received patch model of Villa is null");
                    return BadRequest();
                }

                var villa = await _villaRepository.GetAsync(i => i.Id == id, false);

                VillaUpdateDTO villaDTO = _mapper.Map<VillaUpdateDTO>(villa);

                if (villa == null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;

                    _logger.LogError($"Villa with similar id {id} not found for partial update");
                    return BadRequest();
                }

                patchDTO.ApplyTo(villaDTO, ModelState);

                Villa model = _mapper.Map<Villa>(villaDTO);

                await _villaRepository.UpdateAsync(model);

                _response.StatusCode = HttpStatusCode.NoContent;

                _logger.LogInformation("Villa was partial updated with id: " + id);
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string>() { ex.ToString() };
                _logger.LogInformation(ex.ToString());
            }
            return _response;
        }
    }
}

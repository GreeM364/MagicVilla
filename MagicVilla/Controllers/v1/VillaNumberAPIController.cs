using AutoMapper;
using MagicVilla.Models;
using MagicVilla.Models.Dto;
using MagicVilla.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace MagicVilla.Controllers.v1
{
    [Route("api/v{version:apiVersion}/VillaNumberAPI")]
    [ApiController]
    [ApiVersion("1.0")]
    public class VillaNumberAPIController : ControllerBase
    {
        protected APIResponse _response;
        private readonly IMapper _mapper;
        private readonly ILogger<VillaNumberAPIController> _logger;
        private readonly IVillaNumberRepository _villaNumberRepository;
        private readonly IVillaRepository _villaRepository;
        public VillaNumberAPIController(IMapper mapper, ILogger<VillaNumberAPIController> logger,
                                        IVillaNumberRepository villaNumberRepository, IVillaRepository villaRepository)
        {
            _mapper = mapper;
            _logger = logger;
            _response = new APIResponse();
            _villaNumberRepository = villaNumberRepository;
            _villaRepository = villaRepository;
        }

        [HttpGet]
        [ResponseCache(CacheProfileName = "Default30")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<APIResponse>> GetVillaNumbers()
        {
            try
            {
                IEnumerable<VillaNumber> villaNumberList = await _villaNumberRepository.GetAllAsync(includeProperties: "Villa");
                _response.Result = _mapper.Map<List<VillaNumberDTO>>(villaNumberList);
                _response.StatusCode = HttpStatusCode.OK;

                _logger.LogInformation("Getting all Villas Numbers");
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

        [HttpGet("{id:int}", Name = "GetVillaNumber")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<APIResponse>> GetVillaNumber(int id)
        {
            try
            {
                var villaNumber = await _villaNumberRepository.GetAsync(u => u.VillaNo == id);

                if (villaNumber == null)
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;

                    _logger.LogError("Get Villa Number NotFound with Id: " + id);
                    return NotFound(_response);
                }

                _response.Result = _mapper.Map<VillaNumberDTO>(villaNumber);
                _response.StatusCode = HttpStatusCode.OK;

                _logger.LogInformation("Getting Villa Number with Id: " + id);
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

        [HttpPost]
        [Authorize(Roles = "admin")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<APIResponse>> CreateVillaNumber([FromBody] VillaNumberCreateDTO createDTO)
        {
            try
            {
                if (await _villaNumberRepository.GetAsync(u => u.VillaNo == createDTO.VillaNo) != null)
                {
                    _logger.LogError("Villa Number alredy exsists");
                    ModelState.AddModelError("ErrorMessages", "Villa Number already Exists!");
                    return BadRequest(ModelState);
                }
                if (await _villaRepository.GetAsync(u => u.Id == createDTO.VillaID) == null)
                {
                    _logger.LogError($"Villa id {createDTO.VillaID} is Invalid");
                    ModelState.AddModelError("ErrorMessages", "Villa ID is Invalid!");
                    return BadRequest(ModelState);
                }
                if (createDTO == null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;

                    _logger.LogError("The resulting model of Villa Number is null");
                    return BadRequest(createDTO);
                }

                VillaNumber villaNumber = _mapper.Map<VillaNumber>(createDTO);

                await _villaNumberRepository.CreateAsync(villaNumber);

                _response.Result = _mapper.Map<VillaNumberDTO>(villaNumber);
                _response.StatusCode = HttpStatusCode.Created;

                return CreatedAtRoute("GetVillaNumber", new { id = villaNumber.VillaNo }, _response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string>() { ex.ToString() };
                _logger.LogInformation(ex.ToString());
            }
            return _response;
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<APIResponse>> DeleteVillaNumber(int id)
        {
            try
            {
                var villaNumber = await _villaNumberRepository.GetAsync(u => u.VillaNo == id);

                if (villaNumber == null)
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;

                    _logger.LogError($"Villa Number with similar id {id} not found for deletion");
                    return NotFound(_response);
                }

                await _villaNumberRepository.RemoveAsync(villaNumber);

                _response.StatusCode = HttpStatusCode.NoContent;

                _logger.LogInformation("Villa Number was removed with id: " + id);
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

        [HttpPut("{id:int}")]
        [Authorize(Roles = "admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<APIResponse>> UpdateVillaNumber(int id, [FromBody] VillaNumberUpdateDTO updateDTO)
        {
            try
            {
                if (updateDTO == null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;

                    _logger.LogError("The received model of Villa Number is null");
                    return BadRequest(_response);
                }
                if (id != updateDTO.VillaNo)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;

                    _logger.LogError($"The specified id {id} does not match the model id: {updateDTO.VillaNo}");
                    return BadRequest(_response);
                }
                if (await _villaRepository.GetAsync(u => u.Id == updateDTO.VillaID) == null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;

                    _logger.LogError($"Villa id {updateDTO.VillaID} is Invalid");
                    return BadRequest(_response);
                }

                VillaNumber model = _mapper.Map<VillaNumber>(updateDTO);

                await _villaNumberRepository.UpdateAsync(model);

                _response.StatusCode = HttpStatusCode.NoContent;

                _logger.LogInformation($"Villa Number with id {id} was updated");
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

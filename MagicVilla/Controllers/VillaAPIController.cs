using MagicVilla.Data;
using MagicVilla.Models.Dto;
using Microsoft.AspNetCore.Mvc;

namespace MagicVilla.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VillaAPIController : ControllerBase
    {
        [HttpGet]
        public IEnumerable<VillaDTO> GetVillas() 
        {
            return VillaStore.villaList;            
        }

        [HttpGet("{id}")]
        public VillaDTO? GetVilla(int id)
        {
            return VillaStore.villaList.FirstOrDefault(u => u.Id == id);
        }
    }
}

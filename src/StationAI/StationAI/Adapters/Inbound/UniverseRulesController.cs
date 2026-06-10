using Microsoft.AspNetCore.Mvc;
using StationAI.Core.Interfaces;

namespace StationAI.Adapters.Inbound
{

    [ApiController]
    [Route("api/[controller]")]
    public class UniverseRulesController : ControllerBase
    {
        private readonly IRulesRepository _rulesRepository;

        public UniverseRulesController(IRulesRepository rulesRepository)
        {
            _rulesRepository = rulesRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetRules()
        {
            var rules = await _rulesRepository.GetRules();
            return Ok(rules ?? "No current intel on the state of the universe is available.");
        }

        [HttpPut]
        public async Task<IActionResult> SaveRules([FromBody] string rules)
        {
            await _rulesRepository.SaveRules(rules);
            return NoContent();
        }
    }
}

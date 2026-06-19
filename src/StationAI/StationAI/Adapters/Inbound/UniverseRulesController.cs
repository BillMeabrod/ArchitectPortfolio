using Microsoft.AspNetCore.Mvc;
using StationAI.Core;
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
            var universeIntel = await _rulesRepository.GetRules();
            return Ok(new
            {
                coreDirective = AriaIdentity.CoreDirective,
                universeIntel = universeIntel ?? "No current intel on the state of the universe is available."
            });
        }

        [HttpPut]
        public async Task<IActionResult> SaveRules([FromBody] string rules)
        {
            await _rulesRepository.SaveRules(rules);
            return Ok();
        }
    }
}

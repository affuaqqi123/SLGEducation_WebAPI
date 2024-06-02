using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Localization;
using System.Globalization;
using System.Reflection;
using System.Resources;
using System.Text.RegularExpressions;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LocalizationController : Controller
    {
        private readonly IStringLocalizer<LocalizationController> _localizer;
        private readonly ILogger<LocalizationController> _logger;

        public LocalizationController(IStringLocalizer<LocalizationController> localizer, ILogger<LocalizationController> logger)
        {
            _localizer = localizer;
            _logger = logger;

        }



        //    [HttpGet("{key}")]
        //    public IActionResult GetLocalizedString(string key)
        //    {
        //        CultureInfo.CurrentCulture = new CultureInfo("nb-NO");
        //        CultureInfo.CurrentUICulture = new CultureInfo("nb-NO");
        //        var localizedString = _localizer[key]?.Value; // Retrieve the localized string value
        //        if (localizedString == null)
        //        {
        //            return NotFound(); // Return a 404 Not Found if the localized string is not found
        //        }
        //        return Ok(localizedString);
        //    }
        //}


        //[HttpGet("{culture}")]
        //public IActionResult GetLocalizedString(string culture)
        //{
        //        var localizedString = _localizer[key].Value;    
        //        return Ok(localizedString);
        //    }

        [HttpGet("{culture}")]       
        public IActionResult GetLocalizedString(string culture)
        {
            try
            {
                var cultureSpecificLocalizedStrings = _localizer.GetAllStrings()
                    .Where(x => x.Name.EndsWith($":{culture}", StringComparison.OrdinalIgnoreCase))
                    .ToDictionary(x => x.Name.Substring(0, x.Name.Length - culture.Length - 1), x => x.Value);

                _logger.LogInformation($"LocalizationController - Retrieved localized strings for culture '{culture}'.");

                return Ok(cultureSpecificLocalizedStrings);
            }
            catch (Exception ex)
            {
                _logger.LogError($"LocalizationController - Error occurred while retrieving localized strings for culture '{culture}': {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
        }

    }
}
using Asp.Versioning;
using IranJob.Modules.Identity.Domain.Constants;
using IranJob.Modules.ReferenceData.Application.Abstractions;
using IranJob.Modules.ReferenceData.Application.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace IranJob.Modules.ReferenceData.Presentation.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/locations")]
public sealed class LocationsController(IReferenceDataService service) : ControllerBase
{
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(LocationItem), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LocationItem>> Get(Guid id, CancellationToken cancellationToken) =>
        Ok(await service.GetLocationAsync(id, cancellationToken));

    [HttpGet("countries")]
    [ProducesResponseType(typeof(IReadOnlyList<CountryItem>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CountryItem>>> GetCountries(CancellationToken cancellationToken) =>
        Ok(await service.GetCountriesAsync(cancellationToken));

    [HttpGet("provinces")]
    [ProducesResponseType(typeof(IReadOnlyList<ProvinceItem>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<ProvinceItem>>> GetProvinces([FromQuery] Guid countryId, CancellationToken cancellationToken) =>
        Ok(await service.GetProvincesAsync(countryId, cancellationToken));

    [HttpGet("cities")]
    [ProducesResponseType(typeof(IReadOnlyList<CityItem>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<CityItem>>> GetCities([FromQuery] Guid provinceId, CancellationToken cancellationToken) =>
        Ok(await service.GetCitiesAsync(provinceId, cancellationToken));

    [HttpGet("countries/{id:guid}")]
    [ProducesResponseType(typeof(CountryItem), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CountryItem>> GetCountry(Guid id, CancellationToken cancellationToken) =>
        Ok(await service.GetCountryAsync(id, cancellationToken));

    [HttpPost("countries")]
    [Authorize(Roles = $"{IdentityRoles.Admin},{IdentityRoles.SuperAdmin}")]
    [ProducesResponseType(typeof(CountryItem), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CountryItem>> CreateCountry(CountryInput input, CancellationToken cancellationToken)
    {
        var created = await service.CreateCountryAsync(input, cancellationToken);
        return CreatedAtAction(nameof(GetCountry), new { id = created.Id }, created);
    }

    [HttpPut("countries/{id:guid}")]
    [Authorize(Roles = $"{IdentityRoles.Admin},{IdentityRoles.SuperAdmin}")]
    [ProducesResponseType(typeof(CountryItem), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CountryItem>> UpdateCountry(Guid id, CountryInput input, CancellationToken cancellationToken) =>
        Ok(await service.UpdateCountryAsync(id, input, cancellationToken));

    [HttpDelete("countries/{id:guid}")]
    [Authorize(Roles = $"{IdentityRoles.Admin},{IdentityRoles.SuperAdmin}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeactivateCountry(Guid id, CancellationToken cancellationToken)
    {
        await service.DeactivateCountryAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpGet("provinces/{id:guid}")]
    [ProducesResponseType(typeof(ProvinceItem), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProvinceItem>> GetProvince(Guid id, CancellationToken cancellationToken) =>
        Ok(await service.GetProvinceAsync(id, cancellationToken));

    [HttpPost("provinces")]
    [Authorize(Roles = $"{IdentityRoles.Admin},{IdentityRoles.SuperAdmin}")]
    [ProducesResponseType(typeof(ProvinceItem), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProvinceItem>> CreateProvince(ProvinceInput input, CancellationToken cancellationToken)
    {
        var created = await service.CreateProvinceAsync(input, cancellationToken);
        return CreatedAtAction(nameof(GetProvince), new { id = created.Id }, created);
    }

    [HttpPut("provinces/{id:guid}")]
    [Authorize(Roles = $"{IdentityRoles.Admin},{IdentityRoles.SuperAdmin}")]
    [ProducesResponseType(typeof(ProvinceItem), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProvinceItem>> UpdateProvince(Guid id, ProvinceInput input, CancellationToken cancellationToken) =>
        Ok(await service.UpdateProvinceAsync(id, input, cancellationToken));

    [HttpDelete("provinces/{id:guid}")]
    [Authorize(Roles = $"{IdentityRoles.Admin},{IdentityRoles.SuperAdmin}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeactivateProvince(Guid id, CancellationToken cancellationToken)
    {
        await service.DeactivateProvinceAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpGet("cities/{id:guid}")]
    [ProducesResponseType(typeof(CityItem), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CityItem>> GetCity(Guid id, CancellationToken cancellationToken) =>
        Ok(await service.GetCityAsync(id, cancellationToken));

    [HttpPost("cities")]
    [Authorize(Roles = $"{IdentityRoles.Admin},{IdentityRoles.SuperAdmin}")]
    [ProducesResponseType(typeof(CityItem), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CityItem>> CreateCity(CityInput input, CancellationToken cancellationToken)
    {
        var created = await service.CreateCityAsync(input, cancellationToken);
        return CreatedAtAction(nameof(GetCity), new { id = created.Id }, created);
    }

    [HttpPut("cities/{id:guid}")]
    [Authorize(Roles = $"{IdentityRoles.Admin},{IdentityRoles.SuperAdmin}")]
    [ProducesResponseType(typeof(CityItem), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CityItem>> UpdateCity(Guid id, CityInput input, CancellationToken cancellationToken) =>
        Ok(await service.UpdateCityAsync(id, input, cancellationToken));

    [HttpDelete("cities/{id:guid}")]
    [Authorize(Roles = $"{IdentityRoles.Admin},{IdentityRoles.SuperAdmin}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateCity(Guid id, CancellationToken cancellationToken)
    {
        await service.DeactivateCityAsync(id, cancellationToken);
        return NoContent();
    }
}

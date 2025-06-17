using APBD_example_test1_2025.Exceptions;
using APBD_example_test1_2025.Models.DTOs;
using APBD_example_test1_2025.Services;
using Microsoft.AspNetCore.Mvc;

namespace APBD_example_test1_2025.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ArtifactsController : ControllerBase
    {
        private readonly IDbService _dbService;

        public ArtifactsController(IDbService dbService)
        {
            _dbService = dbService;
        }

        [HttpGet("projects/{id}")]
        public async Task<IActionResult> GetProject(int id)
        {
            try
            {
                var result = await _dbService.GetProjectsAsync(id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddArtifactWithProject(int artifactId,
            CreateNewArtifactDto createNewArtifactDto)
        {
            try 
            {
                await _dbService.AddNewArtifactWithProjectAsync(artifactId, createNewArtifactDto); 
            }
            catch (Exception ex)
            {
                return Conflict(ex.Message);
            }
            return CreatedAtAction(nameof(GetProject), artifactId, createNewArtifactDto);
        }
    }
}
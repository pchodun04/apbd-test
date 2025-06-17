using APBD_example_test1_2025.Models.DTOs;

namespace APBD_example_test1_2025.Services;

public interface IDbService
{
    Task<PreservationProjectHistoryDto> GetProjectsAsync(int projectId);
    Task AddNewArtifactWithProjectAsync(int artifactId, CreateNewArtifactDto dto);
}
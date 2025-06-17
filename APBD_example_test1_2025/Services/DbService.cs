using System.Data.Common;
using APBD_example_test1_2025.Exceptions;
using APBD_example_test1_2025.Models.DTOs;
using Microsoft.Data.SqlClient;

namespace APBD_example_test1_2025.Services;

public class DbService : IDbService
{
    private readonly string _connectionString;
    public DbService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("Default") ?? string.Empty;
    }
    
    public async Task<PreservationProjectHistoryDto> GetProjectsAsync(int projectId)
    {
        var query =    @"SELECT p.project_id, p.objective, p.start_date, p.end_date,
            a.name AS artifact_name, a.origin_date, 
            i.institution_id, i.name AS institution_name, i.founded_year,
            s.first_name, s.last_name, s.hire_date, pa.role
            FROM Project p
            JOIN Artifact a ON p.artifact_id = a.artifact_id
            JOIN Institution i ON a.institution_id = i.institution_id
            LEFT JOIN ProjectAssignment pa ON pa.project_id = p.project_id
            LEFT JOIN Staff s ON s.staff_id = pa.staff_id
            WHERE p.project_id = @id";
        
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand();

        command.Connection = connection;
        command.CommandText = query;
        

        command.Parameters.AddWithValue("@id", projectId);

        await connection.OpenAsync();
        var reader = await command.ExecuteReaderAsync();

        PreservationProjectHistoryDto? result = null;

        while (await reader.ReadAsync())
        {
            if (result == null)
            {
                result = new PreservationProjectHistoryDto
                {
                    ProjectId = reader.GetInt32(0),
                    Objective = reader.GetString(1),
                    StartDate = reader.GetDateTime(2),
                    EndDate = reader.IsDBNull(3) ? null : reader.GetDateTime(3),
                    Artifact = new ArtifactDto
                    {
                        Name = reader.GetString(4),
                        OriginDate = reader.GetDateTime(5),
                        Institution = new InstitutionDto
                        {
                            InstitutionId = reader.GetInt32(6),
                            Name = reader.GetString(7),
                            FoundedYear = reader.GetInt32(8)
                        }
                    },
                    StaffAssignments = new List<StaffAssignmentDto>()
                };
            }

            if (!reader.IsDBNull(9))
            {
                result.StaffAssignments.Add(new StaffAssignmentDto
                {
                    FirstName = reader.GetString(9),
                    LastName = reader.GetString(10),
                    HireDate = reader.GetDateTime(11),
                    Role = reader.GetString(12)
                });
            }
        }

        if (result == null)
        {
            throw new Exception("Project not found");
        }

        return result;
    }

    public async Task AddNewArtifactWithProjectAsync(int artifactId, CreateNewArtifactDto dto)
{
    await using SqlConnection connection = new SqlConnection(_connectionString);
    await using SqlCommand command = new SqlCommand();

    command.Connection = connection;
    await connection.OpenAsync();

    DbTransaction transaction = await connection.BeginTransactionAsync();
    command.Transaction = transaction as SqlTransaction;

    try
    {
        command.Parameters.Clear();
        command.CommandText = "SELECT 1 FROM Institution WHERE institution_id = @InstitutionId";
        command.Parameters.AddWithValue("@InstitutionId", artifactId);

        var exists = await command.ExecuteScalarAsync();
        if (exists is null)
            throw new Exception($"Institution with ID {artifactId} not found");
        
        command.Parameters.Clear();
        command.CommandText = @"INSERT INTO Artifact (artifact_id, name, origin_date, institution_id)
                                VALUES (@ArtifactId, @Name, @OriginDate, @InstitutionId)";

        command.Parameters.AddWithValue("@ArtifactId", dto.NewArtifactDto.ArtifactId);
        command.Parameters.AddWithValue("@Name", dto.NewArtifactDto.Name);
        command.Parameters.AddWithValue("@OriginDate", dto.NewArtifactDto.OriginDate);
        command.Parameters.AddWithValue("@InstitutionId", dto.NewArtifactDto.InstitutionId);

        await command.ExecuteNonQueryAsync();
        
        command.Parameters.Clear();
        command.CommandText = @"INSERT INTO Project (project_id, objective, start_date, end_date, artifact_id)
                                VALUES (@ProjectId, @Objective, @StartDate, @EndDate, @ArtifactId)";

        command.Parameters.AddWithValue("@ProjectId", dto.NewProjectDto.ProjectId);
        command.Parameters.AddWithValue("@Objective", dto.NewProjectDto.Objective);
        command.Parameters.AddWithValue("@StartDate", dto.NewProjectDto.StartDate);
        command.Parameters.AddWithValue("@EndDate", (object?)dto.NewProjectDto.EndDate ?? DBNull.Value);
        command.Parameters.AddWithValue("@ArtifactId", dto.NewArtifactDto.ArtifactId);

        await command.ExecuteNonQueryAsync();

        await transaction.CommitAsync();
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }
}

}
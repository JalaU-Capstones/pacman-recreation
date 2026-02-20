using System.Collections.Generic;
using System.Threading.Tasks;
using PacmanGame.Models.CustomLevel;

namespace PacmanGame.Services.Interfaces;

public interface ICustomLevelManagerService
{
    Task<IReadOnlyList<CustomLevelSummary>> GetCustomLevelsAsync();
    Task<CustomLevelSummary> ImportProjectAsync(string filePath);
    Task DeleteCustomLevelAsync(string id);
}

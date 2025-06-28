namespace Prototype.Services.Interfaces;

/// <summary>
/// Provides database seeding functionality for initial data population
/// </summary>
public interface IDatabaseSeeder
{
    /// <summary>
    /// Seeds the database with initial required data and development sample data
    /// </summary>
    /// <returns>A task representing the asynchronous seeding operation</returns>
    Task SeedAsync();
}
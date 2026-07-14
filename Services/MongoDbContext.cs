using MongoDB.Driver;
using MarvelTimelineApi.Models;
using System.Text.Json;
using System.IO;

namespace MarvelTimelineApi.Services;

public class MongoDbContext
{
    private readonly IMongoDatabase? _database;
    private readonly ILogger<MongoDbContext> _logger;

    public MongoDbContext(ILogger<MongoDbContext> logger, IConfiguration configuration)
    {
        _logger = logger;
        try
        {
            var connectionString = Environment.GetEnvironmentVariable("MONGO_URI")
                ?? configuration["MongoDb:ConnectionString"];
            var databaseName = Environment.GetEnvironmentVariable("MONGO_DATABASE")
                ?? configuration["MongoDb:Database"] ?? "sample_mflix";
            var collectionName = Environment.GetEnvironmentVariable("MONGO_COLLECTION")
                ?? configuration["MongoDb:Collection"] ?? "movies";

            if (string.IsNullOrEmpty(connectionString))
            {
                _logger.LogWarning("MongoDB connection string is empty. App will use local JSON fallback.");
                return;
            }

            var settings = MongoClientSettings.FromConnectionString(connectionString);
            
            // Explicitly force TLS 1.2 to resolve macOS SslStream / AppleCrypto handshake issues
            settings.SslSettings = new SslSettings
            {
                EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12
            };

            // Fail fast (3-second timeout instead of 30-second default) if database is offline/unreachable
            settings.ServerSelectionTimeout = TimeSpan.FromSeconds(3);

            var client = new MongoClient(settings);
            _database = client.GetDatabase(databaseName);
            Movies = _database.GetCollection<Movie>(collectionName);

            _logger.LogInformation("MongoDB client initialized: {Database}/{Collection}", databaseName, collectionName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize MongoDB client. App will use local JSON fallback.");
        }
    }

    public IMongoCollection<Movie>? Movies { get; }

    public async Task SeedDataAsync(string contentRootPath)
    {
        if (Movies == null)
        {
            _logger.LogWarning("Cannot seed database because database connection is unavailable.");
            return;
        }

        try
        {
            var count = await Movies.CountDocumentsAsync(Builders<Movie>.Filter.Empty);
            if (count == 0)
            {
                _logger.LogInformation("Database is empty. Seeding movies from local data...");
                var filePath = Path.Combine(contentRootPath, "Data", "movies.json");
                if (File.Exists(filePath))
                {
                    var jsonText = await File.ReadAllTextAsync(filePath);
                    var movies = JsonSerializer.Deserialize<List<Movie>>(jsonText, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    if (movies != null && movies.Count > 0)
                    {
                        await Movies.InsertManyAsync(movies);
                        _logger.LogInformation("Successfully seeded {Count} movies.", movies.Count);
                    }
                    else
                    {
                        _logger.LogWarning("No movies found in seed file.");
                    }
                }
                else
                {
                    _logger.LogWarning("Seed data file not found at {FilePath}.", filePath);
                }
            }
            else
            {
                _logger.LogInformation("Database already has {Count} movies. Skipping seeding.", count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while seeding database.");
        }
    }
}

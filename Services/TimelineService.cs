using MarvelTimelineApi.Models;
using MongoDB.Driver;
using System.Text.Json;
using System.IO;
using Microsoft.Extensions.Hosting;

namespace MarvelTimelineApi.Services;

public class TimelineService
{
    private readonly MongoDbContext _context;
    private readonly ILogger<TimelineService> _logger;
    private readonly IHostEnvironment _env;
    private List<Movie> _fallbackMovies = new();

    public TimelineService(MongoDbContext context, ILogger<TimelineService> logger, IHostEnvironment env)
    {
        _context = context;
        _logger = logger;
        _env = env;
        _logger.LogInformation("TimelineService initialized");
        LoadFallbackMovies();
    }

    private void LoadFallbackMovies()
    {
        try
        {
            var filePath = Path.Combine(_env.ContentRootPath, "Data", "movies.json");
            if (File.Exists(filePath))
            {
                var jsonText = File.ReadAllText(filePath);
                var movies = JsonSerializer.Deserialize<List<Movie>>(jsonText, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                if (movies != null)
                {
                    _fallbackMovies = movies;
                    _logger.LogInformation("Successfully loaded {Count} fallback movies from local JSON.", _fallbackMovies.Count);
                }
            }
            else
            {
                _logger.LogWarning("Fallback movies file not found at {FilePath}.", filePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load fallback movies from local JSON.");
        }
    }

    public async Task<List<Movie>> GetAllMoviesAsync()
    {
        try
        {
            if (_context.Movies == null)
            {
                throw new InvalidOperationException("MongoDB collection is unavailable.");
            }
            var movies = await _context.Movies.Find(_ => true).ToListAsync();
            _logger.LogInformation("Retrieved {Count} movies from MongoDB", movies.Count);
            return movies;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "MongoDB query failed. Falling back to local JSON data.");
            return _fallbackMovies;
        }
    }

    public async Task<PagedResponse<Movie>> GetMoviesPagedAsync(int page = 1, int pageSize = 10)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 50) pageSize = 50;

        try
        {
            if (_context.Movies == null)
            {
                throw new InvalidOperationException("MongoDB collection is unavailable.");
            }

            var totalCount = (int)await _context.Movies.CountDocumentsAsync(_ => true);
            var items = await _context.Movies
                .Find(_ => true)
                .Skip((page - 1) * pageSize)
                .Limit(pageSize)
                .ToListAsync();

            return new PagedResponse<Movie>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "MongoDB query failed. Falling back to local JSON data.");
            var items = _fallbackMovies.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            return new PagedResponse<Movie>
            {
                Items = items,
                TotalCount = _fallbackMovies.Count,
                Page = page,
                PageSize = pageSize
            };
        }
    }

    public async Task<List<Movie>> GetMoviesByPhaseAsync(int phase)
    {
        try
        {
            if (_context.Movies == null)
            {
                throw new InvalidOperationException("MongoDB collection is unavailable.");
            }
            var filter = Builders<Movie>.Filter.Eq(m => m.Phase, phase);
            return await _context.Movies.Find(filter).ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "MongoDB query failed. Falling back to local JSON data.");
            return _fallbackMovies.Where(m => m.Phase == phase).ToList();
        }
    }

    public async Task<Movie?> GetMovieByIdAsync(int id)
    {
        try
        {
            if (_context.Movies == null)
            {
                throw new InvalidOperationException("MongoDB collection is unavailable.");
            }
            var filter = Builders<Movie>.Filter.Eq(m => m.Id, id);
            return await _context.Movies.Find(filter).FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "MongoDB query failed. Falling back to local JSON data.");
            return _fallbackMovies.FirstOrDefault(m => m.Id == id);
        }
    }

    // Synchronous versions for backward compatibility
    public List<Movie> GetAllMovies() => GetAllMoviesAsync().GetAwaiter().GetResult();
    public PagedResponse<Movie> GetMoviesPaged(int page = 1, int pageSize = 10) => GetMoviesPagedAsync(page, pageSize).GetAwaiter().GetResult();
    public List<Movie> GetMoviesByPhase(int phase) => GetMoviesByPhaseAsync(phase).GetAwaiter().GetResult();
    public Movie? GetMovieById(int id) => GetMovieByIdAsync(id).GetAwaiter().GetResult();
}

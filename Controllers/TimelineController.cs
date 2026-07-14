using Microsoft.AspNetCore.Mvc;
using MarvelTimelineApi.Models;
using MarvelTimelineApi.Services;

namespace MarvelTimelineApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class TimelineController : ControllerBase
{
    private readonly TimelineService _timelineService;

    public TimelineController(TimelineService timelineService)
    {
        _timelineService = timelineService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<Movie>), StatusCodes.Status200OK)]
    public ActionResult<PagedResponse<Movie>> GetAllMovies([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = _timelineService.GetMoviesPaged(page, pageSize);
        return Ok(result);
    }

    [HttpGet("phase/{phase}")]
    [ProducesResponseType(typeof(List<Movie>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<List<Movie>> GetMoviesByPhase([FromRoute] int phase)
    {
        if (phase < 1 || phase > 5)
        {
            return BadRequest($"Phase must be between 1 and 5. Received: {phase}");
        }

        var movies = _timelineService.GetMoviesByPhase(phase);
        if (movies.Count == 0)
            return NotFound($"No movies found for Phase {phase}");
        return Ok(movies);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Movie), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<Movie> GetMovieById(int id)
    {
        var movie = _timelineService.GetMovieById(id);
        if (movie == null)
            return NotFound($"Movie with ID {id} not found");
        return Ok(movie);
    }
}

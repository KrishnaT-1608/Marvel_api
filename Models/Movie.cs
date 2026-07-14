using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MarvelTimelineApi.Models;

public class Movie
{
    [BsonId]
    [BsonRepresentation(BsonType.Int32)]
    public int Id { get; set; }

    [BsonElement("title")]
    public string Title { get; set; } = string.Empty;

    [BsonElement("phase")]
    public int Phase { get; set; }

    [BsonElement("released")]
    public DateTime ReleaseDate { get; set; }

    [BsonElement("storyorder")]
    public int StoryOrder { get; set; }

    [BsonElement("director")]
    public string Director { get; set; } = string.Empty;

    [BsonElement("synopsis")]
    public string Synopsis { get; set; } = string.Empty;

    [BsonElement("imageUrl")]
    public string ImageUrl { get; set; } = string.Empty;

    [BsonElement("cast")]
    public List<string> Cast { get; set; } = new();

    [BsonElement("postCreditsScene")]
    public bool PostCreditsScene { get; set; }

    [BsonElement("infinityStone")]
    public string? InfinityStone { get; set; }

    [BsonElement("isTvSeries")]
    public bool IsTvSeries { get; set; }

    [BsonElement("trailerUrl")]
    public string? TrailerUrl { get; set; }
}

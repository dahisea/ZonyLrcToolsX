using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ZonyLrcTools.Common.MusicScanner.JsonModel;

public sealed class GetMusicInfoFromNetEaseMusicSongListResponse
{
    [JsonProperty("code")]
    public int Code { get; set; }

    [JsonProperty("playlist")]
    public PlayListModel? PlayList { get; set; }
}

public sealed class PlayListModel
{
    [JsonProperty("tracks")]
    public ICollection<PlayListSongModel>? SongList { get; set; }
}

public sealed class PlayListSongModel
{
    [JsonProperty("name")]
    public string? Name { get; set; }

    [JsonProperty("ar")]
    [JsonConverter(typeof(PlayListSongArtistModelJsonConverter))]
    public ICollection<PlayListSongArtistModel>? Artists { get; set; }

    [JsonProperty("id")]
    public string? SongId { get; set; }

    public string ArtistNames => Artists is not null 
        ? string.Join(" ", Artists.Select(artist => artist.Name).Where(name => !string.IsNullOrEmpty(name))) 
        : string.Empty;
}

public sealed class PlayListSongArtistModel
{
    [JsonProperty("name")]
    public string? Name { get; set; }
}

public class PlayListSongArtistModelJsonConverter : JsonConverter
{
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        // Implement the method if needed
        writer.WriteStartArray();
        if (value is ICollection<PlayListSongArtistModel> artists)
        {
            foreach (var artist in artists)
            {
                serializer.Serialize(writer, artist);
            }
        }
        writer.WriteEndArray();
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
        {
            return null;
        }

        var token = JToken.Load(reader);
        return token.Type switch
        {
            JTokenType.Array => token.ToObject(objectType),
            JTokenType.Object => new List<PlayListSongArtistModel> { token.ToObject<PlayListSongArtistModel>()! },
            _ => null
        };
    }

    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(ICollection<PlayListSongArtistModel>);
    }
}

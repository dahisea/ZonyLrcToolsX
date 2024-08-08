using Newtonsoft.Json;

namespace ZonyLrcTools.Common.Lyrics.Providers.NetEase.JsonModel
{
    public class SongSearchResponse
    {
        [JsonProperty("result")]
        public InnerListItemModel Items { get; set; } = null!;

        [JsonProperty("code")]
        public int StatusCode { get; set; }

        public string? GetFirstMatchSongId(string songName, string? duration, string? songId)
        {
            var perfectMatch = Items.SongItems.FirstOrDefault(x => x.Name == songName);
            if (perfectMatch != null)
            {
                return perfectMatch.SongId;
            }

            if (!string.IsNullOrEmpty(duration))
            {
                var durationMatch = Items.SongItems.FirstOrDefault(x => x.Duration == duration);
                if (durationMatch != null && !string.IsNullOrEmpty(durationMatch.SongId) && durationMatch.SongId != "0")
                {
                    return durationMatch.SongId;
                }
            }

            return Items.SongItems.First().SongId;
        }
    }

    public class InnerListItemModel
    {
        [JsonProperty("songs")]
        public IList<SongModel> SongItems { get; set; } = null!;

        [JsonProperty("songCount")]
        public int SongCount { get; set; }
    }


public sealed class SongModel
{
    /// <summary>
    /// 歌曲的名称。
    /// </summary>
    [JsonProperty("name")]
    public string Name { get; set; }

    /// <summary>
    /// 歌曲的艺术家信息，可能会有多位艺术家/歌手。
    /// </summary>
    [JsonProperty("ar")]
    [JsonConverter(typeof(SongArtistModelJsonConverter))]
    public ICollection<SongArtistModel>? Artists { get; set; }

    /// <summary>
    /// 歌曲的Sid。
    /// </summary>
    [JsonProperty("id")]
    public string SongId { get; set; }

    /// <summary>
    /// 获取所有艺术家名称，用空格分隔。
    /// </summary>
    public string ArtistNames => Artists is not null 
        ? string.Join(" ", Artists.Select(artist => artist.Name)) 
        : string.Empty;
}

public sealed class SongArtistModel
{
    /// <summary>
    /// 艺术家的名称。
    /// </summary>
    [JsonProperty("name")]
    public string? Name { get; set; }
}

public class SongArtistModelJsonConverter : JsonConverter
{
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        var token = JToken.Load(reader);
        return token.Type switch
        {
            JTokenType.Array => token.ToObject(objectType),
            JTokenType.Object => new List<SongArtistModel> { token.ToObject<SongArtistModel>()! },
            _ => null
        };
    }
}
}
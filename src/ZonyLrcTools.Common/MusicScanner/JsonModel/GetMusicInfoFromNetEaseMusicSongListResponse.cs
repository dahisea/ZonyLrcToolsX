using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ZonyLrcTools.Common.MusicScanner.JsonModel
{
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

        // 将艺术家信息序列化为字符串，用空格分隔
        [JsonProperty("ar")]
        [JsonConverter(typeof(PlayListSongArtistModelJsonConverter))]
        public string Artist { get; set; } = string.Empty;

        [JsonProperty("id")]
        public string? SongId { get; set; }
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
            // 序列化为艺术家名称的数组
            writer.WriteStartArray();
            if (value is string artistNames)
            {
                var names = artistNames.Split(' ');
                foreach (var name in names)
                {
                    serializer.Serialize(writer, new PlayListSongArtistModel { Name = name });
                }
            }
            writer.WriteEndArray();
        }

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return string.Empty;
            }

            var token = JToken.Load(reader);
            if (token.Type == JTokenType.Array)
            {
                var artists = token.ToObject<ICollection<PlayListSongArtistModel>>();
                return artists is not null 
                    ? string.Join(" ", artists.Select(artist => artistraw.Name).Where(name => !string.IsNullOrEmpty(name))) 
                    : string.Empty;
            }

            if (token.Type == JTokenType.Object)
            {
                var artist = token.ToObject<PlayListSongArtistModel>();
                return artistraw?.Name ?? string.Empty;
            }

            return string.Empty;
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(string);
        }
    }
}

using Newtonsoft.Json;

namespace ZonyLrcTools.Common.Lyrics.Providers.NetEase.JsonModel
{
    public class GetSongDetailsRequest
    {
        public GetSongDetailsRequest(string? SongId)
        {
            SongId = SongId;
            SongIds = $"%5B{SongId}%5D";
        }

        [JsonProperty("id")] public string? SongId { get; }

        [JsonProperty("ids")] public string? SongIds { get; }
    }
}

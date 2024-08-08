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
    /// 歌曲的Sid。
    /// </summary>
    [JsonProperty("id")]
    public string SongId { get; set; }

 
}
}
using Newtonsoft.Json;

namespace ZonyLrcTools.Common.Lyrics.Providers.KuWo.JsonModel;

public class SongSearchResponse
{
    [JsonProperty("TOTAL")] public int TotalCount { get; set; }

    [JsonProperty("abslist")]
    public IList<SongSearchResponseSongDetail> SongList { get; set; }

    public long GetMatchedMusicId(string musicName, string artistName, long? duration)
    {
        var prefectMatch = SongList.FirstOrDefault(x => x.Name == musicName && x.Artist == artistName);
        if (prefectMatch != null)
        {
            return prefectMatch.MusicId;
        }

        if (duration == null || duration == 0)
        {
            return SongList.First().MusicId;
        }

        // Ensure Duration is not null or empty for comparison
        return SongList
            .OrderBy(t => 
            {
                if (string.IsNullOrEmpty(t.Duration) || t.Duration == "0")
                {
                    return long.MaxValue; // Treat as invalid or very large for sorting
                }
                
                // Convert Duration to a long if needed for comparison
                if (long.TryParse(t.Duration, out var durationValue))
                {
                    return Math.Abs(durationValue - duration.Value);
                }
                return long.MaxValue; // Treat as invalid if parsing fails
            })
            .First()
            .MusicId;
    }
}

public class SongSearchResponseSongDetail
{
    /// <summary>
    /// 专辑名称。
    /// </summary>
    [JsonProperty("ALBUM")]
    public string Album { get; set; }

    /// <summary>
    /// 歌手名称。
    /// </summary>
    [JsonProperty("ARTIST")]
    public string Artist { get; set; }

    /// <summary>
    /// 歌曲名称。
    /// </summary>
    [JsonProperty("SONGNAME")]
    public string Name { get; set; }

    /// <summary>
    /// 歌曲的 ID。
    /// </summary>
    [JsonProperty("DC_TARGETID")]
    public long MusicId { get; set; }

    /// <summary>
    /// 歌曲的时间长度。
    /// </summary>
    [JsonProperty("DURATION")]
    public string Duration { get; set; }
}

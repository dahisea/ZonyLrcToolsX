using Newtonsoft.Json;

namespace ZonyLrcTools.Common.Lyrics.Providers.KuWo.JsonModel
{
    public class SongSearchResponse
    {
        [JsonProperty("TOTAL")]
        public int TotalCount { get; set; }

        [JsonProperty("abslist")]
        public IList<SongSearchResponseSongDetail> SongList { get; set; } = new List<SongSearchResponseSongDetail>();

        public long GetMatchedMusicId(string musicName, string artistName, string? duration)
        {
            var perfectMatch = SongList.FirstOrDefault(x => x.Name == musicName && x.Artist == artistName);
            if (perfectMatch != null)
            {
                return perfectMatch.MusicId;
            }

            if (string.IsNullOrEmpty(duration))
            {
                return SongList.First().MusicId;
            }

            // Handle case where duration might be missing or invalid
            return SongList
                .OrderBy(t => string.IsNullOrEmpty(t.Duration) ? long.MaxValue : Math.Abs(long.Parse(t.Duration) - long.Parse(duration)))
                .First().MusicId;
        }
    }

    public class SongSearchResponseSongDetail
    {

        /// <summary>
        /// 歌手名称。
        /// </summary>
        [JsonProperty("ARTIST")]
        public string Artist { get; set; } = string.Empty;

        /// <summary>
        /// 歌曲名称。
        /// </summary>
        [JsonProperty("SONGNAME")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 歌曲的 ID。
        /// </summary>
        [JsonProperty("DC_TARGETID")]
        public long MusicId { get; set; }

        /// <summary>
        /// 歌曲的时间长度。
        /// </summary>
        [JsonProperty("DURATION")]
        public string Duration { get; set; } = string.Empty;
    }
}

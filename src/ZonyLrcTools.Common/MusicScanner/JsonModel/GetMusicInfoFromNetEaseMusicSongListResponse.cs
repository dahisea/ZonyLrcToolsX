public sealed class PlayListSongModel
{
    /// <summary>
    /// 歌曲的名称。
    /// </summary>
    [JsonProperty("name")]
    public string? Name { get; set; }

    /// <summary>
    /// 歌曲的id。
    /// </summary>
    [JsonProperty("id")]
    public string SongId { get; set; }
    
    /// <summary>
    /// 歌曲的艺术家信息，可能会有多位艺术家/歌手。
    /// </summary>
    [JsonProperty("ar")]
    [JsonConverter(typeof(PlayListSongArtistModelJsonConverter))]
    public ICollection<PlayListSongArtistModel>? ArtistNames { get; set; }

    /// <summary>
    /// 获取以空格分隔的艺术家名称字符串。
    /// </summary>
    public string Artist => ArtistNames != null 
        ? string.Join(" ", ArtistNames.Select(a => a.Name)) 
        : string.Empty;
}

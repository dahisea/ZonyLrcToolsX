public partial class MusicInfo
{
    /// <summary>
    /// 歌曲的名称。
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// 歌曲的作者。
    /// </summary>
    public string Artist { get; set; }

    /// <summary>
    /// 歌曲的 Sid。
    /// </summary>
    public string SongId { get; set; }

    /// <summary>
    /// 歌曲的专辑（如果需要）。
    /// </summary>
    public string Album { get; set; }

    /// <summary>
    /// 歌曲的时长（如果需要）。
    /// </summary>
    public long? Duration { get; set; }

    /// <summary>
    /// 是否下载成功？
    /// </summary>
    public bool IsSuccessful { get; set; } = true;

    /// <summary>
    /// 是否是纯音乐？
    /// </summary>
    public bool IsPruneMusic { get; set; } = false;

    public MusicInfo(string name, string artist, string songId, string album = "", long? duration = null)
    {
        Name = name;
        Artist = artist;
        SongId = songId;
        Album = album;
        Duration = duration;
    }

    public static bool operator ==(MusicInfo? left, MusicInfo? right)
    {
        return left?.SongId == right?.SongId;
    }

    public static bool operator !=(MusicInfo? left, MusicInfo? right)
    {
        return !(left == right);
    }
}

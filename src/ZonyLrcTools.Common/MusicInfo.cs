public class MusicInfo
{
    public string Name { get; set; }
    public string Artist { get; set; }
    public string SongId { get; set; }
    public bool IsSuccessful { get; set; }
    public bool IsPruneMusic { get; set; }

    public MusicInfo(string name, string artist, string songId)
    {
        Name = name;
        Artist = artist;
        SongId = songId;
    }
}

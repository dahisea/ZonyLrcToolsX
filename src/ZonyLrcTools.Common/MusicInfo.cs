public class MusicInfo
{
    public string Name { get; set; }
    public string Artist { get; set; }
    public string SongId { get; set; }
    public bool IsSuccessful { get; set; }
    public bool IsPureMusic { get; set; }

    public MusicInfo(string name, string artistNames, string songId)
    {
        Name = name;
        Artist = artistNames;
        SongId = songId;
    }
}

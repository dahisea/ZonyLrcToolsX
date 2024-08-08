public class MusicInfo
{
    public string FilePath { get; set; }
    public string Name { get; set; }
    public string Artist { get; set; }
    public string SongId { get; set; }
    public bool IsSuccessful { get; set; }
    public bool IsPureMusic { get; set; }

    public MusicInfo(string name, string artist, string songId, string filePath)
    {
        FilePath = filePath;
        Name = name;
        Artist = artist;
        SongId = songId;
    }
}

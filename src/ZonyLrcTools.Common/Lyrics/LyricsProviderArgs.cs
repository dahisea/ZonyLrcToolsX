namespace ZonyLrcTools.Common.Lyrics
{
    public class LyricsProviderArgs
    {
        public string Name { get; set; }

        public string Artist { get; set; }

        public string Duration { get; set; }
        
        public string SongId { get; set; }

        public LyricsProviderArgs(string name, string artist, string songId, string? duration)
        {
            Name = name;
            Artist = artist;
            Duration = duration;
            SongId = songId;
        }
    }
}

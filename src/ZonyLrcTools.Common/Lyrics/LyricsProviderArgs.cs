namespace ZonyLrcTools.Common.Lyrics
{
    public class LyricsProviderArgs
    {
        public string SongName { get; set; }

        public string Artist { get; set; }

        public string Duration { get; set; }
        
        public string SongId { get; set; }

        public LyricsProviderArgs(string songName, string artist, string duration)
        {
            SongName = songName;
            Artist = artist;
            Duration = duration;
        }
    }
}

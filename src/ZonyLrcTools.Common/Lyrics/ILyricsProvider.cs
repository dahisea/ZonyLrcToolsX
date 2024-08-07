namespace ZonyLrcTools.Common.Lyrics
{
    public interface ILyricsProvider
    {
        ValueTask<LyricsItemCollection> DownloadAsync(string songName, string artist, string songId, string? duration = null);
        string DownloaderName { get; }
    }
}

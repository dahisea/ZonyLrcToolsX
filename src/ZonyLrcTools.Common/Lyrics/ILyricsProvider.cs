namespace ZonyLrcTools.Common.Lyrics
{
public interface ILyricsProvider
{
    string DownloaderName { get; }

    Task<LyricsItemCollection> DownloadAsync(string songName, string artist, string songId);
}

}

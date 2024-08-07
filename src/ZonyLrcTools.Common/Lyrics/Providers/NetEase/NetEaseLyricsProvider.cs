using System.Text.RegularExpressions;
using ZonyLrcTools.Common.MusicScanner;
using ZonyLrcTools.Common.Lyrics.Providers.NetEase.JsonModel;
using Newtonsoft.Json;

namespace ZonyLrcTools.Common.Lyrics.Providers.NetEase
{
    public class NetEaseLyricsProvider : LyricsProvider
    {
        public override string DownloaderName => InternalLyricsProviderNames.NetEase;

        private readonly IWarpHttpClient _warpHttpClient;
        private readonly ILyricsItemCollectionFactory _lyricsItemCollectionFactory;
        private readonly GlobalOptions _options;

        private const string NetEaseSearchMusicUrl = @"https://music.163.com/weapi/search/get";
        private const string NetEaseGetLyricUrl = @"https://music.163.com/weapi/song/lyric?csrf_token=";

        private const string NetEaseRequestReferer = @"https://music.163.com/song?id=";

        public NetEaseLyricsProvider(IWarpHttpClient warpHttpClient,
            ILyricsItemCollectionFactory lyricsItemCollectionFactory,
            IOptions<GlobalOptions> options)
        {
            _warpHttpClient = warpHttpClient;
            _lyricsItemCollectionFactory = lyricsItemCollectionFactory;
            _options = options.Value;
        }

        protected override async ValueTask<object> DownloadDataAsync(LyricsProviderArgs args)
        {
            var secretKey = NetEaseMusicEncryptionHelper.CreateSecretKey(16);
            var encSecKey = NetEaseMusicEncryptionHelper.RsaEncode(secretKey);

            var searchResult = await _warpHttpClient.PostAsync<SongSearchResponse>(NetEaseSearchMusicUrl,
                requestOption: request =>
                {
                    request.Headers.Referrer = new Uri(NetEaseRequestReferer);
                    request.Content = new FormUrlEncodedContent(HandleRequest(
                        new SongSearchRequest(args.SongName, args.Artist, _options.Provider.Lyric.GetLyricProviderOption(DownloaderName).Depth),
                        secretKey,
                        encSecKey));
                });

            ValidateSongSearchResponse(searchResult, args);

            // 通过 MusicInfo 获取 songId
            var songId = searchResult.GetFirstMatchSongId(args.SongName, args.Duration);
            var getLyricRequest = new GetLyricRequest(SongId);

            return await _warpHttpClient.PostAsync(NetEaseGetLyricUrl,
                requestOption: request =>
                {
                    request.Headers.Referrer = new Uri(NetEaseRequestReferer);
                    request.Content = new FormUrlEncodedContent(HandleRequest(getLyricRequest, secretKey, encSecKey));
                });
        }

        protected override async ValueTask<LyricsItemCollection> GenerateLyricAsync(object lyricsObject, LyricsProviderArgs args)
        {
            await ValueTask.CompletedTask;

            var json = JsonConvert.DeserializeObject<GetLyricResponse>((lyricsObject as string)!);
            if (json?.OriginalLyric == null || string.IsNullOrEmpty(json.OriginalLyric.Text))
            {
                return new LyricsItemCollection(null);
            }

            if (json.OriginalLyric.Text.Contains("纯音乐，请欣赏"))
            {
                return new LyricsItemCollection(null);
            }

            return _lyricsItemCollectionFactory.Build(
                json.OriginalLyric.Text,
                json.TranslationLyric?.Text);
        }

        protected virtual void ValidateSongSearchResponse(SongSearchResponse response, LyricsProviderArgs args)
        {
            if (response?.StatusCode != SongSearchResponseStatusCode.Success)
            {
                throw new ErrorCodeException(ErrorCodes.TheReturnValueIsIllegal, attachObj: args);
            }

            if (response.Items is not { SongCount: > 0 })
            {
                throw new ErrorCodeException(ErrorCodes.NoMatchingSong, attachObj: args);
            }
        }

        private Dictionary<string, string> HandleRequest(object srcParams, string secretKey, string encSecKey)
        {
            return new Dictionary<string, string>
            {
                {
                    "params", NetEaseMusicEncryptionHelper.AesEncode(
                        NetEaseMusicEncryptionHelper.AesEncode(
                            JsonConvert.SerializeObject(srcParams), NetEaseMusicEncryptionHelper.Nonce), secretKey)
                },
                { "encSecKey", encSecKey }
            };
        }
    }
}

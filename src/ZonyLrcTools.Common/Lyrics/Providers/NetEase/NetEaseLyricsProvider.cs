using System.Net.Http.Headers;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using ZonyLrcTools.Common.Configuration;
using ZonyLrcTools.Common.Infrastructure.Encryption;
using ZonyLrcTools.Common.Infrastructure.Exceptions;
using ZonyLrcTools.Common.Infrastructure.Network;
using ZonyLrcTools.Common.Lyrics.Providers.NetEase.JsonModel;

namespace ZonyLrcTools.Common.Lyrics.Providers.NetEase
{
    public class NetEaseLyricsProvider : LyricsProvider
    {
        public override string DownloaderName => InternalLyricsProviderNames.NetEase;

        private readonly IWarpHttpClient _warpHttpClient;
        private readonly ILyricsItemCollectionFactory _lyricsItemCollectionFactory;
        private readonly GlobalOptions _options;

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

            // 直接使用提供的 songId 请求歌词
            var lyricResponse = await _warpHttpClient.PostAsync(NetEaseGetLyricUrl,
                requestOption: request =>
                {
                    request.Headers.Referrer = new Uri(NetEaseRequestReferer);
                    request.Content = new FormUrlEncodedContent(HandleRequest(
                        new GetLyricRequest(args.SongId), secretKey, encSecKey));
                });

            return await lyricResponse.Content.ReadAsStringAsync();
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

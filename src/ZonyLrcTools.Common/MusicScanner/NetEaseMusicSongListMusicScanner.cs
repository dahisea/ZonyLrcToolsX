using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QRCoder;
using ZonyLrcTools.Common.Infrastructure.DependencyInject;
using ZonyLrcTools.Common.Infrastructure.Encryption;
using ZonyLrcTools.Common.Infrastructure.Exceptions;
using ZonyLrcTools.Common.Infrastructure.Network;
using ZonyLrcTools.Common.MusicScanner.JsonModel;

namespace ZonyLrcTools.Common.MusicScanner
{
    public class NetEaseMusicSongListMusicScanner : ISingletonDependency
    {
        private readonly IWarpHttpClient _warpHttpClient;
        private readonly ILogger<NetEaseMusicSongListMusicScanner> _logger;
        private const string Host = "https://music.163.com";
        private static readonly object _lock = new object();  // 添加锁对象

        private string Cookie { get; set; } = string.Empty;
        private string CsrfToken { get; set; } = string.Empty;

        public NetEaseMusicSongListMusicScanner(IWarpHttpClient warpHttpClient, ILogger<NetEaseMusicSongListMusicScanner> logger)
        {
            _warpHttpClient = warpHttpClient;
            _logger = logger;
        }

        public async Task<List<MusicInfo>> GetMusicInfoFromNetEaseMusicSongListAsync(string songListIds, string outputDirectory, string pattern)
        {
            await EnsureLoggedInAsync();

            var tasks = songListIds.Split(';')
                                   .Select(id => GetMusicInfoBySongIdAsync(id, outputDirectory, pattern))
                                   .ToList();

            var results = await Task.WhenAll(tasks);
            return results.SelectMany(info => info).ToList();
        }

        private async Task EnsureLoggedInAsync()
        {
            // 使用锁防止并发访问登录逻辑
            lock (_lock)
            {
                if (!string.IsNullOrEmpty(Cookie)) return;
            }

            var loginResponse = await LoginViaQrCodeAsync();

            lock (_lock)
            {
                if (string.IsNullOrEmpty(Cookie))
                {
                    Cookie = loginResponse.cookieContainer?.GetCookieHeader(new Uri(Host)) ?? string.Empty;
                    CsrfToken = loginResponse.csrfToken ?? string.Empty;
                }
            }
        }

        private async Task<List<MusicInfo>> GetMusicInfoBySongIdAsync(string songId, string outputDirectory, string pattern)
        {
            var secretKey = NetEaseMusicEncryptionHelper.CreateSecretKey(16);
            var encSecKey = NetEaseMusicEncryptionHelper.RsaEncode(secretKey);

            var response = await _warpHttpClient.PostAsync<GetMusicInfoFromNetEaseMusicSongListResponse>(
                $"{Host}/weapi/v6/playlist/detail?csrf_token={CsrfToken}", requestOption: request =>
                {
                    request.Headers.Add("Cookie", Cookie);
                    request.Content = new FormUrlEncodedContent(HandleRequest(new
                    {
                        csrf_token = CsrfToken,
                        id = songId,
                        n = 1000,
                        offset = 0,
                        total = true,
                        limit = 1000,
                    }, secretKey, encSecKey));
                    request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
                });

            if (response.Code != 200 || response.PlayList?.SongList == null)
            {
                throw new ErrorCodeException(ErrorCodes.NotSupportedFileEncoding);
            }

            return response.PlayList.SongList
                .Where(song => !string.IsNullOrEmpty(song.Name))
                .Select(song =>
                {
                    var filePath = Path.Combine(outputDirectory, pattern.Replace("{Name}", song.Name).Replace("{Artist}", song.Artist));
                    var songId = song.SongId;
                    return new MusicInfo(filePath, song.Name, song.Artist, songId);
                }).ToList();
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

        private async Task<(string? csrfToken, CookieContainer? cookieContainer)> LoginViaQrCodeAsync()
        {
            var qrCodeKeyJson = await (await PostAsync($"{Host}/weapi/login/qrcode/unikey", new
            {
                type = 1
            })).Content.ReadAsStringAsync();
            var uniKey = JObject.Parse(qrCodeKeyJson).SelectToken("$.unikey")!.Value<string>();
            if (string.IsNullOrEmpty(uniKey)) return (null, null);

            var qrCodeLink = $"{Host}/login?codekey={uniKey}";

            var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(qrCodeLink, QRCodeGenerator.ECCLevel.L);
            var qrCode = new AsciiQRCode(qrCodeData);
            var asciiQrCodeString = qrCode.GetGraphic(1, drawQuietZones: false);

            _logger.LogInformation("请扫码登录:");
            _logger.LogInformation("\n{AsciiQrCodeString}", asciiQrCodeString);
            _logger.LogInformation("链接如下:");
            _logger.LogInformation(qrCodeLink);

            var isLogin = false;
            while (!isLogin)
            {
                var (isSuccess, cookieContainer) = await CheckIsLoginAsync(uniKey);
                isLogin = isSuccess;

                if (!isLogin)
                {
                    await Task.Delay(2000);
                }
                else
                {
                    return (cookieContainer?.GetCookies(new Uri(Host))["__csrf"]?.Value, cookieContainer);
                }
            }

            return (null, null);
        }

        private async Task<(bool isSuccess, CookieContainer? cookieContainer)> CheckIsLoginAsync(string uniKey)
        {
            var responseMessage = await PostAsync($"{Host}/weapi/login/qrcode/client/login", new
            {
                key = uniKey,
                type = 1
            });

            var responseString = await responseMessage.Content.ReadAsStringAsync();
            var responseCode = JObject.Parse(responseString)["code"]?.Value<int>();

            if (responseCode != 803)
            {
                return (false, null);
            }

            if (!responseMessage.Headers.TryGetValues("Set-Cookie", out var cookies))
            {
                return (false, null);
            }

            var cookieContainer = new CookieContainer();
            foreach (var cookie in cookies)
            {
                cookieContainer.SetCookies(new Uri(Host), cookie);
            }

            return (true, cookieContainer);
        }

        private async Task<HttpResponseMessage> PostAsync(string url, object @params)
        {
            var secretKey = NetEaseMusicEncryptionHelper.CreateSecretKey(16);
            var encSecKey = NetEaseMusicEncryptionHelper.RsaEncode(secretKey);

            return await _warpHttpClient.PostReturnHttpResponseAsync(url, requestOption:
                request =>
                {
                    request.Content = new FormUrlEncodedContent(HandleRequest(@params, secretKey, encSecKey));
                    request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
                });
        }
    }
}

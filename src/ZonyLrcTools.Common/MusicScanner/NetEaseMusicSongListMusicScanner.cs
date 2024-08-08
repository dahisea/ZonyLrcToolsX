using System;
using System.Collections.Generic;
using System.IO;
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

        private string _cookie = string.Empty;
        private string _csrfToken = string.Empty;

        public NetEaseMusicSongListMusicScanner(IWarpHttpClient warpHttpClient, ILogger<NetEaseMusicSongListMusicScanner> logger)
        {
            _warpHttpClient = warpHttpClient;
            _logger = logger;
        }

        public async Task<List<MusicInfo>> GetMusicInfoFromNetEaseMusicSongListAsync(string songListIds, string outputDirectory, string pattern)
        {
            await EnsureLoggedInAsync();

            var musicInfoList = new List<MusicInfo>();
            foreach (var songListId in songListIds.Split(';'))
            {
                _logger.LogInformation("正在获取歌单 {SongListId} 的歌曲列表。", songListId);
                var musicInfos = await GetMusicInfoBySongListIdAsync(songListId, outputDirectory, pattern);
                musicInfoList.AddRange(musicInfos);
            }

            return musicInfoList;
        }

        private async Task EnsureLoggedInAsync()
        {
            if (string.IsNullOrEmpty(_cookie))
            {
                var loginResponse = await LoginViaQrCodeAsync();
                _csrfToken = loginResponse.CsrfToken ?? string.Empty;
                _cookie = loginResponse.CookieContainer?.GetCookieHeader(new Uri(Host)) ?? string.Empty;

                if (string.IsNullOrEmpty(_cookie) || string.IsNullOrEmpty(_csrfToken))
                {
                    throw new Exception("登录失败，无法获取必要的 Cookie 和 CSRF 令牌。");
                }
            }
        }

        private async Task<List<MusicInfo>> GetMusicInfoBySongListIdAsync(string songListId, string outputDirectory, string pattern)
        {
            var secretKey = NetEaseMusicEncryptionHelper.CreateSecretKey(16);
            var encSecKey = NetEaseMusicEncryptionHelper.RsaEncode(secretKey);
            var response = await PostAsync<GetMusicInfoFromNetEaseMusicSongListResponse>(
                $"{Host}/weapi/v6/playlist/detail?csrf_token={_csrfToken}",
                new
                {
                    csrf_token = _csrfToken,
                    id = songListId,
                    n = 1000,
                    offset = 0,
                    total = true,
                    limit = 1000,
                });

            if (response?.Code != 200 || response.PlayList?.SongList == null)
            {
                throw new ErrorCodeException(ErrorCodes.NotSupportedFileEncoding);
            }

            return response.PlayList.SongList
                .Where(song => !string.IsNullOrEmpty(song.Name))
                .Select(song =>
                {
                    var artistNames = song.ArtistNames;
                    var fakeFilePath = Path.Combine(outputDirectory, pattern.Replace("{Name}", song.Name).Replace("{Artist}", artistNames));
                    return new MusicInfo(song.Name!, artistNames, song.SongId);
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

        private async Task<LoginResponse> LoginViaQrCodeAsync()
        {
            var qrCodeKeyJson = await PostAsync<LoginQrCodeKeyResponse>($"{Host}/weapi/login/qrcode/unikey", new { type = 1 });
            var uniKey = qrCodeKeyJson?.Unikey;

            if (string.IsNullOrEmpty(uniKey)) return new LoginResponse { CsrfToken = null, CookieContainer = null };

            var qrCodeLink = $"{Host}/login?codekey={uniKey}";

            DisplayQrCode(qrCodeLink);

            while (true)
            {
                var checkLoginResponse = await CheckIsLoginAsync(uniKey);
                if (checkLoginResponse.IsSuccess)
                {
                    return new LoginResponse
                    {
                        CsrfToken = checkLoginResponse.CookieContainer?.GetCookies(new Uri(Host))["__csrf"]?.Value,
                        CookieContainer = checkLoginResponse.CookieContainer
                    };
                }
                await Task.Delay(2000);
            }
        }

        private async Task<CheckLoginResponse> CheckIsLoginAsync(string uniKey)
        {
            var response = await PostAsync<CheckLoginResponse>($"{Host}/weapi/login/qrcode/client/login", new { key = uniKey, type = 1 });
            var responseCode = response.Code;

            if (responseCode != 803) return new CheckLoginResponse { IsSuccess = false, CookieContainer = null };

            return new CheckLoginResponse
            {
                IsSuccess = true,
                CookieContainer = response.CookieContainer
            };
        }

        private async Task<T> PostAsync<T>(string url, object parameters)
        {
            var secretKey = NetEaseMusicEncryptionHelper.CreateSecretKey(16);
            var encSecKey = NetEaseMusicEncryptionHelper.RsaEncode(secretKey);

            var response = await _warpHttpClient.PostReturnHttpResponseAsync(url, requestOption: request =>
            {
                request.Content = new FormUrlEncodedContent(HandleRequest(parameters, secretKey, encSecKey));
                request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
                request.Headers.Add("Cookie", _cookie);
            });

            var jsonString = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(jsonString)!;
        }

        private void DisplayQrCode(string qrCodeLink)
        {
            var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(qrCodeLink, QRCodeGenerator.ECCLevel.L);
            var qrCode = new AsciiQRCode(qrCodeData);
            var asciiQrCodeString = qrCode.GetGraphic(1, drawQuietZones: false);

            _logger.LogInformation("请扫码登录:");
            _logger.LogInformation("\n{AsciiQrCodeString}", asciiQrCodeString);
            _logger.LogInformation("链接: {QrCodeLink}", qrCodeLink);
        }
    }

    // Helper classes to match the expected structure for responses
    public class LoginResponse
    {
        public string? CsrfToken { get; set; }
        public CookieContainer? CookieContainer { get; set; }
    }

    public class CheckLoginResponse
    {
        public bool IsSuccess { get; set; }
        public CookieContainer? CookieContainer { get; set; }
    }

    public class LoginQrCodeKeyResponse
    {
        [JsonProperty("unikey")]
        public string? Unikey { get; set; }
    }

    public class CheckLoginResponse
    {
        [JsonProperty("code")]
        public int Code { get; set; }
        public CookieContainer? CookieContainer { get; set; }
    }
}

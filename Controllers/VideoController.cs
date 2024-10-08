using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
using Xabe.FFmpeg.Downloader;
using Xabe.FFmpeg;
using YoutubeExplode.Common;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

namespace YoutubeConverter.Controllers
{
    public class VideoController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Download(string url)
        {
            var youtube = new YoutubeClient();

            // Obter informações do vídeo
            var video = await youtube.Videos.GetAsync(url);
            var streamManifest = await youtube.Videos.Streams.GetManifestAsync(video.Id);

            // Obter o stream de vídeo Full HD (1920x1080)
            var videoStream = streamManifest.GetVideoStreams()
                .FirstOrDefault(s => s.VideoResolution == new Resolution(1920, 1080));

            // Obter o stream de áudio com a maior qualidade
            var audioStream = streamManifest.GetAudioStreams().GetWithHighestBitrate();

            // Definir a pasta e o caminho dos arquivos
            var mediaFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "Media");
            if (!Directory.Exists(mediaFolderPath))
            {
                Directory.CreateDirectory(mediaFolderPath);
            }

            // Limpar o título do vídeo para criar nomes de arquivos válidos
            var sanitizedTitle = Regex.Replace(video.Title, @"[<>:""/\\|?*]", string.Empty);
            var videoFilePath = Path.Combine(mediaFolderPath, $"{sanitizedTitle}_video.mp4");
            var audioFilePath = Path.Combine(mediaFolderPath, $"{sanitizedTitle}_audio.mp3");
            var finalFilePath = Path.Combine(mediaFolderPath, $"{sanitizedTitle}.mp4");

            // Baixar os streams de vídeo e áudio
            await youtube.Videos.Streams.DownloadAsync(videoStream, videoFilePath);
            await youtube.Videos.Streams.DownloadAsync(audioStream, audioFilePath);

            // Baixar FFmpeg se necessário
            await FFmpegDownloader.GetLatestVersion(FFmpegVersion.Official, mediaFolderPath);
            FFmpeg.SetExecutablesPath(mediaFolderPath);

            // Obter as informações dos arquivos de vídeo e áudio
            var videoInfo = await FFmpeg.GetMediaInfo(videoFilePath);
            var audioInfo = await FFmpeg.GetMediaInfo(audioFilePath);

            // Obter os streams de vídeo e áudio a partir dos objetos MediaInfo
            var videoStreamInfo = videoInfo.VideoStreams.FirstOrDefault();
            var audioStreamInfo = audioInfo.AudioStreams.FirstOrDefault();

            // Usar Xabe.FFmpeg para combinar vídeo e áudio
            var conversion = await FFmpeg.Conversions.New()
                .AddStream(videoStreamInfo)
                .AddStream(audioStreamInfo)
                .SetOutput(finalFilePath)
                .AddParameter("-preset ultrafast") // Adiciona a opção de conversão rápida
                .Start();

            // Ler o arquivo final para retorno
            var memory = new MemoryStream();
            using (var stream = new FileStream(finalFilePath, FileMode.Open, FileAccess.Read))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;

            // Retornar o arquivo combinado para download
            return File(memory, "video/mp4", $"{sanitizedTitle}.mp4");
        }
    }
}

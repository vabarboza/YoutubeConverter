using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

namespace YoutubeConverter.Controllers
{
    public class MusicController : Controller
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
            var finalFilePath = Path.Combine(mediaFolderPath, $"{sanitizedTitle}.mp3");

            // Baixar o stream de áudio
            await youtube.Videos.Streams.DownloadAsync(audioStream, finalFilePath);

            // Ler o arquivo final para retorno
            var memory = new MemoryStream();
            using (var stream = new FileStream(finalFilePath, FileMode.Open, FileAccess.Read))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;

            // Retornar o arquivo de áudio para download como MP3
            return File(memory, "audio/mpeg", $"{sanitizedTitle}.mp3");
        }

    }
}

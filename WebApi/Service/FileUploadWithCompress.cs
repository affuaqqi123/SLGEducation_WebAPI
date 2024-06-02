using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;

namespace WebApi.Service

{
    public class FileUploadWithCompress
    {
        public async Task CompressAndSaveImageAsync(IFormFile imageFile, string outputPath, int quality = 75)
        {

            //int quality = 75: (higher values mean better quality but larger file size, and lower values mean more compression but lower quality)
            using var imageStream = imageFile.OpenReadStream();
            using var image = Image.Load(imageStream);
            var encoder = new JpegEncoder
            {
                Quality = quality, // Adjust this value for desired compression quality.
            };

            await Task.Run(() => image.Save(outputPath, encoder));
        }
    }
}
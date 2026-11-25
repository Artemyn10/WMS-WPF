using System.IO;
using System.Windows.Media.Imaging;

namespace WMS.Helpers;

public static class ImageHelper
{
    /// Превращает байты файла в картинку для WPF. Если это не изображение - вернёт null.
    public static BitmapImage? FromBytes(byte[]? data)
    {
        if (data == null || data.Length == 0)
        {
            return null;
        }

        try
        {
            using var stream = new MemoryStream(data);
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = stream;
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch
        {
            return null;
        }
    }
}
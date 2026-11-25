using WMS.Models;

namespace WMS.ViewModels;

public class ProductFileRowViewModel
{
    // 0 - новый файл, который ещё не сохранён в базе
    public int Id { get; }
    public string FileName { get; }
    public ProductFileKind Kind { get; }
    public int Size { get; }

    // Содержимое хранится здесь только у новых файлов, у сохранённых оно загружается по требованию
    public byte[]? Data { get; }

    public string KindDisplay => Kind == ProductFileKind.Photo ? "Фото" : "Документ";

    public string SizeDisplay => Size >= 1024 * 1024
        ? $"{Size / 1048576.0:0.0} МБ"
        : $"{Math.Max(1, Size / 1024)} КБ";

    public string StatusDisplay => Id == 0 ? "новый" : string.Empty;

    public ProductFileRowViewModel(int id, string fileName, ProductFileKind kind, int size, byte[]? data)
    {
        Id = id;
        FileName = fileName;
        Kind = kind;
        Size = size;
        Data = data;
    }
}
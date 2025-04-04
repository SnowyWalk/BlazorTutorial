using BlazorApp2.Attributes;
using BlazorApp2.Util;
using System.ComponentModel;
namespace BlazorApp2.Data;

public class ImageDatabaseService : PersistableClass
{
    [Persistable]
    public string Path { get; set; } = "C:/Users/a/Desktop/20250124/downloaded_images";

    [Persistable]
    public List<ImageData> m_imageDatas = new List<ImageData>();
    public Dictionary<DateOnly, Dictionary<string, List<ImageData>>> m_imageDataByDateDic = new Dictionary<DateOnly, Dictionary<string, List<ImageData>>>();

    public void Load()
    {
        var baseUri = new Uri(Path);
        m_imageDatas.Clear();

        if (!Directory.Exists(Path))
            return;

        var pngFiles = Directory.GetFiles(Path, "*.jpg", SearchOption.AllDirectories);

        foreach (var filePath in pngFiles)
        {
            var fileInfo = new FileInfo(filePath);
            var fullUri = new Uri(fileInfo.FullName);
            var relativeUri = baseUri.MakeRelativeUri(fullUri);
            var relativePath = Uri.UnescapeDataString(relativeUri.ToString()).Replace("\\", "/");

            int lastUnderscoreIndex = fileInfo.Name.LastIndexOf('_');
            string groupKey = fileInfo.Name.Substring(0, lastUnderscoreIndex);
            m_imageDatas.Add(new ImageData {
                FileName = fileInfo.Name,
                RelativePath = "/" + relativePath,
                GroupKey = groupKey,
                CreateDateTime = fileInfo.CreationTime,
            });
        }

        BuildImageDataByDateDic();
    }

    private void BuildImageDataByDateDic()
    {
        m_imageDataByDateDic.Clear();

        foreach (ImageData imageData in m_imageDatas)
        {
            DateOnly date = DateOnly.FromDateTime(imageData.CreateDateTime);
            if (m_imageDataByDateDic.TryGetValue(date, out var dicByGroup) == false)
                m_imageDataByDateDic[date] = dicByGroup = new Dictionary<string, List<ImageData>>();
            
            if(dicByGroup.TryGetValue(imageData.GroupKey, out var imageList) == false)
                dicByGroup[imageData.GroupKey] = imageList = new List<ImageData>();
            
            imageList.Add(imageData);
        }
    }
}
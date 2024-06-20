using Gradio.Net.Enums;
namespace Gradio.Net;

public class Audio : Component, IStreamingInput, IHaveChangeEvent, IHaveClearEvent, IHavePlayEvent, IHaveStartRecordingEvent,IHaveStopRecordingEvent, IHavePauseRecordingEvent, IHaveStreamEvent, IHaveUploadEvent,IHaveStopEvent, IHavePauseEvent
{
    internal Audio() { }
    internal AudioFormat Format { get;  set; }
    
    internal int? Height { get;  set; }
    internal int? Width { get;  set; }
    internal IEnumerable<AudioSource> Sources { get;  set; }
    internal bool? ShowDownloadButton { get;  set; }
    internal bool? ShowShareButton { get;  set; }

    internal bool Autoplay { get;  set; }

    internal bool? ShowEditButton { get;  set; }
    internal AudioType Type { get;  set; }

    public void CheckStreamable()
    {
        //todo
    }
    internal override object PreProcess(object data)
    {
        if (data == null)
        {
            return null;
        }

        string? str = data.ToString();

        FileData fileData = JsonUtils.Deserialize<FileData>(str);
        return Context.DownloadableFiles[fileData.Path];
    }

    internal override object PostProcess(string rootUrl, object data)
    {
        if (data == null)
        {
            return null;
        }
        string? str = data.ToString();

        string fileId = str.ToMD5_16();
        Context.DownloadableFiles.TryAdd(fileId, str);
        if (ClientUtils.IsUrl(str))
        {
            return new FileData { Path = null, Url = str };
        }

        return new FileData { Path = fileId, Url = $"{rootUrl}/file={fileId}" };
    }

    public static string Payload(object obj)
    {
        if (obj == null)
        {
            return null;
        }

        if (obj is string str)
        {
            return str;
        }

        throw new ArgumentException($"Payload Type expect string actual {obj.GetType()}");
    }
}

var processes = Process.GetProcessesByName("ffmpeg.exe");
foreach (Process process in processes)
{
    process.Kill();
}

Console.WriteLine("Initial Folders");

#region Initial Folders

#if DEBUG
var ffmpegLocation = "";
var videoPath = "";
var hlsPath = "";

ffmpegLocation = "C:\\ffmpeg-master-latest-win64-gpl\\bin";
videoPath = "C:\\ffmpeg-master-latest-win64-gpl\\bin";
hlsPath = $"{videoPath}\\hls";
#endif

var ffmpegLocation = args[0];
var videoPath = args[1];
var hlsPath = $"{videoPath}\\hls";

#endregion


#region Create Hls Path

if (!Directory.Exists(hlsPath))
{
    Console.WriteLine("Hls Path Created!");

    Directory.CreateDirectory(hlsPath);
}
else
{
    Console.WriteLine("Hls Path Exist!");
}

#endregion

#region Get Videos Path

var videos = Directory.GetFiles(videoPath, "*.mp4", SearchOption.TopDirectoryOnly);

Console.WriteLine($"All MP4 Files : {videos.Length}");

var hlsVideos = Directory.GetFiles(hlsPath, "*.m3u8", SearchOption.TopDirectoryOnly);

Console.WriteLine($"All HLS Files : {hlsVideos.Length}");

#endregion

#region Exluding Converted Videos

//var videosToProccess = videos.Select(x => x.Split("\\")[^1].Split(".")[0]).Except(
//    hlsVideos.Select(x => x.Split("\\")[^1].Split(".")[0]).ToList()
//    ).Select(x => videoPath + "\\" + x + ".mp4")
//.ToArray();

//Console.WriteLine($"All Videos To Convert : {videosToProccess.Length}");
#endregion

Console.WriteLine("Start Processing");
Console.WriteLine("---------------");

var tasks = new List<Task>();

var index = 0;

await Parallel.ForEachAsync(videos, new ParallelOptions { MaxDegreeOfParallelism = 10 }, async (video, ct) =>
{
    Interlocked.Increment(ref index);

    Console.WriteLine($"Starting Video Number {index}");

    var thisIndex = index;

    //var video = videosToProccess[index];

    var videoName = video.Split("\\")[^1].Split(".")[0];


    if (File.Exists($"{hlsPath}\\{videoName}\\video.m3u8"))
    {
        Console.WriteLine($"Skipping Video Number {thisIndex}");
        return;
    }

    try
    {
        #region Sample Command

        //ffmpeg -i some_fun_video_name.mp4 -profile:v baseline -level 3.0 -s 800x480 -start_number 0 -hls_time 4 -hls_list_size 0 -f hls ./media/some_fun_video_name/hls/480_out.m3u8
        //ffmpeg -i some_fun_video_name.mp4 -profile:v baseline -level 3.0 -s 960x540 -start_number 0 -hls_time 4 -hls_list_size 0 -f hls ./media/some_fun_video_name/hls/540_out.m3u8
        //ffmpeg -i some_fun_video_name.mp4 -profile:v baseline -level 3.0 -s 1280x720 -start_number 0 -hls_time 4 -hls_list_size 0 -f hls ./media/some_fun_video_name/hls/720_out.m3u8

        #endregion

        var commands = new string[]
        {
        $"-i {video} -profile:v baseline -level 3.0 -s 800x480 -start_number 0 -hls_time 4 -hls_list_size 0 -f hls {hlsPath}\\{videoName}\\480_out.m3u8",
        $"-i {video} -profile:v baseline -level 3.0 -s 960x540 -start_number 0 -hls_time 4 -hls_list_size 0 -f hls {hlsPath}\\{videoName}\\540_out.m3u8",
        $"-i {video} -profile:v baseline -level 3.0 -s 1280x720 -start_number 0 -hls_time 4 -hls_list_size 0 -f hls {hlsPath}\\{videoName}\\720_out.m3u8"
        };

        Directory.CreateDirectory($"{hlsPath}\\{videoName}");

        var isAllDoneSuccessfully = true;

        foreach (var command in commands)
        {
            var startInfo = new ProcessStartInfo($"{ffmpegLocation}\\ffmpeg.exe", "-y " + command);

            startInfo.CreateNoWindow = true;
            startInfo.RedirectStandardInput = true;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.UseShellExecute = false;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;

            var process = new Process() { StartInfo = startInfo, EnableRaisingEvents = true };

            var exitCode = await process.WaitForExitAsync(true, null);

            if (exitCode != 0)
            {
                isAllDoneSuccessfully = false;
            }
        }

        if (isAllDoneSuccessfully)
        {
            using var streamWriter = new StreamWriter($"{hlsPath}\\{videoName}\\video.m3u8", false, Encoding.UTF8);
            await streamWriter.WriteLineAsync("#EXTM3U");
            await streamWriter.WriteLineAsync("#EXT-X-STREAM-INF:BANDWIDTH=750000,RESOLUTION=854x480");
            await streamWriter.WriteLineAsync("480_out.m3u8");
            await streamWriter.WriteLineAsync("#EXT-X-STREAM-INF:BANDWIDTH=1200000,RESOLUTION=960x540");
            await streamWriter.WriteLineAsync("540_out.m3u8");
            await streamWriter.WriteLineAsync("#EXT-X-STREAM-INF:BANDWIDTH=2000000,RESOLUTION=1280x720");
            await streamWriter.WriteLineAsync("720_out.m3u8");
            await streamWriter.FlushAsync();
            streamWriter.Close();
        }
        else
        {
            if (Directory.Exists($"{hlsPath}\\{videoName}"))
            {
                Directory.Delete($"{hlsPath}\\{videoName}");
            }
        }

        Console.WriteLine($"End Video Number {thisIndex}");
    }
    catch (Exception ex)
    {
        if (Directory.Exists($"{hlsPath}\\{videoName}"))
        {
            Directory.Delete($"{hlsPath}\\{videoName}");
        }
    }

});

//for (int index = 0; index < videosToProccess.Length; index++)
//{
  
//}

//var chunkedTasks = tasks.Chunk(4).ToList();

//foreach (var item in chunkedTasks)
//{
//    await Task.WhenAll(item);
//}


Console.WriteLine("---------------");
Console.WriteLine("Process Complete");

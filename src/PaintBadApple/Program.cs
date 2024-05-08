using System.Diagnostics;
using System.Media;
using MspaintRemote;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

// convert video into frames
Directory.CreateDirectory("tmp");
Directory.CreateDirectory("tmp/frame");

Process.Start(new ProcessStartInfo {
    UseShellExecute = false,
    CreateNoWindow = true,
    FileName = "ffmpeg.exe",
    Arguments = "-i badapple.mp4 -vf fps=30 ./tmp/frame/%d.png"
});

// extract the audio as well
Process.Start(new ProcessStartInfo {
    UseShellExecute = false,
    CreateNoWindow = true,
    FileName = "ffmpeg.exe",
    Arguments = "-i badapple.mp4 ./tmp/audio.wav"
});

var player = new SoundPlayer("./tmp/audio.wav");

var paint = new MspaintController();

var cw = paint.CanvasWidth;
var ch = paint.CanvasHeight;
var buffer = new Bgr24[cw * ch];

player.Play();

var began = DateTime.Now;

int bound = Directory.GetFiles("./tmp/frame").Length;

Console.WriteLine("bad apple!!");

while(true) {
    var past = (DateTime.Now - began).TotalSeconds;
    Console.Title = $"bad apple!! | {past:N2}s / {bound / 30}s";
    
    var i = (int)((DateTime.Now - began).TotalSeconds * 30);

    if (i >= bound)
        break;
    
    using var file = File.OpenRead($"./tmp/frame/{i + 1}.png");
    using var img = Image.Load<Bgr24>(file);
    
    img.Mutate(x => x.Resize(paint.CanvasWidth, paint.CanvasHeight));
    img.Mutate(x => x.Flip(FlipMode.Vertical));
    img.CopyPixelDataTo(buffer);
    
    paint.UpdateCanvas<Bgr24>(buffer);
    Thread.Sleep(0); // relinquish time slice
}

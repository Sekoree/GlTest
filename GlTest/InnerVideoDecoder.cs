using System.Diagnostics;
using System.Runtime.InteropServices;
using Flyleaf.FFmpeg;
using Flyleaf.FFmpeg.Codec;
using Flyleaf.FFmpeg.Codec.Decode;
using Flyleaf.FFmpeg.Filter;
using Flyleaf.FFmpeg.Format;
using Flyleaf.FFmpeg.Format.Demux;
using Flyleaf.FFmpeg.HWAccel;
using Flyleaf.FFmpeg.Spec;

namespace GlTest;

public class InnerVideoDecoder
{
    private readonly Demuxer _demuxer;
    private readonly VideoStream _videoStream;
    private readonly VideoDecoder _videoDecoder;

    private static ImageConverter? _imageConverter;

    private VideoTexture? _texture;
    private VideoFrame? _frame;

    public InnerVideoDecoder(Demuxer demuxer, VideoStream videoStream, VideoDecoder videoDecoder)
    {
        _demuxer = demuxer;
        _videoStream = videoStream;
        _videoDecoder = videoDecoder;
    }

    static InnerVideoDecoder()
    {
        //Debug Path <CurrentDir>/runtime/win-x64/native
        Utils.LoadLibraries(
            "C:\\Users\\Sekoree\\RiderProjects\\GlTest\\GlTest\\bin\\Debug\\net9.0\\runtimes\\win-x64\\native");
        Console.WriteLine("FFmpeg loaded");
    }

    public static unsafe InnerVideoDecoder PrepareVideo(string path)
    {
        var demuxer = new Demuxer()
        {
            MaxProbeBytes = 100 * 1024 * 1024,
            MaxAnalyzeMcs = (long)TimeSpan.FromSeconds(5).TotalMilliseconds,
        };
        demuxer.Open(path);
        demuxer.Analyse();
        demuxer.Dump();

        var videoStream = demuxer.BestVideoStream() ?? throw new Exception("No video stream found");
        demuxer.Enable(videoStream);
        
        HWFramesContext? hwFrames = null;
        HWDeviceContext hwDevice = new HWDeviceContext(AVHWDeviceType.Vulkan, "0");
        AVPixelFormat hwPixelFormat = HWDeviceSpec.FindHWPixelFormat(AVHWDeviceType.Vulkan);
        VideoDecoderSpec videoCodec = CodecSpec.FindHWVideoDecoder(videoStream.CodecId, hwPixelFormat) ??
                                      throw new Exception("No decoder found");
        //videoCodec = CodecSpec.FindVideoDecoder(videoStream.CodecId) ?? throw new Exception("No decoder found");
        VideoDecoder? videoDecoder = null;
        videoDecoder = new(videoCodec, videoStream, GetHWFormat)
        {
            HWDeviceContext = hwDevice
        };
        videoDecoder.Open().ThrowOnFailure();

        AVPixelFormat GetHWFormat(AVCodecContext* s, AVPixelFormat* fmt)
        {
            List<AVPixelFormat> availablePixelFormats = ArrayUtils.GetPixelFormats(fmt);
            if (!availablePixelFormats.Contains(hwPixelFormat))
                throw new Exception($"HW decoding is not supported for {hwPixelFormat} pixel format");

            if (hwFrames == null)
            {
                hwFrames = new(videoDecoder!, hwDevice, hwPixelFormat);
                hwFrames.InitFrames().ThrowOnFailure();
            }

            videoDecoder!.HWFramesContext = hwFrames;

            return hwPixelFormat;
        }

        var a = new InnerVideoDecoder(demuxer, videoStream, videoDecoder);

        a.StartDecodeThread();

        return a;
    }

    static Lock _lock = new();

    public void StartDecodeThread()
    {
        _ = Task.Run(async () =>
        {
            VideoFrame resultFrame = new VideoFrame();
            FFmpegResult ret;

            var packet = new Packet();
            var hwFrame = new VideoFrame();
            
            //60fps
            var frameTime = TimeSpan.FromSeconds(1.0 / 60).Ticks;
            var readStart = Stopwatch.GetTimestamp();
            var putFrame = 0L;
            var timeToWait = 0L;

            while (_demuxer.ReadPacket(packet).Success)
            {
                readStart = Stopwatch.GetTimestamp();
                if (packet.StreamIndex != _videoStream.Index)
                    continue;

                _videoDecoder.SendPacket(packet).ThrowOnFailure();

                while (true)
                {
                    ret = _videoDecoder.RecvFrame(hwFrame);
                    if (ret.Success)
                    {
                        hwFrame.TransferTo(resultFrame).ThrowOnFailure();
                        _imageConverter ??= new ImageConverter(resultFrame.PixelFormat, resultFrame.Width,
                            resultFrame.Height,
                            AVPixelFormat.Rgb0, resultFrame.Width, resultFrame.Height, SwsFlags.Bilinear);
                        var convFrame = new VideoFrame();
                        _imageConverter.Convert(resultFrame, convFrame);
                        resultFrame.Dispose();
                        hwFrame.Dispose();
                        packet.Dispose();
                        
                        lock (_lock)
                        {
                            _frame = convFrame;
                            putFrame = Stopwatch.GetTimestamp();
                        }

                        resultFrame = new VideoFrame();
                        hwFrame = new VideoFrame();
                        packet = new Packet();
                        //Console.WriteLine("Frame decoded");
                    }

                    if (!ret.TryAgain && !ret.Eof)
                        ret.ThrowOnFailure();
                    else
                        break;
                }
                
                //calc wait time based on how long it took to read the packet and put the frame compared to the frame time
                timeToWait = frameTime - (Stopwatch.GetTimestamp() - readStart);
                //if (timeToWait > 0)
                //    await Task.Delay(TimeSpan.FromTicks(timeToWait));
            }
        });
    }

    private VideoFrame? _lastFrame;
    public VideoTexture? GetFrameAsTexture()
    {
        if (_frame == null)
            return _texture ?? null;
        lock (_lock)
        {
            if (_lastFrame == _frame)
                return null;//_texture;
            if (_texture == null)
                _texture = VideoTexture.CreateTexture(_frame!);
            else
                _texture.SwapTexture(_frame!);
            _lastFrame?.Dispose();
            _lastFrame = _frame;
        }

        return _texture;
    }
}
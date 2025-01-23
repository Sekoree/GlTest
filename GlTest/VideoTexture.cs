using Flyleaf.FFmpeg.Codec;
using OpenTK.Graphics.OpenGL;

namespace GlTest;

public class VideoTexture : IDisposable
{
    public int Handle { get; init; }
    
    public VideoFrame Frame { get; private set; }
    
    public static VideoTexture CreateTexture(VideoFrame frame)
    {
        var handle = GL.GenTexture();
        
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2D, handle);
        
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, frame.Width, frame.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, frame.Data[0]);
        
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        
        return new VideoTexture { Handle = handle, Frame = frame };
    }
    
    public void SwapTexture(VideoFrame frame)
    {
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2D, Handle);
        
        GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, frame.Width, frame.Height, PixelFormat.Rgba, PixelType.UnsignedByte, frame.Data[0]);
        
        Frame.Dispose();
        Frame = frame;
    }
    
    public void Use(TextureUnit unit)
    {
        GL.ActiveTexture(unit);
        GL.BindTexture(TextureTarget.Texture2D, Handle);
    }

    public void Dispose()
    {
        GL.DeleteTexture(Handle);
        Frame.Dispose();
    }
}
using System.Diagnostics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace GlTest;

public class Game : GameWindow
{
    // Because we're adding a texture, we modify the vertex array to include texture coordinates.
    // Texture coordinates range from 0.0 to 1.0, with (0.0, 0.0) representing the bottom left, and (1.0, 1.0) representing the top right.
    // The new layout is three floats to create a vertex, then two floats to create the coordinates.
    private readonly float[] _vertices =
    {
        // Position         Texture coordinates
        1f, 1f, 0.0f, 1.0f, 1.0f, // top right
        1f, -1f, 0.0f, 1.0f, 0.0f, // bottom right
        -1f, -1f, 0.0f, 0.0f, 0.0f, // bottom left
        -1f, 1f, 0.0f, 0.0f, 1.0f // top left
    };

    private readonly uint[] _indices =
    {
        0, 1, 3,
        1, 2, 3
    };

    private int _elementBufferObject;

    private int _vertexBufferObject;

    private int _vertexArrayObject;

    private Shader _shader;

    private VideoTexture _texture;
    private InnerVideoDecoder _videoDecoder;


    public Game(int width, int height, string title) : base(GameWindowSettings.Default, new NativeWindowSettings()
    {
        ClientSize = (width, height),
        Title = title,
    })
    {
        //Empty
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);

        if (KeyboardState.IsKeyDown(Keys.Escape))
            this.Close();
    }

    static int textUnit = 0;
    
    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);
        
        //var startTime = Stopwatch.GetTimestamp();

        //GL.Clear(ClearBufferMask.ColorBufferBit);

        //GL.BindVertexArray(_vertexArrayObject);
        
        //var beforeTexture = Stopwatch.GetTimestamp();

        //_texture.Dispose();
        var tex = _videoDecoder.GetFrameAsTexture();
        if (tex == null)
        {
            //SwapBuffers();
            return;
        }
        _texture = tex;
        _shader.Use();
        
        //var afterTexture = Stopwatch.GetTimestamp();

        GL.DrawElements(PrimitiveType.Triangles, _indices.Length, DrawElementsType.UnsignedInt, 0);
        
        //var afterDraw = Stopwatch.GetTimestamp();

        SwapBuffers();
        
        //var endTime = Stopwatch.GetTimestamp();
        
        //textUnit = (textUnit + 1) % 32;

        //Console.WriteLine($"Before Texture: {TimeSpan.FromTicks(beforeTexture - startTime).TotalMilliseconds}ms");
        //Console.WriteLine($"Texture: {TimeSpan.FromTicks(afterTexture - beforeTexture).TotalMilliseconds}ms");
        //Console.WriteLine($"Draw: {TimeSpan.FromTicks(afterDraw - afterTexture).TotalMilliseconds}ms");
        //Console.WriteLine($"Swap: {TimeSpan.FromTicks(endTime - afterDraw).TotalMilliseconds}ms");
        //Console.WriteLine($"Total: {TimeSpan.FromTicks(endTime - startTime).TotalMilliseconds}ms");
    }

    protected override async void OnLoad()
    {
        base.OnLoad();
        
        //_videoDecoder = InnerVideoDecoder.PrepareVideo("C:\\Users\\Sekoree\\Videos\\11_-_Shinjidai.mp4");
        //_videoDecoder = InnerVideoDecoder.PrepareVideo("C:\\Users\\Sekoree\\Desktop\\Show July24\\INTERGALACTICA.mp4");
        _videoDecoder = InnerVideoDecoder.PrepareVideo("C:\\Users\\Sekoree\\Desktop\\HD Ver\\1 2 Fanclub.mp4");

        GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);

        _vertexArrayObject = GL.GenVertexArray();
        GL.BindVertexArray(_vertexArrayObject);

        _vertexBufferObject = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBufferObject);
        GL.BufferData(BufferTarget.ArrayBuffer, _vertices.Length * sizeof(float), _vertices,
            BufferUsageHint.DynamicDraw);

        _elementBufferObject = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _elementBufferObject);
        GL.BufferData(BufferTarget.ElementArrayBuffer, _indices.Length * sizeof(uint), _indices,
            BufferUsageHint.DynamicDraw);

        // The shaders have been modified to include the texture coordinates, check them out after finishing the OnLoad function.
        _shader = new Shader("shader.vert", "shader.frag");
        _shader.Use();

        // Because there's now 5 floats between the start of the first vertex and the start of the second,
        // we modify the stride from 3 * sizeof(float) to 5 * sizeof(float).
        // This will now pass the new vertex array to the buffer.
        var vertexLocation = _shader.GetAttribLocation("aPosition");
        GL.EnableVertexAttribArray(vertexLocation);
        GL.VertexAttribPointer(vertexLocation, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);

        // Next, we also setup texture coordinates. It works in much the same way.
        // We add an offset of 3, since the texture coordinates comes after the position data.
        // We also change the amount of data to 2 because there's only 2 floats for texture coordinates.
        var texCoordLocation = _shader.GetAttribLocation("aTexCoord");
        GL.EnableVertexAttribArray(texCoordLocation);
        GL.VertexAttribPointer(texCoordLocation, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float),
            3 * sizeof(float));
        
        //get adapter name
        var adapter = GL.GetString(StringName.Renderer);
        Console.WriteLine($"Adapter: {adapter}");
        
        var glVersion = GL.GetString(StringName.Version);
        Console.WriteLine($"GL Version: {glVersion}");
        
        //_texture = _videoDecoder.GetFrameAsTexture();
        //_texture.Use(TextureUnit.Texture0);
    }

    protected override void OnFramebufferResize(FramebufferResizeEventArgs e)
    {
        base.OnFramebufferResize(e);

        GL.Viewport(0, 0, Size.X, Size.Y);
    }
}
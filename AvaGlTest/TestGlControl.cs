using System;
using System.IO;
using System.Numerics;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using static Avalonia.OpenGL.GlConsts;

namespace AvaGlTest;

public class TestGlControl : OpenGlControlBase
{
    private int _vbo;
    private int _vao;
    private int _vertexShader;
    private int _fragmentShader;
    private int _shaderProgram;
    private readonly string _fragShaderSource;
    private readonly string _vertShaderSource;

    public TestGlControl()
    {
        var fragShaderSource = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "shader.frag"));
        _fragShaderSource = fragShaderSource;//GlExtensions.GetShader(GlVersion, true, fragShaderSource);
        var vertShaderSource = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "shader.vert"));
        _vertShaderSource = vertShaderSource; //GlExtensions.GetShader(GlVersion, false, vertShaderSource);
    }

    protected override void OnOpenGlInit(GlInterface gl)
    {
        base.OnOpenGlInit(gl);

        _shaderProgram = gl.CreateProgram();

        _vertexShader = gl.CreateShader(GL_VERTEX_SHADER);
        Console.WriteLine(gl.CompileShaderAndGetError(_vertexShader, _vertShaderSource));
        gl.AttachShader(_shaderProgram, _vertexShader);

        _fragmentShader = gl.CreateShader(GL_FRAGMENT_SHADER);
        Console.WriteLine(gl.CompileShaderAndGetError(_fragmentShader, _fragShaderSource));
        gl.AttachShader(_shaderProgram, _fragmentShader);

        Console.WriteLine(gl.LinkProgramAndGetError(_shaderProgram));

        gl.UseProgram(_shaderProgram);

        Vector3[] vertices =
        [
            new (-1f, -1f, 0.0f),
            new (1f, -1f, 0.0f),
            new (0.0f, 1f, 0.0f)
        ];
        
        _vbo = gl.GenBuffer();
        gl.BindBuffer(GL_ARRAY_BUFFER, _vbo);

        unsafe
        {
            fixed(void* pVertices = vertices)
                gl.BufferData(GL_ARRAY_BUFFER, new IntPtr(sizeof(Vector3) * vertices.Length),
                    new IntPtr(pVertices), GL_STATIC_DRAW);   
        }

        _vao = gl.GenVertexArray();
        gl.BindVertexArray(_vao);

        unsafe
        {
            var size = sizeof(Vector3);
            gl.VertexAttribPointer(
                0, 3, GL_FLOAT, 0, sizeof(Vector3), IntPtr.Zero);   
        }
        gl.EnableVertexAttribArray(0);
        
        gl.CheckError();
    }

    protected override void OnOpenGlDeinit(GlInterface gl)
    {
        base.OnOpenGlDeinit(gl);
    }

    protected override void OnOpenGlRender(GlInterface gl, int fb)
    {
        gl.ClearColor(0.2f, 0.4f, 0.0f, 1f);
        gl.Clear(GL_COLOR_BUFFER_BIT);
        
        gl.Viewport(0, 0, (int)Width, (int)Height);
        
        gl.DrawArrays(GL_TRIANGLES, 0, new IntPtr(3));
        gl.CheckError();
    }
}
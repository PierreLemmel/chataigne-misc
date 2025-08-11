using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;

namespace Plml.HandsExtensionRendering;

public class RenderingWindow : GameWindow
{
    private int _vertexBufferObject;
    private int _vertexArrayObject;
    private int _shaderProgram;

    private readonly float[] _vertices =
    {
        -0.5f, -0.5f, 0.0f,  // Bottom-left
         0.5f, -0.5f, 0.0f,  // Bottom-right
         0.0f,  0.5f, 0.0f,  // Top
    };

    public RenderingWindow(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
        : base(gameWindowSettings, nativeWindowSettings)
    {
    }

    protected override void OnLoad()
    {
        base.OnLoad();

        GL.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);


        #region VBO / VAO
        _vertexBufferObject = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBufferObject);
        GL.BufferData(BufferTarget.ArrayBuffer, _vertices.Length * sizeof(float), _vertices, BufferUsageHint.StaticDraw);

        
        _vertexArrayObject = GL.GenVertexArray();
        GL.BindVertexArray(_vertexArrayObject);


        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);


        // Unbind VAO and VBO to avoid accidental modifications
        GL.BindVertexArray(0);
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        #endregion


        #region Shaders
        int vertexShader = GL.CreateShader(ShaderType.VertexShader);
        string vertexShaderSource = File.ReadAllText("Rendering/Shaders/test.vert");
        GL.ShaderSource(vertexShader, vertexShaderSource);
        GL.CompileShader(vertexShader);
        CheckShaderCompileErrors(vertexShader, "VERTEX");

        int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
        string fragmentShaderSource = File.ReadAllText("Rendering/Shaders/test.frag");
        GL.ShaderSource(fragmentShader, fragmentShaderSource);
        GL.CompileShader(fragmentShader);
        CheckShaderCompileErrors(fragmentShader, "FRAGMENT");

        
        _shaderProgram = GL.CreateProgram();
        GL.AttachShader(_shaderProgram, vertexShader);
        GL.AttachShader(_shaderProgram, fragmentShader);
        GL.LinkProgram(_shaderProgram);
        CheckProgramLinkErrors(_shaderProgram);

        GL.DeleteShader(vertexShader);
        GL.DeleteShader(fragmentShader);
        #endregion
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);


        GL.Clear(ClearBufferMask.ColorBufferBit);
        GL.UseProgram(_shaderProgram);
        GL.BindVertexArray(_vertexArrayObject);
        GL.DrawArrays(PrimitiveType.Triangles, 0, 3);

        Context.SwapBuffers();
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);


    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        GL.Viewport(0, 0, Size.X, Size.Y);
    }

    protected override void OnUnload()
    {
        GL.DeleteBuffer(_vertexBufferObject);
        GL.DeleteVertexArray(_vertexArrayObject);
        GL.DeleteProgram(_shaderProgram);

        base.OnUnload();
    }

    private void CheckShaderCompileErrors(int shader, string type)
    {
        GL.GetShader(shader, ShaderParameter.CompileStatus, out int success);
        if (success == 0)
        {
            string infoLog = GL.GetShaderInfoLog(shader);
            Console.WriteLine($"ERROR::SHADER_COMPILATION_ERROR of type: {type}\n{infoLog}");
        }
    }

    private void CheckProgramLinkErrors(int program)
    {
        GL.GetProgram(program, GetProgramParameterName.LinkStatus, out int success);
        if (success == 0)
        {
            string infoLog = GL.GetProgramInfoLog(program);
            Console.WriteLine($"ERROR::PROGRAM_LINKING_ERROR\n{infoLog}");
        }
    }
}
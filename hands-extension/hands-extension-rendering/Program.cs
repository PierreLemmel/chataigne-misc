using Plml.HandsExtensionRendering;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using Plml.OSCQuery;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Vizcon.OSC;


void RenderMainWindow()
{
    var gameWindowSettings = GameWindowSettings.Default;
    gameWindowSettings.UpdateFrequency = 60.0;

    var nativeWindowSettings = NativeWindowSettings.Default;
    nativeWindowSettings.ClientSize = new Vector2i(800, 600);
    nativeWindowSettings.Title = "OpenTK Triangle Example";
    nativeWindowSettings.Profile = ContextProfile.Core;
    nativeWindowSettings.API = ContextAPI.OpenGL;
    nativeWindowSettings.APIVersion = new Version(3, 3);


    using (var rw = new RenderingWindow(gameWindowSettings, nativeWindowSettings))
    {
        rw.Run();
    }
}


using (OSCQueryServer server = new("Test", 45321, 9050))
{
    OSCQueryNode intNode = OSCQueryNodes.CreateIntNode("/test/int", "Integer Node", 14, -666, 123823);
    OSCQueryNode floatNode = OSCQueryNodes.CreateFloatNode("/test/float", "Float Node", 3.14f, -1f, 1f);
    OSCQueryNode stringNode = OSCQueryNodes.CreateStringNode("/test/string", "String Node", "Hello, OSCQuery!", [
        "Hello, OSCQuery!",
        "Option 2",
        "Option 3"
    ]);



    OSCQueryNode booleanNode = OSCQueryNodes.CreateBooleanNode("/test/boolean", "Boolean Node", true);

    OSCQueryNode colorNode = OSCQueryNodes.CreateColorNode("/test2/color", "Color Node", new RGBA(0xff, 0x88, 0x00, 0xff));

    server.AddNode(intNode);
    server.AddNode(floatNode);
    server.AddNode(stringNode);
    server.AddNode(booleanNode);
    server.AddNode(colorNode);

    _ = Task.Run(() => server.StartAsync());


    _ = Task.Run(async () =>
    {
        float t = 0f;
        while (true)
        {
            await Task.Delay(50);

            t += 0.05f;

            float val = MathF.Sin(t);
            server.SetNodeValue("/test/float", val);
        }
    });

    RenderMainWindow();
}

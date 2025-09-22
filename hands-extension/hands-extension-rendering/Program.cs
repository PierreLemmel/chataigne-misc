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
    OSCQueryNode intNode = OSCQueryNodes.CreateIntNode("/test/int", "Integer Node", 14,
        onValueChanged: (newValue) => Console.WriteLine($"Integer node changed to: {newValue}"),
        min: -666,
        max: 123823
    );

    OSCQueryNode floatNode = OSCQueryNodes.CreateFloatNode("/test/float", "Float Node", 3.14f,
        onValueChanged: (newValue) => Console.WriteLine($"Float node changed to: {newValue}"),
        min: -1f,
        max: 1f
    );

    OSCQueryNode stringNode = OSCQueryNodes.CreateStringNode("/test/string", "String Node", "Hello, OSCQuery!",
        onValueChanged: (newValue) => Console.WriteLine($"String node changed to: {newValue}"),
        enumValues: [
            "Hello, OSCQuery!",
            "Option 2",
            "Option 3"
        ]
    );



    OSCQueryNode booleanNode = OSCQueryNodes.CreateBooleanNode("/test/boolean", "Boolean Node", true, 
        onValueChanged: (newValue) => Console.WriteLine($"Boolean node changed to: {newValue}")
    );

    OSCQueryNode colorNode = OSCQueryNodes.CreateColorNode("/test2/color", "Color Node", new RGBA(0xff, 0x88, 0x00, 0xff),
        onValueChanged: (newValue) => Console.WriteLine($"Color node changed to: {OSCQueryColor.RGBAToString(newValue)}")
    );

    server.AddNode(intNode);
    server.AddNode(floatNode);
    server.AddNode(stringNode);
    server.AddNode(booleanNode);
    server.AddNode(colorNode);

    _ = Task.Run(() => server.StartAsync());


    RenderMainWindow();
}

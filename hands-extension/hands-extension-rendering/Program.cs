using Plml.HandsExtensionRendering;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using Plml.OSCQuery;


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
    OSCQueryNode newNode = new()
    {
        FullPath = "/pipi/caca/prout",
        Type = OSCQueryTypes.Tags.Integer,
        Value = [
            8
        ],
        Description = "Prout prout"
    };

    OSCQueryNode fesses = new()
    {
        FullPath = "/pipi/caca/fesses",
        Type = OSCQueryTypes.Tags.String,
        Value = [
            "Juliette Baron elle dit que c'est pas une meuf à cul, mais c'est trop une meuf à cul"
        ],
        Description = "Oui oui miam le cul"
    };

    server.AddNode(newNode);
    server.AddNode(fesses);

    _ = Task.Run(() => server.StartAsync());

    RenderMainWindow();
}
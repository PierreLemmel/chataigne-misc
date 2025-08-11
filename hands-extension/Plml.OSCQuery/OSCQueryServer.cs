using Makaretu.Dns;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Vizcon.OSC;

namespace Plml.OSCQuery;

public class OSCQueryServer : IDisposable
{
    private readonly HttpListener _httpListener;
    private UDPListener? _udpListener;
    private readonly IServiceDiscovery _serviceDiscovery;

    private OSCQueryNode rootNode = new()
    {
        Description = "Root",
        FullPath = "/",
        Type = OSCQueryTypes.Container,
        Contents = [],
        Access = OSCQueryAccess.Read
    };

    private readonly string _serviceName;
    private readonly int _httpPort;
    private readonly int _oscPort;

    public OSCQueryServer(string serviceName, int httpPort, int oscPort)
    {
        _httpListener = new();

        string url = $"http://localhost:{httpPort}/";
        _httpListener.Prefixes.Add(url);

        _serviceName = serviceName;
        _httpPort = httpPort;
        _oscPort = oscPort;

        _serviceDiscovery = new ServiceDiscovery();
    }

    private void HandleOscPacket(OscPacket packet)
    {
        switch (packet)
        {
            case OscMessage msg:
                HandleOscMessage(msg);
                break;
            case OscBundle bundle:
                bundle.Messages.ForEach(HandleOscMessage);
                break;
            default:
                throw new InvalidOperationException($"Unexepected OSC packet type: ${packet.GetType().Name}");
        }
        
    }

    private void HandleOscMessage(OscMessage message)
    {
        Console.WriteLine(message.Address);
        message.Arguments.ForEach(arg =>  Console.WriteLine($"{arg.GetType().Name}: {arg}"));
    }

    public void AddNode(OSCQueryNode node)
    {
        string path = node.FullPath;


        var segments = path.Split('/')
            .Where(seg => !string.IsNullOrEmpty(seg))
            .ToList();
        var nodeName = segments.Last();

        var currentNode = rootNode;
        string fullPath = "/";

        for (int i = 0; i < segments.Count - 1; i++)
        {
            var segment = segments[i];
            fullPath += segment + "/";
         
            currentNode.Contents ??= [];

            if (currentNode.Contents.TryGetValue(segment, out var nextNode))
            {
                currentNode = nextNode;
            }
            else
            {
                OSCQueryNode newNode = new()
                {
                    FullPath = fullPath,
                    Type = OSCQueryTypes.Container,
                    Access = OSCQueryAccess.Read,
                    Contents = [],
                };
                currentNode.Contents[segment] = newNode;
                currentNode = newNode;
            }
        }

        
        currentNode.Contents ??= [];
        currentNode.Contents[nodeName] = node;
    }

    public async Task StartAsync()
    {
        ServiceProfile oscService = new(_serviceName, "_osc._udp", (ushort)_oscPort);
        ServiceProfile oscQueryService = new(_serviceName, "_oscjson._tcp", (ushort)_httpPort);

        if (!_serviceDiscovery.Probe(oscService))
            _serviceDiscovery.Advertise(oscService);
        else
            throw new InvalidOperationException($"OSC Service ${_serviceName} already running on port {_oscPort}");

        if (!_serviceDiscovery.Probe(oscQueryService))
            throw new InvalidOperationException($"OSCQuery Service ${_serviceName} already running on port {_httpPort}");

        _httpListener.Start();
        _udpListener = new(_oscPort, HandleOscPacket);

        while (_httpListener.IsListening)
        {
            try
            {
                var ctx = await _httpListener.GetContextAsync();
                _ = Task.Run(() => HandleRequestAsync(ctx));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }

    public async Task HandleRequestAsync(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;

        var method = request.HttpMethod;

        try
        {       
            var url = request.Url ?? throw new ArgumentNullException("url");


            var query = url.Query.TrimStart('?');
            var queryParams = !string.IsNullOrEmpty(query) ? query.Split("&")
                .Select(chunk =>
                {
                    var split = chunk.Split('=');
                    var key = split[0];
                    var val = split[1];
                    return new
                    {
                        Key = key,
                        Value = val
                    };
                })
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value,
                    StringComparer.InvariantCultureIgnoreCase
                )
                : [];


            if (queryParams is not null && queryParams.TryGetValue("name", out var nameVal))
            {
                if (nameVal.ToLowerInvariant() == "host_info")
                {
                    var hostInfo = GetHostInfo();
                    await JsonResponse(hostInfo);

                    return;
                }
            }

            var path = url.LocalPath;

            var node = GetNode(path);

            if (node != null)
            {
                await JsonResponse(node);
                return;
            }


            await JsonResponse(new { });
            return;
        }
        catch
        {
            response.StatusCode = 500;
            response.StatusDescription = "Internal Server Error";
        }
        


        async Task JsonResponse<T>(T value)
        {
            JsonSerializerOptions options = new()
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
            var json = JsonSerializer.Serialize(value, options);

            byte[] buffer = Encoding.UTF8.GetBytes(json);
            response.ContentType = "application/json";
            response.ContentLength64 = buffer.Length;
            response.StatusCode = 200;

            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
        }


    }

    private OSCQueryHostInfo GetHostInfo()
    {
        OSCQueryHostInfo info = new()
        {
            Name = _serviceName,
            Extensions = new()
            {
                { "ACCESS", true },
                { "CLIPMODE", false },
                { "CRITICAL", false },
                { "RANGE", true },
                { "TAGS", false },
                { "TYPE", true },
                { "UNIT", false },
                { "VALUE", true },
                { "LISTEN", false },
                { "PATH_ADDED", false },
                { "PATH_REMOVED", false },
                { "PATH_RENAMED", false },
                { "PATH_CHANGED", false },
            },
            OSCPort = _oscPort,
            OSCTransport = "UDP",
            WSPort = _httpPort,
            Metadata = new()
            {
                { "VERSION", "1.0.0" }
            }
        };

        return info;
    }

    private OSCQueryNode? GetNode(string path)
    {
        var pathSegments = path.Split("/")
            .Where(seg => !string.IsNullOrEmpty(seg))
            .ToList();

        var currentNode = rootNode;

        foreach (string segment in pathSegments)
        {
            if (currentNode?.Contents is not null && currentNode.Contents.TryGetValue(segment, out var nextNode))
            {
                currentNode = nextNode;
            }
            else
                return null;
        }

        return currentNode;
    }

    public void Dispose()
    {
        _serviceDiscovery.Unadvertise();
        _serviceDiscovery.Dispose();

        if (_udpListener != null)
        {
            _udpListener.Close();
            _udpListener.Dispose();
        }

        if (_httpListener.IsListening)
        {
            _httpListener.Stop();
            _httpListener.Close();
        }
    }
}
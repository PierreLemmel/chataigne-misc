using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Makaretu.Dns;
using Vizcon.OSC;

namespace Plml.OSCQuery;

public class OSCQueryServer : IDisposable
{
    private readonly HttpListener _httpListener;
    private UDPListener? _udpListener;
    private readonly IServiceDiscovery _serviceDiscovery;


    private OSCQueryNode rootNode = new()
    {
        Name = "Root",
        FullPath = "/",
        Type = OSCQueryTypes.Container,
        Contents = [],
        Access = OSCQueryAccess.Read
    };

    private readonly string _serviceName;
    private readonly int _httpPort;
    private readonly int _oscPort;

    public OSCQueryServer(string serviceName, int port) : this(serviceName, port, port)
    {
    }

    public OSCQueryServer(string serviceName, int httpPort, int oscPort)
    {
        _httpListener = new();

        string url = $"http://127.0.0.1:{httpPort}/";
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
        string address = message.Address.ToLowerInvariant();

        var node = GetNode(address);

        if (node is null)
        {
            Console.WriteLine($"Unhandled OSC message on adress: '{address}'");
            return;
        }


        if ((node.Access & OSCQueryAccess.Write) == 0)
        {
            Console.WriteLine($"Attempt to write read-only node '{node.FullPath}'");
            return;
        }

        SetNodeValue(address, message.Arguments.ToArray());
    }

    private async Task SendWSMessage(string type, string data)
    {
        if (_activeWebSocket is null || _activeWebSocket.State != WebSocketState.Open)
            return;

        var msg = new OSCQueryWSMessage
        {
            Command = type,
            Data = data
        };

        JsonSerializerOptions options = new()
        {
            WriteIndented = false,
        };
        var json = JsonSerializer.Serialize(msg, options);

        var responseBuffer = Encoding.UTF8.GetBytes(json);
        await _activeWebSocket.SendAsync(new ArraySegment<byte>(responseBuffer), WebSocketMessageType.Text, true, CancellationToken.None);
    }

    private void NodeAddedMessage(string path) => Task.Run(() => SendWSMessage("PATH_ADDED", path));

    public void SetNodeValue(string address, params object[] args)
    {
        var node = GetNode(address);

        if (node is null)
        {
            Console.WriteLine($"Impossible to set value for address '{address}'");
            return;
        }

        switch (node.Type)
        {
            case OSCQueryTypes.Tags.Integer:
                if (CheckArgLength(1))
                {
                    int? val = args[0] switch
                    {
                        int i => i,
                        float f => (int)MathF.Round(f),
                        string s when int.TryParse(s, out var parsed) => parsed,
                        bool b => b ? 1 : 0,
                        _ => null
                    };

                    if (val.HasValue)
                    {
                        float? minVal = node.Range?.FirstOrDefault()?.Min;
                        float? maxVal = node.Range?.FirstOrDefault()?.Max;
                        int min = minVal is not null ? (int)MathF.Round(minVal.Value) : int.MinValue;
                        int max = maxVal is not null ? (int)MathF.Round(maxVal.Value) : int.MaxValue;

                        float clamped = MoreMaths.Clamp(val.Value, min, max);

                        node.Value = [clamped];
                        NotifyValueChanged(clamped);
                    }
                    else
                    {
                        Console.WriteLine($"Expected integer argument for node '{node.FullPath}', got {args[0].GetType().Name}");
                    }
                }
                break;

            case OSCQueryTypes.Tags.Float:
                if (CheckArgLength(1))
                {
                    float? val = args[0] switch
                    {
                        int i => i,
                        float f => f,
                        string s when float.TryParse(s, out var parsed) => parsed,
                        bool b => b ? 1.0f : 0.0f,
                        _ => null
                    };

                    if (val.HasValue)
                    {
                        node.Value = [val.Value];
                        NotifyValueChanged(val.Value);
                    }
                    else
                        Console.WriteLine($"Expected float argument for node '{node.FullPath}', got {args[0].GetType().Name}");
                }

                break;

            case OSCQueryTypes.Tags.String:
                if (CheckArgLength(1))
                {
                    string value = args[0] is string s ? s : args[0].ToString() ?? "";

                    var enumValues = node.Range?.FirstOrDefault()?.Values;
                    bool notInOptions = (enumValues is not null && !enumValues.Contains(value, StringComparer.InvariantCultureIgnoreCase));

                    if (!notInOptions)
                    {
                        node.Value = [value];
                        NotifyValueChanged(value);
                    }
                    else
                        Console.WriteLine($"String argument for node '{node.FullPath}' not in enum options, got '{value}'");
                }

                break;

            case OSCQueryTypes.Tags.Boolean:
                if (CheckArgLength(1))
                {
                    bool? val = args[0] switch
                    {
                        bool b => b,
                        int i => i != 0,
                        float f => f != 0.0f,
                        string s when bool.TryParse(s, out var parsed) => parsed,
                        _ => null
                    };

                    if (val.HasValue)
                    {
                        node.Value = [val.Value];
                        NotifyValueChanged(val.Value);
                    }
                    else
                        Console.WriteLine($"Expected boolean argument for node '{node.FullPath}', got {args[0].GetType().Name}");
                }

                break;

            case OSCQueryTypes.Tags.Color:
                if (CheckArgLength(1))
                {
                    if (args[0] is RGBA rgba)
                    {
                        var newVal = OSCQueryColor.RGBAToString(rgba);
                        node.Value = [
                            newVal
                        ];

                        NotifyValueChanged(rgba);
                    }
                    else
                        Console.WriteLine($"Unexpected color format");

                }
                break;

            default:
                Console.WriteLine($"Unhandled OSCQuery node type: '{node.Type}'");
                break;
        }

        bool CheckArgLength(int expected)
        {
            if (args.Length < expected)
            {
                Console.WriteLine($"Expected {expected} argument(s) for node '{node.FullPath}', got {args.Length}");
                return false;
            }

            return true;
        }

        void NotifyValueChanged<T>(T val)
        {
            object[] valArray = [val];
            node.OnValueChanged?.Invoke(valArray);


            if (!node.Listen || _activeWebSocket is null || _activeWebSocket.State != WebSocketState.Open)
                return;

            OscMessage oscMsg = new(node.FullPath, valArray);
            oscMsg.GetBytes();

            _activeWebSocket?.SendAsync(new ArraySegment<byte>(oscMsg.GetBytes()), WebSocketMessageType.Binary, true, CancellationToken.None);
        }
    }

    public void AddNode(OSCQueryNode node)
    {
        node.FullPath = node.FullPath.ToLowerInvariant();

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
                    Name = segment,
                    FullPath = fullPath,
                    Type = OSCQueryTypes.Container,
                    Access = OSCQueryAccess.Read,
                    Contents = [],
                };
                currentNode.Contents[segment] = newNode;

                NodeAddedMessage(fullPath);

                currentNode = newNode;
            }
        }

        
        currentNode.Contents ??= [];
        currentNode.Contents[nodeName] = node;

        NodeAddedMessage(node.FullPath);
    }

    public async Task StartAsync()
    {
        ServiceProfile oscService = new(_serviceName, "_osc._udp", (ushort)_oscPort);
        ServiceProfile oscQueryService = new(_serviceName, "_oscjson._tcp", (ushort)_httpPort);

        Console.WriteLine($"Starting server:");
        Console.WriteLine($" - OSC: {_serviceName} on port {_oscPort}");
        Console.WriteLine($" - OSCQuery: {_serviceName} on port {_httpPort}");

        if (!_serviceDiscovery.Probe(oscService))
        {
            _serviceDiscovery.Advertise(oscService);
            _serviceDiscovery.Announce(oscService);
        }
        else
            Console.Error.WriteLine($"OSC Service '{_serviceName}' already running on port {_oscPort}");

        if (!_serviceDiscovery.Probe(oscQueryService))
        {
            _serviceDiscovery.Advertise(oscQueryService);
            _serviceDiscovery.Announce(oscQueryService);
        }
        else
            Console.Error.WriteLine($"OSCQuery Service '{_serviceName}' already running on port {_httpPort}");

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
        if (context.Request.IsWebSocketRequest)
        {
            await HandleWebsocketRequestAsync(context);
        }
        else
        {
            await HandleHttpRequestAsync(context);
        }
    }

    private async Task HandleHttpRequestAsync(HttpListenerContext context)
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
                    var val = split.Length > 1 ? split[1] : "";
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

            bool isHostInfoRequest = queryParams is not null && (
                queryParams.ContainsKey("host_info") ||
                (queryParams.TryGetValue("name", out var name) && name.ToLowerInvariant() == "host_info")
            );

            if (isHostInfoRequest)
            {
                var hostInfo = GetHostInfo();
                await JsonResponse(hostInfo);

                return;
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
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.ToString());
            await ErrorResponse(500, "Internal Server Error");
            return;
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
            response.Close();
        }

        async Task ErrorResponse(int statusCode, string statusDescription)
        {
            response.StatusCode = statusCode;
            response.StatusDescription = statusDescription;
            byte[] buffer = Encoding.UTF8.GetBytes(statusDescription);
            response.ContentType = "text/plain";
            response.ContentLength64 = buffer.Length;

            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
        }
    }

    private async Task HandleWebsocketRequestAsync(HttpListenerContext context)
    {
        HttpListenerWebSocketContext? wsCtx = null;
        try
        {
            wsCtx = await context.AcceptWebSocketAsync(null);

            var webSocket = wsCtx.WebSocket;

            if (webSocket is not null)
            {
                Console.WriteLine("WebSocket connection established.");
                await HandleWebsocketConnection(webSocket);
                Console.WriteLine("WebSocket connection terminated.");
            }
            else
                Console.Error.WriteLine("Can't open Web Socket");
        }
        catch (HttpListenerException)
        {
            Console.Error.WriteLine("HttpListener context operation cancelled or failed.");
        }
        catch (WebSocketException ex)
        {
            Console.Error.WriteLine($"WebSocket error: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error accepting WebSocket: {ex.Message}");
            if (wsCtx?.WebSocket != null &&
                wsCtx.WebSocket.State == WebSocketState.Open)
            {
                await wsCtx.WebSocket.CloseAsync(
                    WebSocketCloseStatus.InternalServerError,
                    "Error during connection",
                    CancellationToken.None
                );
            }
        }
        finally
        {
            context.Response.Close();
        }
    }

    private WebSocket? _activeWebSocket = null;
    private async Task HandleWebsocketConnection(WebSocket webSocket)
    {
        var buffer = new byte[1024 * 4];
        _activeWebSocket = webSocket;

        try
        {
            while (webSocket.State == WebSocketState.Open)
            {
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                }
                else if (result.MessageType == WebSocketMessageType.Text)
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);

                    OnWSMessage(message);
                }
            }
        }
        catch (WebSocketException ex)
        {
            Console.WriteLine($"WebSocket error: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"General error: {ex.Message}");
        }
        finally
        {
            if (webSocket.State != WebSocketState.Closed)
            {
                await webSocket.CloseOutputAsync(
                    WebSocketCloseStatus.NormalClosure,
                    "Closing",
                    CancellationToken.None
                );
            }
            _activeWebSocket = null;
            webSocket.Dispose();
            Console.WriteLine("WebSocket connection closed.");
        }
    }

    private void OnWSMessage(string message)
    {
        var wsMessage = JsonSerializer.Deserialize<OSCQueryWSMessage>(message);

        if (wsMessage is null)
            return;

        var path = wsMessage.Data.ToLowerInvariant();

        var node = GetNode(path);

        if (node is null)
        {
            Console.WriteLine($"Unhandled WS message on path: '{path}'");
            return;
        }

        switch (wsMessage.Command.ToUpperInvariant())
        {
            case "LISTEN":
                node.Listen = true;
                break;

            case "IGNORE":
                node.Listen = false;
                break;
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
                { "LISTEN", true },
                { "PATH_ADDED", true },
                { "PATH_REMOVED", true },
                { "PATH_RENAMED", true },
                { "PATH_CHANGED", false },
            },
            OSCPort = _oscPort,
            OSCTransport = "UDP",
            Metadata = new()
            {
                { "VERSION", "1.0.0" }
            }
        };

        return info;
    }

    private OSCQueryNode? GetNode(string path)
    {
        var pathSegments = path
            .ToLowerInvariant()
            .Split("/")
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
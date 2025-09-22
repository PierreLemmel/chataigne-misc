import { createServer, IncomingMessage, ServerResponse } from 'http';
import { readFileSync, existsSync } from 'fs';
import { join } from 'path';

const port = 3000;

// Sample data to serve
const sampleData = {
  users: [
    { id: 1, name: 'Alice', email: 'alice@example.com' },
    { id: 2, name: 'Bob', email: 'bob@example.com' },
    { id: 3, name: 'Charlie', email: 'charlie@example.com' }
  ],
  products: [
    { id: 1, name: 'Laptop', price: 999.99, category: 'Electronics' },
    { id: 2, name: 'Mouse', price: 29.99, category: 'Electronics' },
    { id: 3, name: 'Keyboard', price: 79.99, category: 'Electronics' }
  ],
  settings: {
    theme: 'dark',
    language: 'en',
    notifications: true
  }
};

// MIME types for different file extensions
const mimeTypes: Record<string, string> = {
  '.html': 'text/html',
  '.css': 'text/css',
  '.js': 'application/javascript',
  '.ts': 'application/typescript',
  '.json': 'application/json',
  '.png': 'image/png',
  '.jpg': 'image/jpeg',
  '.jpeg': 'image/jpeg',
  '.gif': 'image/gif',
  '.svg': 'image/svg+xml',
  '.ico': 'image/x-icon'
};

// Helper function to get MIME type
function getMimeType(filePath: string): string {
  const ext = filePath.substring(filePath.lastIndexOf('.')).toLowerCase();
  return mimeTypes[ext] || 'application/octet-stream';
}

// Helper function to send JSON response
function sendJsonResponse(res: ServerResponse, data: any, statusCode: number = 200): void {
  res.writeHead(statusCode, { 'Content-Type': 'application/json' });
  res.end(JSON.stringify(data, null, 2));
}

// Helper function to send error response
function sendErrorResponse(res: ServerResponse, message: string, statusCode: number = 404): void {
  sendJsonResponse(res, { error: message }, statusCode);
}

// Helper function to serve static files
function serveStaticFile(res: ServerResponse, filePath: string): void {
  try {
    if (!existsSync(filePath)) {
      sendErrorResponse(res, 'File not found', 404);
      return;
    }

    const content = readFileSync(filePath);
    const mimeType = getMimeType(filePath);
    
    res.writeHead(200, { 'Content-Type': mimeType });
    res.end(content);
  } catch (error) {
    console.error('Error serving file:', error);
    sendErrorResponse(res, 'Internal server error', 500);
  }
}

// Request handler
function handleRequest(req: IncomingMessage, res: ServerResponse): void {
  const url = req.url || '/';
  const method = req.method || 'GET';

  console.log(`${method} ${url}`);

  // Set CORS headers
  res.setHeader('Access-Control-Allow-Origin', '*');
  res.setHeader('Access-Control-Allow-Methods', 'GET, POST, PUT, DELETE, OPTIONS');
  res.setHeader('Access-Control-Allow-Headers', 'Content-Type, Authorization');

  // Handle preflight OPTIONS request
  if (method === 'OPTIONS') {
    res.writeHead(200);
    res.end();
    return;
  }

  // API endpoints
  if (url.startsWith('/api/')) {
    handleApiRequest(req, res, url);
    return;
  }

  // Serve static files
  if (method === 'GET') {
    serveStaticFiles(req, res, url);
    return;
  }

  // Default response
  sendErrorResponse(res, 'Not found', 404);
}

// Handle API requests
function handleApiRequest(req: IncomingMessage, res: ServerResponse, url: string): void {
  const path = url.substring(5); // Remove '/api/' prefix

  switch (path) {
    case 'users':
      sendJsonResponse(res, sampleData.users);
      break;
    
    case 'products':
      sendJsonResponse(res, sampleData.products);
      break;
    
    case 'settings':
      sendJsonResponse(res, sampleData.settings);
      break;
    
    case 'data':
      sendJsonResponse(res, sampleData);
      break;
    
    case 'health':
      sendJsonResponse(res, { 
        status: 'ok', 
        timestamp: new Date().toISOString(),
        uptime: process.uptime()
      });
      break;
    
    case 'stats':
      sendJsonResponse(res, {
        totalUsers: sampleData.users.length,
        totalProducts: sampleData.products.length,
        serverTime: new Date().toISOString()
      });
      break;
    
    default:
      sendErrorResponse(res, 'API endpoint not found', 404);
  }
}

// Serve static files
function serveStaticFiles(req: IncomingMessage, res: ServerResponse, url: string): void {
  let filePath = url === '/' ? '/index.html' : url;
  
  // Map common routes to files
  if (filePath === '/index.html') {
    filePath = '/src/index.html';
  }
  
  // Try to serve from different possible locations
  const possiblePaths = [
    join(process.cwd(), filePath),
    join(process.cwd(), 'public', filePath),
    join(process.cwd(), 'src', filePath)
  ];

  for (const path of possiblePaths) {
    if (existsSync(path)) {
      serveStaticFile(res, path);
      return;
    }
  }

  // If no file found, serve a simple HTML page
  if (filePath === '/index.html' || filePath === '/') {
    const html = `
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>HTTP Server</title>
    <style>
        body { font-family: Arial, sans-serif; margin: 40px; }
        .endpoint { background: #f5f5f5; padding: 10px; margin: 10px 0; border-radius: 5px; }
        .method { color: #007acc; font-weight: bold; }
        .url { color: #333; font-family: monospace; }
    </style>
</head>
<body>
    <h1>HTTP Server</h1>
    <p>This server provides both HTTP API endpoints and WebSocket functionality.</p>
    
    <h2>Available API Endpoints:</h2>
    <div class="endpoint">
        <span class="method">GET</span> <span class="url">/api/users</span> - Get all users
    </div>
    <div class="endpoint">
        <span class="method">GET</span> <span class="url">/api/products</span> - Get all products
    </div>
    <div class="endpoint">
        <span class="method">GET</span> <span class="url">/api/settings</span> - Get settings
    </div>
    <div class="endpoint">
        <span class="method">GET</span> <span class="url">/api/data</span> - Get all data
    </div>
    <div class="endpoint">
        <span class="method">GET</span> <span class="url">/api/health</span> - Server health check
    </div>
    <div class="endpoint">
        <span class="method">GET</span> <span class="url">/api/stats</span> - Server statistics
    </div>
    
    <h2>WebSocket Server:</h2>
    <p>WebSocket server is running on port 8080</p>
    
    <h2>Test API:</h2>
    <button onclick="testApi()">Test API Endpoints</button>
    <div id="results"></div>
    
    <script>
        async function testApi() {
            const results = document.getElementById('results');
            results.innerHTML = '<p>Testing API endpoints...</p>';
            
            const endpoints = ['/api/users', '/api/products', '/api/settings', '/api/health'];
            let html = '<h3>API Test Results:</h3>';
            
            for (const endpoint of endpoints) {
                try {
                    const response = await fetch(endpoint);
                    const data = await response.json();
                    html += \`<div class="endpoint"><strong>\${endpoint}</strong>: <pre>\${JSON.stringify(data, null, 2)}</pre></div>\`;
                } catch (error) {
                    html += \`<div class="endpoint"><strong>\${endpoint}</strong>: <span style="color: red;">Error: \${error.message}</span></div>\`;
                }
            }
            
            results.innerHTML = html;
        }
    </script>
</body>
</html>`;
    
    res.writeHead(200, { 'Content-Type': 'text/html' });
    res.end(html);
    return;
  }

  sendErrorResponse(res, 'File not found', 404);
}

// Create and start the server
const server = createServer(handleRequest);

server.listen(port, () => {
  console.log(`HTTP server running on http://localhost:${port}`);
  console.log('Available endpoints:');
  console.log('  GET  /api/users     - Get all users');
  console.log('  GET  /api/products  - Get all products');
  console.log('  GET  /api/settings  - Get settings');
  console.log('  GET  /api/data      - Get all data');
  console.log('  GET  /api/health    - Server health check');
  console.log('  GET  /api/stats     - Server statistics');
  console.log('  GET  /              - Home page with API documentation');
});

// Handle server errors
server.on('error', (error: Error) => {
  console.error('HTTP server error:', error);
});

// Graceful shutdown
process.on('SIGINT', () => {
  console.log('\nShutting down HTTP server...');
  server.close(() => {
    console.log('HTTP server closed');
    process.exit(0);
  });
});

export default server;

import { createReadStream, existsSync, statSync } from 'node:fs';
import { createServer, request } from 'node:http';
import path from 'node:path';

const frontendRoot = process.cwd();
const browserRoot = path.join(frontendRoot, 'dist', 'campusconnect-frontend', 'browser');
const port = Number.parseInt(process.env.E2E_FRONTEND_PORT ?? '4300', 10);
const apiTarget = new URL(process.env.E2E_API_BASE_URL ?? 'http://localhost:5136');

const contentTypes = new Map([
  ['.css', 'text/css; charset=utf-8'],
  ['.html', 'text/html; charset=utf-8'],
  ['.ico', 'image/x-icon'],
  ['.js', 'text/javascript; charset=utf-8'],
  ['.json', 'application/json; charset=utf-8'],
  ['.map', 'application/json; charset=utf-8'],
  ['.svg', 'image/svg+xml; charset=utf-8'],
  ['.txt', 'text/plain; charset=utf-8'],
]);

if (!existsSync(path.join(browserRoot, 'index.html'))) {
  console.error('Build output is missing. Run npm run build before starting the E2E static server.');
  process.exit(1);
}

createServer((incoming, response) => {
  const requestUrl = new URL(incoming.url ?? '/', `http://localhost:${port}`);
  if (requestUrl.pathname.startsWith('/api')) {
    proxyApiRequest(incoming, response, requestUrl);
    return;
  }

  serveStaticFile(response, requestUrl.pathname);
}).listen(port, () => {
  console.log(`CampusConnect E2E frontend listening on http://localhost:${port}`);
});

function proxyApiRequest(incoming, response, requestUrl) {
  const proxyRequest = request(
    {
      hostname: apiTarget.hostname,
      method: incoming.method,
      path: `${requestUrl.pathname}${requestUrl.search}`,
      port: apiTarget.port,
      protocol: apiTarget.protocol,
      headers: {
        ...incoming.headers,
        host: apiTarget.host,
      },
    },
    proxyResponse => {
      response.writeHead(proxyResponse.statusCode ?? 500, proxyResponse.headers);
      proxyResponse.pipe(response);
    }
  );

  proxyRequest.on('error', () => {
    response.writeHead(502, { 'content-type': 'text/plain; charset=utf-8' });
    response.end('API proxy failed.');
  });

  incoming.pipe(proxyRequest);
}

function serveStaticFile(response, pathname) {
  const safePath = decodeURIComponent(pathname).replace(/^\/+/, '');
  const requestedFile = safePath ? path.join(browserRoot, safePath) : path.join(browserRoot, 'index.html');
  const filePath = resolveSafeFile(requestedFile) ?? path.join(browserRoot, 'index.html');
  const extension = path.extname(filePath);

  response.writeHead(200, {
    'content-type': contentTypes.get(extension) ?? 'application/octet-stream',
  });
  createReadStream(filePath).pipe(response);
}

function resolveSafeFile(filePath) {
  const resolved = path.resolve(filePath);
  const relativePath = path.relative(browserRoot, resolved);
  if (relativePath.startsWith('..') || path.isAbsolute(relativePath)) {
    return null;
  }

  if (!existsSync(resolved)) {
    return null;
  }

  return statSync(resolved).isFile() ? resolved : null;
}

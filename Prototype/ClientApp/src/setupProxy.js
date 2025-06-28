const { createProxyMiddleware } = require('http-proxy-middleware');

module.exports = function(app) {
  // Determine target based on environment
  // Use environment variables for Docker, fallback to localhost for local development
  const target = process.env.REACT_APP_API_URL || 'http://localhost:8080';
  const wsTarget = process.env.REACT_APP_WS_URL || 'ws://localhost:8080';

  // API proxy
  app.use(
    '/api',
    createProxyMiddleware({
      target: target,
      changeOrigin: true,
    })
  );

  // Login and auth endpoints
  app.use(
    ['/login', '/logout', '/settings', '/register', '/verify', '/forgot', '/reset'],
    createProxyMiddleware({
      target: target,
      changeOrigin: true,
    })
  );

  // Navigation endpoints
  app.use(
    '/navigation',
    createProxyMiddleware({
      target: target,
      changeOrigin: true,
    })
  );

  // SignalR WebSocket proxy
  app.use(
    '/progressHub',
    createProxyMiddleware({
      target: target,
      ws: true,
      changeOrigin: true,
    })
  );

  // Generic WebSocket proxy (for development server)
  app.use(
    '/ws',
    createProxyMiddleware({
      target: wsTarget,
      ws: true,
      changeOrigin: true,
    })
  );
};
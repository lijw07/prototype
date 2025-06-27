const { createProxyMiddleware } = require('http-proxy-middleware');

module.exports = function(app) {
  // Determine target based on environment - use same logic as API service
  const target = process.env.NODE_ENV === 'development' 
    ? 'http://localhost:8080'  // Force localhost in development
    : (process.env.REACT_APP_API_URL || 'http://localhost:8080');
  const wsTarget = process.env.NODE_ENV === 'development'
    ? 'ws://localhost:8080'   // Force localhost in development
    : (process.env.REACT_APP_WS_URL || 'ws://localhost:8080');

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
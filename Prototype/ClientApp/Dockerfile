# === Base Setup ===
FROM node:20-alpine AS base
WORKDIR /app
EXPOSE 3000

# === Install Dependencies ===
FROM base AS deps
# Copy package files and npm config
COPY package*.json .npmrc ./
# Use npm ci for faster, reliable, reproducible builds
RUN npm ci --only=production && \
    npm cache clean --force

# === Development Dependencies ===
FROM base AS dev-deps
COPY package*.json .npmrc ./
RUN npm ci

# === Dev Stage (for hot reload) ===
FROM dev-deps AS dev
COPY . .
ENV CHOKIDAR_USEPOLLING=true
ENV BROWSER=none
ENV NODE_ENV=development
CMD ["npm", "start"]

# === Build Stage ===
FROM dev-deps AS build
COPY . .
ENV NODE_ENV=production
RUN npm run build

# === Production: Serve with NGINX ===
FROM nginx:1.25-alpine AS prod

# Install dumb-init for proper signal handling
RUN apk add --no-cache dumb-init

# Create nginx user and group
RUN addgroup -g 1001 -S nginx-app && \
    adduser -S nginx-app -u 1001

# Remove default NGINX site config
RUN rm /etc/nginx/conf.d/default.conf

# Copy custom NGINX config
COPY nginx/default.conf /etc/nginx/conf.d/default.conf

# Copy React build output to NGINX's public folder
COPY --from=build /app/build /usr/share/nginx/html

# Set proper permissions
RUN chown -R nginx-app:nginx-app /usr/share/nginx/html && \
    chmod -R 755 /usr/share/nginx/html

# Create nginx cache and log directories
RUN mkdir -p /var/cache/nginx/client_temp && \
    chown -R nginx-app:nginx-app /var/cache/nginx && \
    touch /var/log/nginx/access.log /var/log/nginx/error.log && \
    chown nginx-app:nginx-app /var/log/nginx/*.log

EXPOSE 80

# Use dumb-init and run as non-root user
USER nginx-app
ENTRYPOINT ["dumb-init", "--"]
CMD ["nginx", "-g", "daemon off;"]
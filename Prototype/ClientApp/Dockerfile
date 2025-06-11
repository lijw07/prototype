# === Base Setup ===
FROM node:20 AS base
WORKDIR /app
EXPOSE 3000

# === Install Dependencies ===
FROM base AS deps
COPY package*.json ./
RUN npm install

# === Dev Stage (for hot reload) ===
FROM deps AS dev
COPY . .
ENV CHOKIDAR_USE_POLLING=true
ENV BROWSER=none
CMD ["npm", "start"]

# === Build Stage ===
FROM deps AS build
COPY . .
RUN npm run build

# === Production: Serve with NGINX ===
FROM nginx:alpine AS prod

# Remove default NGINX site config
RUN rm /etc/nginx/conf.d/default.conf

# Copy custom NGINX config into container
COPY ../../nginx/default.conf /etc/nginx/conf.d/default.conf

# Copy React build output to NGINX's public folder
COPY --from=build /app/build /usr/share/nginx/html

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
# Base Runetime Image
FROM node:20 AS base
WORKDIR /app
EXPOSE 3000

# Install dependencies only
FROM base AS deps
COPY package*.json ./
RUN npm install

# Development image (hot reload, file watching)
FROM deps AS dev
COPY . .
ENV CHOKIDAR_USE_POLLING=true
ENV BROWSER=none
CMD ["npm", "start"]

# Production build
FROM deps AS build
COPY . .
RUN npm run build

# Production serve image using a minimal Node.js web server (serve or nginx)
FROM node:20 AS prod
WORKDIR /app
COPY --from=build /app/build ./build
RUN npm install -g serve
EXPOSE 3000
CMD ["serve", "-s", "build", "-l", "3000"]
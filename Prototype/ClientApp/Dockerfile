FROM node:20 AS base
WORKDIR /app
EXPOSE 3000

# --- Dependencies ---
FROM base AS deps
COPY package*.json ./
RUN npm install

# --- Dev (hot reload) ---
FROM deps AS dev
COPY . .
ENV CHOKIDAR_USE_POLLING=true
ENV BROWSER=none
CMD ["npm", "start"]

# --- Build for Production ---
FROM deps AS build
COPY . .
RUN npm run build

# --- Serve Production ---
FROM node:20 AS prod
WORKDIR /app
COPY --from=build /app/build ./build
RUN npm install -g serve
EXPOSE 3000
CMD ["serve", "-s", "build", "-l", "3000"]

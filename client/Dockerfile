FROM node:22-alpine AS build
WORKDIR /app

COPY package*.json ./
RUN npm install

COPY . .
# Baked in at build time by Vite; must be reachable from the visitor's browser.
# Rong = goi API qua nginx cung origin (proxy /api, /hubs, /uploads trong nginx.conf).
# Chi dat URL tuyet doi khi web va API nam tren hai domain khac nhau.
ARG VITE_API_URL=""
ENV VITE_API_URL=$VITE_API_URL
# Optional raster tile template; empty = client/src/lib/mapTiles.ts default (CARTO).
ARG VITE_TILE_URL=""
ENV VITE_TILE_URL=$VITE_TILE_URL
RUN npm run build

FROM nginx:alpine AS runtime
COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80

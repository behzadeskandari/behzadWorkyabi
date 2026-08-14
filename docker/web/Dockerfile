FROM node:22-alpine AS build
WORKDIR /app

COPY frontend/iranjob-web/package*.json ./
RUN npm ci --registry=https://registry.npmjs.org

COPY frontend/iranjob-web/ ./
RUN npm run build

FROM nginx:1.27-alpine AS final
COPY docker/web/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist/iranjob-web/browser /usr/share/nginx/html
EXPOSE 80

FROM node:24-alpine AS build
WORKDIR /src
COPY ShortVideoTranslator.Frontend/package.json ShortVideoTranslator.Frontend/package-lock.json ./
RUN npm ci
COPY ShortVideoTranslator.Frontend/ ./
RUN npm run build

FROM nginx:stable-alpine
COPY docker/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /src/dist/ /usr/share/nginx/html/
EXPOSE 80
HEALTHCHECK --interval=20s --timeout=5s --start-period=10s --retries=3 CMD wget -q -O /dev/null http://127.0.0.1/api/health/ready || exit 1

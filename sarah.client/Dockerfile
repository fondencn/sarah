FROM node:20-alpine AS build
WORKDIR /app
COPY sarah.client/package.json sarah.client/package-lock.json ./
RUN npm ci
COPY sarah.client/ .
ARG PI_HOST=pi
ENV services__deviceservice__http_api__0=http://${PI_HOST}:5001 \
    services__personsservice__http_api__0=http://${PI_HOST}:5002 \
    services__geofencesservice__http_api__0=http://${PI_HOST}:5003 \
    services__roomservice__http_api__0=http://${PI_HOST}:5004 \
    services__monitoringservice__http_api__0=http://${PI_HOST}:5005 \
    services__rulesservice__http_api__0=http://${PI_HOST}:5006 \
    services__dashboardservice__http_api__0=http://${PI_HOST}:5007 \
    services__keycloak__http__0=http://${PI_HOST}:8080
RUN node replace-env-vars.js && npm run build

FROM nginx:alpine AS final
COPY sarah.client/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist/sarah.client/browser /usr/share/nginx/html
EXPOSE 80

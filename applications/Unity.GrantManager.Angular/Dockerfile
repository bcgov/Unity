FROM node:22-alpine AS build
WORKDIR /src

COPY package.json package-lock.json ./
RUN npm ci

COPY . .
RUN npx ng build --configuration production --base-href=/app/

FROM nginxinc/nginx-unprivileged:alpine AS final

COPY nginx.conf /etc/nginx/nginx.conf
COPY --from=build /src/dist/grant-manager-angular/browser /usr/share/nginx/html/app

EXPOSE 8080

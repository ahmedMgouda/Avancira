# FROM node:alpine
# WORKDIR /usr/src/app
# COPY . /usr/src/app
# RUN npm install -g @angular/cli
# RUN npm install
# CMD ["ng", "serve", "--host", "0.0.0.0", "--port", "80"]


# FROM node:alpine
# WORKDIR /usr/src/app
# COPY . /usr/src/app
# RUN npm install -g @angular/cli
# RUN npm install
# CMD ["ng", "serve", "--prod", "--host", "0.0.0.0", "--port", "80"]

# Stage 1: Build Angular App
FROM node:alpine AS build
WORKDIR /usr/src/app
COPY package*.json ./
RUN npm install
COPY . .
RUN npm run build -- --configuration production

# RUN npm install -g http-server
# WORKDIR /usr/src/app/dist/frontend.angular/browser
# EXPOSE 80
# CMD ["http-server", "-p", "80", "-c-1", "--fallback", "index.html"]

# Stage 2: Serve Angular App with nginx
FROM nginx:alpine AS production
COPY --from=build /usr/src/app/dist/frontend.angular/browser/ /usr/share/nginx/html/
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]

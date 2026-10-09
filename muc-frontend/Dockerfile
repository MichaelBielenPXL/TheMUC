FROM node:22-alpine AS build
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci
COPY . .
# The public address of the calendar feed, shown at the top of the page.
ARG VITE_CALENDAR_URL
ENV VITE_CALENDAR_URL=$VITE_CALENDAR_URL
RUN npm run build

FROM nginx:alpine
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html

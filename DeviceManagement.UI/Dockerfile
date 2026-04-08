FROM node:22-alpine AS build
WORKDIR /app

COPY package*.json ./
RUN npm ci

COPY . .
RUN npm run build

FROM node:22-alpine AS runtime
WORKDIR /app

COPY --from=build /app/dist/DeviceManagement.UI ./dist/DeviceManagement.UI

EXPOSE 4000
ENV PORT=4000

CMD ["node", "dist/DeviceManagement.UI/server/server.mjs"]

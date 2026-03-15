# ===========================================================================
# AuthScript Dashboard - Multi-stage Docker build
# Stage 1: Build React SPA with Vite
# Stage 2: Serve via nginx with API reverse proxy
# Build context: repository root
# ===========================================================================

# Build stage
FROM docker.io/node:20-alpine AS build
WORKDIR /app

# Copy all workspace files (node_modules excluded via .dockerignore)
COPY package.json package-lock.json ./
COPY shared/ shared/
COPY apps/dashboard/ apps/dashboard/

# Install dependencies and build
RUN npm ci
RUN npm run build --workspace=shared/types && npm run build --workspace=shared/validation
RUN npm run build --workspace=apps/dashboard

# Serve stage
FROM docker.io/nginx:alpine

# Copy built SPA assets
COPY --from=build /app/apps/dashboard/dist /usr/share/nginx/html

# Copy nginx config template (envsubst resolves $GATEWAY_URL at startup)
COPY apps/dashboard/nginx.conf /etc/nginx/templates/default.conf.template

EXPOSE 80

HEALTHCHECK --interval=30s --timeout=3s --start-period=5s \
    CMD wget -q --spider http://localhost/health || exit 1

# ===========================================================================
# AuthScript Dashboard - Multi-stage Docker Build
# React + Vite with nginx for production serving
# ===========================================================================

# ---------------------------------------------------------------------------
# Stage 1: Build
# ---------------------------------------------------------------------------
FROM node:22-alpine AS builder

WORKDIR /app

# Copy package files first for layer caching
COPY package.json package-lock.json* ./

# Install dependencies
RUN npm ci

# Copy source and build
COPY . ./
RUN npm run build

# ---------------------------------------------------------------------------
# Stage 2: Runtime with nginx
# ---------------------------------------------------------------------------
FROM nginx:1.27-alpine AS runtime

# Copy built assets
COPY --from=builder /app/dist /usr/share/nginx/html

# Copy nginx configuration
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Expose HTTP port
EXPOSE 80

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost/health || exit 1

# nginx runs as root by default but spawns workers as nginx user
CMD ["nginx", "-g", "daemon off;"]

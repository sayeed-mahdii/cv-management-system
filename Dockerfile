# Stage 1: Build the React frontend
FROM node:22-alpine AS web
WORKDIR /web

# Declare build args provided by Render / Docker build, expose to Vite build environment
ARG VITE_SUPABASE_URL
ARG VITE_SUPABASE_ANON_KEY
ENV VITE_SUPABASE_URL=$VITE_SUPABASE_URL
ENV VITE_SUPABASE_ANON_KEY=$VITE_SUPABASE_ANON_KEY

COPY frontend/package*.json ./
RUN npm ci
COPY frontend/ ./
RUN npm run build

# Stage 2: Build the Clean Architecture ASP.NET Core Web API
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS api
WORKDIR /src

# Restore using layer caching for all Clean Architecture projects
COPY backend/Recruitov.Domain/Recruitov.Domain.csproj backend/Recruitov.Domain/
COPY backend/Recruitov.Application/Recruitov.Application.csproj backend/Recruitov.Application/
COPY backend/Recruitov.Infrastructure/Recruitov.Infrastructure.csproj backend/Recruitov.Infrastructure/
COPY backend/Recruitov.Host/Recruitov.Host.csproj backend/Recruitov.Host/
RUN dotnet restore backend/Recruitov.Host/Recruitov.Host.csproj

COPY backend/ backend/
# Copy compiled React frontend into wwwroot of Recruitov.Host
COPY --from=web /web/dist ./backend/Recruitov.Host/wwwroot
RUN dotnet publish backend/Recruitov.Host/Recruitov.Host.csproj -c Release -o /out

# Stage 3: Run the production server
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=api /out .
ENTRYPOINT ["dotnet", "Recruitov.Host.dll"]

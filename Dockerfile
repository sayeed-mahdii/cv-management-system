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

# Stage 2: Build the ASP.NET Core Web API
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS api
WORKDIR /src
COPY backend/*.csproj ./
RUN dotnet restore
COPY backend/ ./
# Copy compiled React frontend into wwwroot of the Web API
COPY --from=web /web/dist ./wwwroot
RUN dotnet publish -c Release -o /out

# Stage 3: Run the production server
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=api /out .
ENTRYPOINT ["dotnet", "backend.dll"]

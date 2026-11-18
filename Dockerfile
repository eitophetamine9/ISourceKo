# Stage 1: Compile minified Tailwind CSS bundle
FROM node:20-alpine AS css-builder
WORKDIR /src
COPY package.json package-lock.json ./
RUN npm ci
COPY tailwind.config.js postcss.config.js ./
COPY Styles/ ./Styles/
COPY Components/ ./Components/
COPY wwwroot/ ./wwwroot/
RUN npm run build:css

# Stage 2: Restore and publish .NET 10 Blazor Web App
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS dotnet-builder
WORKDIR /src
COPY ISourceKO.csproj nuget.config ./
RUN dotnet restore ISourceKO.csproj
COPY . .
COPY --from=css-builder /src/wwwroot/css/tailwind.min.css ./wwwroot/css/tailwind.min.css
RUN dotnet publish ISourceKO.csproj -c Release -o /app/publish --no-restore

# Stage 3: Slim ASP.NET Core 10 runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Prepare persistent data directory (mount Railway volume at /data)
RUN mkdir -p /data/uploads/student-submissions /data/keys

ENV ASPNETCORE_ENVIRONMENT=Production
ENV PORT=8080
ENV DATA_DIR=/data

EXPOSE 8080

COPY --from=dotnet-builder /app/publish ./

ENTRYPOINT ["dotnet", "ISourceKO.dll"]

FROM mcr.microsoft.com/dotnet/sdk:10.0.401-noble@sha256:e70cdb7f80b0348f5cb85f19a8f670fca061f033d57eed12fa003d58b0e06317 AS build
WORKDIR /source

COPY global.json Directory.Build.props Directory.Packages.props NuGet.Config CulinaryBlog.slnx ./
COPY src/CulinaryBlog.Domain/CulinaryBlog.Domain.csproj src/CulinaryBlog.Domain/
COPY src/CulinaryBlog.Application/CulinaryBlog.Application.csproj src/CulinaryBlog.Application/
COPY src/CulinaryBlog.Infrastructure/CulinaryBlog.Infrastructure.csproj src/CulinaryBlog.Infrastructure/
COPY src/CulinaryBlog.Api/CulinaryBlog.Api.csproj src/CulinaryBlog.Api/
RUN dotnet restore src/CulinaryBlog.Api/CulinaryBlog.Api.csproj

COPY src/ src/
RUN dotnet publish src/CulinaryBlog.Api/CulinaryBlog.Api.csproj --no-restore -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0.12-noble@sha256:222759b391a1aaf241166672c8f99b2d4ada452e7b5319f3c6e8f265a37b5ad4 AS runtime
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "CulinaryBlog.Api.dll"]

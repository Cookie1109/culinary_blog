FROM mcr.microsoft.com/dotnet/sdk:10.0.404-noble@sha256:bef136a371963aaa46ec97882845ef6f54dec15728bb4b128b1c2a400ffbc9b4 AS build
WORKDIR /source

COPY global.json Directory.Build.props Directory.Packages.props NuGet.Config CulinaryBlog.slnx ./
COPY src/CulinaryBlog.Domain/CulinaryBlog.Domain.csproj src/CulinaryBlog.Domain/
COPY src/CulinaryBlog.Application/CulinaryBlog.Application.csproj src/CulinaryBlog.Application/
COPY src/CulinaryBlog.Infrastructure/CulinaryBlog.Infrastructure.csproj src/CulinaryBlog.Infrastructure/
COPY src/CulinaryBlog.Api/CulinaryBlog.Api.csproj src/CulinaryBlog.Api/
RUN dotnet restore src/CulinaryBlog.Api/CulinaryBlog.Api.csproj

COPY src/ src/
RUN dotnet publish src/CulinaryBlog.Api/CulinaryBlog.Api.csproj --no-restore -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0.15-noble@sha256:12b1135f5f8b3c0a214f2a680ff2033dff4f684f445d6c697d9524ceeb303586 AS runtime
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl libbrotli1 \
    && apt-get install -y --no-install-recommends --only-upgrade apt libapt-pkg6.0t64 \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app/publish .
RUN mkdir -p /app/logs /app/data-protection && chown "$APP_UID" /app/logs /app/data-protection
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "CulinaryBlog.Api.dll"]

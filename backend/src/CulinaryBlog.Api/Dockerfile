FROM mcr.microsoft.com/dotnet/sdk:10.0.402-noble@sha256:0d99ebf1ccad15037b5c98c9fe8979d6b8dbac23812feae2277a0b277b5c5ffa AS build
WORKDIR /source

COPY global.json Directory.Build.props Directory.Packages.props NuGet.Config CulinaryBlog.slnx ./
COPY src/CulinaryBlog.Domain/CulinaryBlog.Domain.csproj src/CulinaryBlog.Domain/
COPY src/CulinaryBlog.Application/CulinaryBlog.Application.csproj src/CulinaryBlog.Application/
COPY src/CulinaryBlog.Infrastructure/CulinaryBlog.Infrastructure.csproj src/CulinaryBlog.Infrastructure/
COPY src/CulinaryBlog.Api/CulinaryBlog.Api.csproj src/CulinaryBlog.Api/
RUN dotnet restore src/CulinaryBlog.Api/CulinaryBlog.Api.csproj

COPY src/ src/
RUN dotnet publish src/CulinaryBlog.Api/CulinaryBlog.Api.csproj --no-restore -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0.13-noble@sha256:d5633778c754e89a386882eddf8dbee6942447a8a4b8267c19cf1e1158d0b0ac AS runtime
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app/publish .
RUN mkdir -p /app/logs && chown "$APP_UID" /app/logs
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "CulinaryBlog.Api.dll"]

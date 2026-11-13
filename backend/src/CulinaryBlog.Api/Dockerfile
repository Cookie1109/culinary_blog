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

FROM mcr.microsoft.com/dotnet/aspnet:10.0.15-noble@sha256:bfd912c83295ce66d4b706ebb8541b9f14259b0e76fbb88b2751f58ce2399d2e AS runtime
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl libbrotli1 apt libapt-pkg6.0t64 \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app/publish .
RUN mkdir -p /app/logs /app/data-protection && chown "$APP_UID" /app/logs /app/data-protection
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "CulinaryBlog.Api.dll"]

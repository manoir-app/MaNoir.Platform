FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Directory.Build.props", "."]
COPY ["Directory.Packages.props", "."]
COPY ["MaNoir.Platform.slnx", "."]
COPY ["eng/Versions.props", "eng/"]
COPY ["packages/MaNoir.Core/MaNoir.Core.csproj", "packages/MaNoir.Core/"]
COPY ["packages/MaNoir.Core.Contracts/MaNoir.Core.Contracts.csproj", "packages/MaNoir.Core.Contracts/"]
COPY ["packages/MaNoir.Core.AdminUi.Hosting/MaNoir.Core.AdminUi.Hosting.csproj", "packages/MaNoir.Core.AdminUi.Hosting/"]
COPY ["apps/MaNoir.Core.Api/MaNoir.Core.Api.csproj", "apps/MaNoir.Core.Api/"]
COPY ["apps/MaNoir.Core.AdminUi/MaNoir.Core.AdminUi.csproj", "apps/MaNoir.Core.AdminUi/"]

RUN dotnet restore "apps/MaNoir.Core.AdminUi/MaNoir.Core.AdminUi.csproj"

COPY . .

RUN dotnet publish "apps/MaNoir.Core.AdminUi/MaNoir.Core.AdminUi.csproj" \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "MaNoir.Core.AdminUi.dll"]
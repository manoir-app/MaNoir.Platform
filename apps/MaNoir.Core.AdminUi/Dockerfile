FROM node:22 AS ui-build
WORKDIR /src/ui

COPY ["ui/", "./"]

RUN npm --prefix ./MaNoir.Core.AdminUi.Bootstrap install
RUN npm --prefix ./MaNoir.Core.AdminUi.Bootstrap install --no-save @rolldown/binding-linux-x64-gnu@1.0.0-rc.17 lightningcss-linux-x64-gnu@1.32.0
RUN npm --prefix ./MaNoir.Core.AdminUi.Bootstrap run build
RUN npm --prefix ./MaNoir.Core.AdminUi.Front install
RUN npm --prefix ./MaNoir.Core.AdminUi.Front install --no-save @rolldown/binding-linux-x64-gnu@1.0.0-rc.17 lightningcss-linux-x64-gnu@1.32.0
RUN npm --prefix ./MaNoir.Core.AdminUi.Front run build

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
COPY --from=ui-build /src/ui/MaNoir.Core.AdminUi.Bootstrap/dist/ ./wwwroot/bootstrap/
COPY --from=ui-build /src/ui/MaNoir.Core.AdminUi.Front/dist/ ./wwwroot/front/

ENTRYPOINT ["dotnet", "MaNoir.Core.AdminUi.dll"]
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props ./
COPY Directory.Packages.props ./
COPY NuGet.Config ./
COPY eng ./eng
COPY packages ./packages
COPY apps/MaNoir.Agents.Erza ./apps/MaNoir.Agents.Erza

RUN dotnet publish apps/MaNoir.Agents.Erza/MaNoir.Agents.Erza.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app

COPY --from=build /app/publish ./

ENTRYPOINT ["dotnet", "MaNoir.Agents.Erza.dll"]
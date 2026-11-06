FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/FleetDesk.Api/FleetDesk.Api.csproj", "src/FleetDesk.Api/"]
RUN dotnet restore "src/FleetDesk.Api/FleetDesk.Api.csproj"

COPY src/FleetDesk.Api/ src/FleetDesk.Api/
WORKDIR /src/src/FleetDesk.Api
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "FleetDesk.Api.dll"]

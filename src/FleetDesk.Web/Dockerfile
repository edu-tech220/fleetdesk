FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/FleetDesk.Web/FleetDesk.Web.csproj", "src/FleetDesk.Web/"]
RUN dotnet restore "src/FleetDesk.Web/FleetDesk.Web.csproj"

COPY src/FleetDesk.Web/ src/FleetDesk.Web/
WORKDIR /src/src/FleetDesk.Web
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "FleetDesk.Web.dll"]

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY *.slnx .
COPY src/LionSimPlanner.API/*.csproj src/LionSimPlanner.API/
COPY src/LionSimPlanner.Shared/*.csproj src/LionSimPlanner.Shared/
COPY src/Modules/Asset/LionSimPlanner.Asset.Application/*.csproj src/Modules/Asset/LionSimPlanner.Asset.Application/
COPY src/Modules/Asset/LionSimPlanner.Asset.Domain/*.csproj src/Modules/Asset/LionSimPlanner.Asset.Domain/
COPY src/Modules/Asset/LionSimPlanner.Asset.Infrastructure/*.csproj src/Modules/Asset/LionSimPlanner.Asset.Infrastructure/
COPY src/Modules/Personnel/LionSimPlanner.Personnel.Application/*.csproj src/Modules/Personnel/LionSimPlanner.Personnel.Application/
COPY src/Modules/Personnel/LionSimPlanner.Personnel.Domain/*.csproj src/Modules/Personnel/LionSimPlanner.Personnel.Domain/
COPY src/Modules/Personnel/LionSimPlanner.Personnel.Infrastructure/*.csproj src/Modules/Personnel/LionSimPlanner.Personnel.Infrastructure/
COPY src/Modules/Scheduling/LionSimPlanner.Scheduling.Application/*.csproj src/Modules/Scheduling/LionSimPlanner.Scheduling.Application/
COPY src/Modules/Scheduling/LionSimPlanner.Scheduling.Domain/*.csproj src/Modules/Scheduling/LionSimPlanner.Scheduling.Domain/
COPY src/Modules/Scheduling/LionSimPlanner.Scheduling.Infrastructure/*.csproj src/Modules/Scheduling/LionSimPlanner.Scheduling.Infrastructure/
COPY Notifications/LionSimPlanner.Notifications/*.csproj Notifications/LionSimPlanner.Notifications/

RUN dotnet restore src/LionSimPlanner.API/LionSimPlanner.API.csproj

COPY . .
WORKDIR /src/src/LionSimPlanner.API
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV PORT=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "LionSimPlanner.API.dll"]

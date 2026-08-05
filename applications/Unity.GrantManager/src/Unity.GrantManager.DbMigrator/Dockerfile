FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "src/Unity.GrantManager.DbMigrator/Unity.GrantManager.DbMigrator.csproj"
WORKDIR "/src/src/Unity.GrantManager.DbMigrator"
RUN dotnet build "Unity.GrantManager.DbMigrator.csproj" -c Release -o /app/build --no-restore

FROM build AS publish
RUN dotnet publish "Unity.GrantManager.DbMigrator.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
CMD ["dotnet", "Unity.GrantManager.DbMigrator.dll"]
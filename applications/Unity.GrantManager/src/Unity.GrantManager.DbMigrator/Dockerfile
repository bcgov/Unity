FROM mcr.microsoft.com/dotnet/runtime:7.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["NuGet.Config", "."]
COPY ["src/Unity.GrantManager.DbMigrator/Unity.GrantManager.DbMigrator.csproj", "src/Unity.GrantManager.DbMigrator/"]
COPY ["src/Unity.GrantManager.Application.Contracts/Unity.GrantManager.Application.Contracts.csproj", "src/Unity.GrantManager.Application.Contracts/"]
COPY ["src/Unity.GrantManager.Domain.Shared/Unity.GrantManager.Domain.Shared.csproj", "src/Unity.GrantManager.Domain.Shared/"]
COPY ["src/Unity.GrantManager.EntityFrameworkCore/Unity.GrantManager.EntityFrameworkCore.csproj", "src/Unity.GrantManager.EntityFrameworkCore/"]
COPY ["src/Unity.GrantManager.Domain/Unity.GrantManager.Domain.csproj", "src/Unity.GrantManager.Domain/"]
RUN dotnet restore "src/Unity.GrantManager.DbMigrator/Unity.GrantManager.DbMigrator.csproj"
COPY . .
WORKDIR "/src/src/Unity.GrantManager.DbMigrator"
RUN dotnet build "Unity.GrantManager.DbMigrator.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Unity.GrantManager.DbMigrator.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
CMD ["dotnet", "Unity.GrantManager.DbMigrator.dll"]
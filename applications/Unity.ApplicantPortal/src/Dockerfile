FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
ENV ASPNETCORE_URLS="http://+:8080"
EXPOSE 8080 443
WORKDIR /App

# Copy everything
COPY . ./
# Restore as distinct layers
RUN dotnet restore
# Build and publish a release
RUN dotnet publish -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /App
COPY --from=build-env /App/out .

## Logfile volume mount point
RUN mkdir -p /app/logs \
    && chmod 777 /app/logs

ENTRYPOINT ["dotnet", "DotNet.Docker.dll"]
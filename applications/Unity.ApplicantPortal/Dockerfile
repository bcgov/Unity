FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
ENV ASPNETCORE_URLS="http://+:8080"
EXPOSE 8080 443
WORKDIR /app

COPY . .

## Logfile volume mount point
RUN mkdir -p /app/logs \
    && chmod 777 /app/logs

ENTRYPOINT ["dotnet"]

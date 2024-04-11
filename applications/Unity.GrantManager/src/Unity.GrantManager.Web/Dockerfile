FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
ENV ASPNETCORE_URLS="http://+:8080"
EXPOSE 8080 443
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# set up node
ARG NODE_VERSION=18.17.1
ARG YARN_VERSION=1.22.19
ARG NODE_DOWNLOAD_URL=https://nodejs.org/dist/v${NODE_VERSION}/node-v${NODE_VERSION}-linux-x64.tar.gz
ARG NODE_DOWNLOAD_SHA=2cb75f2bc04b0a3498733fbee779b2f76fe3f655188b4ac69ef2887b6721da2d

RUN curl -SL --compressed "${NODE_DOWNLOAD_URL}" --output nodejs.tar.gz \
    && echo "${NODE_DOWNLOAD_SHA} nodejs.tar.gz" | sha256sum -c - \
    && tar -xzf "nodejs.tar.gz" -C /usr/local --strip-components=1 \
    && rm nodejs.tar.gz \
    && ln -s /usr/local/bin/node /usr/local/bin/nodejs \
    # NODE SMOKE TESTS
    && node --version \
    && npm --version \
    ## INSTALL YARN
    && corepack enable \
    && yarn set version ${YARN_VERSION}

COPY ["src/Unity.GrantManager.Domain.Shared/Unity.GrantManager.Domain.Shared.csproj", "src/Unity.GrantManager.Domain.Shared/"]
COPY ["src/Unity.GrantManager.Web/Unity.GrantManager.Web.csproj", "src/Unity.GrantManager.Web/"]
COPY ["src/Unity.GrantManager.Application/Unity.GrantManager.Application.csproj", "src/Unity.GrantManager.Application/"]
COPY ["src/Unity.GrantManager.Domain/Unity.GrantManager.Domain.csproj", "src/Unity.GrantManager.Domain/"]
COPY ["src/Unity.GrantManager.Application.Contracts/Unity.GrantManager.Application.Contracts.csproj", "src/Unity.GrantManager.Application.Contracts/"]
COPY ["src/Unity.GrantManager.HttpApi/Unity.GrantManager.HttpApi.csproj", "src/Unity.GrantManager.HttpApi/"]
COPY ["src/Unity.GrantManager.EntityFrameworkCore/Unity.GrantManager.EntityFrameworkCore.csproj", "src/Unity.GrantManager.EntityFrameworkCore/"]

COPY ["NuGet.Config", "NuGet.Config"]

RUN dotnet restore "src/Unity.GrantManager.Web/Unity.GrantManager.Web.csproj"
COPY . .
WORKDIR "/src/src/Unity.GrantManager.Web"

RUN dotnet tool install -g Volo.Abp.Cli
ENV PATH="${PATH}:/root/.dotnet/tools"
RUN dotnet dev-certs https --trust
RUN abp install-libs

FROM build AS publish
ARG UNITY_BUILD_VERSION
ARG UNITY_BUILD_REVISION
RUN dotnet publish "Unity.GrantManager.Web.csproj" -c Release -p:Version=${UNITY_BUILD_VERSION:-0.0.0} -p:SourceRevisionId=${UNITY_BUILD_REVISION:-0000000} -p:UseAppHost=false -o /app/publish 

FROM base AS final
WORKDIR /app
COPY --from=build /root/.dotnet/corefx/cryptography/x509stores/my/* /root/.dotnet/corefx/cryptography/x509stores/my/
COPY --from=publish /app/publish .

## TODO: Replace with volume
RUN mkdir -p /.aspnet \
    && mkdir -p /.dotnet \
    && chmod 777 /.aspnet \
    && chmod 777 /.dotnet

## Logfile volume mount point
RUN mkdir -p /app/logs \
    && chmod 777 /app/logs

ENTRYPOINT ["dotnet", "Unity.GrantManager.Web.dll"]

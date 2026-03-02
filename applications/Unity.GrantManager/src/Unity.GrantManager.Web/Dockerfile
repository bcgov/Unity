FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
ENV ASPNETCORE_URLS="http://+:8080"
EXPOSE 8080 443
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# set up node
ARG NODE_VERSION=24.13.0
ARG YARN_VERSION=1.22.22
ARG NODE_DOWNLOAD_URL=https://nodejs.org/dist/v${NODE_VERSION}/node-v${NODE_VERSION}-linux-x64.tar.gz
ARG NODE_DOWNLOAD_SHA=6223aad1a81f9d1e7b682c59d12e2de233f7b4c37475cd40d1c89c42b737ffa8

ADD --checksum=sha256:${NODE_DOWNLOAD_SHA} ${NODE_DOWNLOAD_URL} nodejs.tar.gz
RUN tar -xzf "nodejs.tar.gz" -C /usr/local --strip-components=1 \
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

RUN dotnet tool install -g Volo.Abp.Cli --version 9.1.3
ENV PATH="${PATH}:/root/.dotnet/tools"
RUN dotnet dev-certs https --trust
RUN abp install-libs

FROM build AS publish
ARG UNITY_BUILD_VERSION
ARG UNITY_BUILD_REVISION
RUN dotnet publish "Unity.GrantManager.Web.csproj" \
    -c Release \
    -p:Version=${UNITY_BUILD_VERSION:-0.0.0} \
    -p:SourceRevisionId=${UNITY_BUILD_REVISION:-0000000} \
    -p:UseAppHost=false \
    -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /root/.dotnet/corefx/cryptography/x509stores/my/* /root/.dotnet/corefx/cryptography/x509stores/my/
# CAS Dev/Test Certs changed - again Sept 17

COPY ["/src/Unity.GrantManager.Web/certs/cas2024inter.crt", "/usr/local/share/ca-certificates/cas2024inter.crt"]
COPY ["/src/Unity.GrantManager.Web/certs/cas2024root.crt", "/usr/local/share/ca-certificates/cas2024root.crt"]
COPY ["/src/Unity.GrantManager.Web/certs/cas2025inter.crt", "/usr/local/share/ca-certificates/cas2025inter.crt"]
COPY ["/src/Unity.GrantManager.Web/certs/cas2025root.crt", "/usr/local/share/ca-certificates/cas2025root.crt"]
COPY ["/src/Unity.GrantManager.Web/certs/cas2025top.crt", "/usr/local/share/ca-certificates/cas2025top.crt"]

COPY ["/src/Unity.GrantManager.Web/certs/casroot.crt", "/usr/local/share/ca-certificates/casroot.crt"]
COPY ["/src/Unity.GrantManager.Web/certs/cas.crt", "/usr/local/share/ca-certificates/cas.crt"]
COPY ["/src/Unity.GrantManager.Web/certs/sslcomroot.crt", "/usr/local/share/ca-certificates/sslcomroot.crt"]
COPY ["/src/Unity.GrantManager.Web/certs/ssoroot.crt", "/usr/local/share/ca-certificates/ssoroot.crt"]

RUN mkdir -p /.aspnet && \
    mkdir -p /.dotnet && \
    mkdir -p /app/logs && \
    chmod 755 /.aspnet && \
    chmod 755 /.dotnet && \
    chmod 755 /app/logs && \        
    chmod 644 /usr/local/share/ca-certificates/cas2024inter.crt && \
    chmod 644 /usr/local/share/ca-certificates/cas2024root.crt && \ 
    chmod 644 /usr/local/share/ca-certificates/cas2025inter.crt && \
    chmod 644 /usr/local/share/ca-certificates/cas2025root.crt && \
    chmod 644 /usr/local/share/ca-certificates/cas2025top.crt && \
    chmod 644 /usr/local/share/ca-certificates/cas.crt && \
    chmod 644 /usr/local/share/ca-certificates/casroot.crt && \
    chmod 644 /usr/local/share/ca-certificates/sslcomroot.crt && \
    chmod 644 /usr/local/share/ca-certificates/ssoroot.crt && \
    update-ca-certificates

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Unity.GrantManager.Web.dll"]

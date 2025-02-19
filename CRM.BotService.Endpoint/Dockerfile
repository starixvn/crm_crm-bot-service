ARG CI_REGISTRY_IMAGE

FROM $CI_REGISTRY_IMAGE:builder AS deps

RUN mkdir -p /src/packages

FROM hub.nexdev.net/base/dotnet/core/sdk:9.0-alpine AS builder

ENV NUGET_PACKAGES=/src/packages

COPY --from=deps $NUGET_PACKAGES/. $NUGET_PACKAGES/

WORKDIR /src

COPY ["nuget.config", "."]
COPY ["CRM.BotService.Endpoint/CRM.BotService.Endpoint.csproj", "CRM.BotService.Endpoint/"]
COPY ["CRM.BotService.Core/CRM.BotService.Core.csproj", "CRM.BotService.Core/"]

RUN dotnet nuget add source https://nuget.nexdev.net/api/v2

RUN dotnet restore "CRM.BotService.Endpoint/CRM.BotService.Endpoint.csproj"

COPY . .

WORKDIR "/src/CRM.BotService.Endpoint"

RUN dotnet publish -r linux-x64 --self-contained false -o /app

FROM hub.nexdev.net/base/dotnet/core/aspnet:9.0-alpine-25h1 AS base

WORKDIR /app

COPY --from=builder /app .

ENTRYPOINT ["dotnet", "CRM.BotService.Endpoint.dll"]
FROM hub.nexdev.net/base/dotnet/core/sdk:6.0-alpine AS builder

WORKDIR /src

COPY ["nuget.config", "."]
COPY ["CRM.BotService.Endpoint/CRM.BotService.Endpoint.csproj", "CRM.BotService.Endpoint/"]
COPY ["CRM.BotService.Core/CRM.BotService.Core.csproj", "CRM.BotService.Core/"]
COPY ["CRM.Core/CRM.Core.csproj", "CRM.Core/"]
COPY ["CRM.Resources/CRM.Resources.csproj", "CRM.Resources/"]
COPY ["CRM.Models/CRM.Models.csproj", "CRM.Models/"]

RUN dotnet restore "CRM.BotService.Endpoint/CRM.BotService.Endpoint.csproj"

COPY . .

WORKDIR "/src/CRM.BotService.Endpoint"

RUN dotnet publish -r linux-x64 --self-contained false -o /app

FROM hub.nexdev.net/base/dotnet/core/aspnet:6.0-final AS base

WORKDIR /app

COPY --from=builder /app .

ENTRYPOINT ["dotnet", "CRM.BotService.Endpoint.dll"]
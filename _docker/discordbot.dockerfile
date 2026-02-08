FROM mcr.microsoft.com/dotnet/sdk:10.0-preview-alpine AS build
WORKDIR /src

COPY Common/Common/Common.csproj Common/Common/
COPY Common/Common.Hosting/Common.Hosting.csproj Common/Common.Hosting/
COPY Apis/ApiFreeBoxCore/Domain/Domain.csproj Apis/ApiFreeBoxCore/Domain/
COPY Services/DiscordBot/DiscordBot.csproj Services/DiscordBot/
RUN dotnet restore Services/DiscordBot/DiscordBot.csproj

COPY Common/Common/ Common/Common/
COPY Common/Common.Hosting/ Common/Common.Hosting/
COPY Apis/ApiFreeBoxCore/Domain/ Apis/ApiFreeBoxCore/Domain/
COPY Services/DiscordBot/ Services/DiscordBot/
RUN dotnet publish Services/DiscordBot/DiscordBot.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/runtime:10.0-preview AS runtime
WORKDIR /app
COPY --from=build /app .

ENTRYPOINT ["dotnet", "DiscordBot.dll"]

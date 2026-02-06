FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

COPY Common/Common/Common.csproj Common/Common/
COPY Common/Common.Hosting/Common.Hosting.csproj Common/Common.Hosting/
COPY Services/MinecraftLogsToDiscord/MinecraftLogsToDiscord.csproj Services/MinecraftLogsToDiscord/
RUN dotnet restore Services/MinecraftLogsToDiscord/MinecraftLogsToDiscord.csproj

COPY Common/Common/ Common/Common/
COPY Common/Common.Hosting/ Common/Common.Hosting/
COPY Services/MinecraftLogsToDiscord/ Services/MinecraftLogsToDiscord/
RUN dotnet publish Services/MinecraftLogsToDiscord/MinecraftLogsToDiscord.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/runtime:8.0-alpine AS runtime
WORKDIR /app
COPY --from=build /app .

ENTRYPOINT ["dotnet", "MinecraftLogsToDiscord.dll"]

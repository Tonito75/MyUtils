FROM mcr.microsoft.com/dotnet/sdk:10.0-preview-alpine AS build
WORKDIR /src

COPY Services/MonsterBot/MonsterBot.csproj Services/MonsterBot/
RUN dotnet restore Services/MonsterBot/MonsterBot.csproj

COPY Services/MonsterBot/ Services/MonsterBot/
RUN dotnet publish Services/MonsterBot/MonsterBot.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/runtime:10.0-preview AS runtime
WORKDIR /app
COPY --from=build /app .

ENTRYPOINT ["dotnet", "MonsterBot.dll"]

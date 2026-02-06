FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

COPY Common/Common/Common.csproj Common/Common/
COPY Common/Common.Hosting/Common.Hosting.csproj Common/Common.Hosting/
COPY Services/MinecraftWorldToNAS/MinecraftWorldToNAS.csproj Services/MinecraftWorldToNAS/
RUN dotnet restore Services/MinecraftWorldToNAS/MinecraftWorldToNAS.csproj

COPY Common/Common/ Common/Common/
COPY Common/Common.Hosting/ Common/Common.Hosting/
COPY Services/MinecraftWorldToNAS/ Services/MinecraftWorldToNAS/
RUN dotnet publish Services/MinecraftWorldToNAS/MinecraftWorldToNAS.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/runtime:8.0-alpine AS runtime
WORKDIR /app
COPY --from=build /app .

ENTRYPOINT ["dotnet", "MinecraftWorldToNAS.dll"]

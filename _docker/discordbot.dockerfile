FROM mcr.microsoft.com/dotnet/sdk:10.0-preview-alpine AS build
WORKDIR /src

COPY Common/Common/Common.csproj Common/Common/
COPY Common/Common.Hosting/Common.Hosting.csproj Common/Common.Hosting/
COPY Apis/ApiFreeBoxCore/Domain/Domain.csproj Apis/ApiFreeBoxCore/Domain/
COPY Services/FreeBoxBot/FreeBoxBot.csproj Services/FreeBoxBot/
RUN dotnet restore Services/FreeBoxBot/FreeBoxBot.csproj

COPY Common/Common/ Common/Common/
COPY Common/Common.Hosting/ Common/Common.Hosting/
COPY Apis/ApiFreeBoxCore/Domain/ Apis/ApiFreeBoxCore/Domain/
COPY Services/FreeBoxBot/ Services/FreeBoxBot/
RUN dotnet publish Services/FreeBoxBot/FreeBoxBot.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/runtime:10.0-preview AS runtime
WORKDIR /app
COPY --from=build /app .

ENTRYPOINT ["dotnet", "FreeBoxBot.dll"]

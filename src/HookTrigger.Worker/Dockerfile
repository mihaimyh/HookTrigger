#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:5.0-buster-slim AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:5.0-buster-slim AS build
WORKDIR /src
COPY ["HookTrigger.Worker/HookTrigger.Worker.csproj", "HookTrigger.Worker/"]
COPY ["HookTrigger.Core/HookTrigger.Core.csproj", "HookTrigger.Core/"]
RUN dotnet restore "HookTrigger.Worker/HookTrigger.Worker.csproj"
COPY . .
WORKDIR "/src/HookTrigger.Worker"
RUN dotnet build "HookTrigger.Worker.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "HookTrigger.Worker.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "HookTrigger.Worker.dll"]
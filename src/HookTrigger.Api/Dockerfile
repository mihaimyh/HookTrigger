#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:5.0-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:5.0-buster-slim AS build
WORKDIR /src
COPY ["HookTrigger.Api/HookTrigger.Api.csproj", "HookTrigger.Api/"]
COPY ["HookTrigger.Core/HookTrigger.Core.csproj", "HookTrigger.Core/"]
RUN dotnet restore "HookTrigger.Api/HookTrigger.Api.csproj"
COPY . .
WORKDIR "/src/HookTrigger.Api"
RUN dotnet build "HookTrigger.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "HookTrigger.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "HookTrigger.Api.dll"]
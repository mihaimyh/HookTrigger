#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["src/HookTrigger.Api/HookTrigger.Api.csproj", "src/HookTrigger.Api/"]
COPY ["src/HookTrigger.Core/HookTrigger.Core.csproj", "src/HookTrigger.Core/"]
RUN dotnet restore "src/HookTrigger.Api/HookTrigger.Api.csproj"
COPY . .
WORKDIR "/src/src/HookTrigger.Api"
RUN dotnet build "HookTrigger.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "HookTrigger.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "HookTrigger.Api.dll"]
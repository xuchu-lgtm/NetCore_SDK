#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:2.2 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:2.2 AS build
WORKDIR /src
COPY ["test/Space.Web.Test/Space.Web.Test.csproj", "test/Space.Web.Test/"]
COPY ["src/Space.HttpClient.Extensions/Space.HttpClient.Extensions.csproj", "src/Space.HttpClient.Extensions/"]
COPY ["src/Space/Space.csproj", "src/Space/"]
COPY ["src/Space.Consul/Space.Consul.csproj", "src/Space.Consul/"]
COPY ["src/Space.Log4Net/Space.Log4Net.csproj", "src/Space.Log4Net/"]
COPY ["src/Space.Redis/Space.Redis.csproj", "src/Space.Redis/"]
COPY ["src/Space.Log4Net.CustomHttpPush/Space.Log4Net.CustomExtensions.csproj", "src/Space.Log4Net.CustomHttpPush/"]
COPY ["src/Space.Web.AspNetCore/Space.Web.AspNetCore.csproj", "src/Space.Web.AspNetCore/"]
COPY ["src/Space.DnsClient/Space.DnsClient.csproj", "src/Space.DnsClient/"]
RUN dotnet restore "test/Space.Web.Test/Space.Web.Test.csproj"
COPY . .
WORKDIR "/src/test/Space.Web.Test"
RUN dotnet build "Space.Web.Test.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Space.Web.Test.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Space.Web.Test.dll"]
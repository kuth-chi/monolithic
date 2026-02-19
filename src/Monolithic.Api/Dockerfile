FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/Monolithic.Api/Monolithic.Api.csproj", "src/Monolithic.Api/"]
RUN dotnet restore "src/Monolithic.Api/Monolithic.Api.csproj"
COPY . .
WORKDIR "/src/src/Monolithic.Api"
RUN dotnet publish "Monolithic.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "Monolithic.Api.dll"]
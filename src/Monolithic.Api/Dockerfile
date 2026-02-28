FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
# Install libgssapi for Kerberos/GSSAPI support (avoids "Cannot load libgssapi_krb5.so.2" at runtime)
# Install libgssapi for Kerberos/GSSAPI support and docker CLI for self-update feature
RUN apt-get update && apt-get install -y --no-install-recommends \
    libgssapi-krb5-2 \
    ca-certificates \
    curl \
    gnupg \
    && install -m 0755 -d /etc/apt/keyrings \
    && curl -fsSL https://download.docker.com/linux/debian/gpg | gpg --dearmor -o /etc/apt/keyrings/docker.gpg \
    && chmod a+r /etc/apt/keyrings/docker.gpg \
    && echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/debian $(. /etc/os-release && echo \"$VERSION_CODENAME\") stable" > /etc/apt/sources.list.d/docker.list \
    && apt-get update \
    && apt-get install -y --no-install-recommends docker-ce-cli \
    && rm -rf /var/lib/apt/lists/*

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
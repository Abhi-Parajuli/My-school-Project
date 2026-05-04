FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 10000

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore dependencies first (layer caching)
COPY ["SchoolAPI/SchoolAPI.csproj", "SchoolAPI/"]
RUN dotnet restore "SchoolAPI/SchoolAPI.csproj"

# Copy the full repo
COPY . .

# Copy frontend files into wwwroot BEFORE publish
RUN mkdir -p SchoolAPI/wwwroot/css SchoolAPI/wwwroot/js SchoolAPI/wwwroot/photo
RUN cp *.html SchoolAPI/wwwroot/ 2>/dev/null || true
RUN cp css/*.css SchoolAPI/wwwroot/css/ 2>/dev/null || true
RUN cp js/*.js SchoolAPI/wwwroot/js/ 2>/dev/null || true
RUN cp photo/* SchoolAPI/wwwroot/photo/ 2>/dev/null || true

WORKDIR "/src/SchoolAPI"
RUN dotnet publish "SchoolAPI.csproj" -c Release -o /app/publish \
    --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SchoolAPI.dll"]
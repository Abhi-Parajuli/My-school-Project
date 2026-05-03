FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 10000

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# SchoolAPI.csproj lives inside SchoolAPI/ subfolder
COPY ["SchoolAPI/SchoolAPI.csproj", "SchoolAPI/"]
RUN dotnet restore "SchoolAPI/SchoolAPI.csproj"

# Copy entire repo
COPY . .

# Copy frontend files from root into SchoolAPI/wwwroot/
# so the .NET app can serve them as static files
RUN mkdir -p SchoolAPI/wwwroot/css SchoolAPI/wwwroot/js SchoolAPI/wwwroot/photo
RUN cp *.html SchoolAPI/wwwroot/ 2>/dev/null || true
RUN cp css/* SchoolAPI/wwwroot/css/ 2>/dev/null || true
RUN cp js/* SchoolAPI/wwwroot/js/ 2>/dev/null || true
RUN cp photo/* SchoolAPI/wwwroot/photo/ 2>/dev/null || true

WORKDIR "/src/SchoolAPI"
RUN dotnet publish "SchoolAPI.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SchoolAPI.dll"]
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY TaskManager.sln ./
COPY backend/TaskManager.Domain/TaskManager.Domain.csproj backend/TaskManager.Domain/
COPY backend/TaskManager.Application/TaskManager.Application.csproj backend/TaskManager.Application/
COPY backend/TaskManager.Infrastructure/TaskManager.Infrastructure.csproj backend/TaskManager.Infrastructure/
COPY backend/TaskManager.Api/TaskManager.Api.csproj backend/TaskManager.Api/
RUN dotnet restore backend/TaskManager.Api/TaskManager.Api.csproj

COPY backend/ backend/
RUN dotnet publish backend/TaskManager.Api/TaskManager.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

RUN mkdir -p /app/data && chown app:app /app/data

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080 \
    ConnectionStrings__Default="Data Source=/app/data/taskmanager.db"

USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "TaskManager.Api.dll"]

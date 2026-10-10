FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY HabitTracker.sln ./
COPY src/HabitTracker.Api/HabitTracker.Api.csproj src/HabitTracker.Api/
COPY tests/HabitTracker.Tests/HabitTracker.Tests.csproj tests/HabitTracker.Tests/
RUN dotnet restore src/HabitTracker.Api/HabitTracker.Api.csproj
COPY src/ src/
RUN dotnet publish src/HabitTracker.Api/HabitTracker.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app ./
# La conexión a PostgreSQL se pasa por variables de entorno (ConnectionStrings__Default,
# o Database__Host, Database__Name, Database__User y Database__Password).
USER app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "HabitTracker.Api.dll"]

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
# SQLite vive en /data (efímero en el contenedor: se siembra al arrancar).
RUN mkdir /data && chown app:app /data
USER app
ENV ASPNETCORE_URLS=http://+:8080 \
    ConnectionStrings__Default="Data Source=/data/habits.db"
EXPOSE 8080
ENTRYPOINT ["dotnet", "HabitTracker.Api.dll"]

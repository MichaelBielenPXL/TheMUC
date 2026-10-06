FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY muc-servicedefaults/muc-servicedefaults.csproj muc-servicedefaults/
COPY muc-backend/muc-backend.csproj muc-backend/
RUN dotnet restore muc-backend/muc-backend.csproj
COPY muc-servicedefaults/ muc-servicedefaults/
COPY muc-backend/ muc-backend/
RUN dotnet publish muc-backend/muc-backend.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
# The SQLite file lives here; the named volume inherits this ownership when it is first created.
RUN mkdir /data && chown $APP_UID /data
USER $APP_UID
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "muc-backend.dll"]

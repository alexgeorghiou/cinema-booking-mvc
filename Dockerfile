# escape=`
# Windows container: ASP.NET MVC 5 needs the full .NET Framework and IIS.
# Build:  docker build -t cinema-booking-mvc .
# Run:    docker run -d -p 8080:80 -e CINEMA_DB="Server=host.docker.internal,1433;Database=CinemaBooking;User Id=sa;Password=<pwd>;TrustServerCertificate=True" cinema-booking-mvc

FROM mcr.microsoft.com/dotnet/framework/sdk:4.8 AS build
WORKDIR C:\src

COPY nuget.config Cinema.sln Directory.Build.props ./
COPY src/Cinema.Core/Cinema.Core.csproj src/Cinema.Core/
COPY src/Cinema.Web/Cinema.Web.csproj src/Cinema.Web/
COPY tests/Cinema.Core.Tests/Cinema.Core.Tests.csproj tests/Cinema.Core.Tests/
RUN nuget restore Cinema.sln -ConfigFile nuget.config

COPY . .
RUN msbuild src\Cinema.Web\Cinema.Web.csproj /nologo /v:minimal `
      /p:Configuration=Release /p:DeployOnBuild=true /p:WebPublishMethod=FileSystem `
      /p:PublishUrl=C:\publish /p:DeleteExistingFiles=true

FROM mcr.microsoft.com/dotnet/framework/aspnet:4.8 AS runtime
WORKDIR C:\inetpub\wwwroot
COPY --from=build C:\publish .

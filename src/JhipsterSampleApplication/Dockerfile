FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build
WORKDIR /app
COPY ["src/JHipsterNet/JHipsterNet.csproj", "src/JHipsterNet/"]
COPY ["src/JhipsterSampleApplication/JhipsterSampleApplication.csproj", "src/JhipsterSampleApplication/"]
RUN dotnet restore "src/JhipsterSampleApplication/JhipsterSampleApplication.csproj"
COPY . ./
WORKDIR src/JhipsterSampleApplication
RUN apt-get update -yq && apt-get install -yq curl
RUN curl -sL https://deb.nodesource.com/setup_12.x | bash - && \
    apt-get update && \
    apt-get install -yq nodejs && \
    rm -rf /var/lib/apt/lists/*
RUN cd ClientApp && npm install
RUN rm -rf ClientApp/dist && \
    dotnet publish "JhipsterSampleApplication.csproj" -c Release -o /app/out

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1 AS runtime
WORKDIR /app
EXPOSE 80
COPY --from=build /app/out .
ENTRYPOINT ["dotnet", "jhipster-sample-application.dll"]

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS base
WORKDIR /src
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore ResearchWebApi.csproj
RUN dotnet build "./ResearchWebApi.csproj" -c Debug -o /out

FROM build AS publish
RUN dotnet publish ResearchWebApi.csproj -c Debug -o /out

# Building final image used in running container
FROM base AS final
WORKDIR /src
COPY --from=publish /out .
ENV ASPNETCORE_URLS=http://*:5000
# RUN kubectl port-forward --namespace default postgresql-1-postgresql-0 5432

ENTRYPOINT ["dotnet", "ResearchWebApi.dll"]

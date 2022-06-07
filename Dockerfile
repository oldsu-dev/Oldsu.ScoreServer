FROM mcr.microsoft.com/dotnet/sdk:6.0

EXPOSE 8080

WORKDIR /usr/src/app
COPY . .

ENV ASPNETCORE_URLS=http://+:8080
RUN dotnet publish -c Release --output ./dist Oldsu.ScoreServer.sln 

CMD ["dist/Oldsu.ScoreServer"]


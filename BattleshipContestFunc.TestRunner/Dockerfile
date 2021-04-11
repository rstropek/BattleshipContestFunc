FROM mcr.microsoft.com/dotnet/sdk:5.0 AS Builder
WORKDIR /runner
COPY . .
WORKDIR /runner/BattleshipContestFunc.TestRunner
RUN dotnet restore \
    && dotnet build --no-restore --configuration Release \
    && dotnet pack --no-build --no-restore --configuration Release

FROM mcr.microsoft.com/dotnet/sdk:5.0
RUN groupadd -r bscc -g 901 && useradd -u 901 -r -g bscc -m bscc
ENV HOME /home/bscc
USER bscc
WORKDIR /home/bscc
COPY --from=Builder /runner/BattleshipContestFunc.TestRunner/nupkg/*.nupkg .
RUN dotnet tool install --global --add-source . NBattleshipCodingContest.Runner --version 0.3.0-alpha
ENTRYPOINT [ "/home/bscc/.dotnet/tools/bscc" ]
CMD [ "--help" ]

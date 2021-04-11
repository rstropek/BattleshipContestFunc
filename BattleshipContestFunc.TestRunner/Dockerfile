FROM mcr.microsoft.com/dotnet/sdk:5.0 AS Builder
WORKDIR /runner
COPY . .
WORKDIR /runner/BattleshipContestFunc.TestRunner
RUN dotnet restore \
    && dotnet build --no-restore --configuration Release

FROM mcr.microsoft.com/dotnet/runtime:5.0
RUN groupadd -r bscc -g 901 && useradd -u 901 -r -g bscc --no-log-init -m bscc
ENV HOME /home/bscc
USER bscc
WORKDIR /home/bscc
COPY --from=Builder /runner/BattleshipContestFunc.TestRunner/bin/Release/net5.0/ .
ENTRYPOINT [ "/home/bscc/BattleshipContestFunc.TestRunner" ]
CMD [ "--help" ]

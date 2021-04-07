namespace BattleshipContestFunc
{
    public interface IPlayerHttpClientFactory
    {
        IPlayerHttpClient GetHttpClient(string baseUrl);
    }
}

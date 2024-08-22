namespace net.server.coordinator.session;

public record HwidRelevance(string hwid, int relevance)
{
    public int getIncrementedRelevance()
    {
        return relevance < sbyte.MaxValue ? relevance + 1 : relevance;
    }
}

namespace net.server.coordinator.session;

public class HwidAssociationExpiry
{
    public static DateTimeOffset getHwidAccountExpiry(int relevance)
    {
        return DateTimeOffset.FromUnixTimeMilliseconds(Server.getInstance().getCurrentTime()).Add(hwidExpirationUpdate(relevance));
    }

    private static TimeSpan hwidExpirationUpdate(int relevance)
    {
        int degree = getHwidExpirationDegree(relevance);

        long baseHours = degree switch
        {
            0 => 2,
            1 => TimeSpan.FromDays(1).Hours,
            2 => TimeSpan.FromDays(7).Hours,
            _ => TimeSpan.FromDays(70).Hours,
        };

        int subdegreeTime = (degree * 3) + 1;
        if (subdegreeTime > 10)
        {
            subdegreeTime = 10;
        }

        return TimeSpan.FromHours(baseHours + subdegreeTime);
    }

    private static int getHwidExpirationDegree(int relevance)
    {
        int degree = 1;
        int subdegree;
        while ((subdegree = 5 * degree) <= relevance)
        {
            relevance -= subdegree;
            degree++;
        }

        return --degree;
    }
}

public static class FirebaseValue
{
    public static int ToInt(object value)
    {
        if (value == null)
        {
            return 0;
        }

        switch (value)
        {
            case long l:
                return (int)l;
            case double d:
                return (int)d;
            case int i:
                return i;
            default:
                if (int.TryParse(value.ToString(), out int parsed))
                {
                    return parsed;
                }
                return 0;
        }
    }
}

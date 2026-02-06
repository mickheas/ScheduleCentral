public static class YearLevelFormatter
{
    public static string ToOrdinal(int year)
    {
        return year switch
        {
            1 => "1st Year",
            2 => "2nd Year",
            3 => "3rd Year",
            4 => "4th Year",
            5 => "5th Year",
            6 => "6th Year",
            _ => $"{year}th Year"
        };
    }
}

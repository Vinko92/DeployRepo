namespace GoogleScraper.Const
{
    public static class RegexPaterns
    {
        public const string EMAIL_REGEX = @"[A-Za-z0-9_\-\+]+@[A-Za-z0-9\-]+\.([A-Za-z]{2,3})(?:\.[a-z]{2})?";

        public const string PHONE_NUMBER_REGEX = @"((\(\d{3}\) ?)|(\d{3}-))?\d{3}-\d{4}";

        public const string EMAIL_REGEX_FORMAT = @"[A-Za-z0-9_\-\+]+{0}?";

    }
}

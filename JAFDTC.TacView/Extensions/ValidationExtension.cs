namespace JAFDTC.TacView.Extensions
{
    public static class ValidationExtension
    {
        public static void Required(this string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Parameter is required");
        }

        public static void Required(this object obj)
        {
            if (obj == null)
                throw new ArgumentException("Parameter is required");
        }
    }
}

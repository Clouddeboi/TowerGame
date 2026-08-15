using System.Text;
using System.Text.RegularExpressions;
using Game.Inventory.Definitions;

namespace Game.Inventory.Editor
{
    //generates a stable, human readable item id from a display name plus a short
    //random suffix, not a GUID, since ids show up in debug logs and save files where
    //"sword_iron_a3f2" is far more useful than a raw GUID
    public static class StableIdGenerator
    {
        private static readonly System.Random RandomSource = new System.Random();

        public static string Generate(string displayName, string categoryHint = null)
        {
            string slug = Slugify(displayName);

            if (!string.IsNullOrEmpty(categoryHint))
            {
                slug = Slugify(categoryHint) + "_" + slug;
            }

            string suffix = GenerateRandomSuffix(4);

            return $"{slug}_{suffix}";
        }

        //checks the given database for a collision, a live check within the current
        //editor session, not a guarantee against a different branch generating the
        //same id independently
        public static bool IsCollision(string candidateId, ItemDatabase database)
        {
            if (database == null || string.IsNullOrEmpty(candidateId))
            {
                return false;
            }

            return database.Contains(new Core.ItemId(candidateId));
        }

        //generates an id, regenerating the random suffix up to a small retry limit if a
        //collision is detected against the given database
        public static string GenerateNonColliding(string displayName, ItemDatabase database, string categoryHint = null)
        {
            const int maxAttempts = 10;

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                string candidate = Generate(displayName, categoryHint);

                if (!IsCollision(candidate, database))
                {
                    return candidate;
                }
            }

            //extremely unlikely given the suffix space, but fall back to a longer
            //suffix rather than looping forever
            return Generate(displayName, categoryHint) + "_" + GenerateRandomSuffix(4);
        }

        private static string Slugify(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return "item";
            }

            string lowered = input.ToLowerInvariant();
            string alphanumericOnly = Regex.Replace(lowered, @"[^a-z0-9\s]", string.Empty);
            string collapsedWhitespace = Regex.Replace(alphanumericOnly, @"\s+", "_").Trim('_');

            return string.IsNullOrEmpty(collapsedWhitespace) ? "item" : collapsedWhitespace;
        }

        private static string GenerateRandomSuffix(int length)
        {
            const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
            var builder = new StringBuilder(length);

            for (int i = 0; i < length; i++)
            {
                builder.Append(chars[RandomSource.Next(chars.Length)]);
            }

            return builder.ToString();
        }
    }
}
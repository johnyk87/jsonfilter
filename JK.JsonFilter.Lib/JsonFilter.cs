namespace JK.JsonFilter.Lib
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Newtonsoft.Json.Linq;

    public static class JsonFilter
    {
        public static Task FilterJsonAsync(TextReader textReader, TextWriter textWriter, string[] fieldSelectors)
        {
            if (textReader == null)
            {
                throw new ArgumentNullException(nameof(textReader));
            }

            if (textWriter == null)
            {
                throw new ArgumentNullException(nameof(textWriter));
            }

            if (fieldSelectors == null)
            {
                throw new ArgumentNullException(nameof(fieldSelectors));
            }

            return FilterJsonInternalAsync(textReader, textWriter, fieldSelectors);
        }

        private static async Task FilterJsonInternalAsync(TextReader textReader, TextWriter textWriter, string[] fieldSelectors)
        {
            var json = await textReader.ReadToEndAsync().ConfigureAwait(false);

            json = FilterJson(json, fieldSelectors);

            await textWriter.WriteAsync(json).ConfigureAwait(false);
        }

        private static string FilterJson(string json, string[] fieldSelectors)
        {
            var jsonObject = JToken.Parse(json);

            var whiteListedPaths = fieldSelectors
                .SelectMany(path => jsonObject.SelectTokens(path))
                .Where(token => !string.IsNullOrEmpty(token.Path))
                .Select(token => token.Path)
                .ToArray();

            FilterObject(jsonObject, ref whiteListedPaths);

            return jsonObject.ToString();
        }

        private static void FilterObject(JToken jsonObject, ref string[] whiteListedPaths)
        {
            foreach (var token in jsonObject.ToArray())
            {
                var currentPath = token.Path;
                if (!whiteListedPaths.Any(path => IsPathIncluded(path, currentPath)))
                {
                    token.Remove();
                    continue;
                }

                FilterObject(token, ref whiteListedPaths);
            }
        }

        private static bool IsPathIncluded(string fullPath, string candidatePath)
        {
            return fullPath.Equals(candidatePath)
                || fullPath.StartsWith($"{candidatePath}.")
                || fullPath.StartsWith($"{candidatePath}[");
        }
    }
}

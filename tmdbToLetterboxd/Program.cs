using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;
using System.Globalization;

namespace tool
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Connection connection = new Connection();
            if (args.Length != 0 && !string.IsNullOrEmpty(args[0]) && !string.IsNullOrEmpty(args[1]))
            {
                connection.ListId = args[0];
                connection.ApiKey = args[1];
            }
            else
            {
                connection.ListId = "T";
                connection.ApiKey = "your_api_key_here";
            }
            setConnection(connection);
        }

        private static void setConnection(Connection connection)
        {
            connection.currentPage = "1";
            var client = new HttpClient();
            try
            {
                var responseTest = client.GetAsync($"https://api.themoviedb.org/3/authentication?api_key={connection.ApiKey}").Result;
                if (responseTest.IsSuccessStatusCode)
                {
                    Console.WriteLine("\n✅ Connessione riuscita!");

                    string baseUrl = $"https://api.themoviedb.org/3/list/{connection.ListId}?api_key={connection.ApiKey}&language=it-IT&page=";
                    var firstResponse = client.GetAsync(baseUrl + connection.currentPage).Result;
                    if (!firstResponse.IsSuccessStatusCode)
                        return;

                    string firstBody = firstResponse.Content.ReadAsStringAsync().Result;
                    var firstList = JsonSerializer.Deserialize<Response>(firstBody);
                    if (firstList == null)
                        return;

                    int totalPages = firstList.TotalPages;
                    using (var writer = new StreamWriter("watched.csv", false))
                    {
                        writer.WriteLine("Date,Name,Year");
                        void WriteItemsToCsv(List<MovieResult>? items)
                        {
                            if (items == null)
                                return;

                            foreach (var item in items)
                            {
                                string date = item?.ReleaseDate ?? string.Empty;
                                string name = item?.Title ?? string.Empty;
                                string year = string.Empty;
                                if (!string.IsNullOrEmpty(date))
                                {
                                    if (DateTime.TryParse(date, out var dt))
                                        year = dt.Year.ToString(CultureInfo.InvariantCulture);
                                    else if (date.Length >= 4)
                                        year = date.Substring(0, 4);
                                }

                                writer.WriteLine($"{EscapeCsv(date)},{EscapeCsv(name)},{EscapeCsv(year)}");
                            }
                        }
                        if (totalPages > 1)
                        {
                            WriteItemsToCsv(firstList.Items);

                            for (int p = 2; p <= totalPages; p++)
                            {
                                var resp = client.GetAsync(baseUrl + p).Result;
                                if (!resp.IsSuccessStatusCode)
                                    continue;

                                var body = resp.Content.ReadAsStringAsync().Result;
                                var pageList = JsonSerializer.Deserialize<Response>(body);
                                WriteItemsToCsv(pageList?.Items);
                            }
                        }
                        else
                        {
                            WriteItemsToCsv(firstList.Items);
                        }
                    }
                }
                else
                {
                    Console.WriteLine("\n❌ Connessione fallita. Controlla la tua API Key.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ Si è verificato un errore durante la connessione: {ex.Message}");
                return;
            }
        }

        private static string EscapeCsv(string input)
        {
            if (input == null)
                return string.Empty;
            if (input.Contains('"'))
                input = input.Replace("\"", "\"\"");
            if (input.Contains(',') || input.Contains('"') || input.Contains('\n') || input.Contains('\r'))
                return "\"" + input + "\"";
            return input;
        }
    }

    class Connection
    {
        public string ApiKey { get; set; }
        public string ListId { get; set; }

        public string currentPage {  get; set; }
    }



    class Response
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("items")]
        public List<MovieResult>? Items { get; set; }

        [JsonPropertyName("total_pages")]
        public int TotalPages { get; set; }
    }

    class MovieResult
    {
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("release_date")]
        public string? ReleaseDate { get; set; }

        [JsonPropertyName("vote_average")]
        public double VoteAverage { get; set; }
    }

}
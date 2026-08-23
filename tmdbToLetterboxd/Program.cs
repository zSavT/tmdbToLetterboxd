using System.Text.Json;
using System.Text.Json.Serialization;

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
            } else
            {
                connection.ListId = "T";
                connection.ApiKey = "your_api_key_here";
            }

            Console.Write("Tentativo di connessione in corso...");
            setConnection(connection);
        }

        private static void setConnection(Connection connection)
        {
            connection.currentPage = "1";
            var client = new HttpClient();
            try
            {   try
                {
                    var responseTest = client.GetAsync($"https://api.themoviedb.org/3/authentication?api_key={connection.ApiKey}").Result;
                    if (responseTest.IsSuccessStatusCode)
                    {
                        Console.WriteLine("\n✅ Connessione riuscita!");
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

                string url = $"https://api.themoviedb.org/3/list/{connection.ListId}?api_key={connection.ApiKey}&language=it-IT&page={connection.currentPage}";
                var response = client.GetAsync(url).Result;
                if (response.IsSuccessStatusCode)
                {
                    // Leggi la risposta JSON
                }
                else
                {
                    Console.WriteLine("\n❌ Connessione fallita. Controlla la tua API Key.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ Si è verificato un errore: {ex.Message}");
            }
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
using System;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;
using System.Globalization;
using System.Collections.Generic;
#nullable enable
using System.Text;

namespace tool
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Connection connection = new Connection();
            if (args.Length >= 2 && !string.IsNullOrEmpty(args[0]) && !string.IsNullOrEmpty(args[1]))
            {
                connection.ListId = args[0];
                connection.ApiKey = args[1];
            }
            else
            {
                Console.WriteLine("No arguments provided. Please enter values (input hidden).");
                string api = ReadSecret("API Key: ");
                string listInput = ReadSecret("Link or list ID: ");
                connection.ApiKey = api;
                connection.ListId = ExtractListId(listInput);
            }
            setConnection(connection);
        }

        private static string ReadSecret(string prompt)
        {
            Console.Write(prompt);
            var sb = new StringBuilder();
            while (true)
            {
                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    break;
                }
                else if (key.Key == ConsoleKey.Backspace)
                {
                    if (sb.Length > 0)
                    {
                        sb.Length--;
                        Console.Write("\b \b");
                    }
                }
                else
                {
                    sb.Append(key.KeyChar);
                    Console.Write("*");
                }
            }
            return sb.ToString();
        }

        private static string ExtractListId(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;
            if (Uri.TryCreate(input, UriKind.Absolute, out var uri))
            {
                var segs = uri.Segments;
                for (int i = segs.Length - 1; i >= 0; i--)
                {
                    var s = segs[i].Trim('/');
                    if (!string.IsNullOrEmpty(s))
                        return s;
                }
            }
            return input;
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
                    Console.WriteLine("\n✅ Connection successful!");
                    string baseUrl = $"https://api.themoviedb.org/3/list/{connection.ListId}?api_key={connection.ApiKey}&language=it-IT&page=";
                    var firstResponse = client.GetAsync(baseUrl + connection.currentPage).Result;
                    if (!firstResponse.IsSuccessStatusCode)
                        return;
                    string firstBody = firstResponse.Content.ReadAsStringAsync().Result;
                    Response? firstList = null;
                    try
                    {
                        firstList = JsonSerializer.Deserialize<Response>(firstBody);
                    }
                    catch (JsonException jex)
                    {
                        Console.WriteLine($"\n❌ Failed to parse JSON response: {jex.Message}");
                        return;
                    }

                    if (firstList == null)
                        return;

                    int totalPages = firstList.TotalPages;
                    string fileName = "watched.csv";
                    StreamWriter? writer = null;
                    try
                    {
                        writer = new StreamWriter(fileName, false);
                    }
                    catch (IOException)
                    {
                        string alt = $"watched_{DateTime.Now:yyyyMMddHHmmss}.csv";
                        try
                        {
                            writer = new StreamWriter(alt, false);
                            Console.WriteLine($"\nWarning: could not write to '{fileName}', using '{alt}' instead.");
                            fileName = alt;
                        }
                        catch (IOException ioex)
                        {
                            Console.WriteLine($"\n❌ Unable to create CSV file: {ioex.Message}");
                            return;
                        }
                    }

                    using (writer)
                    {
                        try
                        {
                            writer.WriteLine("Date,Name,Year");

                            int processed = 0;
                            int estimatedTotal = (firstList.Items?.Count ?? 0) * totalPages;

                            int WriteItemsToCsv(List<MovieResult>? items)
                            {
                                if (items == null)
                                    return 0;

                                int written = 0;
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
                                    written++;
                                }
                                return written;
                            }

                            if (totalPages > 1)
                            {
                                processed += WriteItemsToCsv(firstList.Items);
                                Console.WriteLine($"Processed {processed} of ~{estimatedTotal} items (page 1 of {totalPages}).");

                                for (int p = 2; p <= totalPages; p++)
                                {
                                    var resp = client.GetAsync(baseUrl + p).Result;
                                    if (!resp.IsSuccessStatusCode)
                                        continue;

                                    var body = resp.Content.ReadAsStringAsync().Result;
                                    Response? pageList = null;
                                    try
                                    {
                                        pageList = JsonSerializer.Deserialize<Response>(body);
                                    }
                                    catch (JsonException)
                                    {
                                        Console.WriteLine($"\nWarning: failed to parse JSON for page {p}, skipping.");
                                        continue;
                                    }

                                    int written = WriteItemsToCsv(pageList?.Items);
                                    processed += written;
                                    Console.WriteLine($"Processed {processed} of ~{estimatedTotal} items (page {p} of {totalPages}, items this page: {written}).");
                                }
                            }
                            else
                            {
                                processed += WriteItemsToCsv(firstList.Items);
                                Console.WriteLine($"Processed {processed} items.");
                            }
                        }
                        catch (Exception writeEx)
                        {
                            Console.WriteLine($"\n❌ Error while writing CSV: {writeEx.Message}");
                            return;
                        }
                    }
                }
                else
                {
                    Console.WriteLine("\n❌ Connection failed. Check your API key.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ An error occurred during connection: {ex.Message}");
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
        public string ApiKey { get; set; } = string.Empty;
        public string ListId { get; set; } = string.Empty;
        public string currentPage { get; set; } = string.Empty;
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
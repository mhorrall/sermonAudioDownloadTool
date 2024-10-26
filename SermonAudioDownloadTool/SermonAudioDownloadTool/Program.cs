using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("SermonAudio Download Tool");
        // API key for SermonAudio
        string apiKey = "Paste your API Key Herer";

        // Path to save the downloaded sermons
        string savePath = "C:/Sermons/";

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            Console.WriteLine("API key cannot be empty. Exiting...");
            return;
        }

        if (!Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
        }

        using (var client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("X-API-Key", apiKey);

            // Iterate through pages of sermons
            for (int page = 1; page <= 1000; page++)
            {
                string apiUrl = $"https://api.sermonaudio.com/v2/node/sermons?featured=false&requireAudio=false&requireVideo=false&includeDrafts=false&includeScheduled=false&includePublished=true&staffPick=false&listenerRecommended=false&page={page}&lite=false&liteBroadcaster=false&cache=false&pageSize=100&sortBy=newest";

                try
                {
                    var response = await client.GetAsync(apiUrl);
                    response.EnsureSuccessStatusCode();
                    var content = await response.Content.ReadAsStringAsync();

                    var jsonObject = JObject.Parse(content);
                    var sermons = jsonObject["results"] as JArray;
                    if (sermons == null)
                    {
                        Console.WriteLine("Error: Unable to parse sermons from the API response.");
                        return;
                    }

                    if (sermons.Count == 0)
                    {
                        Console.WriteLine("No more sermons found. Exiting...");
                        break;
                    }

                    foreach (var sermon in sermons)
                    {
                        string title = new string(sermon["fullTitle"].ToString().Where(c => !Path.GetInvalidFileNameChars().Contains(c)).ToArray());

                        string date = sermon["publishDate"].ToString();
                        string downloadUrl = "";
                        var audioArray = sermon["media"]["audio"] as JArray;
                        if (audioArray == null)
                        {
                            string message = $"Error: Unable to parse audio media from the API response for sermon: {title} {date}";
                            Console.WriteLine(message);
                        }

                        foreach (var audio in audioArray)
                        {
                            if (audio["mediaType"].ToString().ToLower() == "mp3")
                            {
                                downloadUrl = audio["downloadURL"].ToString();

                                if (!string.IsNullOrWhiteSpace(downloadUrl))
                                {
                                    break;
                                }
                            }
                        }

                        if (string.IsNullOrWhiteSpace(downloadUrl))
                        {
                            Console.WriteLine($"Error: Unable to find MP3 download URL for sermon: {title} {date}");
                            continue;
                        }

                        string fileName = Path.Combine(savePath, $"{title}_{date}.mp3");
                        if (File.Exists(fileName))
                        {
                            Console.WriteLine($"Skipping: {title} {date} (already exists)");
                            continue;
                        }


                        Console.WriteLine($"Downloading: {title} {date}");

                        var audioResponse = await client.GetAsync(downloadUrl);
                        audioResponse.EnsureSuccessStatusCode();

                        using (var fs = new FileStream(fileName, FileMode.Create))
                        {
                            await audioResponse.Content.CopyToAsync(fs);
                        }

                        Console.WriteLine($"Downloaded: {fileName}");
                    }

                    
                }
                catch (HttpRequestException e)
                {
                    Console.WriteLine($"Error: {e.Message}");
                }
                Console.WriteLine("All sermons downloaded successfully!");
            }

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}


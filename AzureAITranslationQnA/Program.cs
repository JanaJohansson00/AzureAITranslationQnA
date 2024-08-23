using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace AzureAITranslationQnA
{
    class Program
    {
        private static string qnaEndpoint;
        private static string qnaKey;
        private static string translationEndpoint;
        private static string translationKey;
        private static string translationRegion;

        static async Task Main(string[] args)
        {
            // Loading configuration from appsettings.json
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            // Get settings from configuration
            qnaEndpoint = config["QnASettings:Endpoint"];
            qnaKey = config["QnASettings:ApiKey"];
            translationEndpoint = config["TranslatorSettings:Endpoint"];
            translationKey = config["TranslatorSettings:ApiKey"];
            translationRegion = config["TranslatorSettings:Region"];

            Console.WriteLine("Ask a question about AI (type 'quit' to end the program):");

            while (true)
            {
                
                Console.Write("Your question: ");
                string userQuestion = Console.ReadLine();
                Console.WriteLine("");

                if (userQuestion.ToLower() == "quit")
                {
                    break;
                }

                // Get the answer from the QnA service
                var answer = await GetAnswerFromQ(userQuestion);

                if (answer.ToLower().Contains("no answer was found"))
                {
                    Console.WriteLine("Sorry, no answer was found. Would you like to ask another question? (type 'yes' to ask again, 'quit' to end):");
                    Console.WriteLine("");
                    string retryAction = Console.ReadLine();

                    if (retryAction.ToLower() == "quit")
                    {
                        break;
                    }
                    else if (retryAction.ToLower() != "yes")
                    {
                        continue; 
                    }

                    continue;
                }
                else
                {
                    Console.WriteLine($"Answer: {answer}");
                    Console.WriteLine("");

                    string action;
                    do
                    {
                        Console.WriteLine("Do you want to translate this answer? (type 'translate' to translate, or 'quit' to end):");
                        action = Console.ReadLine().ToLower();
                        Console.WriteLine("");

                        if (action == "quit")
                        {
                            return; 
                        }
                        else if (action == "translate")
                        {
                            // Prompt for the target language for translation
                            Console.WriteLine("Which language do you want to translate to? (type 'fr' for French, 'es' for Spanish, 'it' for Italian):");
                            string languageChoice = Console.ReadLine().ToLower();

                            string translatedAnswer = languageChoice switch
                            {
                                "fr" => await TranslateText(answer, "fr"),
                                "es" => await TranslateText(answer, "es"),
                                "it" => await TranslateText(answer, "it"),
                                _ => "Invalid language choice."
                            };

                            Console.WriteLine($"Translated Answer: {translatedAnswer}");
                            Console.WriteLine("");
                        }
                    } while (action != "translate" && action != "quit");

                    string nextAction;
                    do
                    {
                        Console.WriteLine("Would you like to ask another question? (type 'yes' to ask again, 'quit' to end):");
                        nextAction = Console.ReadLine().ToLower();
                        Console.WriteLine("");

                        if (nextAction == "quit")
                        {
                            return; 
                        }
                    } while (nextAction != "yes" && nextAction != "quit");
                }
            }

            Console.WriteLine("Program ended. Goodbye!");
        }

        // Method to get answer from the QnA service
        static async Task<string> GetAnswerFromQ(string question)
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", qnaKey);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Create the request body with the user's question
                var requestBody = new
                {
                    question = question
                };

                // Serialize the request body to JSON
                var content = new StringContent(JsonConvert.SerializeObject(requestBody), System.Text.Encoding.UTF8, "application/json");

                // Send the request to the QnA endpoint
                var response = await client.PostAsync(qnaEndpoint, content);

                // Check if the response is successful
                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    dynamic data = JsonConvert.DeserializeObject(jsonResponse);
                    return data.answers[0].answer;
                }
                else
                {
                    return "Error: " + response.ReasonPhrase;
                }
            }
        }

        // Method to translate text to the specified language
        static async Task<string> TranslateText(string text, string language)
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", translationKey);
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Region", translationRegion);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Create the request body with the text to translate
                var requestBody = new[]
                {
                    new { Text = text }
                };

                // Serialize the request body to JSON
                var content = new StringContent(JsonConvert.SerializeObject(requestBody), System.Text.Encoding.UTF8, "application/json");

                // Send the request to the translation endpoint
                var response = await client.PostAsync($"{translationEndpoint}&to={language}", content);

                // Check if the response is successful
                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    dynamic data = JsonConvert.DeserializeObject(jsonResponse);
                    return data[0].translations[0].text;
                }
                else
                {
                    var errorResponse = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Translation failed. Status code: {response.StatusCode}, Error: {errorResponse}");
                    return "Translation Error";
                }
            }
        }
    }
}

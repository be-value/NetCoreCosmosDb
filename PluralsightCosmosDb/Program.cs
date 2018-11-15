using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.Documents.Client;
using Microsoft.Extensions.Configuration;
using PluralsightCosmosDb.Demos;

namespace PluralsightCosmosDb
{
    public static class Program
    {
        public static IConfigurationRoot Configuration { get; set; }
        private static IDictionary<string, Func<DocumentClient, Task>> _demoMethods;

        private static void Main()
        {
            var devEnvironmentVariable = Environment.GetEnvironmentVariable("NETCORE_ENVIRONMENT");
            var isDevelopment = string.IsNullOrEmpty(devEnvironmentVariable) || devEnvironmentVariable.ToLower() == "development";

            //Determines the working environment as IHostingEnvironment is unavailable in a console app
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddEnvironmentVariables();

            if (isDevelopment) //only add secrets in development
            {
                builder.AddUserSecrets(typeof(Program).Assembly);
            }

            Configuration = builder.Build();

            DoWork();
        }

        private static void DoWork()
        {
            _demoMethods = new Dictionary<string, Func<DocumentClient, Task>>
            {
                {"DB", DatabasesDemo.Run},
                {"CO", CollectionsDemo.Run},
                {"DO", DocumentsDemo.Run},
                {"IX", IndexingDemo.Run},
                {"UP", UsersAndPermissionsDemo.Run},
                {"SP", StoredProceduresDemo.Run},
                {"TR", TriggersDemo.Run},
                {"UF", UserDefinedFunctionsDemo.Run},
                {"C", Cleanup.Run}
            };

            Task.Run(async () =>
            {
                ShowMenu();
                while (true)
                {
                    Console.Write("Selection: ");
                    var input = Console.ReadLine();
                    // ReSharper disable once PossibleNullReferenceException
                    var demoId = input.ToUpper().Trim();
                    if (_demoMethods.Keys.Contains(demoId))
                    {
                        var demoMethod = _demoMethods[demoId];
                        await RunDemo(demoMethod);
                    }
                    else if (demoId == "Q")
                    {
                        break;
                    }
                    else
                    {
                        Console.WriteLine($"?{input}");
                    }
                }
            }).Wait();
        }

        private static void ShowMenu()
        {
            Console.WriteLine(@"Cosmos DB SQL API .NET SDK demos
===============================================
DB Databases
CO Collections
DO Documents
IX Indexing
UP Users & Permissions

Cosmos DB SQL API Server-Side Programming demos
===============================================
SP Stored procedures
TR Triggers
UF User defined functions

C  Cleanup

Q  Quit
");
        }

        private static async Task RunDemo(Func<DocumentClient, Task> demoMethod)
        {
            try
            {
                var endpoint = Configuration["cosmosDb:account:endpoint"];
                var masterkey = Configuration["cosmosDb:account:masterKey"];
                using (var client = new DocumentClient(new Uri(endpoint), masterkey))
                {
                    await demoMethod(client);
                }
            }
            catch (Exception ex)
            {
                var message = ex.Message;
                while (ex.InnerException != null)
                {
                    ex = ex.InnerException;
                    message += Environment.NewLine + ex.Message;
                }
                Console.WriteLine($"Error: {message}");
            }
            Console.WriteLine();
            Console.Write("Done. Press any key to continue...");
            Console.ReadKey(true);
            Console.Clear();
            ShowMenu();
        }

    }
}

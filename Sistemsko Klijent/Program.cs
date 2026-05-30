using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Sistemsko_Klijent
{
    internal class Program
    {
        static Dictionary<string, object> fileLocks = new Dictionary<string, object>();
        static object globalLock = new object();
        static HttpClient client = new HttpClient()
        {
            BaseAddress = new Uri("http://localhost:5050/")
        };
        static void f1()//napuni kes
        {
            for(int i=1;i<=40;i++)
            {
                Task.Run(async () => await SendRequestAsync($"fajl{i}.csv"));
            }
        }
        static void f2()
        {
            string[] resursi = { "file1.csv", "file2.csv", "file3.csv" };
            foreach (var resurs in resursi)
            {
                Task.Run(async () => await SendRequestAsync(resurs));
            }
        }
        static void f3()
        {
            string[] resursi1 = { "testfile1.csv", "testfile2.csv", "testfile3.csv" };
            foreach (var resurs in resursi1)
            {
                Task.Run(async () => await SendRequestAsync(resurs));
            }
        }
        static void f4()//pogresan tip fajla
        {
            Task.Run(async () => await SendRequestAsync("file.txt"));
        }
        static void f5()//nepostojeci fajl
        {
            Task.Run(async () => await SendRequestAsync("file0.csv"));
        }
        static void f6()//stamoedo
        {
            for (int i = 0; i < 50; i++)
            {
                Task.Run(async () => await SendRequestAsync("file20.csv"));
            }
        }
        static void Main(string[] args)
        {
            // PODIGNI LIMIT KONEKCIJA NA KLIJENTU (Dodaj ove dve linije!)
            System.Net.ServicePointManager.DefaultConnectionLimit = 100;
            System.Net.ServicePointManager.Expect100Continue = false;
            bool work = true;
            while(work)
            {
                string s=Console.ReadLine();
                switch(s)
                {
                    case "1": f1();break;//napuni kes
                    case "2": f2(); break;//test sa manjim fajlovima
                    case "3": f3(); break;//test sa vecim fajlovima
                    case "4": f4(); break;//pogresan tip
                    case "5": f5(); break;//neposotojeci fajl
                    case "6": f6(); break;//stampedo
                    case "end":work = false; break;
                    default:break;
                }
            }
            Console.WriteLine("Pritisni ENTER za izlaz...");
            Console.ReadLine();
        }
        // Menjamo u async Task, a objekat 'state' kastujemo kao i pre
        static async Task SendRequestAsync(string resurs)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            try
            {
                //ASINHRONO slanje zahteva - nit se vraća u pool dok server ne odgovori
                var response = await client.GetAsync(resurs);
                response.EnsureSuccessStatusCode();

                //ASINHRONO čitanje bajtova iz mrežnog strima
                byte[] fileBytes = await response.Content.ReadAsByteArrayAsync();

                sw.Stop(); // Gasimo štopericu čim imamo bajtove u RAM-u

                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string dataFolder = Path.Combine(baseDir, "data");
                Directory.CreateDirectory(dataFolder);

                string fileName = $"{Path.GetFileNameWithoutExtension(resurs)}.xlsx";
                string fullPath = Path.Combine(dataFolder, fileName);

                // Logika oko klijentskih lock-ova ostaje ista jer je brza
                object fileLock;
                lock (globalLock)
                {
                    if (!fileLocks.ContainsKey(fullPath))
                        fileLocks[fullPath] = new object();

                    fileLock = fileLocks[fullPath];
                }
                lock (fileLock)
                {
                    File.WriteAllBytes(fullPath, fileBytes);
                }
                Console.WriteLine($"[{resurs}] Sačuvan u: {fullPath}");
                Console.WriteLine($"[{resurs}] Vreme: {sw.ElapsedMilliseconds} ms");
            }
            catch (Exception ex)
            {
                sw.Stop();
                Console.WriteLine($"[{resurs}] Greška: {ex.Message}");
                Console.WriteLine($"[{resurs}] Vreme: {sw.ElapsedMilliseconds} ms");
            }
        }
    }
}

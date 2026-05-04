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
                ThreadPool.QueueUserWorkItem(SendRequest, $"fajl{i}.csv");
            }
        }
        static void f2()
        {
            string[] resursi = { "file1.csv", "file2.csv", "file3.csv" };
            foreach (var resurs in resursi)
            {
                ThreadPool.QueueUserWorkItem(SendRequest, resurs);
            }
        }
        static void f3()
        {
            string[] resursi1 = { "testfile1.csv", "testfile2.csv", "testfile3.csv" };
            foreach (var resurs in resursi1)
            {
                ThreadPool.QueueUserWorkItem(SendRequest, resurs);
            }
        }
        static void f4()//pogresan tip fajla
        {
            ThreadPool.QueueUserWorkItem(SendRequest, "file.txt");
        }
        static void f5()//nepostojeci fajl
        {
            ThreadPool.QueueUserWorkItem(SendRequest, "file0.csv");
        }
        static void f6()//stamoedo
        {
            for (int i = 0; i < 50; i++)
            {
                ThreadPool.QueueUserWorkItem(SendRequest, "file20.csv");
            }
        }
        static void Main(string[] args)
        {
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
        static void SendRequest(object state)
        {
            string resurs = (string)state;

            Stopwatch sw = new Stopwatch();
            sw.Start();

            try
            {
                var response = client.GetAsync(resurs).Result;
                response.EnsureSuccessStatusCode();
                byte[] fileBytes = response.Content.ReadAsByteArrayAsync().Result;
                sw.Stop();//zanima nas koliko vremena nam treba da dobijemo odgovor
                          //ne i vreme da handlujemo sta treba na strani klijenta
                          // PUTANJA: bin/Debug/.../data
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string dataFolder = Path.Combine(baseDir, "data");

                // napravi folder ako ne postoji
                Directory.CreateDirectory(dataFolder);

                string fileName = $"{Path.GetFileNameWithoutExtension(resurs)}.xlsx";
                string fullPath = Path.Combine(dataFolder, fileName);

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

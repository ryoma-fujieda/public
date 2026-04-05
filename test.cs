using System;
using System.IO;
using System.Text;
using System.Threading;
using JVDTLabLib;

namespace JvLinkTest
{
    internal class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            JVLinkClass jv = new JVLinkClass();

            // ここを必要に応じて変更
            string dataSpec = "RACE";
            string fromTime = "20260101000000"; // YYYYMMDDhhmmss
            int option = 1; // 通常取得

            string outPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "jv_raw_test.txt");

            int readCount = 0;
            int downloadCount = 0;
            string lastFileTimestamp = "";

            try
            {
                int rc = jv.JVInit("UNKNOWN");
                Console.WriteLine($"JVInit rc={rc}");
                if (rc != 0) return;

                rc = jv.JVOpen(
                    dataSpec,
                    fromTime,
                    option,
                    ref readCount,
                    ref downloadCount,
                    out lastFileTimestamp
                );

                Console.WriteLine($"JVOpen rc={rc}");
                Console.WriteLine($"readCount={readCount}");
                Console.WriteLine($"downloadCount={downloadCount}");
                Console.WriteLine($"lastFileTimestamp={lastFileTimestamp}");

                if (rc != 0) return;

                // ダウンロード完了待ち
                while (downloadCount > 0)
                {
                    int status = jv.JVStatus();
                    Console.WriteLine($"JVStatus={status}/{downloadCount}");

                    if (status < 0)
                    {
                        Console.WriteLine($"JVStatus error={status}");
                        return;
                    }

                    if (status >= downloadCount) break;
                    Thread.Sleep(2000);
                }

                if (File.Exists(outPath))
                {
                    File.Delete(outPath);
                }

                int recordNo = 0;

                while (true)
                {
                    string buff;
                    int size;
                    string fileName;

                    int readRc = jv.JVRead(out buff, out size, out fileName);

                    if (readRc == 0)
                    {
                        Console.WriteLine("JVRead end");
                        break;
                    }
                    else if (readRc == -1)
                    {
                        Console.WriteLine("ファイル切り替わり");
                        continue;
                    }
                    else if (readRc == -3)
                    {
                        Console.WriteLine("ダウンロード中。2秒待って再試行します。");
                        Thread.Sleep(2000);
                        continue;
                    }
                    else if (readRc < 0)
                    {
                        Console.WriteLine($"JVRead error={readRc}");
                        break;
                    }

                    recordNo++;

                    // 先頭2文字がレコード種別ID
                    string recId = buff.Length >= 2 ? buff.Substring(0, 2) : "??";

                    string line =
                        $"[{recordNo}] recId={recId} size={size} file={fileName}{Environment.NewLine}" +
                        buff + Environment.NewLine +
                        new string('-', 80) + Environment.NewLine;

                    File.AppendAllText(outPath, line, Encoding.UTF8);

                    if (recordNo <= 50)
                    {
                        string preview = buff.Length > 120 ? buff.Substring(0, 120) : buff;
                        Console.WriteLine($"[{recordNo}] recId={recId} size={size} file={fileName}");
                        Console.WriteLine(preview);
                        Console.WriteLine(new string('-', 80));
                    }
                }

                Console.WriteLine($"出力完了: {outPath}");
            }
            finally
            {
                jv.JVClose();
            }
        }
    }
}
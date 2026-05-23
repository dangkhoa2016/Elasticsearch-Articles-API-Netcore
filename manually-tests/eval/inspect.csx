#r "nuget: Microsoft.EntityFrameworkCore.Sqlite, 10.0.8"

using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.Sqlite;
using System.IO;
using System.Runtime.CompilerServices;

// Lấy tham số cuối cùng của câu lệnh (chính là đường dẫn file inspect.cs bạn gõ)
string scriptPath = Environment.GetCommandLineArgs().Last();
Console.WriteLine($"[DEBUG] scriptPath: {scriptPath}");

string workingDir = Environment.CurrentDirectory;
Console.WriteLine($"[DEBUG] workingDir: {workingDir}");

// Hàm này giúp lấy chính xác đường dẫn của file inspect.cs đang chạy
static string GetScriptFolder([CallerFilePath] string path = "") => Path.GetDirectoryName(path);

// Thư mục chứa file inspect.cs (manually-tests/eval/)
string scriptDir = GetScriptFolder(); 

// Đi ngược lên 2 cấp để vào thư mục gốc Elasticsearch-Articles-API-Netcore
string projectRootDir = Path.GetFullPath(Path.Combine(scriptDir, "..", ".."));

// Trỏ thẳng vào thư mục DB
string dbPath = Path.Combine(projectRootDir, "DB", "development.sqlite3");

Console.WriteLine($"[DEBUG] Script Dir: {scriptDir}");
Console.WriteLine($"[DEBUG] Project Root: {projectRootDir}");
Console.WriteLine($"[DEBUG] DB Path: {dbPath}");

Console.WriteLine($"DB Path: {dbPath}");
if (!File.Exists(dbPath)) {
    Console.WriteLine("Db file does not exists!");
    return;
}

// Tạo Connection String
string connectionString = $"Data Source={dbPath};";

using (var connection = new SqliteConnection(connectionString))
{
    connection.Open();

    // 1. List all tables
    Console.WriteLine("-- list all tables");
    PrintQuery(connection, "SELECT name FROM sqlite_master WHERE type='table';");

    // 2. Schema of articles table
    Console.WriteLine("-- schema of files table");
    PrintQuery(connection, "PRAGMA table_info(articles);");

    // 3. List all indexes
    Console.WriteLine("-- list all indexes");
    PrintQuery(connection, "SELECT name FROM sqlite_master WHERE type='index';");

    // 4. Schema of index_comments_on_article_id
    Console.WriteLine("-- schema of index_comments_on_article_id");
    PrintQuery(connection, "PRAGMA index_info(index_comments_on_article_id);");

    // 5. Count all records in `articles` table
    Console.WriteLine("-- count all records in `articles` table");
    PrintQuery(connection, "SELECT count(id) as total_articles FROM articles;");

    // 6. Count all records in `comments` table
    PrintQuery(connection, "SELECT count(id) as total_comments FROM comments;");

    // 7. List all records in `categories` table
    Console.WriteLine("-- list all reccords in `categories` table");
    PrintQuery(connection, "SELECT * FROM categories;");
}

/// <summary>
/// Hàm chạy query thô và vẽ bảng giống SQLite mode table
/// </summary>
static void PrintQuery(SqliteConnection connection, string query)
{
    using (var command = connection.CreateCommand())
    {
        command.CommandText = query;
        using (var reader = command.ExecuteReader())
        {
            // Lấy danh sách tên cột
            var headers = new List<string>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                headers.Add(reader.GetName(i));
            }

            // Đọc dữ liệu
            var rows = new List<List<string>>();
            while (reader.Read())
            {
                var rowData = new List<string>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    rowData.Add(reader.GetValue(i)?.ToString() ?? "");
                }
                rows.Add(rowData);
            }

            // Tính toán độ rộng lớn nhất cho từng cột để căn lề
            int[] columnWidths = new int[headers.Count];
            for (int i = 0; i < headers.Count; i++)
            {
                columnWidths[i] = headers[i].Length;
                foreach (var row in rows)
                {
                    if (row[i].Length > columnWidths[i])
                    {
                        columnWidths[i] = row[i].Length;
                    }
                }
            }

            // Hàm vẽ đường viền ngăn cách +------+-----+
            Action printDivider = () =>
            {
                Console.Write("+");
                foreach (var width in columnWidths)
                {
                    Console.Write(new string('-', width + 2) + "+");
                }
                Console.WriteLine();
            };

            printDivider();

            // In Tiêu đề cột
            Console.Write("|");
            for (int i = 0; i < headers.Count; i++)
            {
                Console.Write($" {headers[i].PadRight(columnWidths[i])} |");
            }
            Console.WriteLine();

            printDivider();

            // In dữ liệu từng hàng
            foreach (var row in rows)
            {
                Console.Write("|");
                for (int i = 0; i < row.Count; i++)
                {
                    Console.Write($" {row[i].PadRight(columnWidths[i])} |");
                }
                Console.WriteLine();
            }

            printDivider();
            Console.WriteLine();
        }
    }
}

// Gọi lệnh này để xóa sạch các pool chạy ngầm trước khi app kịp crash
Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();

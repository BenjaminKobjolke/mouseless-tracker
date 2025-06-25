using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

internal static class SessionLogger
{
    private static readonly string databaseFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "database.db");
    private const int MinSessionDurationSeconds = 10;

    /// <summary>
    /// Initializes the database and creates the sessions table if it doesn't exist.
    /// </summary>
    public static void InitializeDatabase()
    {
        try
        {
            using var connection = new SqliteConnection($"Data Source={databaseFilePath}");
            connection.Open();

            var createTableCommand = connection.CreateCommand();
            createTableCommand.CommandText = @"
                CREATE TABLE IF NOT EXISTS sessions (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    timestamp DATETIME DEFAULT CURRENT_TIMESTAMP,
                    duration_seconds INTEGER NOT NULL,
                    executable_name TEXT NULL,
                    window_title TEXT NULL
                );";
            createTableCommand.ExecuteNonQuery();

            Console.WriteLine("Database initialized successfully."); // Debug
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error initializing database: {ex.Message}"); // Debug
            // Don't throw - graceful degradation
        }
    }

    /// <summary>
    /// Logs a mouseless session to the database if logging is enabled and session meets minimum duration.
    /// </summary>
    /// <param name="durationSeconds">Duration of the session in seconds</param>
    public static async Task LogSessionAsync(int durationSeconds)
    {
        // Check if logging is enabled
        if (!SettingsManager.LoadLoggingEnabled())
        {
            return; // Logging disabled, exit silently
        }

        // Check minimum duration requirement
        if (durationSeconds < MinSessionDurationSeconds)
        {
            Console.WriteLine($"Session duration ({durationSeconds}s) below minimum ({MinSessionDurationSeconds}s). Not logging."); // Debug
            return;
        }

        // Get active window information
        var (executableName, windowTitle) = GetActiveWindowInfo();

        // Log to database asynchronously
        await LogSessionToDatabaseAsync(durationSeconds, executableName, windowTitle);
    }

    /// <summary>
    /// Gets information about the currently active window.
    /// </summary>
    /// <returns>Tuple containing executable name and window title (both can be null if detection fails)</returns>
    private static (string? executableName, string? windowTitle) GetActiveWindowInfo()
    {
        try
        {
            // Get the foreground window handle
            IntPtr foregroundWindow = NativeMethods.GetForegroundWindow();
            if (foregroundWindow == IntPtr.Zero)
            {
                Console.WriteLine("No foreground window found."); // Debug
                return (null, null);
            }

            // Get window title
            string? windowTitle = GetWindowTitle(foregroundWindow);

            // Get process information
            string? executableName = GetExecutableName(foregroundWindow);

            Console.WriteLine($"Active window: '{windowTitle}' from '{executableName}'"); // Debug
            return (executableName, windowTitle);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting active window info: {ex.Message}"); // Debug
            return (null, null);
        }
    }

    /// <summary>
    /// Gets the window title from a window handle.
    /// </summary>
    /// <param name="windowHandle">Handle to the window</param>
    /// <returns>Window title or null if failed</returns>
    private static string? GetWindowTitle(IntPtr windowHandle)
    {
        try
        {
            int titleLength = NativeMethods.GetWindowTextLength(windowHandle);
            if (titleLength == 0)
            {
                return null; // No title or error
            }

            var titleBuilder = new StringBuilder(titleLength + 1);
            int result = NativeMethods.GetWindowText(windowHandle, titleBuilder, titleBuilder.Capacity);

            if (result > 0)
            {
                return titleBuilder.ToString();
            }
            else
            {
                return null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting window title: {ex.Message}"); // Debug
            return null;
        }
    }

    /// <summary>
    /// Gets the executable name from a window handle.
    /// </summary>
    /// <param name="windowHandle">Handle to the window</param>
    /// <returns>Executable name or null if failed</returns>
    private static string? GetExecutableName(IntPtr windowHandle)
    {
        try
        {
            // Get process ID from window handle
            uint processId;
            uint threadId = NativeMethods.GetWindowThreadProcessId(windowHandle, out processId);
            if (processId == 0)
            {
                Console.WriteLine("Failed to get process ID from window."); // Debug
                return null;
            }

            // Get process information
            using var process = Process.GetProcessById((int)processId);
            return process.ProcessName;
        }
        catch (ArgumentException)
        {
            Console.WriteLine("Process no longer exists."); // Debug
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting executable name: {ex.Message}"); // Debug
            return null;
        }
    }

    /// <summary>
    /// Logs session data to the SQLite database.
    /// </summary>
    /// <param name="durationSeconds">Session duration in seconds</param>
    /// <param name="executableName">Name of the executable (can be null)</param>
    /// <param name="windowTitle">Window title (can be null)</param>
    private static async Task LogSessionToDatabaseAsync(int durationSeconds, string? executableName, string? windowTitle)
    {
        try
        {
            using var connection = new SqliteConnection($"Data Source={databaseFilePath}");
            await connection.OpenAsync();

            var insertCommand = connection.CreateCommand();
            insertCommand.CommandText = @"
                INSERT INTO sessions (duration_seconds, executable_name, window_title)
                VALUES (@duration, @executable, @title);";

            insertCommand.Parameters.AddWithValue("@duration", durationSeconds);
            insertCommand.Parameters.AddWithValue("@executable", executableName ?? (object)DBNull.Value);
            insertCommand.Parameters.AddWithValue("@title", windowTitle ?? (object)DBNull.Value);

            await insertCommand.ExecuteNonQueryAsync();

            Console.WriteLine($"Logged session: {durationSeconds}s, '{executableName}', '{windowTitle}'"); // Debug
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error logging session to database: {ex.Message}"); // Debug
            // Don't throw - graceful degradation
        }
    }
}

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DarshanPlayer.Services
{
    /// <summary>What a launch asked for: files/folders to open, and whether to queue instead of play.</summary>
    public sealed record LaunchRequest(IReadOnlyList<string> Paths, bool Enqueue)
    {
        public static readonly LaunchRequest Empty = new(Array.Empty<string>(), false);

        public bool HasPaths => Paths.Count > 0;

        /// <summary>
        /// Parse command-line arguments. Unknown <c>--switches</c> are ignored rather than being
        /// mistaken for file names (Velopack passes its own on first run and after updates).
        /// </summary>
        public static LaunchRequest Parse(IEnumerable<string>? args)
        {
            if (args == null) return Empty;

            bool enqueue = false;
            var paths = new List<string>();
            foreach (var arg in args)
            {
                if (string.IsNullOrWhiteSpace(arg)) continue;
                if (string.Equals(arg, ShellRegistration.EnqueueArgument, StringComparison.OrdinalIgnoreCase))
                    enqueue = true;
                else if (!arg.StartsWith("--", StringComparison.Ordinal))
                    paths.Add(arg);
            }
            return new LaunchRequest(paths, enqueue);
        }

        // Wire format: first line OPEN or ENQUEUE, then one path per line. Paths cannot contain
        // newlines on Windows, so no escaping is needed.
        internal string Serialize() =>
            string.Join("\n", new[] { Enqueue ? "ENQUEUE" : "OPEN" }.Concat(Paths));

        internal static LaunchRequest Deserialize(string text)
        {
            var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length == 0) return Empty;
            return new LaunchRequest(lines.Skip(1).ToList(), lines[0].Trim() == "ENQUEUE");
        }
    }

    /// <summary>
    /// Decides whether an "open" should replace playback or join the playlist.
    ///
    /// Selecting several files in Explorer and choosing Open makes Windows start one process per
    /// file, all within a moment of each other. Treating each as "play now" would leave only the
    /// last file playing; instead, opens arriving close together are a single batch — the first
    /// plays, the rest queue up behind it.
    /// </summary>
    public sealed class OpenBatchPolicy
    {
        public static readonly TimeSpan DefaultWindow = TimeSpan.FromSeconds(2);

        private readonly Func<DateTime> _clock;
        private readonly TimeSpan _window;
        private DateTime? _lastOpenUtc;

        public OpenBatchPolicy(Func<DateTime>? clock = null, TimeSpan? window = null)
        {
            _clock = clock ?? (() => DateTime.UtcNow);
            _window = window ?? DefaultWindow;
        }

        /// <returns>True to add to the playlist without interrupting; false to play immediately.</returns>
        public bool ShouldEnqueue(LaunchRequest request)
        {
            var now = _clock();
            bool partOfBatch = _lastOpenUtc is { } last && now - last <= _window;
            _lastOpenUtc = now;
            return request.Enqueue || partOfBatch;
        }
    }

    /// <summary>
    /// Keeps one Darshan Player window per user session. A second launch hands its request to the
    /// running instance over a per-user named pipe and exits.
    /// </summary>
    public static class SingleInstance
    {
        private static readonly string Id = "DarshanPlayer." + Environment.UserName;
        private static Mutex? _mutex;
        private static CancellationTokenSource? _serverCts;
        private static readonly ConcurrentQueue<LaunchRequest> Pending = new();
        private static Action<LaunchRequest>? _handler;
        private static readonly object HandlerLock = new();

        /// <summary>Claim the primary role. Returns false when another instance already holds it.</summary>
        public static bool TryBecomePrimary()
        {
            try
            {
                // "Local\" scopes the mutex to this logon session.
                _mutex = new Mutex(initiallyOwned: true, $@"Local\{Id}", out bool createdNew);
                if (!createdNew)
                {
                    _mutex.Dispose();
                    _mutex = null;
                }
                return createdNew;
            }
            catch (Exception ex)
            {
                // Never let the single-instance machinery stop the player starting.
                System.Diagnostics.Debug.WriteLine($"[SingleInstance] mutex failed: {ex.Message}");
                return true;
            }
        }

        /// <summary>
        /// Send a request to the running instance. Retries briefly because a multi-select open
        /// starts every process at once, and the primary may not be listening yet.
        /// </summary>
        public static bool SendToPrimary(LaunchRequest request, TimeSpan timeout)
        {
            var deadline = DateTime.UtcNow + timeout;
            while (DateTime.UtcNow < deadline)
            {
                try
                {
                    using var client = new NamedPipeClientStream(".", Id, PipeDirection.Out, PipeOptions.CurrentUserOnly);
                    var remaining = (int)Math.Max(100, (deadline - DateTime.UtcNow).TotalMilliseconds);
                    client.Connect(Math.Min(remaining, 1000));
                    var bytes = Encoding.UTF8.GetBytes(request.Serialize());
                    client.Write(bytes, 0, bytes.Length);
                    client.Flush();
                    return true;
                }
                catch (TimeoutException) { /* primary not listening yet */ }
                catch (IOException) { Thread.Sleep(100); }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[SingleInstance] send failed: {ex.Message}");
                    return false;
                }
            }
            return false;
        }

        /// <summary>
        /// Start accepting requests. Call right after <see cref="TryBecomePrimary"/> succeeds —
        /// before the UI exists — so early senders connect. Requests queue until
        /// <see cref="SetHandler"/> is called.
        /// </summary>
        public static void StartServer()
        {
            if (_serverCts != null) return;
            _serverCts = new CancellationTokenSource();
            var token = _serverCts.Token;

            _ = Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        using var server = new NamedPipeServerStream(
                            Id, PipeDirection.In, NamedPipeServerStream.MaxAllowedServerInstances,
                            PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);

                        await server.WaitForConnectionAsync(token).ConfigureAwait(false);
                        using var reader = new StreamReader(server, Encoding.UTF8);
                        var text = await reader.ReadToEndAsync(token).ConfigureAwait(false);
                        Dispatch(LaunchRequest.Deserialize(text));
                    }
                    catch (OperationCanceledException) { break; }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[SingleInstance] server error: {ex.Message}");
                        try { await Task.Delay(250, token).ConfigureAwait(false); } catch { break; }
                    }
                }
            }, token);
        }

        /// <summary>Receive requests from other launches; anything that arrived earlier is delivered now.</summary>
        public static void SetHandler(Action<LaunchRequest> handler)
        {
            lock (HandlerLock)
            {
                _handler = handler;
                while (Pending.TryDequeue(out var queued))
                    handler(queued);
            }
        }

        private static void Dispatch(LaunchRequest request)
        {
            lock (HandlerLock)
            {
                if (_handler == null) Pending.Enqueue(request);
                else _handler(request);
            }
        }

        public static void Shutdown()
        {
            try { _serverCts?.Cancel(); } catch { /* ignore */ }
            try { _mutex?.ReleaseMutex(); _mutex?.Dispose(); } catch { /* ignore */ }
            _mutex = null;
        }
    }
}

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;
using Common.Logging;

namespace FrameworkExtensions
{
    public class FileWatcher : IDisposable
    {
        public string Path { get; }

        private readonly ILog _logger;
        readonly FileSystemWatcher _watcher = new FileSystemWatcher();
        public FileWatcher(string name, string path)
        {
            _logger = LogManager.GetLogger(name);
            Path = path;
            setupWatcher();
        }

        private void setupWatcher()
        {
            var dir = new DirectoryInfo(Path);
            if (dir.Exists)
            {
                _watcher.Path = Path;
                _watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName;
                _watcher.IncludeSubdirectories = true;
                _watcher.Filter = "*.*";
                _watcher.Created += File_Changed;
                _watcher.Changed += File_Changed;
                _watcher.Renamed += File_Changed;
                _watcher.EnableRaisingEvents = true;
                _logger.Info("Start watching " + Path);
            }
            else
            {
                _logger.Warn("Cannot find path " + Path);
                _watcher.NotifyFilter = NotifyFilters.DirectoryName;
                _watcher.Created += Dir_Change;
                _watcher.Renamed += Dir_Change;
                fallbackSetupWatcher(dir.Parent, dir.Name);
            }
        }

        private void fallbackSetupWatcher(DirectoryInfo dir, string name)
        {
            if (dir == null)
            {
                _logger.Fatal("Fail to watch " + Path);
                return;
            }
            if (dir.Exists)
            {
                _watcher.Path = dir.FullName;
                _watcher.Filter = name;
                _watcher.EnableRaisingEvents = true;
                _logger.Info("Start watching " + dir.FullName);
            }
            else
                fallbackSetupWatcher(dir.Parent, dir.Name);
        }

        void Dir_Change(object sender, FileSystemEventArgs e)
        {
            _logger.Info("Found " + e.FullPath);
            _watcher.EnableRaisingEvents = false;
            _watcher.Created -= Dir_Change;
            _watcher.Renamed -= Dir_Change;
            setupWatcher();
        }

        readonly ConcurrentDictionary<string, object> _store = new ConcurrentDictionary<string, object>();
        readonly ConcurrentDictionary<string, Func<Stream, object>> _func = new ConcurrentDictionary<string, Func<Stream, object>>(); 
        void File_Changed(object sender, FileSystemEventArgs e)
        {
            Func<Stream, object> func;
            if (!_func.TryGetValue(e.Name, out func))
                return;
            throttlingLoad(e, func);
        }

        async void throttlingLoad(FileSystemEventArgs e, Func<Stream, object> creator)
        {
            if (!_bag.TryAdd(e.Name, true))
                return;
            await Task.Delay(50);
            var value = loadFile(e.Name, e.FullPath, creator);
            if (value != null)
            {
                var old = _store[e.Name] as IDisposable;
                _store[e.Name] = value;
                old?.Dispose();
            }
            bool b;
            _bag.TryRemove(e.Name, out b);
        }

        readonly ConcurrentDictionary<string, bool> _bag = new ConcurrentDictionary<string, bool>();

        object loadFile(string name, string path, Func<Stream, object> func)
        {
            try
            {
                using (var file = File.OpenRead(path))
                {
                    var result = func(file);
                    _logger.Info("Read " + name + " success");
                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Error reading file " + name, ex);
                return null;
            }
        }

        public void Watch(string name, Func<Stream, object> creator, object @default = null)
        {
            _store.GetOrAdd(name, key => createWatch(key, creator, @default));
        }

        public object Get(string name)
        {
            return _store[name];
        }

        object createWatch(string name, Func<Stream, object> creator, object @default)
        {
            _func[name] = creator;
            var filePath = System.IO.Path.Combine(Path, name);
            if (!File.Exists(filePath))
                return @default;
            return loadFile(name, filePath, creator);
        }

        public void Dispose()
        {
            foreach (var kvp in _store)
            {
                var d = kvp.Value as IDisposable;
                d?.Dispose();
            }
        }
    }
}

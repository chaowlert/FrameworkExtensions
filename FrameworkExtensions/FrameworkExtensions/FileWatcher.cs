﻿using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;
using Common.Logging;

namespace FrameworkExtensions
{
    public class FileWatcher
    {
        private readonly ILog _logger;
        readonly string _path;
        readonly FileSystemWatcher _watcher = new FileSystemWatcher();
        public FileWatcher(string name, string path)
        {
            _logger = LogManager.GetLogger(name);
            _path = path;
            setupWatcher();
        }

        private void setupWatcher()
        {
            var dir = new DirectoryInfo(_path);
            if (dir.Exists)
            {
                _watcher.Path = _path;
                _watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName;
                _watcher.IncludeSubdirectories = true;
                _watcher.Filter = "*.*";
                _watcher.Created += File_Changed;
                _watcher.Changed += File_Changed;
                _watcher.Renamed += File_Changed;
                _watcher.EnableRaisingEvents = true;
                _logger.Info("Start watching " + _path);
            }
            else
            {
                _logger.Warn("Cannot find path " + _path);
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
                _logger.Fatal("Fail to watch " + _path);
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
        readonly ConcurrentDictionary<string, Func<Stream, object>> _creator = new ConcurrentDictionary<string, Func<Stream, object>>(); 
        void File_Changed(object sender, FileSystemEventArgs e)
        {
            Func<Stream, object> func;
            if (!_creator.TryGetValue(e.Name, out func))
                return;
            // ReSharper disable once CSharpWarnings::CS4014
            throttlingLoad(e, func);
        }

        async void throttlingLoad(FileSystemEventArgs e, Func<Stream, object> func)
        {
            if (!_bag.TryAdd(e.Name, true))
                return;
            await Task.Delay(50);
            var value = loadFile(e.Name, e.FullPath, func);
            if (value != null)
                _store[e.Name] = value;
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
            _creator[name] = creator;
            var filePath = Path.Combine(_path, name);
            if (!File.Exists(filePath))
                return @default;
            return loadFile(name, filePath, creator);
        } 
    }
}
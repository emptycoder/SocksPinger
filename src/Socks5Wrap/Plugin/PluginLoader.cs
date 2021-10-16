/*
    Socks5 - A full-fledged high-performance socks5 proxy server written in C#. Plugin support included.
    Copyright (C) 2016 ThrDev

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Socks5Wrap.Plugin
{
    public class PluginLoader
    {
        public static bool LoadPluginsFromDisk { get; set; }
        //load plugin staticly.
        private static List<object> _plugins = new List<object>();
        public static void LoadPlugins()
        {
            if (_loaded) return;
            try
            {
                try
                {
                    foreach (Type f in Assembly.GetExecutingAssembly().GetTypes())
                    {
                        try
                        {
                            if (!CheckType(f))
                            {
                                object type = Activator.CreateInstance(f);
                                _plugins.Push(type);
#if DEBUG
                                Console.WriteLine("Loaded Embedded Plugin {0}.", f.FullName);
#endif
                            }
                        }
                        catch (Exception ex) { Console.WriteLine(ex.ToString()); }
                    }
                }
                catch { }
                try
                {
                    foreach (Type f in Assembly.GetEntryAssembly().GetTypes())
                    {
                        try
                        {
                            if (!CheckType(f))
                            {
                                //Console.WriteLine("Loaded type {0}.", f.ToString());
                                object type = Activator.CreateInstance(f);
                                _plugins.Push(type);
#if DEBUG
                                Console.WriteLine("Loaded Plugin {0}.", f.FullName);
#endif
                            }
                        }
                        catch (Exception ex) { Console.WriteLine(ex.ToString()); }
                    }
                }
                catch { }
                //load plugins from disk?
                if (LoadPluginsFromDisk)
                {
                    string pluginPath = Path.Combine(Environment.CurrentDirectory, "Plugins");
                    if (!Directory.Exists(pluginPath)) { Directory.CreateDirectory(pluginPath); }
                    foreach (string filename in Directory.GetFiles(pluginPath))
                    {
                        if (filename.EndsWith(".dll"))
                        {
                            //Initialize unpacker.
                            Assembly g = Assembly.Load(File.ReadAllBytes(filename));
                            //Test to see if it's a module.
                            if (g != null)
                            {
                                foreach (Type f in g.GetTypes())
                                {
                                    if (!CheckType(f))
                                    {
                                        object plug = Activator.CreateInstance(f);
                                        _plugins.Push(plug);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (ReflectionTypeLoadException e)
            {
                foreach (Exception p in e.LoaderExceptions)
                    Console.WriteLine(p.ToString());
            }
            _loaded = true;
        }

        public static bool LoadCustomPlugin(Type f)
        {
            try
            {
                if (!CheckType(f))
                {
                    //Console.WriteLine("Loaded type {0}.", f.ToString());
                    object type = Activator.CreateInstance(f);
                    _plugins.Push(type);
#if DEBUG
                    Console.WriteLine("Loaded Plugin {0}.", f.FullName);
#endif
                    return true;
                }
            }
            catch (Exception ex) { Console.WriteLine(ex.ToString()); }
            return false;
        }

        static List<Type> _pluginTypes = new List<Type>(){ typeof(LoginHandler), typeof(DataHandler), typeof(ConnectHandler), typeof(ClientConnectedHandler), typeof(ConnectSocketOverrideHandler) };

        private static bool CheckType(Type p)
        {
            foreach(Type x in _pluginTypes)
            {
                if (x.IsAssignableFrom(p) && p != x)
                    return false;
                else
                    continue;
            }
            return true;
        }

        static bool _loaded;

        public static List<object> LoadPlugin(Type assemblytype)
        {
            //make sure plugins are loaded.
            List<object> list = new List<object>();
            foreach (object x in _plugins)
            {
                if (assemblytype.IsAssignableFrom(x.GetType()))
                {
                    if(((IGenericPlugin)x).OnStart())
					    if(((IGenericPlugin)x).Enabled)
                        	list.Push(x);
                }
            }
            return list;
        }

        public static List<object> GetPlugins
        {
            get { return _plugins; }
        }

        public static void ChangePluginStatus(bool enabled, Type pluginType)
        {
            foreach (object x in _plugins)
            {
                if(x.GetType() == pluginType)
                {
                    //cast to generic type.
                    ((IGenericPlugin)x).Enabled = enabled;
                    break;
                }
            }
        }
    }
}

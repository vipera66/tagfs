using System;
using System.Collections.Generic;
using Mono.Fuse;
using Mono.Unix.Native;

namespace TagFS
{
	public class MainClass
	{
		public static void Main(string[] args)
		{
			using (var fs = new TagFS())
			{
				string[] unhandled = fs.ParseFuseArguments(args);

				foreach (string key in fs.FuseOptions.Keys)
				{
					Console.WriteLine("Option: {0}={1}", key, fs.FuseOptions[key]);
				}

				if (!fs.ParseArguments(unhandled))
				{
					return;
				}
				
				fs.Start();
			}
		}
	}

	public class TagFS : FileSystem
	{
		protected override Errno OnGetPathStatus(string path, out Stat stat)
		{
			stat = new Stat();
			switch (path)
			{
				case "/":
					stat.st_mode = FilePermissions.S_IFDIR | 
						NativeConvert.FromOctalPermissionString("0755");
					stat.st_nlink = 2;
					break;
				default:
					stat.st_size = 0;
					stat.st_mode = FilePermissions.S_IFREG |
						NativeConvert.FromOctalPermissionString("0444");
					stat.st_nlink = 1;
					break;
			}
			return 0;
		}

		protected override Errno OnReadDirectory(string path, OpenedPathInfo fi,
				out IEnumerable<DirectoryEntry> paths)
		{
			List<DirectoryEntry> entries = new List<DirectoryEntry>();
			entries.Add(new DirectoryEntry("."));
			entries.Add(new DirectoryEntry(".."));

			// add fake file
			entries.Add(new DirectoryEntry("Hello"));

			paths = entries;
			return 0;
		}

		public bool ParseArguments(string[] args)
		{
			foreach (var arg in args)
			{
				switch (arg)
				{
					default:
						if (base.MountPoint == null)
						{
							base.MountPoint = arg;
						}
						break;
				}
			}
			return true;
		}
	}
}

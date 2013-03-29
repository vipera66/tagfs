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
			using (var fs = new TagFileSystem())
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
}

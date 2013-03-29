using System;
using System.Collections.Generic;
using Mono.Fuse;
using Mono.Unix.Native;
using System.IO;
using System.Linq;

namespace TagFS
{
	public class TagFileSystem : FileSystem
	{
		private TagDictionary _tagDictionary = new TagDictionary();
		private string _baseDir;

		public TagFileSystem()
		{
		}

		private void ReadDirectoryEntries(DirectoryInfo directoryInfo)
		{
			foreach (DirectoryInfo di in directoryInfo.GetDirectories())
			{
				ReadDirectoryEntries(di);
			}

			var fi = directoryInfo.GetFiles();

			var tagFi = fi.SingleOrDefault(x => x.Name == ".tags");
			if (tagFi != null)
			{
				var tags = ReadTags(tagFi);
				var files = fi.Where(x => x != tagFi).ToList();
				foreach (var tag in tags)
				{
					_tagDictionary.AddOrUpdate(tag, files, (t, e) => AddFiles(t, e, files));
				}
			}
		}

		private List<FileInfo> AddFiles(Tag tag, List<FileInfo> existingFiles, List<FileInfo> newFiles)
		{
			existingFiles.AddRange(newFiles);
			return existingFiles;
		}

		private List<Tag> ReadTags(FileSystemInfo tagFsi)
		{
			List<Tag> list = new List<Tag>();
			using (var reader = File.OpenText(tagFsi.FullName))
			{
				while (!reader.EndOfStream)
				{
					var line = reader.ReadLine();
					Tag tag = ParseTag(line);
					list.Add(tag);
				}
			}
			return list;
		}

		private Tag ParseTag(string line)
		{
			var key = line.Substring(0, line.IndexOf(":"));
			var value = line.Substring(line.IndexOf(":") + 1).Trim();
			return new Tag {Name = key, Value = value};
		}

		private Tag CreateTag(string path)
		{
			Tag tag = new Tag();
			var items = path.Split(new[]{ '/' }, StringSplitOptions.RemoveEmptyEntries);
			tag.Name = items[0];
			tag.Value = items[1];
			return tag;
		}

		protected override Errno OnGetPathStatus(string path, out Stat stat)
		{
			stat = new Stat();
			var depth = path.Count(x => x == '/');
			switch (depth)
			{
				case 1:
				case 2:
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

		protected override Errno OnAccessPath(string path, AccessModes mask)
		{
			return 0;
//			var realPath = GetRealPath(path);
//			int r = Syscall.access(realPath, mask);
//			if (r == -1)
//			{
//				return Stdlib.GetLastError();
//			}
//			return 0;
		}

		protected override Errno OnReadDirectory(string path, OpenedPathInfo fi,
				out IEnumerable<DirectoryEntry> paths)
		{
			List<DirectoryEntry> entries = new List<DirectoryEntry>();
			entries.Add(new DirectoryEntry("."));
			entries.Add(new DirectoryEntry(".."));

			var depth = path.Count(x => x == '/');

			switch (depth)
			{
				case 1:
					if (path == "/")
					{
						foreach (var name in _tagDictionary.Keys.Select(x => x.Name).Distinct())
						{
							entries.Add(new DirectoryEntry(name));
						}
					}
					else
					{
						foreach (var tag in _tagDictionary.Keys.Where(x => x.Name == path.Substring(1)))
						{
							entries.Add(new DirectoryEntry(tag.Value));
						}
					}
					break;
				case 2:
					var key = CreateTag(path);
					foreach (var file in _tagDictionary[key])
					{
						entries.Add(new DirectoryEntry(file.Name));
					}
					break;
			}

			paths = entries;
			return 0;
		}

		protected override Errno OnOpenHandle(string path, OpenedPathInfo info)
		{
			return 0;
//			string realPath = GetRealPath(path);
//			return ProcessFile(realPath, info.OpenFlags, delegate (int fd)
//			{
//				return 0;
//			}
//			);
		}

		string GetRealPath(string path)
		{
			var tag = CreateTag(path);
			var fileName = path.Substring(path.LastIndexOf("/") + 1);
			var files = _tagDictionary[tag];
			var realPath = files.SingleOrDefault(x => x.Name == fileName).FullName;
			return realPath;
		}

		private delegate int FdCb(int fd);
		private Errno ProcessFile(string path, OpenFlags flags, FdCb cb)
		{
			int fd = Syscall.open(path, flags);
			if (fd == -1)
			{
				return Stdlib.GetLastError();
			}
			int r = cb(fd);
			Errno res = 0;
			if (r == -1)
			{
				res = Stdlib.GetLastError();
			}
			Syscall.close(fd);
			return res;
		}

		protected override unsafe Errno OnReadHandle(string path, OpenedPathInfo info, byte[] buf, 
				long offset, out int bytesRead)
		{
			int br = 0;
			var realPath = GetRealPath(path);
			using (var stream = File.OpenRead(realPath))
			{
				br = stream.Read(buf, (int)offset, buf.Length);
			}
			bytesRead = br;
			return 0;
		}

		protected override Errno OnReleaseHandle(string path, OpenedPathInfo info)
		{
			return 0;
		}

		protected override Errno OnSynchronizeHandle(string path, OpenedPathInfo info, bool onlyUserData)
		{
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
						else
						{
							_baseDir = arg;
						}
						break;
				}
			}

			ReadDirectoryEntries(new DirectoryInfo(_baseDir));

			return true;
		}
	}
}

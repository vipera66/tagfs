using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Mono.Unix.Native;
using Mono.Fuse;

namespace TagFS
{
	public class TagDictionary : ConcurrentDictionary<Tag, List<FileInfo>>
	{
		public TagDictionary()
		{
		}
	}
}


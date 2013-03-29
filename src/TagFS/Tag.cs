using System;
using System.Collections.Generic;
using Mono.Fuse;
using Mono.Unix.Native;
using System.IO;
using System.Linq;

namespace TagFS
{
	public class Tag : IEquatable<Tag>
	{
		public string Name
		{
			get;
			set;
		}

		public string Value
		{
			get;
			set;
		}

		public override int GetHashCode()
		{
			return Name.GetHashCode() & Value.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			if (obj == null || base.GetType() != obj.GetType())
			{
				return false;
			}
			Tag value = (Tag)obj;
			return this.Equals(value);
		}

		public bool Equals(Tag value)
		{
			return this.Name == value.Name && this.Value == value.Value;
		}

		public static bool operator ==(Tag lhs, Tag rhs)
		{
			return object.Equals(lhs, rhs);
		}

		public static bool operator !=(Tag lhs, Tag rhs)
		{
			return !object.Equals(lhs, rhs);
		}
	}
	
}

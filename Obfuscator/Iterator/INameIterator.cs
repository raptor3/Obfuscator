﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Obfuscator.Iterator
{
	public interface INameIterator
	{

		void Reset();

		string Next();

	}
}

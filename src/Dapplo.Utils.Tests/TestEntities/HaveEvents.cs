﻿//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2015-2016 Dapplo
// 
//  For more information see: http://dapplo.net/
//  Dapplo repositories are hosted on GitHub: https://github.com/dapplo
// 
//  This file is part of Dapplo.Utils
// 
//  Dapplo.Utils is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  Dapplo.Utils is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
// 
//  You should have a copy of the GNU Lesser General Public License
//  along with Dapplo.Utils. If not, see <http://www.gnu.org/licenses/lgpl.txt>.

#region using

using System;

#endregion

namespace Dapplo.Utils.Tests.TestEntities
{
	public class HaveEvents : IHaveEvents
	{
		public event EventHandler Blub;
		private event EventHandler _blub;

		public HaveEvents()
		{
			_blub += (s, e) => throw new Exception();
			Blub += (s, e) => throw new Exception();
		}
		public void Invoke()
		{
			_blub?.Invoke(this, EventArgs.Empty);
			Blub?.Invoke(this, EventArgs.Empty);
		}
	}
}

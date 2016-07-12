//  Dapplo - building blocks for desktop applications
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

using System.ComponentModel;

#endregion

namespace Dapplo.Utils.Tests.TestEntities
{
	/// <summary>
	/// Class used for testing the EventObservable with INotifyPropertyChanged
	/// </summary>
	public class NotifyPropertyChangedClass : INotifyPropertyChanged, IHasEvents
	{
		public event PropertyChangedEventHandler PropertyChanged;
		private string _name;

		public string Name
		{
			get { return _name; }
			set
			{
				if (_name != value)
				{
					_name = value;
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
				}
			}
		}

		private string _name2;

		public string Name2
		{
			get { return _name2; }
			set
			{
				if (_name2 != value)
				{
					_name2 = value;
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name2)));
				}
			}
		}

	}
}
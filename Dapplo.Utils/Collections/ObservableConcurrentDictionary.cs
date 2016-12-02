#region Dapplo 2016 - GNU Lesser General Public License

// Dapplo - building blocks for .NET applications
// Copyright (C) 2016 Dapplo
// 
// For more information see: http://dapplo.net/
// Dapplo repositories are hosted on GitHub: https://github.com/dapplo
// 
// This file is part of Dapplo.Utils
// 
// Dapplo.Utils is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Dapplo.Utils is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have a copy of the GNU Lesser General Public License
// along with Dapplo.Utils. If not, see <http://www.gnu.org/licenses/lgpl.txt>.

#endregion

#region Usings

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

#endregion

namespace Dapplo.Utils.Collections
{
	/// <summary>
	///     An observable concurrent dictionary
	/// </summary>
	/// <typeparam name="TKey">Type for the key</typeparam>
	/// <typeparam name="TValue">Type for the value</typeparam>
	public class ObservableConcurrentDictionary<TKey, TValue> : ConcurrentDictionary<TKey, TValue>, INotifyCollectionChanged, INotifyPropertyChanged
	{
		private readonly bool _isNpcType = typeof(INotifyPropertyChanged).GetTypeInfo().IsAssignableFrom(typeof(TValue).GetTypeInfo());

		/// <summary>
		///     Default constructor
		/// </summary>
		public ObservableConcurrentDictionary()
		{
		}

		/// <summary>
		///     Constructor with a comparer
		/// </summary>
		/// <param name="comparer">IEqualityComparer for TKey</param>
		public ObservableConcurrentDictionary(IEqualityComparer<TKey> comparer)
			: base(comparer)
		{
		}

		/// <summary>
		///     Constructor with a collection
		/// </summary>
		/// <param name="collection">IEnumerable of KeyValuePair</param>
		public ObservableConcurrentDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection)
			: base(collection)
		{
		}

		/// <summary>
		///     Constructor with a collection and comparer
		/// </summary>
		/// <param name="collection">IEnumerable of KeyValuePair</param>
		/// <param name="comparer">IEqualityComparer of TKey</param>
		public ObservableConcurrentDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection, IEqualityComparer<TKey> comparer)
			: base(collection, comparer)
		{
		}

		/// <summary>
		///     Constructor with a concurrency level and a capacity
		/// </summary>
		/// <param name="concurrencyLevel">int</param>
		/// <param name="capacity">int</param>
		public ObservableConcurrentDictionary(int concurrencyLevel, int capacity)
			: base(concurrencyLevel, capacity)
		{
		}

		/// <summary>
		///     Constructor with a concurrency level, a capacity and a comparer
		/// </summary>
		/// <param name="concurrencyLevel">int</param>
		/// <param name="capacity">int</param>
		/// <param name="comparer">IEqualityComparer of TKey</param>
		public ObservableConcurrentDictionary(int concurrencyLevel, int capacity, IEqualityComparer<TKey> comparer)
			: base(concurrencyLevel, capacity, comparer)
		{
		}

		/// <summary>
		///     Constructor with a concurrency level, a collection and a comparer
		/// </summary>
		/// <param name="concurrencyLevel">int</param>
		/// <param name="collection">IEnumerable of KeyValuePair</param>
		/// <param name="comparer">IEqualityComparer of TKey</param>
		public ObservableConcurrentDictionary(int concurrencyLevel, IEnumerable<KeyValuePair<TKey, TValue>> collection, IEqualityComparer<TKey> comparer)
			: base(concurrencyLevel, collection, comparer)
		{
		}

		/// <summary>
		///     Notify of collection changed events
		/// </summary>
		public event NotifyCollectionChangedEventHandler CollectionChanged;

		/// <summary>
		///     Notify of Property changed events
		/// </summary>
		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>
		///     Add or update the value
		/// </summary>
		/// <param name="key">TKey</param>
		/// <param name="addValue">TValue</param>
		/// <param name="updateValueFactory">Func which can decide if an update needs to be made</param>
		/// <returns>TValue</returns>
		public new TValue AddOrUpdate(TKey key, TValue addValue, Func<TKey, TValue, TValue> updateValueFactory)
		{
			return AddOrUpdate(key, k => addValue, updateValueFactory);
		}

		/// <summary>
		///     Enable NPC forwarding (if the type implements INotifyPropertyChanged )
		/// </summary>
		/// <param name="value">TValue</param>
		private void AddNotifyPropertyChangedForwarding(TValue value)
		{
			if (!_isNpcType)
			{
				return;
			}
			var npcValue = value as INotifyPropertyChanged;
			if (npcValue != null)
			{
				npcValue.PropertyChanged += ForwardPropertyChanged;
			}
		}

		/// <summary>
		///     Disable NPC forwarding (if the type implements INotifyPropertyChanged )
		/// </summary>
		/// <param name="value">TValue</param>
		private void RemoveNotifyPropertyChangedForwarding(TValue value)
		{
			if (!_isNpcType)
			{
				return;
			}
			var npcValue = value as INotifyPropertyChanged;
			if (npcValue != null)
			{
				npcValue.PropertyChanged -= ForwardPropertyChanged;
			}
		}

		/// <summary>
		///     Forwarder in it's own method, so we can deregister again
		/// </summary>
		/// <param name="sender">object</param>
		/// <param name="propertyChangedEventArgs">PropertyChangedEventArgs</param>
		private void ForwardPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
		{
			PropertyChanged?.Invoke(sender, propertyChangedEventArgs);
		}

		/// <summary>
		///     Add or update the value
		/// </summary>
		/// <param name="key">TKey</param>
		/// <param name="addValueFactory">Func to add the value</param>
		/// <param name="updateValueFactory">Func to update the value</param>
		/// <returns>TValue</returns>
		public new TValue AddOrUpdate(TKey key, Func<TKey, TValue> addValueFactory, Func<TKey, TValue, TValue> updateValueFactory)
		{
			return base.AddOrUpdate(key, k =>
				{
					var newValue = addValueFactory(k);

					// 1. Check if TValue implements INotifyPropertyChanged and wire its PropertyChanged event
					AddNotifyPropertyChangedForwarding(newValue);

					// 2. Notify
					CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, newValue));

					// 3. Return
					return newValue;
				},
				(k, oldValue) =>
				{
					// 1. Check if TValue implements INotifyPropertyChanged and wire its PropertyChanged event
					AddNotifyPropertyChangedForwarding(oldValue);

					// 2. Update the value using the provided factory
					var updatedValue = updateValueFactory(k, oldValue);

					// 3. Add/remove NPC
					if (!ReferenceEquals(oldValue, updatedValue))
					{
						RemoveNotifyPropertyChangedForwarding(oldValue);
					}
					AddNotifyPropertyChangedForwarding(updatedValue);

					// 4. Return
					return updatedValue;
				});
		}

		/// <summary>
		///     Clear the dictionary and notify of reset
		/// </summary>
		public new void Clear()
		{
			// 1. Remove NPC
			foreach (var value in Values.ToList())
			{
				RemoveNotifyPropertyChangedForwarding(value);
			}

			// 2. Clear
			base.Clear();

			// 3. Notify
			CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
		}

		/// <summary>
		///     Get or add the value
		/// </summary>
		/// <param name="key">TKey</param>
		/// <param name="value">TValue</param>
		/// <returns>TValue</returns>
		public new TValue GetOrAdd(TKey key, TValue value)
		{
			return GetOrAdd(key, k => value);
		}

		/// <summary>
		///     Get or add, using a value factory
		/// </summary>
		/// <param name="key"></param>
		/// <param name="valueFactory">Func to generate a value</param>
		/// <returns>TValue</returns>
		public new TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
		{
			return base.GetOrAdd(key, k =>
			{
				// 1. Create the value
				var newValue = valueFactory(k);

				// 3. Add NPC
				AddNotifyPropertyChangedForwarding(newValue);

				// 3. Notify
				CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, new List<TValue> {newValue}));

				// 4. Return
				return newValue;
			});
		}

		/// <summary>
		///     Try to add a value by key
		/// </summary>
		/// <param name="key">TKey</param>
		/// <param name="value">TValue</param>
		/// <returns>bool true if adding worked</returns>
		public new bool TryAdd(TKey key, TValue value)
		{
			if (!base.TryAdd(key, value))
			{
				return false;
			}
			AddNotifyPropertyChangedForwarding(value);

			// 1. Notify
			CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, value));

			// 2. Return
			return true;
		}

		/// <summary>
		/// Remove an element by key
		/// </summary>
		/// <param name="key">TKey</param>
		public void Remove(TKey key)
		{
			TValue ignore;
			TryRemove(key, out ignore);
		}

		/// <summary>
		///     Try to remove a value by key
		/// </summary>
		/// <param name="key"></param>
		/// <param name="value"></param>
		/// <returns>bool true if the remove succeded</returns>
		public new bool TryRemove(TKey key, out TValue value)
		{
			if (!base.TryRemove(key, out value))
			{
				return false;
			}
			// 1. Remove previously added PropertyChanged (as ForwardPropertyChanged)
			RemoveNotifyPropertyChangedForwarding(value);

			// 2. Notify
			CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, value));

			// 3. Return
			return true;
		}

		/// <summary>
		///     Try to update a value
		/// </summary>
		/// <param name="key">TKey</param>
		/// <param name="newValue">TValue</param>
		/// <param name="comparisonValue">TValue</param>
		/// <returns>bool true if update succeded</returns>
		public new bool TryUpdate(TKey key, TValue newValue, TValue comparisonValue)
		{
			if (!base.TryUpdate(key, newValue, comparisonValue))
			{
				return false;
			}

			// 1. Remove previously added PropertyChanged (as ForwardPropertyChanged)
			RemoveNotifyPropertyChangedForwarding(comparisonValue);

			// 2. Add PropertyChanged (as ForwardPropertyChanged)
			AddNotifyPropertyChangedForwarding(newValue);

			// 3. Notify
			CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, newValue, comparisonValue));

			// 4. Return
			return true;
		}
	}
}
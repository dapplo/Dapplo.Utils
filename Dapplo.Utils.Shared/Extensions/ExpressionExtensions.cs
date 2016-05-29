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

using System;
using System.Linq.Expressions;

#endregion

namespace Dapplo.Utils.Extensions
{
	/// <summary>
	///     Lambda expressions Utils
	/// </summary>
	public static class ExpressionExtensions
	{
		/// <summary>
		///     Get the name of the member in a Lambda expression
		/// </summary>
		/// <param name="memberSelector">LambdaExpression</param>
		/// <returns>string with the member name</returns>
		public static string GetMemberName(this LambdaExpression memberSelector)
		{
			Func<Expression, string> nameSelector = null; //recursive func
			nameSelector = e =>
			{
				//or move the entire thing to a separate recursive method
				switch (e.NodeType)
				{
					case ExpressionType.Parameter:
						return ((ParameterExpression) e).Name;
					case ExpressionType.MemberAccess:
						return ((MemberExpression) e).Member.Name;
					case ExpressionType.Call:
						return ((MethodCallExpression) e).Method.Name;
					case ExpressionType.Convert:
					case ExpressionType.ConvertChecked:
						return nameSelector(((UnaryExpression) e).Operand);
					case ExpressionType.Invoke:
						return nameSelector(((InvocationExpression) e).Expression);
					case ExpressionType.ArrayLength:
						return "Length";
					default:
						throw new Exception("not a proper member selector");
				}
			};

			return nameSelector(memberSelector.Body);
		}
	}
}
#region Copyright 2008-2014 by Roger Knapp, Licensed under the Apache License, Version 2.0
/* Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *   http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
#endregion
using System;

namespace CSharpTest.Collections.Generic;

/// <summary>
/// provides a set of runtime validations
/// </summary>
public static class Check
{
    /// <summary>
    /// Verifies that the condition is true and if it fails constructs the specified type of
    /// exception and throws.
    /// </summary>
    public static void Assert<TException>(bool condition) where TException : Exception, new()
    {
        if (!condition)
            throw new TException();
    }

	/// <summary>
	/// Verifies that the condition is true and if it fails throws InvalidOperationException
	/// </summary>
	public static void Assert(bool condition, string message)
	{
        if (!condition)
            throw new InvalidOperationException(message);
	}

	/// <summary>
	/// Verifies that value is not null and returns the value or throws ArgumentNullException
	/// </summary>
	public static T NotNull<T>(T value)
    {
        if (value == null) 
            throw new ArgumentNullException(nameof(value));
        
        return value;
    }

    /// <summary>
    /// Verfies that the string is not null and not empty and returns the string.
    /// throws ArgumentNullException, ArgumentOutOfRangeException
    /// </summary>
    public static string NotEmpty(string value)
    {
		ArgumentNullException.ThrowIfNull(value);
		if (value.Length == 0) 
            throw new ArgumentOutOfRangeException(nameof(value));
        
        return value;
    }


    /// <summary>
    /// Verifies that the value is min, max, or between the two.
    /// throws ArgumentOutOfRangeException
    /// </summary>
    public static T InRange<T>(T value, T min, T max) where T : IComparable<T>
    {
        if (value == null) throw new ArgumentNullException(nameof(value));
        if (value.CompareTo(min) < 0)
            throw new ArgumentOutOfRangeException(nameof(value));
        if (value.CompareTo(max) > 0)
            throw new ArgumentOutOfRangeException(nameof(value));
        return value;
    }
}
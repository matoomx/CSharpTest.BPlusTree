#region Copyright 2011 by Roger Knapp, Licensed under the Apache License, Version 2.0
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

public class InvalidConfigurationValueException : Exception
{
    public InvalidConfigurationValueException(string message, Exception innerException = null) : base(message, innerException) { }

    public static void Assert(bool condition, string format, params object[] args)
    {
        if (!condition)
        {
            if (args != null && args.Length > 0)
            {
                try 
                { 
                    format = String.Format(format, args); 
                }
                catch (Exception e)
                { 
                    format = String.Format("{0} format error: {1}", format, e.Message); 
                }
            }
            throw new InvalidConfigurationValueException(format, null);
        }
	}
}

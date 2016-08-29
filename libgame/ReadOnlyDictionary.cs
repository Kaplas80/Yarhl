//-----------------------------------------------------------------------
// <copyright file="ReadOnlyDictionary.cs" company="none">
// Copyright (C) 2013
//
//   This program is free software: you can redistribute it and/or modify
//   it under the terms of the GNU General Public License as published by 
//   the Free Software Foundation, either version 3 of the License, or
//   (at your option) any later version.
//
//   This program is distributed in the hope that it will be useful, 
//   but WITHOUT ANY WARRANTY; without even the implied warranty of
//   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//   GNU General Public License for more details. 
//
//   You should have received a copy of the GNU General Public License
//   along with this program.  If not, see "http://www.gnu.org/licenses/". 
// </copyright>
// <author>pleoNeX</author>
// <email>benito356@gmail.com</email>
// <date>22/09/2013</date>
//-----------------------------------------------------------------------
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;

namespace Libgame
{
    public class ReadOnlyDictionary<TKey, TValue>
    {
        private Dictionary<TKey, TValue> dict;

        public ReadOnlyDictionary(Dictionary<TKey, TValue> dict)
        {
            this.dict = dict;
        }

        public TValue this[TKey key] {
            get { return this.dict[key]; }
        }

        public Dictionary<TKey, TValue>.KeyCollection Keys {
            get { return dict.Keys; }
        }

        public Dictionary<TKey, TValue>.ValueCollection Values {
            get { return dict.Values; }
        }

        public bool ContainsKey(TKey key)
        {
            foreach (TKey k in this.Keys)
                if (k.Equals(key))
                    return true;

            return false;
        }
    }
}

